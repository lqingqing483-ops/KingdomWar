<#
.SYNOPSIS
    Reset project to the latest stable commit. Discards all uncommitted changes.
    
    Usage: .\scripts\emergency\reset-to-latest-stable.ps1 [-Commit "optional-specific-hash"]
    
    Without -Commit: resets to the last known good commit (HEAD~1 or tagged stable)
    With -Commit: resets to the specified commit hash
    
    Exit: 0 = success, 1 = failed
#>

param(
    [string]$Commit = ""
)

$ProjectPath = (Get-Item $PSScriptRoot).Parent.Parent.FullName

Write-Host "=== Emergency Reset ===" -ForegroundColor Red
Write-Host "This will DISCARD all uncommitted changes!" -ForegroundColor Red
Write-Host "Project: $ProjectPath"

# Check freeze flag
$freezeFile = Join-Path $ProjectPath ".opencode\emergency-freeze.txt"
if (-not (Test-Path $freezeFile)) {
    Write-Host "WARNING: No freeze flag found. Run freeze-ai.ps1 first if AI is still active." -ForegroundColor Yellow
}

Push-Location $ProjectPath
try {
    if ($Commit) {
        Write-Host "Resetting to specified commit: $Commit" -ForegroundColor Yellow
        git reset --hard $Commit 2>&1 | ForEach-Object { Write-Host $_ }
        if ($LASTEXITCODE -ne 0) {
            Write-Host "FAILED: Could not reset to $Commit" -ForegroundColor Red
            exit 1
        }
    }
    else {
        # Try to find a stable tag
        $stableTag = git tag -l "stable-*" | Select-Object -Last 1
        if ($stableTag) {
            Write-Host "Found stable tag: $stableTag" -ForegroundColor Green
            git reset --hard $stableTag 2>&1 | ForEach-Object { Write-Host $_ }
        }
        else {
            # Fall back to HEAD~1
            Write-Host "No stable tag found. Resetting to HEAD~1" -ForegroundColor Yellow
            git reset --hard HEAD~1 2>&1 | ForEach-Object { Write-Host $_ }
        }
        if ($LASTEXITCODE -ne 0) {
            Write-Host "FAILED: Could not reset" -ForegroundColor Red
            exit 1
        }
    }
    
    # Clean untracked files
    git clean -fd 2>&1 | Out-Null
    
    Write-Host "=== Reset complete ===" -ForegroundColor Green
    Write-Host "Current HEAD: $(git log --oneline -1)" -ForegroundColor Cyan
    Write-Host "Run verify-health.ps1 to confirm project is healthy" -ForegroundColor Yellow
}
finally {
    Pop-Location
}

exit 0
