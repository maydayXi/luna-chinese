# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Luna Chinese is a Chinese-learning application with Web and Android clients. It is early-stage. The backend's `LunaChinese.API` project is still the default ASP.NET scaffold (a `/weatherforecast` endpoint in `Program.cs`), while the domain layer for the first real feature — word analysis — is being built out in `LunaChinese.Core`. `android/` is an empty placeholder. Expect to replace scaffold code (the weatherforecast endpoint) rather than extend it.

## Layout

- `backend/LunaChinese/` — .NET 10 solution (`LunaChinese.sln`) with two projects:
  - `LunaChinese.API` (`Microsoft.NET.Sdk.Web`) — minimal-API host; endpoints and DI are wired directly in `Program.cs`, not controllers. Still scaffold; does **not** yet reference `LunaChinese.Core`.
  - `LunaChinese.Core` (`Microsoft.NET.Sdk`) — domain/business layer with no ASP.NET dependency.
- `android/` — Kotlin Android client (not yet scaffolded).

## Backend commands

Run these from `backend/LunaChinese/`:

```bash
dotnet build                              # build the solution
dotnet run --project LunaChinese.API      # run the API (http profile → http://localhost:5247, https → https://localhost:7266)
dotnet test                               # run tests (no test project exists yet)
```

Run a single test once a test project exists: `dotnet test --filter "FullyQualifiedName~<TestName>"`.

OpenAPI is mapped only in the Development environment (`app.MapOpenApi()` at `/openapi`). `LunaChinese.API/LunaChinese.API.http` contains ready-to-send example requests.

## Architecture

`LunaChinese.Core` is organized as vertical feature slices:

- `Features/<Feature>/` holds the abstractions and services for one feature. `Features/WordsAnalysis/` defines `IWordAnalysisService` (batch analysis with optional `IProgress<WordAnalysisProgress>` reporting) and `IAnalysisCache` (a cache so previously analyzed words skip the AI provider). `WordAnalysisService` is currently a stub that throws `NotImplementedException`.
- `Models/` holds immutable `record` types shared across the feature. Word analysis follows a batch-with-per-item-outcome shape: `AnalyzeWordsCommand` (request) → `BatchWordAnalysisResult` (aggregate counts) containing one `WordAnalysisItemResult` per word, each either a success (carrying `WordAnalysis`) or a failure (carrying an error code/message). Use the `WordAnalysisItemResult.Success`/`Failure` factory methods rather than the primary constructor.
- `Enums/` holds shared enumerations, e.g. `AiOperationErrorCode` for classifying AI-provider failures (rate limited, overloaded, invalid response, timeout).

The intended flow: the analysis service processes words in batches (`WordAnalysisService.BatchSize = 10`), consulting `IAnalysisCache` before calling the AI provider, reporting progress per word, and returning a partial-success-tolerant batch result (individual words can fail without failing the batch).

## Conventions

- Target framework is `net10.0` with `Nullable` and `ImplicitUsings` enabled — respect nullable annotations and avoid redundant `using` directives.
- Domain types are immutable `record`s; public API surface is documented with English XML doc comments (`<summary>`, `<param>`, `<returns>`).