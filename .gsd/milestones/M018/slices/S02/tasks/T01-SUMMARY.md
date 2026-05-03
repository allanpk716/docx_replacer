---
id: T01
parent: S02
milestone: M018
key_files:
  - scripts/e2e-portable-update-test.bat
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-05-03T09:27:46.578Z
blocker_discovered: false
---

# T01: Create e2e-portable-update-test.bat for automated portable update E2E testing against local HTTP server

**Create e2e-portable-update-test.bat for automated portable update E2E testing against local HTTP server**

## What Happened

Created `scripts/e2e-portable-update-test.bat` — a fully automated E2E test script for verifying the portable app self-update flow via a local HTTP server. The script implements 11 phases:

1. **Prerequisites check** — verifies vpk, python, dotnet are available
2. **Version save & directory prep** — captures original csproj version, creates clean e2e-portable-test directory tree
3. **Build v1.0.0** — sets csproj version, publishes, packs with vpk
4. **Build v1.1.0** — same pipeline for the new version
5. **Restore csproj** — reverts to original version immediately after packing
6. **Extract Portable.zip** — extracts v1.0.0 portable zip, verifies DocuFiller.exe and Update.exe exist
7. **Configure update URL** — creates/backs up `%USERPROFILE%\.docx_replacer\update-config.json` pointing to localhost:8081
8. **Start HTTP server** — launches e2e-serve.py serving v1.1.0 artifacts, verifies it responds
9. **Update check (read-only)** — runs `DocuFiller.exe update` without --yes, parses output
10. **Update with --yes** — runs `DocuFiller.exe update --yes` via `start /b` with 30s timeout to handle ApplyUpdatesAndRestart killing the process
11. **Verify & summarize** — checks for version upgrade via file version, download progress, and apply indicators; prints structured PASS/WARN summary

Key design decisions:
- Uses port 8081 (not 8080) to avoid conflict with any running e2e-update-test.bat instance
- Handles ApplyUpdatesAndRestart by running update process in background with `start /b` and polling for process exit
- Comprehensive cleanup: stops HTTP server, restores update-config.json backup, restores csproj version
- All error paths (ERROR_EXIT label) perform cleanup
- Supports `--dry-run` flag for syntax-only validation
- No Chinese characters per project BAT script convention

## Verification

Verified through:
1. `cmd /c scripts\e2e-portable-update-test.bat --dry-run` — exits 0, prints all dry-run phases
2. `grep -c "[E2E-PORTABLE]"` — 108 tagged output lines for easy parsing
3. PASS/FAIL markers present in result summary section (OVERALL: PASS/PARTIAL PASS, E2E PORTABLE UPDATE TEST: PASS/FAIL)
4. No Chinese characters confirmed via Python script
5. All 12 task plan requirements verified present via structural check

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `cmd //c scripts\e2e-portable-update-test.bat --dry-run` | 0 | pass | 3000ms |
| 2 | `python -c "check no Chinese chars"` | 0 | pass | 500ms |
| 3 | `python -c "structural check 12 requirements"` | 0 | pass | 500ms |

## Deviations

Used port 8081 instead of 8080 to avoid conflicts with the existing e2e-update-test.bat which uses 8080. The version verification uses a multi-strategy approach (file version, log parsing, releases.win.json presence) because ApplyUpdatesAndRestart may exit the process before JSONL output is fully flushed.

## Known Issues

ApplyUpdatesAndRestart kills the process, so JSONL output capture may be incomplete. The script handles this gracefully by using a background process with timeout and checking multiple verification signals. The version upgrade verification is best-effort — full verification may require checking the portable directory state after the script completes.

## Files Created/Modified

- `scripts/e2e-portable-update-test.bat`
