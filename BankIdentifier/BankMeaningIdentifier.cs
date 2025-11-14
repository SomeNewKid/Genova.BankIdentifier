// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using Genova.Common.Attributes;
using Genova.Common.Utilities;
using Genova.MiniML;

namespace Genova.BankIdentifier;

/// <summary>
/// Provides functionality to determine the meaning of the word "bank"
/// in a user-provided sentence using contextual embeddings.
/// <para>
/// This class:
/// </para>
/// <list type="bullet">
/// <item>Loads a MiniLM-based ONNX embedding model.</item>
/// <item>Loads precomputed centroid embeddings and a similarity threshold for senses of "bank".</item>
/// <item>Classifies a new sentence as <c>river_bank</c>, <c>financial_bank</c>, or <c>other</c>.</item>
/// </list>
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by other Genova components.")]
public sealed class BankMeaningIdentifier : IDisposable
{
    /// <summary>
    /// The name of the embedded JSON file that contains the bank centroid data.
    /// </summary>
    internal const string BankCentroidsFileName = "bank_centroids.json";

    private readonly IEmbeddingModel _model;
    private readonly BankEmbeddingCentroids _centroids;

    /// <summary>
    /// Initializes a new instance of the <see cref="BankMeaningIdentifier"/> class.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the centroid data cannot be loaded, if its embedding size does not
    /// match the embedding size of the underlying model, if no similarity threshold is defined,
    /// or if the verb-bank centroid is missing or has an unexpected length.
    /// </exception>
    public BankMeaningIdentifier()
    {
        _model = new OnnxEmbeddingModel();

        _centroids = LoadCentroids();
        if (_centroids.EmbeddingSize != _model.EmbeddingSize)
        {
            throw new InvalidOperationException(
                $"Centroid embedding size ({_centroids.EmbeddingSize}) does not match " +
                $"model embedding size ({_model.EmbeddingSize}).");
        }

        if (_centroids.SimilarityThreshold <= 0.0)
        {
            throw new InvalidOperationException(
                "SimilarityThreshold must be a positive value in BankEmbeddingCentroids.");
        }

        if (_centroids.VerbBankCentroid == null ||
            _centroids.VerbBankCentroid.Length != _centroids.EmbeddingSize)
        {
            throw new InvalidOperationException(
                "VerbBankCentroid must be defined and match the embedding size in BankEmbeddingCentroids.");
        }
    }

    /// <summary>
    /// Determines the meaning of the word "bank" in the specified sentence.
    /// </summary>
    /// <param name="text">The user-provided sentence that contains the word "bank".</param>
    /// <returns>
    /// A string representing the inferred meaning of "bank" in the sentence:
    /// <list type="bullet">
    /// <item><c>river_bank</c> – for river or shoreline uses.</item>
    /// <item><c>financial_bank</c> – for monetary or institution uses.</item>
    /// <item><c>other</c> – for verb/tilt uses or if the best similarity is below the learned threshold.</item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="text"/> is <c>null</c>, empty, or consists only of white-space characters.
    /// </exception>
    [SuppressMessage(
        "StyleCop.CSharp.SpacingRules",
        "SA1011:Closing square brackets should be spaced correctly",
        Justification = "Conflicting style rules.")]
    public BankMeaning GetMeaning(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text must be non-empty.", nameof(text));
        }

        TokenizedEmbedding embedding = _model.EmbedWithTokens(text);

        // Try to get the embedding for "bank" directly.
        float[]? bankVector = embedding.GetTokenVector("bank");

        // Fallback: any token containing "bank" (e.g., "banking", "banks").
        bankVector ??= GetFirstTokenContainingBank(embedding);

        if (bankVector == null)
        {
            // No relevant token found; we cannot determine a specific sense.
            return BankMeaning.None;
        }

        // Compute cosine similarity to each centroid.
        double riverSim = CosineSimilarity(
            bankVector,
            _centroids.RiverBankCentroid);

        double financialSim = CosineSimilarity(
            bankVector,
            _centroids.FinancialBankCentroid);

        double verbSim = CosineSimilarity(
            bankVector,
            _centroids.VerbBankCentroid);

        // If the verb (tilt) sense is the strongest, treat it as "other".
        if (verbSim >= riverSim && verbSim >= financialSim)
        {
            return BankMeaning.Other;
        }

        // Otherwise, choose between river and financial and apply the similarity threshold.
        BankMeaning bestLabel;
        double bestSim;

        if (riverSim >= financialSim)
        {
            bestLabel = BankMeaning.River;
            bestSim = riverSim;
        }
        else
        {
            bestLabel = BankMeaning.Financial;
            bestSim = financialSim;
        }

        if (bestSim < _centroids.SimilarityThreshold)
        {
            return BankMeaning.Other;
        }

        return bestLabel;
    }

    /// <summary>
    /// Releases the resources used by the current <see cref="BankMeaningIdentifier"/> instance.
    /// </summary>
    public void Dispose()
    {
        _model.Dispose();
    }

    private static BankEmbeddingCentroids LoadCentroids()
    {
        Assembly assembly = typeof(BankMeaningIdentifier).Assembly;
        Stream? stream = FileHelper.GetEmbeddedResourceStream(assembly, "Data.bank_centroids.json");

        if (stream == null)
        {
            throw new InvalidOperationException(
                "Embedded resource 'Data.bank_centroids.json' could not be found.");
        }

        using (stream)
        {
            BankEmbeddingCentroids? centroids =
                JsonSerializer.Deserialize<BankEmbeddingCentroids>(stream);

            if (centroids == null)
            {
                throw new InvalidOperationException(
                    "Failed to deserialize bank_centroids.json into BankEmbeddingCentroids.");
            }

            return centroids;
        }
    }

    [SuppressMessage(
        "StyleCop.CSharp.SpacingRules",
        "SA1011:Closing square brackets should be spaced correctly",
        Justification = "Conflicting style rules.")]
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

    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a == null || b == null)
        {
            throw new ArgumentNullException(
                a == null ? nameof(a) : nameof(b),
                "Cosine similarity vectors must not be null.");
        }

        if (a.Length != b.Length)
        {
            throw new ArgumentException(
                $"Vector lengths do not match: {a.Length} vs {b.Length}.");
        }

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
