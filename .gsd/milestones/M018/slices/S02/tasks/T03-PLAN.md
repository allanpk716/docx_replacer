---
estimated_steps: 7
estimated_files: 3
skills_used: []
---

# T03: Update E2E test guide and validate script syntax

Update `docs/plans/e2e-update-test-guide.md` to add portable-specific E2E test sections:
1. Add 'Portable Update via Local HTTP' section referencing e2e-portable-update-test.bat
2. Add 'Portable Update via Go Server' section referencing e2e-portable-go-update-test.sh
3. Add prerequisites section for portable testing (portable extraction, update-config.json setup)
4. Update troubleshooting table for portable-specific issues
5. Validate both scripts parse correctly (bash -n for .sh, cmd /c echo check for .bat)
6. Run dotnet build to confirm no production code was modified

## Inputs

- `docs/plans/e2e-update-test-guide.md`
- `scripts/e2e-portable-update-test.bat`
- `scripts/e2e-portable-go-update-test.sh`

## Expected Output

- `docs/plans/e2e-update-test-guide.md`

## Verification

bash -n scripts/e2e-portable-go-update-test.sh
grep -c 'Portable' docs/plans/e2e-update-test-guide.md
dotnet build --no-restore -v q 2>&1 | tail -5
