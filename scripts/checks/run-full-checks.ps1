# scripts/checks/run-full-checks.ps1
#
# Full-base quality gate — manual execution only (not required for merge).
# Run locally or via the quality-full-manual GitHub Actions workflow_dispatch.
#
# Gates (in order):
#   [1/4] C# build + analyzer diagnostics on full solution (blocking)
#   [2/4] C# IDE/Roslyn cosmetic diagnostics full solution  (non-blocking warning)
#   [3/4] Web ESLint full src/Web/src tree                  (blocking)
#   [4/4] Core coverage full production scope               (blocking, threshold 80%)

param(
    [double]$CoverageThreshold = 80
)

$ErrorActionPreference = "Continue"

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $RepoRoot

$LibRoot = Join-Path $RepoRoot "scripts/lib"
. (Join-Path $LibRoot "common.ps1")
. (Join-Path $LibRoot "coverage.ps1")

$Failed        = $false
$ArtifactsRoot = Join-Path $RepoRoot "artifacts"
$DotNetArtRoot = Join-Path $ArtifactsRoot "tmp-dotnet/full/$(Get-Date -Format 'yyyyMMdd-HHmmss')"

New-Item -ItemType Directory -Path $DotNetArtRoot -Force | Out-Null

Write-Host "[optional] full-base gate — run on demand for a full health check" -ForegroundColor Yellow

# ── [1/4] Full C# build + analyzer gate ────────────────────────────────────

Write-Header "[1/4] C# full-base build + analyzer gate"
Write-Host "Isolated .NET artifacts: $DotNetArtRoot" -ForegroundColor DarkGray

$buildOutput = @(& dotnet build KspNavComputer.slnx -t:Rebuild `
    --artifacts-path $DotNetArtRoot `
    /p:TreatWarningsAsErrors=true `
    /nr:false 2>&1)
$buildExitCode = $LASTEXITCODE
$buildLines    = @($buildOutput | ForEach-Object { $_.ToString() })
$blockingLines = Get-DotNetDiagnosticLines -OutputLines $buildLines -AllowedSeverities @("warning", "error")

if ($buildExitCode -ne 0) {
    if ($blockingLines.Count -gt 0) {
        $blockingLines | ForEach-Object { Write-Host $_ -ForegroundColor Red }
        Write-Host ""
    }
    Write-Host "[FAIL] Full-base C# build has blocking violations." -ForegroundColor Red
    $Failed = $true
}
else {
    Write-Host "[PASS] Full-base C# build passed." -ForegroundColor Green
}

# ── [2/4] Full cosmetic diagnostics (non-blocking) ─────────────────────────

Write-Header "[2/4] C# full-base cosmetic diagnostics (non-blocking)"

if ($buildExitCode -eq 0) {
    $cosmeticResult = Invoke-DotNetFormatDiagnostics -RepoRoot $RepoRoot

    if ($cosmeticResult.TechnicalFailure) {
        Write-Host "[WARN] dotnet format could not evaluate cosmetic diagnostics." -ForegroundColor Yellow
    }
    else {
        Show-DotNetDiagnosticReport `
            -Title        "Full-base cosmetic diagnostics are present (non-blocking)." `
            -Diagnostics  $cosmeticResult.Diagnostics `
            -EmptyMessage "No cosmetic diagnostics in the full solution." `
            -FollowUpNote "Fix cosmetic diagnostics on touched files or note a short deferral reason. Do not suppress."
    }
}
else {
    Write-Host "[SKIP] Cosmetic diagnostics skipped — build did not complete." -ForegroundColor Yellow
}

# ── [3/4] Full Web ESLint gate ──────────────────────────────────────────────

Write-Header "[3/4] Web full-base ESLint gate"

Push-Location (Join-Path $RepoRoot "src/Web")
try {
    & npx eslint src --ext .ts,.tsx --max-warnings=0
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[FAIL] Full-base Web ESLint found violations." -ForegroundColor Red
        $Failed = $true
    }
    else {
        Write-Host "[PASS] Full-base Web ESLint passed." -ForegroundColor Green
    }
}
finally {
    Pop-Location
}

# ── [4/4] Full Core coverage gate ───────────────────────────────────────────

Write-Header "[4/4] Core full-base coverage gate (threshold: $CoverageThreshold%)"

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
        -RepoRoot        $RepoRoot

    $passed = Show-CoverageReport -CoverageData $coverageData -Threshold $CoverageThreshold -ShowFiles $false

    if (-not $passed) {
        Write-Host "[FAIL] Full Core coverage is below $CoverageThreshold%." -ForegroundColor Red
        $Failed = $true
    }
}

# ── Summary ──────────────────────────────────────────────────────────────────

Set-Location $RepoRoot
Write-Host ""

if ($Failed) {
    Write-Host "=======================================================" -ForegroundColor Red
    Write-Host "  FULL-BASE CHECKS FAILED."                             -ForegroundColor Red
    Write-Host "=======================================================" -ForegroundColor Red
    exit 1
}

Write-Host "=======================================================" -ForegroundColor Green
Write-Host "  Full-base quality gates passed."                      -ForegroundColor Green
Write-Host "=======================================================" -ForegroundColor Green
exit 0
