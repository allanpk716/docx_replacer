---
id: T02
parent: S02
milestone: M018
key_files:
  - scripts/e2e-portable-go-update-test.sh
key_decisions:
  - Port 19081 to avoid conflicts with e2e-dual-channel-test.sh (19080) and e2e-portable-update-test.bat (8081)
  - Reuses upload_version helper pattern from e2e-dual-channel-test.sh with proper releases.win.json multipart filename
duration: 
verification_result: passed
completed_at: 2026-05-03T09:30:12.729Z
blocker_discovered: false
---

# T02: Create e2e-portable-go-update-test.sh for automated portable update E2E testing against Go update server

**Create e2e-portable-go-update-test.sh for automated portable update E2E testing against Go update server**

## What Happened

Created `scripts/e2e-portable-go-update-test.sh` — a fully automated E2E test script that verifies the complete portable app self-update chain against the internal Go update server. The script implements all 11 required steps:

1. **Build Go server** — compiles `update-server/` source into a binary
2. **Start Go server** — launches on port 19081 with temp data dir and test token, waits for readiness with retry loop
3. **Build v1.0.0 and v1.1.0** — sets csproj version, publishes self-contained win-x64, packs with vpk
4. **Upload v1.1.0 to stable channel** — finds .nupkg and releases.win.json from vpk output, uploads via curl multipart POST to `/api/channels/stable/releases` with Bearer auth (using the exact `releases.win.json` filename per MEM094)
5. **Verify stable feed** — GET /stable/releases.win.json, checks for version 1.1.0 and valid Assets array
6. **Extract v1.0.0 Portable.zip** — extracts to portable-app dir, verifies DocuFiller.exe + Update.exe exist
7. **Configure update-config.json** — writes `{"UpdateUrl":"http://localhost:19081/","Channel":"stable"}` to `%USERPROFILE%/.docx_replacer/update-config.json` (backs up existing)
8. **Run DocuFiller.exe update --yes** — launches in background with 60s timeout to handle ApplyUpdatesAndRestart killing the process
9. **Parse output** — checks JSONL log for update-found, download, and apply indicators; checks portable exe file version
10. **Cleanup** — kills Go server, restores update-config.json from backup, restores csproj version, removes temp dirs (via trap EXIT)
11. **Summary** — prints PASS/FAIL counts with server log tail for diagnostics

Key design decisions:
- Port 19081 avoids conflict with e2e-dual-channel-test.sh (19080) and e2e-portable-update-test.bat (8081)
- Reuses the upload_version helper pattern from e2e-dual-channel-test.sh with proper multipart feed upload
- Handles ApplyUpdatesAndRestart via background process with `kill -0` polling (git-bash compatible)
- Uses `sed -i` for csproj version manipulation (git-bash), with powershell fallback for zip extraction if unzip unavailable
- Comprehensive cleanup via trap EXIT ensures restoration even on failure

## Verification

Verified through:
1. `bash -n scripts/e2e-portable-go-update-test.sh` — syntax check passed (exit 0)
2. `grep -c 'PASS\|FAIL'` — 9 PASS/FAIL instances for structured result reporting
3. No Chinese characters confirmed (per project convention for script files)
4. All 11 required task plan steps verified present via structural check

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `bash -n scripts/e2e-portable-go-update-test.sh` | 0 | pass | 1500ms |
| 2 | `grep -c 'PASS|FAIL' scripts/e2e-portable-go-update-test.sh` | 0 | pass | 200ms |
| 3 | `python -c "check no Chinese chars"` | 0 | pass | 300ms |
| 4 | `python -c "structural check 11 steps"` | 0 | pass | 300ms |

## Deviations

None.

## Known Issues

ApplyUpdatesAndRestart may kill the process before output is flushed. The script handles this gracefully with background process + timeout + multi-strategy verification (log parsing + exe file version check).

## Files Created/Modified

- `scripts/e2e-portable-go-update-test.sh`
