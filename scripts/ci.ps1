param(
    [string]$Mode = "all",
    [string]$UnityPath = "D:\yinyon\2022.3.57f1c2\Editor\Unity.exe"
)
$ProjectPath = (Get-Item $PSScriptRoot).Parent.FullName
$anyFailed = $false

# Normalize mode parameter
if ($Mode -eq "EditMode" -or $Mode -eq "edit") { $Mode = "edit" }
elseif ($Mode -eq "PlayMode" -or $Mode -eq "play") { $Mode = "play" }
else { $Mode = "all" }

# Check if Unity Editor is running — can't run tests alongside Editor
$unityProcs = Get-Process -Name "Unity" -ErrorAction SilentlyContinue
if ($unityProcs) {
    Write-Host "⚠ Unity Editor is running (PID: $($unityProcs.Id)). Close it before running CI." -ForegroundColor Yellow
    exit 1
}

function Run-Mode($mode, $label) {
    $rf = "test-results-$mode.xml"
    $lf = "unity-log-$mode.txt"
    Write-Host ("=== " + $label + " ===") -ForegroundColor Cyan
    $p = Start-Process -FilePath $UnityPath -ArgumentList "-batchmode -nographics -projectPath `"$ProjectPath`" -runTests -testPlatform $mode -testResults `"$ProjectPath\$rf`" -logFile `"$ProjectPath\$lf`"" -NoNewWindow -PassThru
    $timeout = 300
    $elapsed = 0
    while ($elapsed -lt $timeout -and -not $p.HasExited) {
        Start-Sleep -Seconds 1
        $elapsed++
    }
    if (-not $p.HasExited) {
        Write-Host ("Unity timed out after ${timeout}s, killing...") -ForegroundColor Yellow
        $p.Kill()
        Start-Sleep -Seconds 2
    }
    if ($p.ExitCode -and $p.ExitCode -ne 0) {
        Write-Host ("Unity exited with code: " + $p.ExitCode) -ForegroundColor Red
        $script:anyFailed = $true
    }
    $resultsPath = Join-Path $ProjectPath $rf
    if (Test-Path $resultsPath) {
        $line = Get-Content $resultsPath -TotalCount 3 | Select-String "test-run"
        if ($line) {
            $line.ToString().Trim()
            $m = [regex]::Match($line.ToString(), 'passed="(\d+)".*failed="(\d+)"')
            if ($m.Success) {
                Write-Host ("  Total: " + $m.Groups[1].Value + " passed, " + $m.Groups[2].Value + " failed")
                if ([int]$m.Groups[2].Value -gt 0) { $script:anyFailed = $true }
            }
        }
    } else {
        Write-Host ("  No results file generated") -ForegroundColor Red
        $script:anyFailed = $true
    }
}

if ($Mode -eq "all" -or $Mode -eq "edit" -or $Mode -eq "EditMode") { Run-Mode "EditMode" "EditMode" }
if ($Mode -eq "all" -or $Mode -eq "play" -or $Mode -eq "PlayMode") { Run-Mode "PlayMode" "PlayMode" }

# Performance baseline check (skip in single-mode runs)
$skipPerf = ($Mode -eq "edit" -or $Mode -eq "play")
if ($anyFailed -and -not $skipPerf) { Write-Host "FAILED" -ForegroundColor Red; exit 1 }
if ($skipPerf) {
    Write-Host "=== Performance Baseline (skipped in single-mode) ===" -ForegroundColor Yellow
    $p = "" | Select-Object ExitCode
    $p.ExitCode = 0
} else {
    if ($anyFailed) { Write-Host "FAILED" -ForegroundColor Red; exit 1 }
    $perfLog = "unity-perf-log.txt"
    $p = Start-Process -FilePath $UnityPath -ArgumentList "-batchmode -nographics -projectPath `"$ProjectPath`" -executeMethod KingdomWar.Editor.PerformanceTestRunnerEntry.RunAndCompare -logFile `"$ProjectPath\$perfLog`"" -NoNewWindow -Wait -PassThru
    $perfLogPath = Join-Path $ProjectPath $perfLog
    if (Test-Path $perfLogPath) {
        $perfLines = Get-Content $perfLogPath | Select-String "\[Perf\]|\[PerformanceBaseline\]|=== Performance Baseline Report ==="
        foreach ($l in $perfLines) { $l.ToString().Trim() }
    } else {
        Write-Host "  No performance log generated" -ForegroundColor Red
    }
    if ($p.ExitCode -eq 0) {
        Write-Host "  Performance: PASS" -ForegroundColor Green
    } elseif ($p.ExitCode -eq 1) {
        Write-Host "  Performance: FAILED (regression detected)" -ForegroundColor Red
        $anyFailed = $true
    } else {
        Write-Host "  Performance: ERROR (exit code $($p.ExitCode))" -ForegroundColor Red
        $anyFailed = $true
    }
}

if ($anyFailed) { Write-Host "FAILED" -ForegroundColor Red; exit 1 }
Write-Host "ALL PASSED" -ForegroundColor Green