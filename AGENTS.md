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
│   │   ├── pre-commit                              — runs changed-code quality gate before every commit
│   │   ├── commit-msg                              — enforces Conventional Commit format; agent trailer policy
│   │   ├── pre-push                                — blocks direct push to main; enforces Conventional Branch names
│   │   └── prepare-commit-msg                      — auto-appends Agent trailer for salakist-agent commits
│   ├── checks/
│   │   ├── run-checks.ps1                          — changed-code quality gate (pre-commit + PR)
│   │   └── run-full-checks.ps1                     — full-base quality gate (manual / workflow_dispatch)
│   ├── lib/
│   │   ├── common.ps1                              — shared: headers, .NET diagnostic parsing, format helpers
│   │   ├── change-detection.ps1                    — staged/PR/working-tree scope detection; file classifiers
│   │   └── coverage.ps1                            — Core coverage collection and threshold reporting
│   └── setup/
│       ├── setup-hooks.ps1                         — installs all hooks + commitlint deps (run once per clone)
│       └── new-branch.ps1                          — creates a Conventional Branch-compliant branch
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

See [`docs/planning/roadmap.md`](docs/planning/roadmap.md) for the full increment status table and one-line descriptions.

Current state: increments 1a, 1b, 1c complete. Increments 2–9 not started.

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
- **Planning and increments**: [`docs/planning/roadmap.md`](docs/planning/roadmap.md) (tracked); [`local-docs/planning.md`](local-docs/planning.md) (gitignored, richer actuals)
- **Design docs**: [`docs/AGENTS.md`](docs/AGENTS.md) — full file map of all tracked docs
- **Branch naming**: Conventional Branch format — `<type>/<description>`
- **No direct push to `main`**: all changes via pull request, owner approval required

## Documentation maintenance policy

When making code changes, update the corresponding `docs/` file **in the same commit**:

| Changed file(s) | Update this doc |
|-----------------|----------------|
| `LambertSolver.cs`, `KeplerSolver.cs` | [`docs/algorithms/lambert.md`](docs/algorithms/lambert.md) |
| `ManeuverComputer.cs`, `TransferComputer.cs`, `PlaneChangeComputer.cs` | [`docs/algorithms/delta-v.md`](docs/algorithms/delta-v.md) |
| `PorkchopComputer.cs`, `TransferType.cs` | [`docs/algorithms/porkchop.md`](docs/algorithms/porkchop.md) |
| `CelestialBody.cs`, `OrbitalElements.cs`, `BodyDatabase.cs` | [`docs/body-data-schema.md`](docs/body-data-schema.md) |
| Commit/PR workflow changes | [`docs/commit-policy.md`](docs/commit-policy.md) |
| Increment scoped or delivered | [`docs/planning/roadmap.md`](docs/planning/roadmap.md) + relevant `increment-N.md` |
