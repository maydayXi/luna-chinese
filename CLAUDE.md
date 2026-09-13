# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Luna Chinese is a Chinese-learning application with Web and Android clients. It is early-stage: the domain layer for the first real feature — word analysis — lives in `LunaChinese.Core`, its persistence is implemented in `LunaChinese.Infrastructure` (EF Core), and `LunaChinese.API` wires the two together as a minimal-API host. `android/` is an empty placeholder.

The target learner is **Korean-speaking**, and this shapes the domain model: every analysis carries both Korean and English glosses, and each character is described in Hanja terms — `Hun` (훈, meaning gloss) and `Eum` (음, Korean reading) on `CharacterAnalysis`, plus `HanjaReading` on `WordAnalysis`. Words are also stored in both Traditional and Simplified forms. New AI-facing fields should follow the same Korean-first, English-alongside pattern.

## Layout

- `backend/LunaChinese/` — .NET 10 solution (`LunaChinese.sln`) with four projects:
  - `LunaChinese.API` (`Microsoft.NET.Sdk.Web`) — minimal-API host; endpoints and DI are wired directly in `Program.cs`, not controllers. References both `Core` and `Infrastructure` and registers `EfCoreAnalysisCache` as `IAnalysisCache`. No feature endpoints exist yet — only DB setup on startup.
  - `LunaChinese.Core` (`Microsoft.NET.Sdk`) — domain/business layer with no ASP.NET or EF Core dependency.
  - `LunaChinese.Infrastructure` (`Microsoft.NET.Sdk`) — EF Core persistence for `Core`'s abstractions (currently `IAnalysisCache`). References `Core`; not referenced by it.
  - `LunaChinese.Core.Test` — xUnit + NSubstitute tests for both `LunaChinese.Core` and `LunaChinese.Infrastructure` (there is no separate `Infrastructure.Test` project).
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

