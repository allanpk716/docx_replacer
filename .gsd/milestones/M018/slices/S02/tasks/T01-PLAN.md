---
estimated_steps: 19
estimated_files: 2
skills_used: []
---

# T01: Create local HTTP E2E portable update test script

Create `scripts/e2e-portable-update-test.bat` that automates the full portable update verification against a local HTTP server (e2e-serve.py).

The script should:
1. Check prerequisites (vpk, python, dotnet)
2. Build v1.0.0 and v1.1.0 from source, pack with vpk
3. Extract v1.0.0 Portable.zip to a test directory
4. Start e2e-serve.py serving v1.1.0 artifacts
5. Configure the portable app's update URL by creating update-config.json in %USERPROFILE%\.docx_replacer\
6. Run `DocuFiller.exe update --yes` from the portable directory
7. Parse JSONL output to verify update check succeeded (found new version, download progress, apply)
8. After update completes, verify the version upgraded by checking the running executable or JSONL output
9. Restore original csproj version, clean up
10. Print PASS/FAIL summary

Key constraints:
- BAT script files must not contain Chinese characters
- Use e2e-serve.py from scripts/ directory
- Restore csproj version on both success and failure paths
- Handle cleanup of background HTTP server process
- The portable update applies via Velopack Update.exe — after `update --yes`, the process may exit/restart, so the script must handle this
- If ApplyUpdatesAndRestart causes the process to not return, use a timeout and check the result from the output captured so far

## Inputs

- `scripts/e2e-update-test.bat`
- `scripts/e2e-serve.py`
- `Services/UpdateService.cs`
- `Cli/Commands/UpdateCommand.cs`

## Expected Output

- `scripts/e2e-portable-update-test.bat`

## Verification

cmd /c scripts\e2e-portable-update-test.bat --dry-run 2>&1 || echo 'dry-run mode: validate script syntax only'
findstr /n /c:"[E2E-PORTABLE]" scripts/e2e-portable-update-test.bat | findstr /c:"PASS" /c:"FAIL"
