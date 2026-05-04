# scripts/lib/change-detection.ps1
#
# Changed-file detection helpers and file-type classifiers.
#
# Detection priority:
#   1. PR mode    — KSPNAV_PR_BASE_SHA env var is set (GitHub Actions)
#   2. Staged     — pre-commit (git diff --cached)
#   3. Working    — local manual run
#   4. Upstream   — commits since tracked upstream
#   5. Last commit — fallback when no upstream exists

function Get-UniqueLines([string[]]$Lines) {
    return @($Lines | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
}

function Get-ChangedContext {
    # PR mode: env vars injected by the GitHub Actions quality-pr workflow
    if (-not [string]::IsNullOrWhiteSpace($env:KSPNAV_PR_BASE_SHA)) {
        $head  = if (-not [string]::IsNullOrWhiteSpace($env:KSPNAV_PR_HEAD_SHA)) { $env:KSPNAV_PR_HEAD_SHA } else { "HEAD" }
        $range = "$($env:KSPNAV_PR_BASE_SHA)...$head"
        $files = Get-UniqueLines @(git diff --name-only --diff-filter=ACM $range)
        return @{ Source = "PR diff ($range)"; Files = $files }
    }

    # Pre-commit: staged changes
    $staged = Get-UniqueLines @(git diff --cached --name-only --diff-filter=ACM)
    if ($staged.Count -gt 0) {
        return @{ Source = "staged changes"; Files = $staged }
    }

    # Local manual run: working-tree changes + untracked files
    $workingTree = Get-UniqueLines @(git diff --name-only --diff-filter=ACM HEAD 2>$null)
    $untracked   = Get-UniqueLines @(git ls-files --others --exclude-standard)
    $workingFiles = Get-UniqueLines @($workingTree + $untracked)
    if ($workingFiles.Count -gt 0) {
        return @{ Source = "working tree changes"; Files = $workingFiles }
    }

    # Fallback: commits since tracked upstream
    $upstream = git rev-parse --abbrev-ref --symbolic-full-name "@{upstream}" 2>$null
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($upstream)) {
        $files = Get-UniqueLines @(git diff --name-only --diff-filter=ACMR "$upstream...HEAD")
        return @{ Source = "changes since $upstream"; Files = $files }
    }

    # Fallback: last commit
    git rev-parse --verify "HEAD~1" 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) {
        $files = Get-UniqueLines @(git diff --name-only --diff-filter=ACMR "HEAD~1..HEAD")
        return @{ Source = "last commit"; Files = $files }
    }

    return @{ Source = "tracked files"; Files = Get-UniqueLines @(git ls-files) }
}

# Any .cs file in the solution
function Is-CSharpFile([string]$Path) {
    return $Path -match '\.cs$'
}

# Production C# files under src/Core (used for coverage gating)
function Is-CoreProductionFile([string]$Path) {
    return $Path -match '^src[/\\]Core[/\\].*\.cs$'
}

# Any C# file under src/ (Core + Api — included in diagnostics gates, not coverage)
function Is-SourceCSharpFile([string]$Path) {
    return $Path -match '^src[/\\].*\.cs$'
}

# TypeScript/TSX files under src/Web/src (ESLint gate)
function Is-WebLintFile([string]$Path) {
    return $Path -match '^src[/\\]Web[/\\]src[/\\].*\.(ts|tsx)$'
}