**EF Core migrations** are authored against `LunaChinese.Infrastructure`, using `LunaChinese.API` as the startup project (it holds the package reference EF's design-time tooling needs):

```bash
dotnet ef migrations add <Name> --project LunaChinese.Infrastructure --startup-project LunaChinese.API
```

Design-time tooling resolves the context through `LunaChineseDbContextFactory`, which reads the Postgres connection string from the `LUNA_CHINESE_DB_CONNECTION` environment variable (not from any `appsettings.json`) and fails fast if it's unset — set it in your shell before running `dotnet ef` commands. **Never commit real connection strings/passwords** into `appsettings.*.json`; the app resolves them from configuration/environment at run time (see below), so a file value is only ever a local convenience.

## Architecture

`LunaChinese.Core` is organized as vertical feature slices:

- `Features/<Feature>/` holds the abstractions and the orchestrating service for one feature.
- `Models/` holds immutable `record` types shared across the feature.
- `Enums/` holds shared enumerations, e.g. `AiOperationErrorCode` for classifying AI-provider failures (rate limited, overloaded, invalid response, timeout).

### Word analysis (`Features/WordsAnalysis/`)

Three interfaces split the work, and the split is the important part:

- `IWordAnalysisService` — the feature's entry point: batch analysis with optional `IProgress<WordAnalysisProgress>` reporting.
- `IAnalysisCache` — persistence for completed analyses, with a `GetManyAsync` bulk lookup so a whole request costs one cache round-trip. Implemented by `EfCoreAnalysisCache` in `LunaChinese.Infrastructure`.
- `IWordAnalysisAiClient` — owns *all* provider-specific concerns: prompt assembly, HTTP, JSON parsing, retries/resilience. It takes a batch of words and returns one `WordAnalysisItemResult` per word. **No implementation exists yet**, and nothing is registered in DI for it. Keep provider details (model names, prompts, API keys, retry policy) out of `WordAnalysisService` — they belong behind this interface.

`WordAnalysisService.AnalyzeBatchAsync` flow: trim/de-duplicate the requested words → one `cache.GetManyAsync` for all of them → chunk the misses into batches of `BatchSize` (private const, 10), one `aiClient.AnalyzeAsync` call per chunk → cache each success → emit results in the original distinct-word order. `WordAnalysisProgress.FromCache` distinguishes cache hits (reported up front) from fresh analyses (reported per batch). Cancellation is checked once per batch, before the provider call.

Failures are per word, not per batch: `BatchWordAnalysisResult` exposes `SuccessCount`/`FailureCount`/`IsPartiallySuccessful`, and the service even synthesizes an `AiOperationErrorCode.Unknown` failure for any word the AI client silently drops. Build results with the `WordAnalysisItemResult.Success`/`Failure` factory methods — the constructor is private precisely so no one can create a result that is both.

### Cache keys (`WordCacheKey`)

Every cache lookup and write goes through `WordCacheKey.Build`, so the keying scheme lives in one place. It folds away differences that are **presentational only**: canonical Unicode composition (`FormC`), full-width ASCII and the ideographic space folded to half-width, interior whitespace runs collapsed to one space, and the ends trimmed. Blank input maps to `string.Empty`. `Build` is idempotent — safe to apply on both write and read.

Traditional and Simplified forms deliberately keep **distinct** keys: many Traditional characters share one Simplified form, so folding scripts together would be lossy. Cross-script de-duplication belongs in an aliasing layer above the key, not in `Build` — that aliasing layer is `EfCoreAnalysisCache` (below).

### Persistence (`LunaChinese.Infrastructure`)

`LunaChineseDbContext` (`Context/`) exposes two internal `DbSet`s — `Analyses` (`CachedAnalysis`) and `Aliases` (`AnalysisAlias`), both in `Entity/`:

- `CachedAnalysis` stores one `WordAnalysis` as a JSON document (`ContentJson`), keyed by a canonical cache key. The JSON round-trip uses `JsonSerializerDefaults.Web` on both write and read — changing those options would make already-cached rows unreadable.
- `AnalysisAlias` maps a surface form (`Alias`, primary key) to the `CanonicalKey` of the `CachedAnalysis` it resolves to, cascade-deleted with its analysis.

`EfCoreAnalysisCache` (`Features/WordsAnalysis/`) is the `IAnalysisCache` implementation and is where cross-script de-duplication actually happens: `SetAsync` writes alias rows for the requested key *and* for `WordCacheKey.Build` of the analysis's `Word`, `TraditionalWord`, and `SimplifiedWord`, so any surface form later reaches the same stored entry. It is **first-writer-wins**: `SetAsync` checks whether any of those aliases already resolves to an entry before inserting, and also catches `DbUpdateException` from the alias primary key to absorb a concurrent writer that won the race — either way the existing analysis is kept and the caller's write is silently dropped, so callers should treat `SetAsync` as best-effort rather than assuming their value was the one stored.

`ContextExtensions.AddLunaChineseContext` (extension method on `IServiceCollection`, called from `Program.cs`) chooses the EF Core provider by host environment: SQLite in Development (`ConnectionStrings:Sqlite` in config, falling back to a local `lunachinese.dev.db` file), Postgres otherwise (`ConnectionStrings:Postgres`, required — throws if missing rather than silently falling back to a file DB in production). `Program.cs` then calls `EnsureCreated()` in Development and `Migrate()` otherwise on startup, inside a scoped service resolution.

`LunaChineseDbContextFactory` is a separate, **design-time-only** path used by `dotnet ef` tooling (see Backend commands above) — it always uses Postgres via `LUNA_CHINESE_DB_CONNECTION` regardless of environment, since design-time tools can't resolve the app's own DI-configured `IConfiguration`/`IHostEnvironment`.

## Testing

`LunaChinese.Core.Test` covers both `LunaChinese.Core` and `LunaChinese.Infrastructure`, using two different strategies depending on what's under test:

- **`WordAnalysisServiceTests`** tests behavior through the public entry point, substituting `IAnalysisCache` and `IWordAnalysisAiClient` with NSubstitute so each test isolates one aspect of the orchestration.
  - `TestData` builds the value objects (`Analysis`, `Command`) so tests don't repeat `WordAnalysis`'s many required members.
  - Uses `GivenCached(...)` / `GivenAiSucceedsForAll()` helpers to state the scenario; `GivenCached()` with no arguments makes the whole request a cache miss.
  - Assert on progress with the test's own `RecordingProgress`, not `Progress<T>` — `Progress<T>` marshals callbacks through the captured synchronization context, so assertions can run before the reports arrive.
  - When verifying which words reached a substitute, match on **contents** (`Arg.Is<IReadOnlyCollection<string>>(w => w.SequenceEqual(expected))`), not on a collection value. The service passes the `Chunk` array, and a collection expression like `Arg.Is<IReadOnlyCollection<string>>([word])` compiles to a different type that never compares equal.
- **`EfCoreAnalysisCacheTests`** runs against a real SQLite database held in memory (`DataSource=:memory:` over a single open `SqliteConnection`, since SQLite drops such a database once the connection closes) rather than a substitute, because the aliasing/JSON-persistence behavior can't be verified without exercising the storage layer. Each test method gets a fresh `LunaChineseDbContext` per read/write (`NewContext()`/`NewCache()`) so results genuinely round-trip through the database instead of being served from a previous write's change tracker.
- Tests are laid out in `#region Arrange / Act / Assert` blocks and carry an XML `<summary>` saying what the case pins down and why it matters.

## Conventions

- Target framework is `net10.0` with `Nullable` and `ImplicitUsings` enabled — respect nullable annotations and avoid redundant `using` directives.
- Domain types are immutable `record`s; public API surface is documented with English XML doc comments (`<summary>`, `<param>`, `<returns>`). Multi-field domain records also carry an `<example>` block showing a filled-in instance — see `WordAnalysis` and `CharacterAnalysis`.
- Services use primary constructors for dependencies and are `sealed` unless meant to be extended.
- Async library code awaits with `.ConfigureAwait(false)`, and every provider-facing method takes a `CancellationToken cancellationToken = default`.
- Commit messages follow Conventional Commits (`feat(core):`, `feat(infra):`, `test(core):`, `docs:`, `chore:`).
