# scripts/setup/new-branch.ps1
#
# Creates and checks out a new branch, validating that the name follows
# Conventional Branch format before creation.
# Applies to all contributors (humans and agents).
#
# Usage:
#   pwsh scripts/setup/new-branch.ps1 feat/my-feature
#   pwsh scripts/setup/new-branch.ps1 fix/lambert-edge-case

param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$BranchName
)

$CONVENTIONAL_BRANCH_REGEX = '^(feat|fix|docs|style|refactor|perf|test|build|ci|chore|ops)/[a-z0-9][a-z0-9\-]*$'

$VALID_TYPES = @('feat', 'fix', 'docs', 'style', 'refactor', 'perf', 'test', 'build', 'ci', 'chore', 'ops')

if (-not ($BranchName -match $CONVENTIONAL_BRANCH_REGEX)) {
    Write-Host "ERROR: '$BranchName' is not a valid Conventional Branch name." -ForegroundColor Red
    Write-Host ""
    Write-Host "Expected format: <type>/<description>"
    Write-Host "  type        — one of: $($VALID_TYPES -join ', ')"
    Write-Host "  description — kebab-case (lowercase letters, digits, hyphens)"
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  feat/transfer-window-porkchop-plot"
    Write-Host "  fix/lambert-anti-podal-geometry"
    Write-Host "  docs/update-increment-2-plan"
    Write-Host "  ci/add-quality-gate-workflow"
    exit 1
}

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Push-Location $RepoRoot
try {
    # Ensure we're on a clean working tree (warn only)
    $status = & git status --porcelain 2>$null
    if ($status) {
        Write-Host "[WARN] Working tree is not clean. Stash or commit changes before switching branches." -ForegroundColor Yellow
    }

    & git checkout -b $BranchName
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: git checkout -b $BranchName failed." -ForegroundColor Red
        exit 1
    }

    Write-Host ""
    Write-Host "Branch '$BranchName' created and checked out." -ForegroundColor Green
}
finally {
    Pop-Location
}
