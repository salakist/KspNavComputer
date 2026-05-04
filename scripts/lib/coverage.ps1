# scripts/lib/coverage.ps1
#
# Core coverage collection and threshold reporting.
# Uses the coverlet DataCollector already referenced in the test project.
# Produces a Cobertura XML report and evaluates line coverage per file.

function Invoke-CoreCoverage([string]$ArtifactsRoot) {
    $coverageDir = Join-Path $ArtifactsRoot "coverage"
    if (Test-Path $coverageDir) { Remove-Item $coverageDir -Recurse -Force }
    New-Item -ItemType Directory -Path $coverageDir -Force | Out-Null

    $dotnetArgs = @(
        'test',
        'tests/Core/KspNavComputer.Core.Tests.csproj',
        '--collect', 'XPlat Code Coverage',
        '--results-directory', $coverageDir,
        '--nologo',
        '/nr:false',
        '--',
        'DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura'
    )

    & dotnet @dotnetArgs | Out-Host
    $exitCode = $LASTEXITCODE

    $coverageFile = Get-ChildItem -Path $coverageDir -Recurse -Filter "*.cobertura.xml" |
        Select-Object -First 1

    return [pscustomobject]@{
        ExitCode     = $exitCode
        CoverageFile = if ($coverageFile) { $coverageFile.FullName } else { $null }
    }
}

function Get-CoreCoverageData(
    [string]$CoverageXmlPath,
    [string]$RepoRoot,
    [string[]]$FilterFiles = @()) {

    [xml]$xml = Get-Content $CoverageXmlPath
    $repoNormalized = $RepoRoot.Replace('\', '/').TrimEnd('/')

    $totalHits  = 0
    $totalLines = 0
    $byFile     = @{}

    foreach ($package in $xml.coverage.packages.package) {
        foreach ($class in $package.classes.class) {
            # Normalize to forward-slash relative path
            $classFile = $class.filename.Replace('\', '/')
            if ($classFile.StartsWith($repoNormalized, [System.StringComparison]::OrdinalIgnoreCase)) {
                $classFile = $classFile.Substring($repoNormalized.Length).TrimStart('/')
            }

            # Apply filter when provided
            if ($FilterFiles.Count -gt 0) {
                $found = $false
                foreach ($f in $FilterFiles) {
                    if ($classFile -ieq $f.Replace('\', '/')) { $found = $true; break }
                }
                if (-not $found) { continue }
            }

            $classHits  = 0
            $classLines = 0
            foreach ($line in $class.lines.line) {
                $classLines++
                if ([int]$line.hits -gt 0) { $classHits++ }
            }

            if ($classLines -gt 0) {
                $totalHits  += $classHits
                $totalLines += $classLines

                if ($byFile.ContainsKey($classFile)) {
                    # Partial class — accumulate
                    $existing = $byFile[$classFile]
                    $byFile[$classFile] = [pscustomobject]@{
                        Hits  = $existing.Hits + $classHits
                        Lines = $existing.Lines + $classLines
                    }
                }
                else {
                    $byFile[$classFile] = [pscustomobject]@{ Hits = $classHits; Lines = $classLines }
                }
            }
        }
    }

    $overall = if ($totalLines -gt 0) { [Math]::Round(($totalHits / $totalLines) * 100, 1) } else { 100.0 }

    return [pscustomobject]@{
        OverallPercent = $overall
        TotalLines     = $totalLines
        TotalHits      = $totalHits
        ByFile         = $byFile
    }
}

function Show-CoverageReport(
    [pscustomobject]$CoverageData,
    [double]$Threshold,
    [bool]$ShowFiles = $true) {

    $passed = $CoverageData.OverallPercent -ge $Threshold
    $color  = if ($passed) { "Green" } else { "Red" }
    $status = if ($passed) { "PASS" } else { "FAIL" }

    Write-Host "[$status] Coverage: $($CoverageData.OverallPercent)% (threshold: $Threshold%, $($CoverageData.TotalHits)/$($CoverageData.TotalLines) lines)" -ForegroundColor $color

    if ($ShowFiles -and $CoverageData.ByFile.Count -gt 0) {
        foreach ($entry in $CoverageData.ByFile.GetEnumerator() | Sort-Object Name) {
            $v       = $entry.Value
            $filePct = [Math]::Round(($v.Hits / $v.Lines) * 100, 1)
            $fc      = if ($filePct -ge $Threshold) { "DarkGray" } else { "Yellow" }
            Write-Host "  $($entry.Name): $filePct% ($($v.Hits)/$($v.Lines) lines)" -ForegroundColor $fc
        }
    }

    return $passed
}
