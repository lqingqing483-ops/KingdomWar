<#
.SYNOPSIS
    Restore critical configuration files from git history.
    
    Usage: .\scripts\emergency\restore-config.ps1
    Restores: opencode.json, AGENTS.md, and all .opencode/ config files
#>

$ProjectPath = (Get-Item $PSScriptRoot).Parent.Parent.FullName
$filesToRestore = @(
    "opencode.json",
    "AGENTS.md",
    ".opencode\agents\orchestrator.md",
    ".opencode\agents\build.md",
    ".opencode\agents\review.md",
    ".opencode\heartbeat.ps1"
)

Write-Host "=== Config Restore ===" -ForegroundColor Cyan
Write-Host "Restoring critical config files from git HEAD~1..." -ForegroundColor Yellow

Push-Location $ProjectPath
try {
    foreach ($f in $filesToRestore) {
        $fullPath = Join-Path $ProjectPath $f
        if (Test-Path $fullPath) {
            # Backup current version
            $backup = "$fullPath.bak.$(Get-Date -Format 'yyyyMMddHHmmss')"
            Copy-Item -Path $fullPath -Destination $backup -Force
        }
        
        # Restore from HEAD~1
        git checkout HEAD~1 -- $f 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  OK: Restored $f" -ForegroundColor Green
        }
        else {
            Write-Host "  SKIP: $f not in git history (new file)" -ForegroundColor Yellow
        }
    }
    
    Write-Host "=== Config restore complete ===" -ForegroundColor Green
    Write-Host "Backups saved with .bak.* extension" -ForegroundColor Yellow
}
finally {
    Pop-Location
}

exit 0
