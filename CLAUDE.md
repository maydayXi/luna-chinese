# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Luna Chinese is a Chinese-learning application with Web and Android clients. It is early-stage: the backend is still the default ASP.NET scaffold (a `/weatherforecast` endpoint in `Program.cs`) and `android/` is an empty placeholder. Expect to replace scaffold code rather than extend it.

## Layout

- `backend/` — .NET 10 Web API. The solution lives at `backend/LunaChinese/LunaChinese.sln` with a single project, `LunaChinese.API` (minimal-API style; endpoints and DI are wired directly in `Program.cs`, not controllers).
- `android/` — Kotlin Android client (not yet scaffolded).

## Backend commands

Run these from `backend/LunaChinese/`:

```bash
dotnet build                              # build the solution
dotnet run --project LunaChinese.API      # run the API (http profile → http://localhost:5247, https → https://localhost:7266)
dotnet test                               # run tests (no test project exists yet)
```

Run a single test once a test project exists: `dotnet test --filter "FullyQualifiedName~<TestName>"`.

OpenAPI is mapped only in the Development environment (`app.MapOpenApi()` at `/openapi`). `LunaChinese.API.http` contains ready-to-send example requests.

## Conventions

- Target framework is `net10.0` with `Nullable` and `ImplicitUsings` enabled — respect nullable annotations and avoid redundant `using` directives.