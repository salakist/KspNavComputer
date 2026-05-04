# scripts/setup/setup-hooks.ps1
#
# Installs the versioned git hooks from scripts/hooks/ into .git/hooks/.
# Run once after cloning (or after any hook file is updated):
#
#   pwsh scripts/setup/setup-hooks.ps1
#
# Also installs commitlint Node dependencies required by the commit-msg hook.

$RepoRoot    = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$GitHooksDir = Join-Path $RepoRoot ".git/hooks"
$HooksSource = Join-Path $RepoRoot "scripts/hooks"

if (-not (Test-Path (Join-Path $RepoRoot ".git"))) {
    Write-Error "ERROR: .git directory not found. Are you in the KspNavComputer repo root?"
    exit 1
}

# ── Install Node dependencies for commit-msg (commitlint) ──────────────────

Write-Host "Installing commitlint dependencies (scripts/package.json)..."
Push-Location (Join-Path $RepoRoot "scripts")
try {
    & npm install
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[WARN] npm install in scripts/ failed. The commit-msg hook will not enforce" -ForegroundColor Yellow
        Write-Host "       Conventional Commit format until dependencies are installed." -ForegroundColor Yellow
    }
    else {
        Write-Host "  [OK] commitlint dependencies installed" -ForegroundColor Green
    }
}
finally {
    Pop-Location
}

# ── Copy hooks ──────────────────────────────────────────────────────────────

$hooks = @("pre-commit", "commit-msg", "pre-push", "prepare-commit-msg")

Write-Host ""
Write-Host "Installing git hooks..."

foreach ($hook in $hooks) {
    $src  = Join-Path $HooksSource $hook
    $dest = Join-Path $GitHooksDir $hook

    if (-not (Test-Path $src)) {
        Write-Host "  [SKIP] $hook — source not found at $src" -ForegroundColor Yellow
        continue
    }

    Copy-Item -Path $src -Destination $dest -Force

    # Make executable on Linux/macOS
    if ($IsLinux -or $IsMacOS) {
        & chmod +x $dest
    }

    # Verify content matches
    $srcHash  = (Get-FileHash -Path $src  -Algorithm SHA256).Hash
    $destHash = (Get-FileHash -Path $dest -Algorithm SHA256).Hash

    if ($srcHash -eq $destHash) {
        Write-Host "  [OK] $hook installed and verified" -ForegroundColor Green
    }
    else {
        Write-Host "  [FAIL] $hook content mismatch after copy" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Hook installation complete." -ForegroundColor Green
Write-Host ""
Write-Host "Hooks now active:"
Write-Host "  pre-commit      — runs changed-code quality gate before every commit"
Write-Host "  commit-msg      — enforces Conventional Commit format and agent trailer policy"
Write-Host "  pre-push        — blocks direct push to main; enforces Conventional Branch names"
Write-Host "  prepare-commit-msg — auto-appends Agent trailer for salakist-agent commits"
Write-Host ""
Write-Host "To run changed-code checks manually:  .\scripts\checks\run-checks.ps1"
Write-Host "To run full-base checks manually:     .\scripts\checks\run-full-checks.ps1"
Write-Host "To create a correctly-named branch:   .\scripts\setup\new-branch.ps1 feat/my-feature"
