# scripts/lib/common.ps1
#
# Shared helper functions: console output, .NET diagnostic parsing,
# file-scoped filtering, dotnet format cosmetic diagnostics, and report display.

function Write-Header([string]$Text) {
    Write-Host ""
    Write-Host "=======================================================" -ForegroundColor Cyan
    Write-Host "  $Text" -ForegroundColor Cyan
    Write-Host "=======================================================" -ForegroundColor Cyan
}

function Get-DotNetDiagnosticLines(
    [string[]]$OutputLines,
    [string[]]$AllowedSeverities = @("warning", "error", "info", "suggestion")) {
    if (-not $OutputLines -or $OutputLines.Count -eq 0) {
        return @()
    }

    $severityPattern = ($AllowedSeverities | ForEach-Object { [Regex]::Escape($_) }) -join '|'

    return @($OutputLines |
        ForEach-Object { $_.ToString() } |
        Where-Object { $_ -match ": ($severityPattern) [A-Za-z]{2,}\d+:" } |
        Select-Object -Unique)
}

function Get-FileScopedDiagnostics(
    [string[]]$DiagnosticLines,
    [string[]]$Files,
    [string]$RepoRoot) {
    if (-not $DiagnosticLines -or $DiagnosticLines.Count -eq 0 -or -not $Files -or $Files.Count -eq 0) {
        return @()
    }

    $matches = New-Object 'System.Collections.Generic.List[string]'

    foreach ($line in $DiagnosticLines) {
        $lowerLine = $line.ToLowerInvariant()
        foreach ($file in $Files) {
            $relativeNeedle = $file.ToLowerInvariant().Replace('/', [System.IO.Path]::DirectorySeparatorChar.ToString())
            $absoluteNeedle = (Join-Path $RepoRoot $file.Replace('/', [System.IO.Path]::DirectorySeparatorChar.ToString())).ToLowerInvariant()
            if ($lowerLine.Contains($relativeNeedle) -or $lowerLine.Contains($absoluteNeedle)) {
                $matches.Add($line)
                break
            }
        }
    }

    return @($matches | Select-Object -Unique)
}

function Invoke-DotNetFormatDiagnostics(
    [string]$RepoRoot,
    [string[]]$IncludeFiles = @()) {
    $allDiagnostics = New-Object 'System.Collections.Generic.List[string]'
    $technicalFailure = $false

    Push-Location $RepoRoot
    try {
        foreach ($subcommand in @("style", "analyzers")) {
            $formatArgs = @(
                "format",
                "KspNavComputer.slnx",
                $subcommand,
                "--verify-no-changes",
                "--severity", "info",
                "--no-restore"
            )

            if ($IncludeFiles -and $IncludeFiles.Count -gt 0) {
                $formatArgs += "--include"
                $formatArgs += $IncludeFiles
            }

            $formatOutput = @(& dotnet @formatArgs 2>&1)
            $formatLines  = @($formatOutput | ForEach-Object { $_.ToString() })
            $diagnostics  = Get-DotNetDiagnosticLines -OutputLines $formatLines -AllowedSeverities @("info", "suggestion")

            foreach ($d in $diagnostics) { $allDiagnostics.Add($d) }

            if ($LASTEXITCODE -ne 0 -and $diagnostics.Count -eq 0) {
                $technicalFailure = $true
            }
        }
    }
    finally {
        Pop-Location
    }

    return [pscustomobject]@{
        Diagnostics      = @($allDiagnostics | Select-Object -Unique)
        TechnicalFailure = $technicalFailure
    }
}

function Show-DotNetDiagnosticReport(
    [string]$Title,
    [string[]]$Diagnostics,
    [string]$EmptyMessage,
    [string]$FollowUpNote = "",
    [ConsoleColor]$Color = [ConsoleColor]::Yellow) {
    if (-not $Diagnostics -or $Diagnostics.Count -eq 0) {
        if ($EmptyMessage) {
            Write-Host "[PASS] $EmptyMessage" -ForegroundColor Green
        }
        return
    }

    Write-Host "[WARN] $Title" -ForegroundColor $Color

    $ruleCounts = @{}
    foreach ($line in $Diagnostics) {
        if ($line -match ': (warning|error|info|suggestion) ([A-Za-z]{2,}\d+):') {
            $key = "$($Matches[2]) [$($Matches[1])]"
            if ($ruleCounts.ContainsKey($key)) { $ruleCounts[$key]++ }
            else { $ruleCounts[$key] = 1 }
        }
    }

    if ($ruleCounts.Count -gt 0) {
        Write-Host "Rule summary:" -ForegroundColor $Color
        foreach ($entry in $ruleCounts.GetEnumerator() | Sort-Object Name) {
            Write-Host "  $($entry.Name): $($entry.Value)" -ForegroundColor $Color
        }
    }

    Write-Host "Details:" -ForegroundColor $Color
    $Diagnostics | ForEach-Object { Write-Host "  $_" -ForegroundColor $Color }

    if ($FollowUpNote) {
        Write-Host $FollowUpNote -ForegroundColor $Color
    }
}
