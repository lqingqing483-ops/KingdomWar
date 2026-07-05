<#
.SYNOPSIS
    Append a structured change record to the changelog.
    Changes are append-only — AI cannot modify or delete past entries.

    Usage: .opencode\changelog\changelog.ps1 -Agent "build-1" -Task "Implement login" -Files "src/auth/login.cs" -Status "passed"

.PARAMETER Agent
    Agent name (orchestrator, build-1, review-1, etc.)

.PARAMETER Task
    Task description

.PARAMETER Action
    Action type: create / modify / delete / review / test

.PARAMETER Files
    Comma-separated list of affected files

.PARAMETER Status
    Result: started / passed / failed / rejected

.PARAMETER Detail
    Optional detail message

.EXAMPLE
    .opencode\changelog\changelog.ps1 -Agent "build-1" -Task "Implement login API" -Action "create" -Files "src/auth/login.cs" -Status "passed"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Agent,
    
    [Parameter(Mandatory = $true)]
    [string]$Task,
    
    [ValidateSet("create", "modify", "delete", "review", "test", "merge", "other")]
    [string]$Action = "modify",
    
    [string]$Files = "",
    
    [ValidateSet("started", "passed", "failed", "rejected", "completed")]
    [string]$Status = "started",
    
    [string]$Detail = ""
)

$logDir = Split-Path -Parent $PSCommandPath
$dateStr = Get-Date -Format "yyyy-MM-dd"
$logFile = Join-Path $logDir "$dateStr.jsonl"
$id = "chg_$(Get-Random -Maximum 99999999)-$(Get-Date -Format 'HHmmss')"

$record = @{
    id        = $id
    timestamp = (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
    agent     = $Agent
    task      = $Task
    action    = $Action
    files     = ($Files -split ',') | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }
    status    = $Status
    detail    = $Detail
}

$json = ($record | ConvertTo-Json -Compress)
Add-Content -Path $logFile -Value $json -Encoding UTF8

# Write to console
$color = switch ($Status) {
    "passed"    { "Green" }
    "failed"    { "Red" }
    "rejected"  { "Red" }
    "started"   { "Cyan" }
    "completed" { "Green" }
    default     { "Gray" }
}
$msg = "[CHANGELOG] $id | $Agent | $Task | $Status"
Write-Host $msg -ForegroundColor $color

exit 0
