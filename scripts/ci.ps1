param(
    [string]$Mode = "all",
    [string]$UnityPath = "D:\yinyon\2022.3.57f1c2\Editor\Unity.exe"
)
$ProjectPath = (Get-Item $PSScriptRoot).Parent.FullName
$anyFailed = $false

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
    if ($p.ExitCode -ne 0) {
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
    $procs = Get-Process -Name "Unity" -ErrorAction SilentlyContinue
    if ($procs) { $procs | Stop-Process -Force; Start-Sleep -Seconds 3 }
}

if ($Mode -eq "all" -or $Mode -eq "edit" -or $Mode -eq "EditMode") { Run-Mode "EditMode" "EditMode" }
if ($Mode -eq "all" -or $Mode -eq "play" -or $Mode -eq "PlayMode") { Run-Mode "PlayMode" "PlayMode" }

if ($anyFailed) { Write-Host "FAILED" -ForegroundColor Red; exit 1 }

# Performance baseline check
Write-Host "=== Performance Baseline ===" -ForegroundColor Cyan
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
$procs = Get-Process -Name "Unity" -ErrorAction SilentlyContinue
if ($procs) { $procs | Stop-Process -Force; Start-Sleep -Seconds 3 }

if ($anyFailed) { Write-Host "FAILED" -ForegroundColor Red; exit 1 }
Write-Host "ALL PASSED" -ForegroundColor Green