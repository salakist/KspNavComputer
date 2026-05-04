[CmdletBinding()]
param(
    [int]$ApiPort        = 5000,
    [int]$FrontendPort   = 5173,
    [switch]$RestartRunning,
    [switch]$NoPrompt,
    [switch]$OpenBrowser
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$RepoRoot         = Split-Path -Parent $PSScriptRoot
$ApiProjectPath   = Join-Path $RepoRoot 'src\Api\KspNavComputer.Api.csproj'
$FrontendRoot     = Join-Path $RepoRoot 'src\Web'

# Prefer pwsh (PS 7+); fall back to powershell.exe
$_pwshCmd = Get-Command pwsh -ErrorAction SilentlyContinue
$PwshExe = if ($_pwshCmd) { $_pwshCmd.Source } else { 'powershell.exe' }

function Write-Header([string]$Text) {
    Write-Host ''
    Write-Host '=======================================================' -ForegroundColor Cyan
    Write-Host "  $Text" -ForegroundColor Cyan
    Write-Host '=======================================================' -ForegroundColor Cyan
}

function Read-YesNo {
    param([string]$Prompt)
    while ($true) {
        $response = (Read-Host $Prompt).Trim()
        if ([string]::IsNullOrWhiteSpace($response)) { return $false }
        switch ($response.ToUpperInvariant()) {
            'Y'   { return $true  }
            'YES' { return $true  }
            'N'   { return $false }
            'NO'  { return $false }
            default { Write-Host '[warn] Please answer Y or N.' -ForegroundColor Yellow }
        }
    }
}

function Get-ListeningSnapshot {
    param([int[]]$Ports)

    $connections = @()
    foreach ($port in ($Ports | Sort-Object -Unique)) {
        try { $connections += @(Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction Stop) }
        catch { }
    }

    $pids = @($connections | Select-Object -ExpandProperty OwningProcess -Unique)
    $processNames = foreach ($processId in $pids) {
        try { (Get-Process -Id $processId -ErrorAction Stop).ProcessName }
        catch { "PID $processId" }
    }

    [pscustomobject]@{
        IsRunning    = $pids.Count -gt 0
        Ports        = @($connections | Select-Object -ExpandProperty LocalPort -Unique | Sort-Object)
        Pids         = @($pids | Sort-Object)
        ProcessNames = @($processNames | Sort-Object -Unique)
    }
}

function Wait-ForPortsState {
    param([int[]]$Ports, [bool]$ShouldBeListening, [int]$TimeoutSeconds = 20)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        $snapshot = Get-ListeningSnapshot -Ports $Ports
        if ($snapshot.IsRunning -eq $ShouldBeListening) { return $snapshot }
        Start-Sleep -Seconds 1
    } while ((Get-Date) -lt $deadline)

    return (Get-ListeningSnapshot -Ports $Ports)
}

function Stop-ListeningProcesses {
    param([string]$ComponentName, [int[]]$Ports)

    $initial = Get-ListeningSnapshot -Ports $Ports
    if (-not $initial.IsRunning) {
        return [pscustomobject]@{ Success = $true; Failed = @() }
    }

    $failed = @()
    foreach ($processId in $initial.Pids) {
        try { Stop-Process -Id $processId -ErrorAction Stop }
        catch { $failed += "$ComponentName PID $($processId): $($_.Exception.Message)" }
    }

    $after = Wait-ForPortsState -Ports $Ports -ShouldBeListening $false -TimeoutSeconds 10
    if ($after.IsRunning) {
        foreach ($processId in $after.Pids) {
            try { Stop-Process -Id $processId -Force -ErrorAction Stop }
            catch { $failed += "$ComponentName PID $($processId) (force): $($_.Exception.Message)" }
        }
        $after = Wait-ForPortsState -Ports $Ports -ShouldBeListening $false -TimeoutSeconds 5
        if ($after.IsRunning) {
            $failed += "$ComponentName still listening on port(s): $($after.Ports -join ', ')"
        }
    }

    [pscustomobject]@{ Success = $failed.Count -eq 0; Failed = $failed }
}

function Start-Component {
    param([pscustomobject]$Component)

    if (-not (Get-Command $Component.CommandName -ErrorAction SilentlyContinue)) {
        return [pscustomobject]@{
            Success = $false
            Message = "Required command '$($Component.CommandName)' was not found in PATH."
        }
    }

    try {
        $escapedDir    = $Component.WorkingDirectory.Replace("'", "''")
        $commandText   = "Set-Location '$escapedDir'; $($Component.LaunchCommand)"
        $encodedCommand = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($commandText))

        Start-Process -FilePath $PwshExe `
            -WorkingDirectory $Component.WorkingDirectory `
            -ArgumentList @('-NoLogo', '-ExecutionPolicy', 'Bypass', '-EncodedCommand', $encodedCommand) `
            -PassThru `
            -ErrorAction Stop | Out-Null

        $after = Wait-ForPortsState -Ports $Component.Ports -ShouldBeListening $true -TimeoutSeconds 20
        if ($after.IsRunning) {
            return [pscustomobject]@{
                Success = $true
                Message = "$($Component.Name) started - listening on port(s) $($after.Ports -join ', '). -> $($Component.Url)"
            }
        }

        return [pscustomobject]@{
            Success = $false
            Message = "$($Component.Name) launch window opened but no listener detected on port(s) $($Component.Ports -join ', ') within 20 s."
        }
    }
    catch {
        return [pscustomobject]@{
            Success = $false
            Message = "Failed to start $($Component.Name): $($_.Exception.Message)"
        }
    }
}

