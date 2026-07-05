<#
.SYNOPSIS
    Verify project health after emergency recovery.
    Checks: git status, compile, tests, key files existence.

    Usage: .\scripts\emergency\verify-health.ps1
    Exit: 0 = all checks pass, 1 = some checks failed
#>

$ProjectPath = (Get-Item $PSScriptRoot).Parent.Parent.FullName
$failed = $false

Write-Host "=== Health Verification ===" -ForegroundColor Cyan

# 1. Git status
Write-Host "[1/5] Git status..." -ForegroundColor Yellow
Push-Location $ProjectPath
try {
    $status = git status --porcelain
    if ($status) {
        Write-Host "  WARNING: Uncommitted changes:" -ForegroundColor Yellow
        $status | ForEach-Object { Write-Host "    $_" }
    }
    else {
        Write-Host "  OK: Clean working tree" -ForegroundColor Green
    }
}
finally { Pop-Location }

# 2. Key directories exist
Write-Host "[2/5] Project structure..." -ForegroundColor Yellow
$keyPaths = @(
    "opencode.json",
    "AGENTS.md",
    ".opencode\agents",
    "scripts",
    "skills"
)
foreach ($p in $keyPaths) {
    $full = Join-Path $ProjectPath $p
    if (Test-Path $full) {
        Write-Host "  OK: $p" -ForegroundColor Green
    }
    else {
        Write-Host "  MISSING: $p" -ForegroundColor Red
        $failed = $true
    }
}

# 3. Try compile check
Write-Host "[3/5] Compile check..." -ForegroundColor Yellow
if (Test-Path (Join-Path $ProjectPath "scripts\compile-check.ps1")) {
    & "$ProjectPath\scripts\compile-check.ps1" 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  OK: Compile passes" -ForegroundColor Green
    }
    else {
        Write-Host "  FAIL: Compile errors" -ForegroundColor Red
        $failed = $true
    }
}
else {
    Write-Host "  SKIP: No compile-check script" -ForegroundColor Yellow
}

# 4. Try Lua check
Write-Host "[4/5] Lua check..." -ForegroundColor Yellow
if (Test-Path (Join-Path $ProjectPath "scripts\lint-lua.ps1")) {
    & "$ProjectPath\scripts\lint-lua.ps1" 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  OK: Lua check passes" -ForegroundColor Green
    }
    else {
        Write-Host "  FAIL: Lua errors" -ForegroundColor Red
        $failed = $true
    }
}
else {
    Write-Host "  SKIP: No lint-lua script" -ForegroundColor Yellow
}

# 5. Try Rust build
Write-Host "[5/5] Rust build..." -ForegroundColor Yellow
if (Test-Path (Join-Path $ProjectPath "scripts\build-rust.ps1")) {
    & "$ProjectPath\scripts\build-rust.ps1" 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  OK: Rust build passes" -ForegroundColor Green
    }
    else {
        Write-Host "  FAIL: Rust build errors" -ForegroundColor Red
        $failed = $true
    }
}
else {
    Write-Host "  SKIP: No build-rust script" -ForegroundColor Yellow
}

if ($failed) {
    Write-Host "=== Health: ISSUES FOUND ===" -ForegroundColor Red
    exit 1
}
else {
    Write-Host "=== Health: ALL CHECKS PASSED ===" -ForegroundColor Green
    exit 0
}
