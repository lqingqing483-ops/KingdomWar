<#
.SYNOPSIS
    Heartbeat script for agent orchestration. Writes progress to task-state.json
    and detects stuck sub-tasks by checking last heartbeat timestamp.

.PARAMETER Task
    Top-level task name

.PARAMETER Subtask
    Current sub-task (e.g. "2/5")

.PARAMETER Step
    Current step description

.PARAMETER Status
    One of: running, stuck, failed, completed

.PARAMETER Elapsed
    Seconds elapsed since this sub-task started

.PARAMETER Timeout
    Maximum allowed seconds since last heartbeat before declaring stuck (default: 35)

.EXAMPLE
    .opencode/heartbeat.ps1 -Task Arena -Subtask 2/5 -Step TrophyManager -Status running -Elapsed 30

.NOTES
    Exit codes:
      0 = OK
      2 = STUCK (last heartbeat exceeds timeout threshold)
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Task,

    [Parameter(Mandatory = $true)]
    [string]$Subtask,

    [Parameter(Mandatory = $true)]
    [string]$Step,

    [ValidateSet("running", "stuck", "failed", "completed")]
    [string]$Status = "running",

    [int]$Elapsed = 0,

    [int]$Timeout = 35
)

$stateDir = Split-Path -Parent $PSCommandPath
$stateFile = Join-Path $stateDir "task-state.json"
$now = [DateTime]::UtcNow
$nowStr = $now.ToString("yyyy-MM-ddTHH:mm:ssZ")

# Check previous heartbeat for stuck detection
if ($Status -eq "running" -and (Test-Path $stateFile)) {
    try {
        $content = Get-Content $stateFile -Raw -Encoding UTF8
        $prev = $content | ConvertFrom-Json
        if ($prev.status -eq "running" -and $prev.last_heartbeat) {
            $lastTime = [DateTime]::Parse($prev.last_heartbeat)
            $gap = [DateTime]::UtcNow - $lastTime
            if ($gap.TotalSeconds -gt $Timeout) {
                Write-Warning "HEARTBEAT: last heartbeat was $([int]$gap.TotalSeconds)s ago, exceeds ${Timeout}s threshold!"
                $stuckState = @{
                    task            = $Task
                    subtask         = $Subtask
                    step            = $Step
                    status          = "stuck"
                    elapsed_seconds = $Elapsed
                    last_heartbeat  = $nowStr
                } | ConvertTo-Json
                $stuckState | Set-Content $stateFile -Encoding UTF8
                exit 2
            }
        }
    }
    catch {
        Write-Warning "HEARTBEAT: cannot parse previous state, starting fresh"
    }
}

# Build state object
$state = @{
    task            = $Task
    subtask         = $Subtask
    step            = $Step
    status          = $Status
    elapsed_seconds = $Elapsed
    last_heartbeat  = $nowStr
}

# Write to file
$json = $state | ConvertTo-Json
Set-Content -Path $stateFile -Value $json -Encoding UTF8

# Report to console
$msg = "[HEARTBEAT] " + $Task + " | " + $Subtask + " | " + $Step + " | " + $Elapsed + "s | " + $Status
if ($Status -eq "completed") {
    Write-Host $msg -ForegroundColor Green
}
elseif ($Status -eq "failed") {
    Write-Host $msg -ForegroundColor Red
}
else {
    Write-Host $msg -ForegroundColor Cyan
}

exit 0
