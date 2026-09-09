# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Luna Chinese is a Chinese-learning application with Web and Android clients. It is early-stage. The backend's `LunaChinese.API` project is still the default ASP.NET scaffold (a `/weatherforecast` endpoint in `Program.cs`), while the domain layer for the first real feature — word analysis — is being built out in `LunaChinese.Core`. `android/` is an empty placeholder. Expect to replace scaffold code (the weatherforecast endpoint) rather than extend it.

The target learner is **Korean-speaking**, and this shapes the domain model: every analysis carries both Korean and English glosses, and each character is described in Hanja terms — `Hun` (훈, meaning gloss) and `Eum` (음, Korean reading) on `CharacterAnalysis`, plus `HanjaReading` on `WordAnalysis`. Words are also stored in both Traditional and Simplified forms. New AI-facing fields should follow the same Korean-first, English-alongside pattern.

## Layout

- `backend/LunaChinese/` — .NET 10 solution (`LunaChinese.sln`) with three projects:
  - `LunaChinese.API` (`Microsoft.NET.Sdk.Web`) — minimal-API host; endpoints and DI are wired directly in `Program.cs`, not controllers. Still scaffold; does **not** yet reference `LunaChinese.Core`.
  - `LunaChinese.Core` (`Microsoft.NET.Sdk`) — domain/business layer with no ASP.NET dependency.
  - `LunaChinese.Core.Test` — xUnit + NSubstitute tests for `LunaChinese.Core`.
- `android/` — Kotlin Android client (not yet scaffolded).

## Backend commands

Run these from `backend/LunaChinese/`:

```bash
dotnet build                              # build the solution
dotnet run --project LunaChinese.API      # run the API (http profile → http://localhost:5247, https → https://localhost:7266)
dotnet test                               # run all tests
dotnet test --filter "FullyQualifiedName~<TestName>"       # one test or one test class
```

**SDK selection:** the solution targets `net10.0`, and there is no `global.json`. If `dotnet build` fails with `NETSDK1045: The current .NET SDK does not support targeting .NET 10.0`, the `dotnet` first on `PATH` is an older SDK — check `dotnet --list-sdks` and invoke the 10.x SDK explicitly (e.g. `~/.dotnet/dotnet test`).

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

`WordAnalysisService.AnalyzeBatchAsync` flow: trim/de-duplicate the requested words → one `cache.GetManyAsync` for all of them → chunk the misses into batches of `BatchSize` (private const, 10), one `aiClient.AnalyzeAsync` call per chunk → cache each success → emit results in the original distinct-word order. `WordAnalysisProgress.FromCache` distinguishes cache hits (reported up front) from fresh analyses (reported per batch). Cancellation is checked once per batch, before the provider call.

Failures are per word, not per batch: `BatchWordAnalysisResult` exposes `SuccessCount`/`FailureCount`/`IsPartiallySuccessful`, and the service even synthesizes an `AiOperationErrorCode.Unknown` failure for any word the AI client silently drops. Build results with the `WordAnalysisItemResult.Success`/`Failure` factory methods — the constructor is private precisely so no one can create a result that is both.

### Cache keys (`WordCacheKey`)

Every cache lookup and write goes through `WordCacheKey.Build`, so the keying scheme lives in one place. It folds away differences that are **presentational only**: canonical Unicode composition (`FormC`), full-width ASCII and the ideographic space folded to half-width, interior whitespace runs collapsed to one space, and the ends trimmed. Blank input maps to `string.Empty`. `Build` is idempotent — safe to apply on both write and read.

Traditional and Simplified forms deliberately keep **distinct** keys: many Traditional characters share one Simplified form, so folding scripts together would be lossy. Cross-script de-duplication belongs in an aliasing layer above the key, not in `Build`.

## Testing

`LunaChinese.Core.Test` tests behavior through the public entry points, substituting `IAnalysisCache` and `IWordAnalysisAiClient` with NSubstitute so each test isolates one aspect of the orchestration.

- `TestData` builds the value objects (`Analysis`, `Command`) so tests don't repeat `WordAnalysis`'s many required members.
- `WordAnalysisServiceTests` uses `GivenCached(...)` / `GivenAiSucceedsForAll()` helpers to state the scenario; `GivenCached()` with no arguments makes the whole request a cache miss.
- Assert on progress with the test's own `RecordingProgress`, not `Progress<T>` — `Progress<T>` marshals callbacks through the captured synchronization context, so assertions can run before the reports arrive.
- When verifying which words reached a substitute, match on **contents** (`Arg.Is<IReadOnlyCollection<string>>(w => w.SequenceEqual(expected))`), not on a collection value. The service passes the `Chunk` array, and a collection expression like `Arg.Is<IReadOnlyCollection<string>>([word])` compiles to a different type that never compares equal.
- Tests are laid out in `#region Arrange / Act / Assert` blocks and carry an XML `<summary>` saying what the case pins down and why it matters.

## Conventions

- Target framework is `net10.0` with `Nullable` and `ImplicitUsings` enabled — respect nullable annotations and avoid redundant `using` directives.
- Domain types are immutable `record`s; public API surface is documented with English XML doc comments (`<summary>`, `<param>`, `<returns>`). Multi-field domain records also carry an `<example>` block showing a filled-in instance — see `WordAnalysis` and `CharacterAnalysis`.
- Services use primary constructors for dependencies and are `sealed` unless meant to be extended.
- Async library code awaits with `.ConfigureAwait(false)`, and every provider-facing method takes a `CancellationToken cancellationToken = default`.
- Commit messages follow Conventional Commits (`feat(core):`, `test(core):`, `docs:`, `chore:`).
