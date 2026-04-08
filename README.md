# Genova.BankIdentifier

Identifies the meaning of the word `bank` in a sentence as river, financial, other, or none using contextual embeddings.

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

## License

GNU General Public License v3.0
