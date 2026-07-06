<#
.SYNOPSIS
    Local CI runner — runs all checks on this machine.
    
    Behavior:
    - Unity Editor open? → Run compile check, then tell you to run tests in Unity Test Runner
    - Unity Editor closed? → Run everything including tests automatically

    Usage: .\scripts\run-local-ci.ps1 [-Mode edit]
    Exit: 0 = all passed, 1 = any failure
#>

param(
    [ValidateSet("edit", "all")]
    [string]$Mode = "edit"
)

$ProjectPath = (Get-Item $PSScriptRoot).Parent.FullName
$startTime = Get-Date
$results = @()
$unityOpen = $null -ne (Get-Process -Name "Unity" -ErrorAction SilentlyContinue)

Write-Host ""
Write-Host "=== Local CI ===" -ForegroundColor Cyan
Write-Host "Project: $ProjectPath"
Write-Host "Started: $($startTime.ToString('yyyy-MM-dd HH:mm:ss'))"
Write-Host "Unity Editor: $(if($unityOpen){'OPEN'}else{'CLOSED'})" -ForegroundColor $(if($unityOpen){"Yellow"}else{"Green"})
Write-Host ""

function Run-Step {
    param($Name, $ScriptPath, $ScriptArgs)
    
    Write-Host "--- $Name ---" -ForegroundColor Yellow
    $stepStart = Get-Date
    
    if (-not (Test-Path $ScriptPath)) {
        Write-Host "  [SKIP] Script not found: $ScriptPath" -ForegroundColor Yellow
        return "skipped"
    }
    
    if ($ScriptArgs) {
        & $ScriptPath $ScriptArgs 2>&1 | ForEach-Object { Write-Host "  $_" }
    } else {
        & $ScriptPath 2>&1 | ForEach-Object { Write-Host "  $_" }
    }
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

# ── Step 1: Non-Unity checks (always run) ──

# Config validation
Write-Host "--- Config Check ---" -ForegroundColor Yellow
$configOk = $true
foreach ($f in @("opencode.json", "AGENTS.md", ".github/workflows/ci.yml")) {
    if (Test-Path (Join-Path $ProjectPath $f)) {
        Write-Host "  OK $f" -ForegroundColor Green
    } else {
        Write-Host "  MISSING $f" -ForegroundColor Red
        $configOk = $false
    }
}
$results += "" | Select-Object @{N="Name";E={"Config Files"}}, @{N="Result";E={if($configOk){"passed"}else{"failed"}}}

# Git status
Write-Host "--- Git Status ---" -ForegroundColor Yellow
Push-Location $ProjectPath
$dirty = git status --porcelain
Pop-Location
if ($dirty) {
    Write-Host "  [WARN] Uncommitted changes:" -ForegroundColor Yellow
    $dirty | ForEach-Object { Write-Host "    $_" }
    $rGit = "dirty"
} else {
    Write-Host "  [OK] Clean working tree" -ForegroundColor Green
    $rGit = "passed"
}
$results += "" | Select-Object @{N="Name";E={"Git Clean"}}, @{N="Result";E={$rGit}}

# ── Step 2: Unity checks ──

if ($unityOpen) {
    # Unity Editor is open → can't run batchmode tests
    $rCompile = Run-Step "C# Unity Compile" (Join-Path $ProjectPath "scripts\compile-check.ps1")
    $results += "" | Select-Object @{N="Name";E={"C# Unity Compile"}}, @{N="Result";E={$rCompile}}
    
    # Remind user to run tests in Editor
    Write-Host ""
    Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║  Unity Editor 正在运行，无法通过脚本运行测试            ║" -ForegroundColor Cyan
    Write-Host "║                                                       ║" -ForegroundColor Cyan
    Write-Host "║  请在 Unity 中手动跑测试：                              ║" -ForegroundColor Cyan
    Write-Host "║  Window → General → Test Runner                       ║" -ForegroundColor Cyan
    if ($Mode -eq "edit") {
        Write-Host "║  → EditMode 标签 → Run All                            ║" -ForegroundColor Cyan
    } else {
        Write-Host "║  → EditMode 标签 → Run All                            ║" -ForegroundColor Cyan
        Write-Host "║  → PlayMode 标签 → Run All                            ║" -ForegroundColor Cyan
    }
    Write-Host "║                                                       ║" -ForegroundColor Cyan
    Write-Host "║  预期: $(if($Mode -eq 'edit'){'146'}else{'211'}) 个测试，0 失败                             ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
    
    $results += "" | Select-Object @{N="Name";E={"C# Unity Tests"}}, @{N="Result";E={"skipped"}}
} else {
    # Unity Editor is closed → run everything
    $rCompile = Run-Step "C# Unity Compile" (Join-Path $ProjectPath "scripts\compile-check.ps1")
    $results += "" | Select-Object @{N="Name";E={"C# Unity Compile"}}, @{N="Result";E={$rCompile}}
    
    $ciPath = Join-Path $ProjectPath "scripts\ci.ps1"
    $ciArg = if ($Mode -eq "edit") { "-Mode EditMode" } else { $null }
    $rTest = Run-Step "C# Unity Tests ($Mode)" $ciPath $ciArg
    $results += "" | Select-Object @{N="Name";E={"C# Unity Tests"}}, @{N="Result";E={$rTest}}
}

# ── Summary ──
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
if ($unityOpen) {
    Write-Host "  Unity Editor 未关闭 — 编译检查通过后请在 Unity Test Runner 中手动跑测试" -ForegroundColor Yellow
}
Write-Host "  $passedCount passed, $failedCount failed, $skippedCount skipped" -ForegroundColor $(if($failedCount -eq 0){"Green"}else{"Red"})
Write-Host "  Duration: $totalDuration s"
Write-Host ""

# Record to changelog
$cl = Join-Path $ProjectPath ".opencode\changelog\changelog.ps1"
if (Test-Path $cl) {
    $statusText = if ($failedCount -eq 0) { "passed" } else { "failed" }
    & $cl -Agent "local-ci" -Task "Local CI run" -Action "test" -Files "multiple" -Status $statusText -Detail "$passedCount passed, $failedCount failed, $skippedCount skipped (Unity: $(if($unityOpen){'open,manual tests'}else{'auto'}))"
}

# If we ran tests automatically (Editor was closed), reopen Unity
if (-not $unityOpen -and $failedCount -eq 0) {
    Write-Host "测试完成，正在重新打开 Unity..." -ForegroundColor Cyan
    $unityHub = "C:\Users\A\AppData\Local\Unity\Hub\Unity Hub.exe"
    if (Test-Path $unityHub) {
        Start-Process -FilePath $unityHub
    } else {
        Start-Process -FilePath "D:\yinyon\2022.3.57f1c2\Editor\Unity.exe"
    }
}

if ($failedCount -gt 0) { exit 1 }
exit 0
