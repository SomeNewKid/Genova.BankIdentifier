// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.BankIdentifier;

/// <summary>
/// Represents the centroid embeddings for different senses of the word "bank".
/// This structure is serialized to JSON and used by the runtime toy application.
/// </summary>
internal sealed class BankEmbeddingCentroids
{
    /// <summary>
    /// Gets or sets the dimensionality of the embedding vectors for this model.
    /// </summary>
    public int EmbeddingSize { get; set; }

    /// <summary>
    /// Gets or sets the centroid embedding for the "river bank" sense.
    /// </summary>
    public float[] RiverBankCentroid { get; set; } = [];

    /// <summary>
    /// Gets or sets the centroid embedding for the "financial bank" sense.
    /// </summary>
    public float[] FinancialBankCentroid { get; set; } = [];

    /// <summary>
    /// Gets or sets the centroid embedding for "bank" as a verb.
    /// </summary>
    public float[] VerbBankCentroid { get; set; } = [];

    /// <summary>
    /// Gets or sets the similarity threshold used to decide whether a sentence
    /// is close enough to the river/financial clusters to be classified as such,
    /// or should instead be treated as "other".
    /// </summary>
    public double SimilarityThreshold { get; set; }
}
