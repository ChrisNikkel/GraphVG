# Contributing to GraphVG

Thanks for your interest in contributing.

## Prerequisites

- .NET 8 SDK
- Git

## Local Development

Clone and enter the repository:

```bash
git clone https://github.com/ChrisNikkel/GraphVG.git
cd GraphVG
```

Build:

```bash
dotnet build
```

Run tests:

```bash
dotnet test
```

Run the example app:

```bash
dotnet run --project Examples/Example
```

## Pull Requests

- Keep changes focused and small when possible.
- Add or update tests for behavior changes.
- Ensure `dotnet build` and `dotnet test` pass locally before opening a PR.
- Update docs when public behavior changes.

## Coding Style

- Follow repository conventions in `CLAUDE.md`.
- Keep APIs pipeline-friendly (`create`, `with*`, `add*`, `to*`).
- Prefer clear naming over abbreviations.

## Reporting Issues

When filing an issue, include:

- Expected behavior
- Actual behavior
- Minimal reproduction
- Environment details (OS, .NET SDK version)
