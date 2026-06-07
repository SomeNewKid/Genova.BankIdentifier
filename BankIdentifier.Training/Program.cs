// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Genova.MiniML;

namespace Genova.BankIdentifier.Training;

/// <summary>
/// Entry point for the training utility that builds centroid embeddings
/// and a similarity threshold for different senses of the word "bank"
/// using a MiniLM-based ONNX model.
/// </summary>
public static class Program
{
    private const string RiverCorpusFileName = "river-bank.txt";
    private const string FinancialCorpusFileName = "financial-bank.txt";
    private const string OtherCorpusFileName = "other-bank.txt";
    private const string VerbCorpusFileName = "verb-bank.txt";

    /// <summary>
    /// Application entry point. Loads the three "bank" corpora, computes
    /// centroid vectors for each sense, derives a similarity threshold,
    /// and writes them to a JSON file.
    /// </summary>
    public static int Main(string[] args)
    {
        try
        {
            string solutionFolder = FindSolutionFolder();
            string inputDirectory = Path.Combine(solutionFolder, "BankIdentifier.Training", "Input");
            string outputDirectory = Path.Combine(solutionFolder, "BankIdentifier", "Data");

            Console.WriteLine("Genova.BankIdentifier.Training - Building bank centroids...");
            Console.WriteLine($"Input directory : {inputDirectory}");
            Console.WriteLine($"Output directory: {outputDirectory}");
            Console.WriteLine();

            using IEmbeddingModel model = new OnnxEmbeddingModel();

            int embeddingSize = model.EmbeddingSize;
            Console.WriteLine($"Model embedding size: {embeddingSize}");
            Console.WriteLine();

            // Load vectors for each sense.
            List<float[]> riverVectors = LoadCorpusVectors(
                model,
                Path.Combine(inputDirectory, RiverCorpusFileName),
                "river_bank");

            List<float[]> financialVectors = LoadCorpusVectors(
                model,
                Path.Combine(inputDirectory, FinancialCorpusFileName),
                "financial_bank");

            List<float[]> otherVectors = LoadCorpusVectors(
                model,
                Path.Combine(inputDirectory, OtherCorpusFileName),
                "other");

            List<float[]> verbVectors = LoadCorpusVectors(
                model,
                Path.Combine(inputDirectory, VerbCorpusFileName),
                "verb_bank");

            if (riverVectors.Count == 0)
            {
                throw new InvalidOperationException("No valid 'bank' embeddings found in river-bank corpus.");
            }

            if (financialVectors.Count == 0)
            {
                throw new InvalidOperationException("No valid 'bank' embeddings found in financial-bank corpus.");
            }

            if (verbVectors.Count == 0)
            {
                throw new InvalidOperationException("No valid 'bank' embeddings found in verb-bank corpus.");
            }

            if (otherVectors.Count == 0)
            {
                Console.WriteLine("Warning: no valid 'bank' embeddings found in other-bank corpus.");
            }

            float[] riverCentroid = ComputeCentroid(riverVectors, embeddingSize);
            float[] financialCentroid = ComputeCentroid(financialVectors, embeddingSize);
            float[] verbCentroid = ComputeCentroid(verbVectors, embeddingSize);

            double similarityThreshold = ComputeSimilarityThreshold(
                riverVectors,
                financialVectors,
                riverCentroid,
                financialCentroid);

            BankEmbeddingCentroids centroids = new BankEmbeddingCentroids
            {
                EmbeddingSize = embeddingSize,
                RiverBankCentroid = riverCentroid,
                FinancialBankCentroid = financialCentroid,
                VerbBankCentroid = verbCentroid,
                SimilarityThreshold = similarityThreshold
            };

            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, BankMeaningIdentifier.BankCentroidsFileName);

            JsonSerializerOptions options = new ()
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            string json = JsonSerializer.Serialize(centroids, options);
            File.WriteAllText(outputPath, json);

            Console.WriteLine();
            Console.WriteLine($"Centroids and threshold written to: {outputPath}");
            Console.WriteLine("Done.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error while building bank centroids:");
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static string FindSolutionFolder()
    {
        const string solutionFileName = "Genova.BankIdentifier.sln";
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, solutionFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Solution folder containing '{solutionFileName}' could not be found.");
    }

    private static List<float[]> LoadCorpusVectors(
        IEmbeddingModel model,
        string filePath,
        string label)
    {
        Console.WriteLine($"Loading corpus for '{label}' from: {filePath}");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Corpus file not found: {filePath}", filePath);
        }

        List<float[]> vectors = new List<float[]>();

        string[] lines = File.ReadAllLines(filePath);

        int lineNumber = 0;
        foreach (string rawLine in lines)
        {
            lineNumber++;

            string line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            try
            {
                TokenizedEmbedding embedding = model.EmbedWithTokens(line);

                float[]? bankVector = embedding.GetTokenVector("bank");
                if (bankVector == null)
                {
                    bankVector = GetFirstTokenContainingBank(embedding);
                }

                if (bankVector == null)
                {
                    Console.WriteLine(
                        $"  [Info] No 'bank' token found in line {lineNumber}: \"{line}\"");
                    continue;
                }

                vectors.Add(bankVector);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"  [Warning] Failed to embed line {lineNumber}: \"{line}\". Error: {ex.Message}");
            }
        }

