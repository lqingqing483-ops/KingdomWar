<#
.SYNOPSIS
    Emergency freeze — stops all AI operations immediately.
    Call this when AI has caused unexpected damage to the project.

    Usage: .\scripts\emergency\freeze-ai.ps1
    Effect: Writes freeze flag, kills hanging processes, preserves error context
#>

$ProjectPath = (Get-Item $PSScriptRoot).Parent.Parent.FullName
$freezeFile = Join-Path $ProjectPath ".opencode\emergency-freeze.txt"
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

# Step 1: Write freeze flag (AI agents check for this before any operation)
@"
EMERGENCY FREEZE
Triggered at: $timestamp
Status: FROZEN — all AI operations STOPPED

DO NOT REMOVE THIS FILE until a human confirms it's safe.
To resume: delete this file and run scripts/emergency/verify-health.ps1
"@ | Set-Content -Path $freezeFile -Encoding UTF8

Write-Host "!!! EMERGENCY FREEZE ACTIVATED !!!" -ForegroundColor Red
Write-Host "Freeze flag written to: $freezeFile"

# Step 2: Kill hanging processes
Get-Process -Name "Unity" -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name "cargo" -ErrorAction SilentlyContinue | Stop-Process -Force

# Step 3: Capture error context
$errorLog = Join-Path $ProjectPath ".opencode\emergency-snapshot.txt"
@"
Emergency Snapshot — $timestamp
================================

Git Status:
$(git status 2>&1)

Recent Commits:
$(git log --oneline -10 2>&1)

Uncommitted Files:
$(git diff --name-only 2>&1)
"@ | Set-Content -Path $errorLog -Encoding UTF8

Write-Host "Error context saved to: $errorLog" -ForegroundColor Yellow
Write-Host "Next: run scripts/emergency/reset-to-latest-stable.ps1 or diagnose manually" -ForegroundColor Yellow

exit 0
