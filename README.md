# Genova.BankIdentifier

Identifies the meaning of the word `bank` in a sentence as river, financial, other, or none using contextual embeddings.

> [!WARNING]
> This is an experimental project and should not be considered production-ready. It exists to explore a small AI, ML, agent, or demo idea within the broader Genova ecosystem.

> [!IMPORTANT]
> A fresh public clone of this repository should not be expected to restore or build without additional Genova infrastructure. Many Genova dependencies are distributed through a private authenticated NuGet feed, and the public source does not include feed credentials or a complete public package graph.

## Installation

```bash
dotnet restore
dotnet build
```

## Usage

Run the console app:

```bash
dotnet run --project BankIdentifier.Terminal
```

Or use the core library from code:

```csharp
using var identifier = new BankMeaningIdentifier();
var meaning = identifier.GetMeaning("He sat on the bank of the river.");
```

## Features

* Classifies `bank` as river, financial, other, or none
* Exposes a reusable `BankMeaningIdentifier` API
* Includes a console app for interactive testing
* Includes a training utility for generating centroid data from sample corpora

## Notes

* The runtime classifier expects embedded centroid data in `Data/bank_centroids.json`.
* The training project uses hard-coded Windows paths for its input and output directories.

## Thanks

* MiniLM-based ONNX embeddings
* Microsoft.ML / ONNX Runtime

## Third-Party Notices

This project has direct runtime dependencies on third-party NuGet packages, including `Microsoft.Extensions.*` packages (MIT), `Microsoft.ML*` packages (MIT). See each package's NuGet license metadata for full license and notice terms.

## License

GNU General Public License v3.0. See the `LICENSE` file for details.