        Console.WriteLine($"  -> {vectors.Count} 'bank' embeddings collected for '{label}'.");
        Console.WriteLine();

        return vectors;
    }

    private static float[]? GetFirstTokenContainingBank(TokenizedEmbedding embedding)
    {
        for (int i = 0; i < embedding.Tokens.Count; i++)
        {
            string token = embedding.Tokens[i];
            if (token.IndexOf("bank", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return embedding.TokenVectors[i];
            }
        }

        return null;
    }

    private static float[] ComputeCentroid(IReadOnlyList<float[]> vectors, int embeddingSize)
    {
        if (vectors == null || vectors.Count == 0)
        {
            throw new ArgumentException("At least one vector is required to compute a centroid.", nameof(vectors));
        }

        float[] centroid = new float[embeddingSize];

        foreach (float[] vector in vectors)
        {
            for (int i = 0; i < embeddingSize; i++)
            {
                centroid[i] += vector[i];
            }
        }

        for (int i = 0; i < embeddingSize; i++)
        {
            centroid[i] /= vectors.Count;
        }

        return centroid;
    }

    private static double ComputeSimilarityThreshold(
        IReadOnlyList<float[]> riverVectors,
        IReadOnlyList<float[]> financialVectors,
        float[] riverCentroid,
        float[] financialCentroid)
    {
        double minPositiveSim = double.PositiveInfinity;

        foreach (float[] vector in riverVectors)
        {
            double sim = CosineSimilarity(vector, riverCentroid);
            if (sim < minPositiveSim)
            {
                minPositiveSim = sim;
            }
        }

        foreach (float[] vector in financialVectors)
        {
            double sim = CosineSimilarity(vector, financialCentroid);
            if (sim < minPositiveSim)
            {
                minPositiveSim = sim;
            }
        }

        if (double.IsPositiveInfinity(minPositiveSim))
        {
            minPositiveSim = 0.0;
        }

        const double margin = 0.02;

        double threshold = minPositiveSim - margin;

        if (threshold < 0.5)
        {
            threshold = 0.5;
        }

        Console.WriteLine($"Derived similarity threshold (positives only): {threshold:F4}");
        Console.WriteLine($"  Min positive similarity : {minPositiveSim:F4}");
        Console.WriteLine();

        return threshold;
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        double dot = 0.0;
        double normA = 0.0;
        double normB = 0.0;

        for (int i = 0; i < a.Length; i++)
        {
            double va = a[i];
            double vb = b[i];

            dot += va * vb;
            normA += va * va;
            normB += vb * vb;
        }

        if (normA == 0.0 || normB == 0.0)
        {
            return 0.0;
        }

        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
