<#
.SYNOPSIS
    Run performance tests and check against baseline.
    Exits with code 1 if performance regresses beyond threshold.
#>

param(
    [string]$UnityPath = "",
    [string]$ProjectPath = (Get-Item $PSScriptRoot).Parent.FullName
)

if (-not $UnityPath) { $UnityPath = $env:UNITY_PATH }
if (-not $UnityPath) { $UnityPath = "D:\yinyon\2022.3.57f1c2\Editor\Unity.exe" }

$logFile = Join-Path $env:TEMP "unity-perf-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"

Write-Host "=== Performance Gate ===" -ForegroundColor Cyan

# Check if Unity Editor is running
$unityProcs = Get-Process -Name "Unity" -ErrorAction SilentlyContinue
if ($unityProcs) {
    Write-Host "⚠ Unity Editor is running. Close it before running performance gate." -ForegroundColor Yellow
    exit 0
}

$process = Start-Process -FilePath $UnityPath -ArgumentList @(
    "-batchmode", "-nographics",
    "-projectPath", "`"$ProjectPath`"",
    "-executeMethod", "KingdomWar.Editor.PerformanceTestRunnerEntry.RunAndCompare",
    "-logFile", "`"$logFile`""
) -NoNewWindow -PassThru -RedirectStandardOutput "$env:TEMP\unity-perf-stdout.txt"

$timeout = 120
$elapsed = 0
$exited = $false
while ($elapsed -lt $timeout) {
    Start-Sleep -Seconds 1
    $elapsed++
    if ($process.HasExited) { $exited = $true; break }
}

if (-not $exited) {
    Write-Host "Performance run timed out after ${timeout}s" -ForegroundColor Red
    $process.Kill()
    exit 2
}

# Check result (exit code 1 = regression detected)
if ($process.ExitCode -eq 1) {
    Write-Host "FAILED: Performance regression detected!" -ForegroundColor Red
    exit 1
} elseif ($process.ExitCode -eq 0) {
    Write-Host "PASSED: Performance within baseline" -ForegroundColor Green
    exit 0
} else {
    Write-Host "Performance run error (exit code $($process.ExitCode))" -ForegroundColor Yellow
    exit $process.ExitCode
}
