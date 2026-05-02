# KSP Navigation Computer

A desktop application for planning missions in Kerbal Space Program (KSP 1.12.x).
Handles transfer windows, delta-v budgets, burn details, and copy-paste into the
Precise Maneuver mod — including bodies added by Outer Planets Mod and Minor Planets
Expansion.

---

## Developer setup

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [PowerShell 7+](https://github.com/PowerShell/PowerShell/releases) (`pwsh`)
- [GitHub CLI](https://cli.github.com/) (`gh`)

### Local git identity (human contributors)

Use your personal git identity. Do not use the agent identity (`salakist-agent`).

### Agent git identity

For agent-authored commits, configure the repo-local agent identity:

```powershell
git config --local user.name  "salakist-agent"
git config --local user.email "salakist-agent@local"
```

Reset to your personal identity when done:

```powershell
git config --local --unset user.name
git config --local --unset user.email
```

### salakist-agent GitHub App — token generation

Agent-authored pull requests are opened using the `salakist-agent` GitHub App so that the
repository owner can approve them. The app must be installed on this repository at
[github.com/apps/salakist-agent](https://github.com/apps/salakist-agent).

Credentials required locally (not committed — covered by `.gitignore`):

| File | Contents |
|------|----------|
| `.env` | `GITHUB_APP_ID=<id>` |
| `salakist-agent.*.pem` | RSA private key downloaded from the GitHub App settings |

Generate a token:

```powershell
$env:GH_TOKEN = (pwsh ./scripts/get-agent-token.ps1).Trim()
```

Tokens expire after 60 minutes. Regenerate if a `gh` call returns an auth error.

### Commit and PR policy

See [`COMMIT-POLICY.md`](COMMIT-POLICY.md).

---

## Credits and inspirations

| Reference | Used for |
|-----------|----------|
| [Transfer Window Planner](https://github.com/TriggerAu/TransferWindowPlanner) (TriggerAu) | Inspiration for porkchop-plot transfer window visualisation (Increment 3) |
| [Launch Window Planner](https://alexmoon.github.io/ksp/) (alexmoon) | Inspiration for the overall mission-planning UX and delta-v presentation |
| Bate, Mueller & White — *[Fundamentals of Astrodynamics](https://www.amazon.com/dp/0486600610)* (1971), §5.3 | Universal-variable Lambert solver (Stumpff C/S functions, bisection on ψ) |
| Izzo — *[Revisiting Lambert's Problem](https://link.springer.com/article/10.1007/s10569-015-9617-9)* (2015) | Reference for multi-revolution Lambert extensions |
| [KSP Wiki — Celestial bodies](https://wiki.kerbalspaceprogram.com/wiki/Category:Celestial_bodies) | Gravitational parameters, radii, SOI radii, and orbital elements for all stock bodies |
| [Precise Maneuver mod](https://github.com/hxtk/KSP-Precise-Maneuver) | Target format for maneuver-node copy-paste export (Increment 2) |
| [Outer Planets Mod](https://github.com/Kopernicus/Outer-Planets-Mod) | Body data for OPM bodies (Increment 4) |
| [Minor Planets Expansion](https://github.com/ProximaCentauri-star/MinorPlanetsExpansion) | Body data for MPE bodies (Increment 4) |
