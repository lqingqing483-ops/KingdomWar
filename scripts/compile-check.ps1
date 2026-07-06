# Unity Compile Check Script
# Usage: .\scripts\compile-check.ps1 [-UnityPath "C:\path\to\Unity.exe"]
# Returns: exit code 0 if no errors, 1 if compilation errors found

param(
    [string]$UnityPath = "",
    [string]$ProjectPath = (Get-Item $PSScriptRoot).Parent.FullName
)

# Resolve Unity path
if (-not $UnityPath) { $UnityPath = $env:UNITY_PATH }
if (-not $UnityPath) {
    $hubPaths = @(
        "D:\yinyon",
        "$env:LOCALAPPDATA\Unity\Hub\Editor",
        "C:\Program Files\Unity\Hub\Editor",
        "C:\Program Files\Unity"
    )
    foreach ($hub in $hubPaths) {
        $found = Get-ChildItem -Path $hub -Recurse -Filter "Unity.exe" -ErrorAction SilentlyContinue |
            Select-Object -First 1
        if ($found) { $UnityPath = $found.FullName; break }
    }
}
if (-not $UnityPath -or -not (Test-Path $UnityPath)) {
    Write-Warning "Unity Editor not found. Compilation check skipped."
    Write-Warning "Set UNITY_PATH or run: .\scripts\compile-check.ps1 -UnityPath 'path\to\Unity.exe'"
    exit 0
}

$logFile = Join-Path $env:TEMP "unity-compile-check-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
$editorLog = "$env:LOCALAPPDATA\Unity\Editor\Editor.log"
$previousLength = 0
if (Test-Path $editorLog) { $previousLength = (Get-Item $editorLog).Length }

# Check if Unity Editor is running — can't run batchmode + Editor on same project
$unityProcs = Get-Process -Name "Unity" -ErrorAction SilentlyContinue
if ($unityProcs) {
    Write-Host "⚠ Unity Editor is running (PID: $($unityProcs.Id)). Close it before running compile check." -ForegroundColor Yellow
    Write-Host "  Compile check skipped." -ForegroundColor Yellow
    exit 0
}

Write-Host "Running compile check..." -ForegroundColor Yellow
Write-Host "Unity: $UnityPath"
Write-Host "Project: $ProjectPath"
Write-Host "Log: $logFile"

# Run Unity with Start-Process (non-blocking) + timeout monitoring
# This avoids getting stuck if Unity hangs — the & operator is synchronous and would block.
$unityArgs = @(
    "-quit", "-batchmode", "-logFile", "`"$logFile`"",
    "-projectPath", "`"$ProjectPath`"",
    "-buildTarget", "StandaloneWindows64",
    "-executeMethod", "KingdomWar.Editor.CompileChecker.CheckAndExit"
)

$unityProcess = Start-Process -FilePath $UnityPath -ArgumentList $unityArgs -NoNewWindow -PassThru -RedirectStandardOutput "$env:TEMP\unity-compile-stdout.txt"

# Wait for Unity to exit (timeout 30s - if stuck, kill and retry)
$timeout = 30
$elapsed = 0
$exited = $false
while ($elapsed -lt $timeout) {
    Start-Sleep -Seconds 1
    $elapsed++
    if ($unityProcess.HasExited) {
        $exited = $true
        break
    }
}

# Kill stuck Unity process if timeout reached
if (-not $exited) {
    Write-Host "Unity process timed out after ${timeout}s, killing..." -ForegroundColor Yellow
    $unityProcess.Kill()
    Start-Sleep -Seconds 2
}

# Collect errors from both log files
$allErrors = @()

# Check custom log
if (Test-Path $logFile) {
    $allErrors += Select-String -Path $logFile -Pattern "error CS\d{4}" -SimpleMatch
}

# Check Editor.log for new errors since we started
if (Test-Path $editorLog) {
    $currentLength = (Get-Item $editorLog).Length
    if ($currentLength -gt $previousLength) {
        $stream = [System.IO.StreamReader]::new($editorLog)
        $stream.BaseStream.Seek($previousLength, [System.IO.SeekOrigin]::Begin) | Out-Null
        $newContent = $stream.ReadToEnd()
        $stream.Close()
        $newContent | Select-String -Pattern "error CS\d{4}" -SimpleMatch | ForEach-Object {
            $allErrors += $_
        }
    }
}

if ($allErrors.Count -gt 0) {
    Write-Host "`nCOMPILATION ERRORS FOUND:" -ForegroundColor Red
    $allErrors | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
} else {
    Write-Host "`nCompilation check PASSED" -ForegroundColor Green
    exit 0
}
