# scripts/checks/run-checks.ps1
#
# Changed-code quality gate — runs before every commit (via pre-commit hook) and
# on every pull request (via the quality-pr GitHub Actions workflow).
#
# Gates (in order):
#   [1/4] C# build + analyzer diagnostics on changed source files (blocking)
#   [2/4] C# IDE/Roslyn cosmetic diagnostics on changed files     (non-blocking warning)
#   [3/4] Web ESLint on changed src/Web/src files                 (blocking)
#   [4/4] Core coverage on changed production Core files          (blocking, threshold 80%)
#
# Change scope detection (automatic):
#   - CI (PR):    KSPNAV_PR_BASE_SHA + KSPNAV_PR_HEAD_SHA env vars set by workflow
#   - Pre-commit: staged files (git diff --cached)
#   - Local run:  working-tree changes, then upstream diff, then last commit

param(
    [double]$CoverageThreshold = 80
)

$ErrorActionPreference = "Continue"

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $RepoRoot

$LibRoot = Join-Path $RepoRoot "scripts/lib"
. (Join-Path $LibRoot "common.ps1")
. (Join-Path $LibRoot "change-detection.ps1")
. (Join-Path $LibRoot "coverage.ps1")

$Failed         = $false
$ArtifactsRoot  = Join-Path $RepoRoot "artifacts"
$DotNetArtRoot  = Join-Path $ArtifactsRoot "tmp-dotnet/changed/$(Get-Date -Format 'yyyyMMdd-HHmmss')"

# ── Detect changed files ────────────────────────────────────────────────────

