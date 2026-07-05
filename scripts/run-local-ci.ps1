<#
.SYNOPSIS
    Local CI runner — runs all checks on this machine using installed Unity + tools.
    Use this when GitHub Actions can't run Unity tests (no license / no runner).

    Usage: .\scripts\run-local-ci.ps1
    Exit: 0 = all passed, 1 = any failure

    This runs the same checks the GitHub workflow would run,
    but locally instead of in Docker.
#>

$ProjectPath = (Get-Item $PSScriptRoot).Parent.FullName
$startTime = Get-Date
$results = @()

Write-Host ""
Write-Host "=== Local CI ===" -ForegroundColor Cyan
Write-Host "Project: $ProjectPath"
Write-Host "Started: $($startTime.ToString('yyyy-MM-dd HH:mm:ss'))"
Write-Host ""

function Run-Step {
    param($Name, $ScriptPath)
    
    Write-Host "--- $Name ---" -ForegroundColor Yellow
    $stepStart = Get-Date
    
    if (-not (Test-Path $ScriptPath)) {
        Write-Host "  [SKIP] Script not found: $ScriptPath" -ForegroundColor Yellow
        return "skipped"
    }
    
    & $ScriptPath 2>&1 | ForEach-Object { Write-Host "  $_" }
    $exitCode = $LASTEXITCODE
    $duration = [math]::Round((Get-Date).Subtract($stepStart).TotalSeconds, 1)
    
    if ($exitCode -eq 0) {
        Write-Host "  [PASS] ($duration s)" -ForegroundColor Green
        return "passed"
    } else {
        Write-Host "  [FAIL] (exit $exitCode, $duration s)" -ForegroundColor Red
        return "failed"
    }
}

# Step 1: C# Unity Compile
$r1 = Run-Step "C# Unity Compile" (Join-Path $ProjectPath "scripts\compile-check.ps1")
$results += "" | Select-Object @{N="Name";E={"C# Unity Compile"}}, @{N="Result";E={$r1}}

# Step 2: EditMode tests
$r2 = Run-Step "C# EditMode Tests" (Join-Path $ProjectPath "scripts\ci.ps1")
$results += "" | Select-Object @{N="Name";E={"C# EditMode Tests"}}, @{N="Result";E={$r2}}

# Step 3: Git status
Write-Host "--- Git Status ---" -ForegroundColor Yellow
Push-Location $ProjectPath
$dirty = git status --porcelain
Pop-Location
if ($dirty) {
    Write-Host "  [WARN] Uncommitted changes:" -ForegroundColor Yellow
    $dirty | ForEach-Object { Write-Host "    $_" }
    $r3 = "dirty"
} else {
    Write-Host "  [OK] Clean working tree" -ForegroundColor Green
    $r3 = "passed"
}
$results += "" | Select-Object @{N="Name";E={"Git Clean"}}, @{N="Result";E={$r3}}

# Summary
Write-Host ""
Write-Host "=== CI Results Summary ===" -ForegroundColor Cyan
Write-Host ""

$passedCount = 0
$failedCount = 0
$skippedCount = 0
foreach ($r in $results) {
    switch ($r.Result) {
        "passed"  { Write-Host "  [PASS] $($r.Name)" -ForegroundColor Green; $passedCount++ }
        "failed"  { Write-Host "  [FAIL] $($r.Name)" -ForegroundColor Red; $failedCount++ }
        "skipped" { Write-Host "  [SKIP] $($r.Name)" -ForegroundColor Yellow; $skippedCount++ }
        "dirty"   { Write-Host "  [DIRTY] $($r.Name)" -ForegroundColor Yellow; $skippedCount++ }
    }
}

$totalDuration = [math]::Round((Get-Date).Subtract($startTime).TotalSeconds, 1)
Write-Host ""
Write-Host "  $passedCount passed, $failedCount failed, $skippedCount skipped" -ForegroundColor $(if($failedCount -eq 0){"Green"}else{"Red"})
Write-Host "  Duration: $totalDuration s"
Write-Host ""

# Record to changelog
$cl = Join-Path $ProjectPath ".opencode\changelog\changelog.ps1"
if (Test-Path $cl) {
    $statusText = if ($failedCount -eq 0) { "passed" } else { "failed" }
    & $cl -Agent "local-ci" -Task "Local CI run" -Action "test" -Files "multiple" -Status $statusText -Detail "$passedCount passed, $failedCount failed, $skippedCount skipped"
}

if ($failedCount -gt 0) { exit 1 }
exit 0
