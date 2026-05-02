---
id: T01
parent: S03
milestone: M008-4uyz6m
key_files:
  - scripts/build-internal.bat
  - scripts/build.bat
key_decisions:
  - Used curl -s -o nul -w %%{http_code} pattern to capture HTTP status while suppressing output, consistent with BAT scripting constraints
  - Gated upload behind `if defined CHANNEL` for full backward compatibility when channel is omitted
  - Upload stops on first failure (exit /b 1) rather than attempting remaining files — fail-fast to surface issues immediately
duration: 
verification_result: passed
completed_at: 2026-04-25T00:00:34.580Z
blocker_discovered: false
---

# T01: Add optional channel parameter (stable/beta) to build-internal.bat with auto-upload to Go update server after Velopack packaging

**Add optional channel parameter (stable/beta) to build-internal.bat with auto-upload to Go update server after Velopack packaging**

## What Happened

Modified build-internal.bat to accept an optional second parameter CHANNEL (stable/beta). Added channel validation that rejects any value other than stable/beta. After VPK_PACK completes successfully, if CHANNEL is defined, the new :UPLOAD subroutine is called. The upload subroutine checks for UPDATE_SERVER_URL and UPDATE_SERVER_TOKEN environment variables, uploads releases.win.json and all .nupkg files via curl multipart POST to {UPDATE_SERVER_URL}/api/channels/{CHANNEL}/releases with Bearer auth. Each upload reports HTTP status with [UPLOAD] tagged messages following the existing script convention. If CHANNEL is omitted, the upload step is skipped entirely (backward compatible). Also updated build.bat to accept and pass the channel parameter, and expanded help text to document the channel option and required environment variables. No Chinese characters in either BAT file.

## Verification

Ran grep verification checks: UPLOAD count=28 (>=3), CHANNEL count=10 (>=3), UPDATE_SERVER_URL found, UPDATE_SERVER_TOKEN found. Verified no Chinese characters in either file. Confirmed :UPLOAD subroutine exists at line 169 and is called from main flow at line 72. Verified build.bat passes %CHANNEL% to build-internal.bat. Backward compatibility preserved: when no channel is provided, upload section is gated by `if defined CHANNEL` and completely skipped.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `grep -c "UPLOAD" scripts/build-internal.bat` | 0 | ✅ pass (28 >= 3) | 500ms |
| 2 | `grep -c "CHANNEL" scripts/build-internal.bat` | 0 | ✅ pass (10 >= 3) | 500ms |
| 3 | `grep -q "UPDATE_SERVER_URL" scripts/build-internal.bat` | 0 | ✅ pass | 500ms |
| 4 | `grep -q "UPDATE_SERVER_TOKEN" scripts/build-internal.bat` | 0 | ✅ pass | 500ms |
| 5 | `grep -Pc "[\x{4e00}-\x{9fff}]" scripts/build-internal.bat` | 0 | ✅ pass (0 Chinese chars) | 500ms |
| 6 | `grep -Pc "[\x{4e00}-\x{9fff}]" scripts/build.bat` | 0 | ✅ pass (0 Chinese chars) | 500ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `scripts/build-internal.bat`
- `scripts/build.bat`
