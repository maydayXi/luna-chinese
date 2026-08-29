# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Luna Chinese is a Chinese-learning application with Web and Android clients. It is early-stage. The backend's `LunaChinese.API` project is still the default ASP.NET scaffold (a `/weatherforecast` endpoint in `Program.cs`), while the domain layer for the first real feature — word analysis — is being built out in `LunaChinese.Core`. `android/` is an empty placeholder. Expect to replace scaffold code (the weatherforecast endpoint) rather than extend it.

The target learner is **Korean-speaking**, and this shapes the domain model: every analysis carries both Korean and English glosses, and each character is described in Hanja terms — `Hun` (훈, meaning gloss) and `Eum` (음, Korean reading) on `CharacterAnalysis`, plus `HanjaReading` on `WordAnalysis`. Words are also stored in both Traditional and Simplified forms. New AI-facing fields should follow the same Korean-first, English-alongside pattern.

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

- `Features/<Feature>/` holds the abstractions and the orchestrating service for one feature.
- `Models/` holds immutable `record` types shared across the feature.
- `Enums/` holds shared enumerations, e.g. `AiOperationErrorCode` for classifying AI-provider failures (rate limited, overloaded, invalid response, timeout).

### Word analysis (`Features/WordsAnalysis/`)

Three interfaces split the work, and the split is the important part:

- `IWordAnalysisService` — the feature's entry point: batch analysis with optional `IProgress<WordAnalysisProgress>` reporting.
- `IAnalysisCache` — persistence for completed analyses, with a `GetManyAsync` bulk lookup so a whole request costs one cache round-trip.
- `IWordAnalysisAiClient` — owns *all* provider-specific concerns: prompt assembly, HTTP, JSON parsing, retries/resilience. It takes a batch of words and returns one `WordAnalysisItemResult` per word.

`WordAnalysisService` is the only implementation so far; **no `IAnalysisCache` or `IWordAnalysisAiClient` implementation exists yet**, and nothing is registered in DI. Keep provider details (model names, prompts, API keys, retry policy) out of `WordAnalysisService` — they belong behind `IWordAnalysisAiClient`.

`WordAnalysisService.AnalyzeBatchAsync` flow: trim/de-duplicate the requested words → one `cache.GetManyAsync` for all of them → chunk the misses into batches of `BatchSize` (private const, 10), one `aiClient.AnalyzeAsync` call per chunk → cache each success → emit results in the original distinct-word order. `WordAnalysisProgress.FromCache` distinguishes cache hits (reported up front) from fresh analyses (reported per batch). Cache keys go through the private `BuildCacheKey` (currently identity) so the keying scheme can gain normalization or versioning in one place.

Failures are per word, not per batch: `BatchWordAnalysisResult` exposes `SuccessCount`/`FailureCount`/`IsPartiallySuccessful`, and the service even synthesizes an `AiOperationErrorCode.Unknown` failure for any word the AI client silently drops. Build results with the `WordAnalysisItemResult.Success`/`Failure` factory methods — the constructor is private precisely so no one can create a result that is both.

## Conventions

- Target framework is `net10.0` with `Nullable` and `ImplicitUsings` enabled — respect nullable annotations and avoid redundant `using` directives.
- Domain types are immutable `record`s; public API surface is documented with English XML doc comments (`<summary>`, `<param>`, `<returns>`). Multi-field domain records also carry an `<example>` block showing a filled-in instance — see `WordAnalysis` and `CharacterAnalysis`.
- Services use primary constructors for dependencies and are `sealed` unless meant to be extended.
- Async library code awaits with `.ConfigureAwait(false)`, and every provider-facing method takes a `CancellationToken cancellationToken = default`.