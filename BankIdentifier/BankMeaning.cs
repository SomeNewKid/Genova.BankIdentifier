// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.BankIdentifier;

/// <summary>
/// Represents the inferred meaning of the word "bank" in a user-provided sentence.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by other Genova components.")]
public enum BankMeaning
{
    /// <summary>
    /// The word "bank" does not appear.
    /// </summary>
    None,

    /// <summary>
    /// The word "bank" refers to the edge or shore of a river or other body of water.
    /// </summary>
    River,

    /// <summary>
    /// The word "bank" refers to a financial institution or monetary context.
    /// </summary>
    Financial,

    /// <summary>
    /// The word "bank" refers to another sense, such as the verb meaning "to tilt",
    /// or any meaning not related to rivers or financial institutions.
    /// </summary>
    Other,
}
