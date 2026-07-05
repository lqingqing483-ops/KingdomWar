<#
.SYNOPSIS
    Run EditMode tests with Code Coverage collection.
    Outputs coverage report to CoverageReport/ directory.
    Exits with code 1 if coverage is below threshold.

.PARAMETER Threshold
    Minimum line coverage percentage (default: 50).
#>

param(
    [int]$Threshold = 50
)

$ProjectPath = (Get-Item $PSScriptRoot).Parent.FullName
$UnityPath = $env:UNITY_PATH
if (-not $UnityPath) {
    $UnityPath = "D:\yinyon\2022.3.57f1c2\Editor\Unity.exe"
}

$coverageDir = Join-Path $ProjectPath "CoverageReport"
if (Test-Path $coverageDir) { Remove-Item -Path $coverageDir -Recurse -Force }

Write-Host "=== Code Coverage Run ===" -ForegroundColor Cyan
Write-Host "Threshold: ${Threshold}%"

$logFile = Join-Path $env:TEMP "unity-coverage-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"

$args = @(
    "-batchmode", "-nographics",
    "-projectPath", "`"$ProjectPath`"",
    "-logFile", "`"$logFile`"",
    "-runTests", "-testPlatform", "EditMode",
    "-testResults", "`"$ProjectPath\test-results-coverage.xml`"",
    "-coverageResultsPath", "`"$coverageDir`"",
    "-enableCodeCoverage",
    "-coverageOptions", "generateAdditionalMetrics;assemblyFilters:+KingdomWar*"
)

$process = Start-Process -FilePath $UnityPath -ArgumentList $args -NoNewWindow -PassThru

$timeout = 180
$elapsed = 0
$exited = $false
while ($elapsed -lt $timeout) {
    Start-Sleep -Seconds 1
    $elapsed++
    if ($process.HasExited) { $exited = $true; break }
}

if (-not $exited) {
    Write-Host "Coverage run timed out after ${timeout}s" -ForegroundColor Red
    $process.Kill()
    exit 2
}

# Parse coverage results
$reportPath = Join-Path $coverageDir "Report\Summary.xml"
if (Test-Path $reportPath) {
    $xml = [xml](Get-Content $reportPath)
    $coverage = $xml.CoverageReport.Summary.SequenceCoverage
    $lineCoverage = [double]$coverage
    Write-Host "Line Coverage: $lineCoverage%" -ForegroundColor Yellow
    
    if ($lineCoverage -lt $Threshold) {
        Write-Host "FAILED: Coverage $lineCoverage% < threshold ${Threshold}%" -ForegroundColor Red
        exit 1
    }
    Write-Host "PASSED: Coverage $lineCoverage% >= threshold ${Threshold}%" -ForegroundColor Green
} else {
    Write-Host "No coverage report found at $reportPath" -ForegroundColor Yellow
    Write-Host "Coverage data may not be available. Check Unity version supports code coverage."
}

exit $process.ExitCode
