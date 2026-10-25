# Luna Chinese

Luna Chinese is a Chinese-learning application with Web and Android clients, built for **Korean-speaking learners**: word analyses carry Korean and English glosses side by side, and characters are described in Hanja terms (훈 meaning, 음 reading).

**Current version: `0.16.0`** — see [Versioning](#versioning).

## Features

| Feature | Status | Description |
|---|---|---|
| Word extraction | ✅ Available | Upload a photo of a textbook vocabulary page; returns the numbered, printed headwords (handwritten notes and example sentences are ignored). |
| Word analysis | 🚧 In progress | Per-word breakdown (pinyin, Traditional/Simplified forms, Korean/English meanings, Hanja reading, per-character Hun/Eum) with a persistent cache. Domain and cache are done; the AI client and API endpoint are not. |

## Repository structure

- `backend/` — .NET 10 Web API
  - `LunaChinese.API` — ASP.NET Core host (controllers, DI, configuration)
  - `LunaChinese.Core` — domain layer: models, feature abstractions, services (no ASP.NET or EF Core dependency)
  - `LunaChinese.Infrastructure` — implementations of Core's abstractions: EF Core persistence and the Gemini AI client
  - `LunaChinese.Core.Test` — xUnit + NSubstitute tests for Core and Infrastructure
- `android/` — Kotlin Android application (not yet scaffolded)

## Tech stack

- **Backend:** .NET 10, ASP.NET Core, EF Core 10
- **Database:** SQLite in Development, PostgreSQL in other environments
- **AI provider:** Google Gemini (`generateContent` API with schema-constrained JSON output)
- **Testing:** xUnit, NSubstitute, in-memory SQLite

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A [Google Gemini API key](https://aistudio.google.com/apikey)

### Configure

The Gemini API key is read from configuration and **must not be committed**. Store it with user-secrets:

```bash
cd backend/LunaChinese
dotnet user-secrets set "Gemini:ApiKey" "<your-key>" --project LunaChinese.API
```

| Setting | Where | Notes |
|---|---|---|
| `Gemini:ApiKey` | user-secrets / environment | Required; the API fails to start without it. |
| `Gemini:Model` | `appsettings.Development.json` | Required. Lowercase, case-sensitive model ID (currently `gemini-3.5-flash-lite`). |
| `ConnectionStrings:Sqlite` | `appsettings.Development.json` | Optional; defaults to a local `lunachinese.dev.db`. |
| `ConnectionStrings:Postgres` | environment | Required outside Development. |

### Run and test

From `backend/LunaChinese/`:

```bash
dotnet build                              # build the solution
dotnet run --project LunaChinese.API      # http://localhost:5247 (https profile: https://localhost:7266)
dotnet test                               # run all tests
```

In Development, the SQLite schema is created automatically on startup, and the OpenAPI document is served at `/openapi/v1.json`.

## API

### `POST /api/Word/extract`

Extracts vocabulary from a page image.

| Form field | Type | Description |
|---|---|---|
| `image` | file | The page photo (JPEG/PNG). |
| `mode` | `All` \| `MarkedOnly` | Which words to extract. Defaults to `All`; `MarkedOnly` is not implemented yet. |

```bash
curl -X POST http://localhost:5247/api/Word/extract \
  -F "image=@page.jpg;type=image/jpeg" -F "mode=All"
```

```json
{ "words": ["銷售", "彼此", "感想", "實行"] }
```

| Status | Meaning |
|---|---|
| `200` | Success; `words` may be empty if nothing eligible was found. |
| `400` | No image uploaded. |
| `501` | The requested `mode` is not implemented. |
| `429` / `503` / `504` / `502` | Provider rate-limited / overloaded / timed out / returned an unusable response. |
| `500` | Any other provider or server failure. |

`backend/LunaChinese/LunaChinese.API/LunaChinese.API.http` has ready-to-send requests; put a test image at `LunaChinese.API/samples/page.jpg` (git-ignored).

## Versioning

The project is pre-1.0 and versioned as **`0.MINOR.0`**:

- **Every commit that changes the system bumps `MINOR` by one**: features, fixes, refactors, tests, and build/config chores.
- **Documentation-only commits don't count** (`docs:` commits, or any commit that only touches docs such as `README.md` / `CLAUDE.md`). Neither do merge commits.
- The table is updated **once per completed feature**, not per commit. Each row gives the version reached at the end of that feature and the commit range it covers.

| Version | Commits | Date | Description |
|---|---|---|---|
| 0.16.0 | `f642b98`..`1552fad` | 2026-10-25 | Initial backend: .NET 10 solution; word analysis domain, service, and EF Core alias cache; image word extraction via Gemini (`POST /api/Word/extract`); fixes from the first code review. |

## Contributing

- **Commits** follow [Conventional Commits](https://www.conventionalcommits.org/): `feat(core):`, `fix:`, `docs:`, `chore:`, `refactor:`, `test(infra):`.
- **Branches** are short-lived, one per PR, named `<type>/<scope>-<description>` (e.g. `fix/extraction-error-handling`), and deleted after merge.
- Architecture details and coding conventions are in [`CLAUDE.md`](CLAUDE.md).
