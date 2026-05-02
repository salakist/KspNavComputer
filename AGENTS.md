# KSP Navigation Computer — Root AGENTS.md

This file is the entry point for AI agents working in this repository.
Read this file first, then consult the sub-folder `AGENTS.md` for the area you are working in.

---

## Project overview

A desktop application for planning KSP 1.12.x missions: transfer windows, delta-v budgets,
burn details, and Precise Maneuver mod copy-paste. Supports stock bodies plus Outer Planets
Mod (OPM) and Minor Planets Expansion (MPE) bodies.

---

## Tech stack

| Layer | Technology |
|-------|-----------|
| Core library | C# / .NET 8 class library |
| API | ASP.NET Core 8 |
| Frontend | Vite + React + TypeScript |
| Tests | xUnit (.NET 8) |
| Scripting | PowerShell 7 |

---

## Repository structure

```
KspNavComputer/
├── src/
│   ├── Core/          KspNavComputer.Core          — orbital mechanics, body data
│   ├── Api/           KspNavComputer.Api           — ASP.NET Core REST API
│   └── Web/           KspNavComputer.Web           — Vite + React + TypeScript
├── tests/
│   └── Core/          KspNavComputer.Core.Tests    — xUnit unit tests
├── scripts/
│   ├── get-agent-token.ps1                         — salakist-agent GitHub App token
│   └── checks/
│       └── run-checks.ps1                          — quality gate (created end of Increment 1)
├── AGENTS.md                                       — this file
├── COMMIT-POLICY.md                                — commit and PR workflow policy
└── README.md                                       — contributor setup
```

> `src/`, `tests/`, and `scripts/checks/` do not exist yet. They are created in Increment 1.

---

## Build and test

> Not yet available. Commands will be added here at the end of Increment 1.
> Expected commands once established:
> ```powershell
> dotnet build
> dotnet test
> ./scripts/checks/run-checks.ps1
> ```

---

## Increment status

| Increment | Description | Status |
|-----------|-------------|--------|
| 1a | Scaffold + one-way circular transfer | not started |
| 1b | Round trip | not started |
| 1c | Inclined / elliptical parking orbits | not started |
| 2  | Precise Maneuver export | not started |
| 3  | Transfer window porkchop plot | not started |
| 4  | OPM + MPE body support | not started |
| 5  | KSP save file import | not started |
| 6  | Landed start support | not started |
| 7  | Integrated delta-V map | not started |
| 8  | Sub-system transfers | not started |
| 9  | In-game mod | not started |

Full increment descriptions: [`local-docs/planning.md`](local-docs/planning.md)

---

## Sub-folder AGENTS.md routing

| Folder | AGENTS.md covers |
|--------|-----------------|
| `src/Core/` | Domain model, orbital mechanics algorithms, body data conventions |
| `src/Api/` | API conventions, endpoint contracts, DTO structure |
| `src/Web/` | Frontend conventions, component structure, API client |
| `tests/Core/` | Test conventions, known validation references |

Each sub-folder `AGENTS.md` is created when that folder is first scaffolded.

---

## Key policies and references

- **Commit and PR policy**: [`COMMIT-POLICY.md`](COMMIT-POLICY.md)
- **Contributor setup**: [`README.md`](README.md)
- **Planning and increments**: [`local-docs/planning.md`](local-docs/planning.md) (gitignored)
- **Branch naming**: Conventional Branch format — `<type>/<description>`
- **No direct push to `main`**: all changes via pull request, owner approval required
