<#
.SYNOPSIS
    Run a Unity command with timeout protection. Kills Unity if it hangs.
    Use this instead of calling Unity.exe directly to avoid infinite hangs.

.PARAMETER UnityPath
    Path to Unity.exe. Defaults to $env:UNITY_PATH or auto-discovery.

.PARAMETER Arguments
    Arguments to pass to Unity (e.g. "-quit -batchmode -executeMethod X.Y")

.PARAMETER TimeoutSeconds
    Maximum seconds to wait before killing Unity. Default: 60.

.PARAMETER LogFile
    Path to log file. Default: $env:TEMP\unity-invoke-<timestamp>.log

.EXAMPLE
    .\scripts\Invoke-Unity.ps1 -Arguments "-quit -batchmode -projectPath . -executeMethod MyEditorScript.Run" -TimeoutSeconds 30
#>

param(
    [string]$UnityPath = "",
    [parameter(Mandatory = $true)]
    [string]$Arguments,
    [int]$TimeoutSeconds = 60,
    [string]$LogFile = ""
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
    Write-Warning "Unity Editor not found."
    exit 1
}

if (-not $LogFile) {
    $LogFile = Join-Path $env:TEMP "unity-invoke-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
}

# Kill any stale Unity processes first
Get-Process -Name "Unity" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

Write-Host "[Invoke-Unity] Running: $UnityPath $Arguments" -ForegroundColor Yellow
Write-Host "[Invoke-Unity] Log: $LogFile"
Write-Host "[Invoke-Unity] Timeout: ${TimeoutSeconds}s"

# Parse arguments string into array
$argList = @()
$argsArray = [Management.Automation.PSParser]::Tokenize($Arguments, [ref]$null) | Where-Object { $_.Type -eq 'CommandArgument' -or $_.Type -eq 'String' } | ForEach-Object { $_.Content }
if (-not $argsArray) {
    # Fallback: split by spaces, handling quotes
    $argsArray = @()
    foreach ($part in ($Arguments -split ' ')) {
        $trimmed = $part.Trim('"')
        if ($trimmed) { $argsArray += $trimmed }
    }
}

$process = Start-Process -FilePath $UnityPath -ArgumentList $Arguments -NoNewWindow -PassThru -RedirectStandardOutput "$env:TEMP\unity-invoke-stdout.txt"

# Monitor with timeout
$elapsed = 0
$exited = $false
while ($elapsed -lt $TimeoutSeconds) {
    Start-Sleep -Seconds 1
    $elapsed++
    if ($process.HasExited) {
        $exited = $true
        break
    }
}

if (-not $exited) {
    Write-Host "[Invoke-Unity] TIMED OUT after ${TimeoutSeconds}s, killing Unity..." -ForegroundColor Red
    $process.Kill()
    Start-Sleep -Seconds 2
    exit 2  # exit code 2 = timed out
}

Write-Host "[Invoke-Unity] Completed with exit code: $($process.ExitCode)" -ForegroundColor Green
exit $process.ExitCode