# ---------------------------------------------------------------------------

Write-Header 'Start KSP Navigation Computer'

$components = @(
    [pscustomobject]@{
        Name             = 'API'
        Ports            = @($ApiPort)
        Url              = "http://localhost:$ApiPort"
        WorkingDirectory = $RepoRoot
        CommandName      = 'dotnet'
        LaunchCommand    = "dotnet run --project '$($ApiProjectPath.Replace("'", "''"))' --urls 'http://localhost:$ApiPort'"
    },
    [pscustomobject]@{
        Name             = 'Frontend'
        Ports            = @($FrontendPort)
        Url              = "http://localhost:$FrontendPort"
        WorkingDirectory = $FrontendRoot
        CommandName      = 'npm'
        LaunchCommand    = 'npm run dev'
    }
)

$statuses = @{}
foreach ($component in $components) {
    $status = Get-ListeningSnapshot -Ports $component.Ports
    $statuses[$component.Name] = $status

    if ($status.IsRunning) {
        $processSummary = if ($status.ProcessNames.Count -gt 0) { $status.ProcessNames -join ', ' } else { 'unknown process' }
        Write-Host "[running] $($component.Name) is already listening on port(s) $($status.Ports -join ', ') via $processSummary." -ForegroundColor Yellow
    }
    else {
        Write-Host "[ready] $($component.Name) is not currently listening on port(s) $($component.Ports -join ', ')." -ForegroundColor DarkGray
    }
}

$runningComponents = @($components | Where-Object { $statuses[$_.Name].IsRunning })
$restartDetectedComponents = $RestartRunning.IsPresent

if ($runningComponents.Count -gt 0 -and -not $RestartRunning.IsPresent -and -not $NoPrompt.IsPresent) {
    $names = $runningComponents.Name -join ', '
    $restartDetectedComponents = Read-YesNo -Prompt "Restart the already running component(s): $names ? [Y/N]"
}

$started       = New-Object System.Collections.Generic.List[string]
$alreadyRunning = New-Object System.Collections.Generic.List[string]
$restarted     = New-Object System.Collections.Generic.List[string]
$issues        = New-Object System.Collections.Generic.List[string]

foreach ($component in $components) {
    $status = $statuses[$component.Name]

    if ($status.IsRunning) {
        if ($restartDetectedComponents) {
            Write-Host "[action] Restarting $($component.Name)..." -ForegroundColor Cyan
            $stopResult = Stop-ListeningProcesses -ComponentName $component.Name -Ports $component.Ports
            if (-not $stopResult.Success) {
                $issues.Add("Could not fully stop $($component.Name) for restart: $($stopResult.Failed -join '; ')")
                continue
            }
            $startResult = Start-Component -Component $component
            if ($startResult.Success) {
                $restarted.Add($component.Name)
                Write-Host "[started] $($startResult.Message)" -ForegroundColor Green
            }
            else {
                $issues.Add($startResult.Message)
                Write-Host "[issue] $($startResult.Message)" -ForegroundColor Red
            }
        }
        else {
            $alreadyRunning.Add($component.Name)
            Write-Host "[skip] $($component.Name) was already running and was left unchanged." -ForegroundColor Yellow
        }
        continue
    }

    Write-Host "[action] Starting $($component.Name)..." -ForegroundColor Cyan
    $startResult = Start-Component -Component $component
    if ($startResult.Success) {
        $started.Add($component.Name)
        Write-Host "[started] $($startResult.Message)" -ForegroundColor Green
    }
    else {
        $issues.Add($startResult.Message)
        Write-Host "[issue] $($startResult.Message)" -ForegroundColor Red
    }
}

Write-Host ''
Write-Host 'Summary' -ForegroundColor Cyan
Write-Host '-------' -ForegroundColor Cyan

if ($started.Count -gt 0)       { Write-Host "Started:        $($started -join ', ')"        -ForegroundColor Green  }
if ($restarted.Count -gt 0)     { Write-Host "Restarted:      $($restarted -join ', ')"      -ForegroundColor Green  }
if ($alreadyRunning.Count -gt 0){ Write-Host "Already running: $($alreadyRunning -join ', ')" -ForegroundColor Yellow }

if ($issues.Count -gt 0) {
    Write-Host 'Issues:' -ForegroundColor Red
    foreach ($issue in $issues) { Write-Host "  - $issue" -ForegroundColor Red }
    Write-Host ''
    Write-Host '[FAIL] Stack start finished with issues.' -ForegroundColor Red
    exit 1
}

if ($started.Count -eq 0 -and $restarted.Count -eq 0) {
    Write-Host 'No new processes were started — requested components were already running.' -ForegroundColor Yellow
}

if ($OpenBrowser.IsPresent) {
    Write-Host ''
    Write-Host '[action] Opening the frontend in your default browser...' -ForegroundColor Cyan
    try { Start-Process "http://localhost:$FrontendPort" } catch { }
}

Write-Host ''
Write-Host '[PASS] Stack start finished.' -ForegroundColor Green
exit 0
