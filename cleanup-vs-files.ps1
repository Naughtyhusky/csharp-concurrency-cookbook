# ========================================
# Clean up .vs folder Git tracking
# ========================================
# Note: Using English to avoid encoding issues

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Cleanup .vs folder Git tracking" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# 1. Check current status
Write-Host "[Step 1] Checking tracked .vs files..." -ForegroundColor Yellow
$trackedFiles = git ls-files .vs
$fileCount = ($trackedFiles | Measure-Object -Line).Lines

if ($fileCount -eq 0) {
    Write-Host "OK: .vs folder is not tracked, no cleanup needed" -ForegroundColor Green
    exit 0
}

Write-Host "WARN: Found $fileCount tracked files:`n" -ForegroundColor Red
$trackedFiles | ForEach-Object { Write-Host "   - $_" -ForegroundColor Gray }

# 2. Ask for confirmation
Write-Host "`n[Step 2] Ready to remove Git tracking..." -ForegroundColor Yellow
Write-Host "NOTE: This will only remove Git tracking, local files will be kept" -ForegroundColor Magenta

$confirmation = Read-Host "`nContinue? (Y/N)"
if ($confirmation -ne 'Y' -and $confirmation -ne 'y') {
    Write-Host "CANCEL: Operation cancelled" -ForegroundColor Red
    exit 1
}

# 3. Remove Git tracking
Write-Host "`n[Step 3] Removing .vs folder from Git tracking..." -ForegroundColor Yellow
git rm -r --cached .vs

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to remove tracking" -ForegroundColor Red
    exit 1
}

Write-Host "OK: Successfully removed Git tracking" -ForegroundColor Green

# 4. Check .gitignore
Write-Host "`n[Step 4] Checking .gitignore configuration..." -ForegroundColor Yellow
$gitignoreContent = Get-Content .gitignore -Raw

if ($gitignoreContent -match '\.vs/') {
    Write-Host "OK: .gitignore contains .vs/ rule" -ForegroundColor Green
} else {
    Write-Host "WARN: .vs/ rule not found in .gitignore" -ForegroundColor Magenta
    Write-Host "Suggestion: Add the following to .gitignore:`n" -ForegroundColor Yellow
    Write-Host ".vs/" -ForegroundColor White
}

# 5. Show current status
Write-Host "`n[Step 5] Current Git status:" -ForegroundColor Yellow
git status --short

# 6. Show next steps
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Cleanup completed!" -ForegroundColor Green
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "Next steps:`n" -ForegroundColor Yellow

Write-Host "1. Commit changes:" -ForegroundColor Cyan
Write-Host '   git commit -m "chore: remove .vs folder from repository"' -ForegroundColor White

Write-Host "`n2. Push to remote:" -ForegroundColor Cyan
Write-Host "   git push origin dev" -ForegroundColor White

Write-Host "`n3. Verify:" -ForegroundColor Cyan
Write-Host "   git ls-files .vs  # should have no output" -ForegroundColor White

Write-Host "`n========================================`n" -ForegroundColor Cyan

# Ask for auto-commit
$autoCommit = Read-Host "Auto-commit changes? (Y/N)"
if ($autoCommit -eq 'Y' -or $autoCommit -eq 'y') {
    Write-Host "`nCommitting..." -ForegroundColor Yellow

    git commit -m "chore: remove .vs folder from repository

- Remove Visual Studio personal config files
- These files should not be version controlled
- .gitignore is properly configured"

    if ($LASTEXITCODE -eq 0) {
        Write-Host "OK: Commit successful" -ForegroundColor Green

        $autoPush = Read-Host "`nPush to remote? (Y/N)"
        if ($autoPush -eq 'Y' -or $autoPush -eq 'y') {
            Write-Host "`nPushing..." -ForegroundColor Yellow
            git push origin dev

            if ($LASTEXITCODE -eq 0) {
                Write-Host "OK: Push successful!" -ForegroundColor Green

                # Final verification
                Write-Host "`nFinal verification:" -ForegroundColor Yellow
                $finalCheck = git ls-files .vs
                if ($finalCheck) {
                    Write-Host "WARN: Some files still tracked" -ForegroundColor Red
                    $finalCheck
                } else {
                    Write-Host "OK: .vs folder completely removed from tracking" -ForegroundColor Green
                }
            } else {
                Write-Host "ERROR: Push failed" -ForegroundColor Red
            }
        }
    } else {
        Write-Host "ERROR: Commit failed" -ForegroundColor Red
    }
}

Write-Host "`nScript completed!" -ForegroundColor Cyan