$changedContext = Get-ChangedContext
$changedFiles   = @($changedContext.Files | ForEach-Object { $_.Replace('\', '/') })
$changedFiles   = Get-UniqueLines $changedFiles

Write-Host "[mandatory] changed-code gate" -ForegroundColor Yellow
Write-Header "Changed-code analysis target"
Write-Host "Scope: $($changedContext.Source)" -ForegroundColor Yellow

if ($changedFiles.Count -eq 0) {
    Write-Host "No changed files detected. Skipping changed-code gates." -ForegroundColor Yellow
    exit 0
}

($changedFiles | Select-Object -First 20) | ForEach-Object { Write-Host "  $_" }
if ($changedFiles.Count -gt 20) {
    Write-Host "  ... and $($changedFiles.Count - 20) more file(s)." -ForegroundColor Yellow
}

New-Item -ItemType Directory -Path $DotNetArtRoot -Force | Out-Null

$changedSourceCSharp = @($changedFiles | Where-Object { Is-SourceCSharpFile $_ })
$changedCoreFiles    = @($changedFiles | Where-Object { Is-CoreProductionFile $_ })
$changedWebFiles     = @($changedFiles | Where-Object { Is-WebLintFile $_ })

# ── [1/4] C# build + analyzer gate ─────────────────────────────────────────

Write-Header "[1/4] C# changed-file analyzer gate"

if ($changedSourceCSharp.Count -eq 0) {
    Write-Host "[SKIP] No changed C# source files." -ForegroundColor Yellow
}
else {
    $buildOutput   = @(& dotnet build KspNavComputer.slnx -t:Rebuild --artifacts-path $DotNetArtRoot /nr:false 2>&1)
    $buildExitCode = $LASTEXITCODE
    $buildLines    = @($buildOutput | ForEach-Object { $_.ToString() })

    $allBlockingLines     = Get-DotNetDiagnosticLines -OutputLines $buildLines -AllowedSeverities @("warning", "error")
    $changedBlockingLines = Get-FileScopedDiagnostics -DiagnosticLines $allBlockingLines -Files $changedSourceCSharp -RepoRoot $RepoRoot

    if ($changedBlockingLines.Count -gt 0) {
        $changedBlockingLines | ForEach-Object { Write-Host $_ -ForegroundColor Red }
        Write-Host ""
        Write-Host "[FAIL] Changed C# files have blocking analyzer or compiler violations." -ForegroundColor Red
        $Failed = $true
    }
    elseif ($buildExitCode -ne 0) {
        Write-Host "[FAIL] Solution build failed. Commit is blocked until the repository builds cleanly." -ForegroundColor Red
        $Failed = $true
    }
    else {
        Write-Host "[PASS] No blocking analyzer/compiler violations in changed C# files." -ForegroundColor Green
    }

    # ── [2/4] Cosmetic diagnostics (non-blocking) ──────────────────────────

    Write-Header "[2/4] C# changed-file cosmetic diagnostics (non-blocking)"

    if ($buildExitCode -eq 0) {
        $cosmeticResult = Invoke-DotNetFormatDiagnostics -RepoRoot $RepoRoot -IncludeFiles $changedSourceCSharp

        if ($cosmeticResult.TechnicalFailure) {
            Write-Host "[WARN] dotnet format could not evaluate cosmetic diagnostics." -ForegroundColor Yellow
        }
        else {
            $changedCosmetic = Get-FileScopedDiagnostics -DiagnosticLines $cosmeticResult.Diagnostics -Files $changedSourceCSharp -RepoRoot $RepoRoot
            Show-DotNetDiagnosticReport `
                -Title       "Changed C# files have cosmetic diagnostics." `
                -Diagnostics $changedCosmetic `
                -EmptyMessage "No cosmetic diagnostics in changed C# files." `
                -FollowUpNote "Fix cosmetic diagnostics on touched files or note a short deferral reason. Do not suppress."
        }
    }
    else {
        Write-Host "[SKIP] Cosmetic diagnostics skipped — build did not complete." -ForegroundColor Yellow
    }
}

# ── [3/4] Web ESLint gate ───────────────────────────────────────────────────

Write-Header "[3/4] Web ESLint changed-file gate"

if ($changedWebFiles.Count -eq 0) {
    Write-Host "[SKIP] No changed Web source files." -ForegroundColor Yellow
}
else {
    # Paths relative to src/Web
    $eslintTargets = @($changedWebFiles | ForEach-Object { $_ -replace '^src/Web/', '' })
    Push-Location (Join-Path $RepoRoot "src/Web")
    try {
        & npx eslint --max-warnings=0 $eslintTargets
        if ($LASTEXITCODE -ne 0) {
            Write-Host "[FAIL] ESLint violations in changed Web files." -ForegroundColor Red
            $Failed = $true
        }
        else {
            Write-Host "[PASS] No ESLint violations in changed Web files." -ForegroundColor Green
        }
    }
    finally {
        Pop-Location
    }
}

# ── [4/4] Core coverage gate ────────────────────────────────────────────────

Write-Header "[4/4] Core changed-file coverage gate (threshold: $CoverageThreshold%)"

if ($changedCoreFiles.Count -eq 0) {
    Write-Host "[SKIP] No changed Core production files." -ForegroundColor Yellow
}
else {
    $coverageResult = Invoke-CoreCoverage -ArtifactsRoot $ArtifactsRoot

    if ($coverageResult.ExitCode -ne 0) {
        Write-Host "[FAIL] Core tests failed before coverage evaluation." -ForegroundColor Red
        $Failed = $true
    }
    elseif (-not $coverageResult.CoverageFile) {
        Write-Host "[FAIL] Coverage report not found after test run." -ForegroundColor Red
        $Failed = $true
    }
    else {
        $coverageData = Get-CoreCoverageData `
            -CoverageXmlPath $coverageResult.CoverageFile `
            -RepoRoot        $RepoRoot `
            -FilterFiles     $changedCoreFiles

        if ($coverageData.TotalLines -eq 0) {
            Write-Host "[SKIP] No coverable lines found in changed Core files." -ForegroundColor Yellow
        }
        else {
            $passed = Show-CoverageReport -CoverageData $coverageData -Threshold $CoverageThreshold -ShowFiles $true
            if (-not $passed) {
                Write-Host "[FAIL] Changed Core files are below $CoverageThreshold% line coverage." -ForegroundColor Red
                $Failed = $true
            }
        }
    }
}

# ── Summary ─────────────────────────────────────────────────────────────────

Set-Location $RepoRoot
Write-Host ""

if ($Failed) {
    Write-Host "=======================================================" -ForegroundColor Red
    Write-Host "  CHANGED-CODE CHECKS FAILED. Commit is blocked."        -ForegroundColor Red
    Write-Host "  Resolve the issues above, then re-run:"               -ForegroundColor Red
    Write-Host "    .\scripts\checks\run-checks.ps1"                    -ForegroundColor Red
    Write-Host "=======================================================" -ForegroundColor Red
    exit 1
}

Write-Host "=======================================================" -ForegroundColor Green
Write-Host "  Changed-code quality gates passed."                   -ForegroundColor Green
Write-Host "=======================================================" -ForegroundColor Green
exit 0
