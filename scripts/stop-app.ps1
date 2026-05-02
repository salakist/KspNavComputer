[CmdletBinding()]
param(
    [int]$ApiPort      = 5000,
    [int]$FrontendPort = 5173,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

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
    param([int[]]$Ports, [bool]$ShouldBeListening, [int]$TimeoutSeconds = 15)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        $snapshot = Get-ListeningSnapshot -Ports $Ports
        if ($snapshot.IsRunning -eq $ShouldBeListening) { return $snapshot }
        Start-Sleep -Seconds 1
    } while ((Get-Date) -lt $deadline)

    return (Get-ListeningSnapshot -Ports $Ports)
}

function Stop-Component {
    param([pscustomobject]$Component)

    $initial = Get-ListeningSnapshot -Ports $Component.Ports
    if (-not $initial.IsRunning) {
        return [pscustomobject]@{
            Success        = $true
            AlreadyStopped = $true
            Message        = "$($Component.Name) is not currently running on port(s) $($Component.Ports -join ', ')."
            Failed         = @()
        }
    }

    $failed = @()
    foreach ($processId in $initial.Pids) {
        try { Stop-Process -Id $processId -ErrorAction Stop }
        catch { $failed += "$($Component.Name) PID $($processId): $($_.Exception.Message)" }
    }

    $after = Wait-ForPortsState -Ports $Component.Ports -ShouldBeListening $false -TimeoutSeconds 10
    if ($after.IsRunning) {
        foreach ($processId in $after.Pids) {
            try { Stop-Process -Id $processId -Force -ErrorAction Stop }
            catch { $failed += "$($Component.Name) PID $($processId) (force): $($_.Exception.Message)" }
        }
        $after = Wait-ForPortsState -Ports $Component.Ports -ShouldBeListening $false -TimeoutSeconds 5
        if ($after.IsRunning) {
            $failed += "$($Component.Name) still listening on port(s): $($after.Ports -join ', ')"
        }
    }

    if ($failed.Count -gt 0) {
        return [pscustomobject]@{
            Success        = $false
            AlreadyStopped = $false
            Message        = "Tried to stop $($Component.Name), but some issues remained."
            Failed         = $failed
        }
    }

    return [pscustomobject]@{
        Success        = $true
        AlreadyStopped = $false
        Message        = "$($Component.Name) stopped successfully."
        Failed         = @()
    }
}

# ---------------------------------------------------------------------------

Write-Header 'Stop KSP Navigation Computer'

$components = @(
    [pscustomobject]@{ Name = 'API';      Ports = @($ApiPort)      },
    [pscustomobject]@{ Name = 'Frontend'; Ports = @($FrontendPort) }
)

$runningComponents = @()
foreach ($component in $components) {
    $status = Get-ListeningSnapshot -Ports $component.Ports
    if ($status.IsRunning) {
        $runningComponents += $component
        $processSummary = if ($status.ProcessNames.Count -gt 0) { $status.ProcessNames -join ', ' } else { 'unknown process' }
        Write-Host "[running] $($component.Name) is listening on port(s) $($status.Ports -join ', ') via $processSummary." -ForegroundColor Yellow
    }
    else {
        Write-Host "[info] $($component.Name) is already stopped on port(s) $($component.Ports -join ', ')." -ForegroundColor DarkGray
    }
}

if ($runningComponents.Count -eq 0) {
    Write-Host ''
    Write-Host '[PASS] Nothing needed to be stopped.' -ForegroundColor Green
    exit 0
}

if (-not $Force.IsPresent) {
    $names = $runningComponents.Name -join ', '
    $confirmed = Read-YesNo -Prompt "Stop the detected component(s): $names ? [Y/N]"
    if (-not $confirmed) {
        Write-Host ''
        Write-Host '[info] Stop cancelled by user. No processes were stopped.' -ForegroundColor Yellow
        exit 0
    }
}

$stopped       = New-Object System.Collections.Generic.List[string]
$alreadyStopped = New-Object System.Collections.Generic.List[string]
$issues        = New-Object System.Collections.Generic.List[string]

foreach ($component in $components) {
    $result = Stop-Component -Component $component

    if ($result.AlreadyStopped) {
        $alreadyStopped.Add($component.Name)
        Write-Host "[skip] $($result.Message)" -ForegroundColor Yellow
        continue
    }

    if ($result.Success) {
        $stopped.Add($component.Name)
        Write-Host "[stopped] $($result.Message)" -ForegroundColor Green
        continue
    }

    $issues.AddRange($result.Failed)
    Write-Host "[issue] $($result.Message)" -ForegroundColor Red
}

Write-Host ''
Write-Host 'Summary' -ForegroundColor Cyan
Write-Host '-------' -ForegroundColor Cyan

if ($stopped.Count -gt 0)       { Write-Host "Stopped:        $($stopped -join ', ')"        -ForegroundColor Green  }
if ($alreadyStopped.Count -gt 0){ Write-Host "Already stopped: $($alreadyStopped -join ', ')" -ForegroundColor Yellow }

if ($issues.Count -gt 0) {
    Write-Host 'Issues:' -ForegroundColor Red
    foreach ($issue in $issues) { Write-Host "  - $issue" -ForegroundColor Red }
    Write-Host ''
    Write-Host '[FAIL] Stack stop finished with issues.' -ForegroundColor Red
    exit 1
}

Write-Host ''
Write-Host '[PASS] Stack stop finished.' -ForegroundColor Green
exit 0
