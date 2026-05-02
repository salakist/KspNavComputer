# KSP Navigation Computer

A desktop application for planning missions in Kerbal Space Program (KSP 1.12.x).
Handles transfer windows, delta-v budgets, burn details, and copy-paste into the
Precise Maneuver mod — including bodies added by Outer Planets Mod and Minor Planets
Expansion.

See [`local-docs/planning.md`](local-docs/planning.md) for the full feature roadmap
and increment plan.

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
