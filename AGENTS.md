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
├── bruno/                                          — Bruno API collection (reference cases + inclination demo)
├── docs/                                           — tracked design and policy docs (see docs/AGENTS.md)
│   ├── algorithms/                                 — algorithm reference docs (Lambert, Δv pipeline)
│   ├── planning/                                   — increment planning, roadmap, overview
│   ├── body-data-schema.md                         — CelestialBody / OrbitalElements field reference
│   └── commit-policy.md                            — commit and PR workflow policy
├── scripts/
│   ├── get-agent-token.ps1                         — salakist-agent GitHub App token
│   ├── hooks/
│   │   └── prepare-commit-msg                      — auto-appends Agent trailer for salakist-agent commits
│   └── checks/
│       └── run-checks.ps1                          — quality gate (created end of Increment 1)
├── AGENTS.md                                       — this file
└── README.md                                       — contributor setup
```

---

## Build and test

```powershell
# Build all .NET projects
dotnet build

# Run all xUnit tests
dotnet test

# Start both API + frontend in separate windows (http://localhost:5000 / :5173)
pwsh scripts/start-app.ps1

# Stop both components
pwsh scripts/stop-app.ps1

# Start the API only (http://localhost:5000)
cd src/Api ; dotnet run

# Start the frontend dev server only (http://localhost:5173)
cd src/Web ; npm run dev

# Production frontend build
cd src/Web ; npm run build
```

---

## Increment status

| Increment | Description | Status |
|-----------|-------------|--------|
| 1a | Scaffold + one-way circular transfer | complete |
| 1b | Round trip | complete |
| 1c | Inclined / elliptical parking orbits | complete |
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

- **Commit and PR policy**: [`docs/commit-policy.md`](docs/commit-policy.md)
- **Contributor setup**: [`README.md`](README.md)
- **Planning and increments**: [`local-docs/planning.md`](local-docs/planning.md) (gitignored)
- **Branch naming**: Conventional Branch format — `<type>/<description>`
- **No direct push to `main`**: all changes via pull request, owner approval required
