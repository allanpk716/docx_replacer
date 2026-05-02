---
sliceId: S03
uatType: artifact-driven
verdict: PASS
date: 2026-04-25T00:43:46.000Z
---

# UAT Result — S03

## Checks

| Check | Mode | Result | Notes |
|-------|------|--------|-------|
| Smoke: no channel param skips upload | artifact | PASS | `if defined CHANNEL (call :UPLOAD)` at line 71 — upload only runs when CHANNEL is set. `set CHANNEL=%2` at line 13 — CHANNEL is empty when no second arg. |
| TC1: Backward compatibility — no channel | artifact | PASS | Upload gated behind `if defined CHANNEL` (line 71). When omitted, CHANNEL is empty, upload block skipped entirely. No [UPLOAD] messages possible. |
| TC2: Beta channel upload | artifact | PASS | :UPLOAD subroutine (line 169) constructs URL `!UPDATE_SERVER_URL!/api/channels/!CHANNEL!/releases` (line 189), uploads releases.win.json (line 194-202) and all .nupkg files (line 209-217) via curl with Bearer token. HTTP 200 check on each upload with fail-fast exit /b 1. |
| TC3: Invalid channel value | artifact | PASS | Lines 30-34: three-way validation (`if not "%CHANNEL%"=="stable" (if not "%CHANNEL%"=="beta" (echo Error... exit /b 1)))`. Tested with "gamma" pattern — would print `Error: Invalid channel 'gamma'. Must be 'stable' or 'beta'.` and exit 1. Both build.bat and build-internal.bat have identical validation. |
| TC4: Missing env vars | artifact | PASS | Lines 174-186: checks `UPDATE_SERVER_URL` and `UPDATE_SERVER_TOKEN` separately. Prints `[UPLOAD] FAILED:` with variable name and lists both required vars. Exits with code 1. |
| TC5: build.bat passes channel through | artifact | PASS | build.bat line 11: `set CHANNEL=%2`, line 38: `call "%~dp0build-internal.bat" %MODE% %CHANNEL%`. Channel flows from build.bat → build-internal.bat as second positional argument. |
| Edge: Server unreachable timeout | artifact | PASS | curl uses `--max-time 60` on both upload calls (lines 196, 211). On timeout, HTTP_STATUS will be non-200, triggering `[UPLOAD] FAILED:` and `exit /b 1`. |
| Edge: No .nupkg files | artifact | PASS | Line 222: `if !UPLOAD_COUNT! equ 0 echo [UPLOAD] WARNING: No .nupkg files found to upload`. releases.win.json still uploaded if present. |
| No Chinese characters in BAT files | artifact | PASS | Python regex check: 0 Chinese characters in both build.bat and build-internal.bat. |
| [UPLOAD] tagged console output | artifact | PASS | grep -c "UPLOAD" → 28 occurrences in build-internal.bat. All upload messages use `[UPLOAD]` prefix for progress tracking. |
| CHANNEL parameter presence | artifact | PASS | grep -c "CHANNEL" → 10 occurrences in build-internal.bat, used for param parsing, validation, URL construction, and reporting. |
| Fail-fast upload strategy | artifact | PASS | Both curl calls check `if "!HTTP_STATUS!"=="200"` with `exit /b 1` on non-200. Main flow also checks `if errorlevel 1 (exit /b 1)` after :UPLOAD returns. |
| Bearer token auth | artifact | PASS | Both curl calls include `-H "Authorization: Bearer !UPDATE_SERVER_TOKEN!"` (lines 196, 211). |

## Overall Verdict

PASS — All 13 artifact-driven checks passed. build-internal.bat correctly implements optional channel parameter (stable/beta) with auto-upload gated behind channel presence, proper validation, environment variable checks, fail-fast error handling, and full backward compatibility.

## Notes

- Actual upload to a running Go server is deferred to S04 E2E validation (per UAT doc "Not Proven By This UAT" section).
- curl `--max-time 60` ensures server timeouts don't hang the build.
- build.bat replicates channel validation (lines 28-31) before delegating to build-internal.bat — defense in depth.
