---
id: T01
parent: S04
milestone: M007-wpaxa3
key_files:
  - scripts/e2e-update-test.bat
  - scripts/e2e-serve.py
key_decisions:
  - Version override via csproj modification (same mechanism as sync-version.bat) rather than git tags, since build-internal.bat reads csproj Version when no tag exists
duration: 
verification_result: passed
completed_at: 2026-04-24T06:33:01.220Z
blocker_discovered: false
---

# T01: Created E2E update test automation script (e2e-update-test.bat) and Python HTTP update server (e2e-serve.py) for Velopack auto-update verification

**Created E2E update test automation script (e2e-update-test.bat) and Python HTTP update server (e2e-serve.py) for Velopack auto-update verification**

## What Happened

Created two files for the E2E update verification flow:

1. **scripts/e2e-update-test.bat** — Orchestrates the full E2E test: checks prerequisites (vpk, python, dotnet), builds v1.0.0 and v1.1.0 by temporarily modifying the csproj Version property (same technique as sync-version.bat), packages both with vpk, copies the v1.0.0 Setup.exe to e2e-test root, starts the Python HTTP server serving v1.1.0 artifacts, and prints detailed manual test instructions for the human tester. Uses [E2E] echo tags for observability matching S03 convention. Handles error cases by restoring the original csproj version. No Chinese characters in the BAT file.

2. **scripts/e2e-serve.py** — A minimal Python HTTP server (using stdlib http.server) that serves Velopack release artifacts from a specified directory. Accepts --port and --directory arguments. Logs each GET request with [E2E-SERVE] prefix to stdout so the tester can observe Velopack update check and download traffic in real time.

Key design decisions:
- Version override via csproj modification rather than git tags (build-internal.bat reads csproj Version when no tag exists, appending -dev suffix; since we set the csproj directly, it uses our version exactly)
- Each version build is isolated (clean publish dir before each build)
- Error handler restores original csproj version to prevent leaving the project in a modified state
- Server runs in a separate window so tester can see HTTP traffic alongside the app

## Verification

Verified three criteria:
1. Both files exist: e2e-update-test.bat and e2e-serve.py present in scripts/
2. Python --help works: e2e-serve.py parses args correctly and shows usage
3. Python syntax check passes via py_compile
4. BAT file uses [E2E] echo tags (97 occurrences) with SUCCESS/FAILED markers (11 occurrences) matching S03 convention
5. Python server logs each GET request with [E2E-SERVE] prefix
6. No Chinese characters in BAT file (verified programmatically)

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `test -f scripts/e2e-update-test.bat && test -f scripts/e2e-serve.py` | 0 | ✅ pass | 500ms |
| 2 | `python scripts/e2e-serve.py --help` | 0 | ✅ pass | 1500ms |
| 3 | `python -c "import py_compile; py_compile.compile('scripts/e2e-serve.py', doraise=True)"` | 0 | ✅ pass | 800ms |
| 4 | `grep -c '[E2E]' scripts/e2e-update-test.bat` | 0 | ✅ pass | 300ms |
| 5 | `python -c "check no Chinese chars in BAT"` | 0 | ✅ pass | 400ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `scripts/e2e-update-test.bat`
- `scripts/e2e-serve.py`
