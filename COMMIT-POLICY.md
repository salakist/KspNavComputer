# Agent Commit Policy

This file is the authoritative policy for **agent or automation-authored commits and pull requests**
in this repository.

For repository architecture and routing, see root `AGENTS.md` (created in Increment 1).
For contributor setup — git identity configuration, GitHub App installation, hook installation —
see `README.md`.

---

## Git identity

Commits authored by an agent must use the repo-local agent identity:

- **Name**: `salakist-agent`
- **Email**: `salakist-agent@local`

Set with:

```powershell
git config --local user.name  "salakist-agent"
git config --local user.email "salakist-agent@local"
```

Human-authored commits must not use this identity.

---

## Branch naming

Follow the [Conventional Branch](https://conventional-branch.github.io/) format:

```
<type>/<description>
```

- `<type>` must be one of: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`,
  `build`, `ci`, `chore`, `ops`
- `<description>` is kebab-case, concise, present-tense
- Examples: `feat/1a-scaffold-and-kepler-solver`, `fix/lambert-elliptical-edge-case`,
  `docs/update-agents-increment-1a`

One branch per sub-increment (i.e. per testable deliverable). Do not bundle multiple
sub-increments on one branch.

**No direct push to `main`.** All changes go through a pull request.

---

## Commit message format

Use [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<optional scope>): <description>

<optional body>

Agent: GitHub Copilot
```

Rules:
- Summary line: `type(scope): description` — imperative mood, no trailing period
- Supported types: same set as branch types above
- Each non-empty body or trailer line must be **≤ 100 characters**
- The `Agent: GitHub Copilot` trailer is required on all agent-authored commits
- Human-authored commits must not include an `Agent:` trailer

---

## Pull request workflow

PRs must be opened using the `salakist-agent` GitHub App identity, not the contributor's
personal GitHub account. This enables the contributor to act as the required approver.

**Token generation:**

```powershell
$env:GH_TOKEN = (pwsh ./scripts/get-agent-token.ps1).Trim()
```

The token is short-lived (≤ 60 minutes). Regenerate if a subsequent `gh` call fails with
an auth error.

**Opening a PR:**

```powershell
$env:GH_TOKEN = (pwsh ./scripts/get-agent-token.ps1).Trim()
gh pr create `
  --title "<Conventional Commit summary>" `
  --body  "<body>" `
  --base  main `
  --head  <branch>
```

PR title must follow the same Conventional Commit format as the commit summary line.

**Adding changes to an open PR:**

Push new commits — do not amend or force-push onto a branch with an open PR.
Each follow-up change (fixes, review feedback, additions) must be a fresh commit on the
same branch. Commits will be squashed on merge, so the on-branch history does not need to
be clean.

**Merge policy:**
- PRs require **1 approving review** (the repository owner) before merge
- An agent may open and push to a PR without approval
- An agent must not merge a PR — merge is always a human action

---

## Pre-commit workflow

### Step 1 — Quality gate

Run the quality gate script and retain the log path:

```powershell
./scripts/checks/run-checks.ps1
```

> **Note**: This script does not exist yet. It will be created at the end of Increment 1 and
> will run `dotnet build` + `dotnet test` at minimum. Until then, Step 1 must be marked
> `SKIPPED – quality gate not yet established` for all commits that include no production
> code (scaffolding, docs, config only). Any commit touching production code after Increment 1
> is complete must not skip Step 1.

Non-blocking cosmetic diagnostics reported by the gate still require handling: fix them in
touched files or state a deferral reason in chat. Do not suppress with `#pragma warning disable`,
`NoWarn`, or similar.

Never bypass hooks with `--no-verify`.

### Step 2 — Documentation alignment

Review the staged diff and update any of the following documents if their described
responsibilities, key files, or dependencies changed:

- **`AGENTS.md`** files — architecture, file descriptions, conventions for the affected area
- **`README.md`** — credits/inspirations table when a new external reference is introduced;
  developer setup section when tooling or scripts change

---

## Checklist to state before `git commit`

```
Step 1: DONE | SKIPPED – <reason>  [log: <path if applicable>]
Step 2: DONE | SKIPPED – No documentation changes required – <reason>
Message preflight: DONE
```

Rules:
- Print the checklist in chat immediately before running `git commit`
- `Message preflight: DONE` means the summary is a valid Conventional Commit, includes the
  `Agent: GitHub Copilot` trailer, and every non-empty body/trailer line is ≤ 100 chars
- If checklist state changes after printing it, print an updated version before committing
- Do not commit if the checklist is missing or any step is neither `DONE` nor validly `SKIPPED`

---

## Skip conditions

1. Any `SKIPPED` step requires a one-line reason.
2. **Step 1** may be `SKIPPED` only when:
   - The quality gate script does not yet exist (pre-Increment 1 completion), **and**
   - The commit contains no production code (no files under `src/` or `tests/`)
   - Once the gate script exists, Step 1 may never be skipped for commits touching
     production code.
3. **Step 2** may be `SKIPPED` only as `No documentation changes required` with a short
   reason tied to the staged changes.
4. A documentation-only or config-only commit may mark both steps `SKIPPED` only when all
   Step 1 skip conditions are satisfied and no further docs updates are needed.
