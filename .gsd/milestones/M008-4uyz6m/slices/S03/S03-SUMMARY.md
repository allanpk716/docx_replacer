---
id: S03
parent: M008-4uyz6m
milestone: M008-4uyz6m
provides:
  - ["build-internal.bat channel parameter (stable/beta) with auto-upload via curl", "build.bat channel pass-through", "[UPLOAD] tagged console output for upload progress tracking"]
requires:
  - slice: S01
    provides: POST /api/channels/{channel}/releases upload endpoint, Bearer token authentication
affects:
  - ["S04"]
key_files:
  - ["scripts/build-internal.bat", "scripts/build.bat"]
key_decisions:
  - ["Gated upload behind 'if defined CHANNEL' for full backward compatibility", "Fail-fast upload strategy (exit /b 1 on first failure) to surface issues immediately", "Used curl -s -o nul -w %%{http_code} pattern for HTTP status capture in BAT context"]
patterns_established:
  - (none)
observability_surfaces:
  - none
drill_down_paths:
  - [".gsd/milestones/M008-4uyz6m/slices/S03/tasks/T01-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-04-25T00:43:42.422Z
blocker_discovered: false
---

# S03: 发布脚本改造

**build-internal.bat now accepts optional channel parameter (stable/beta) with auto-upload of Velopack artifacts to Go update server via curl**

## What Happened

Modified build-internal.bat to accept an optional second parameter CHANNEL (stable/beta). The script validates the channel value and, after successful VPK_PACK, calls a new :UPLOAD subroutine that checks for UPDATE_SERVER_URL and UPDATE_SERVER_TOKEN environment variables, then uploads releases.win.json and all .nupkg files via curl multipart POST to the Go update server. Each upload reports HTTP status with [UPLOAD] tagged messages. Upload is fail-fast — stops on first failure with a descriptive error. When CHANNEL is omitted, the upload step is entirely skipped, preserving full backward compatibility. Also updated build.bat to accept and pass the channel parameter, with expanded help text documenting the channel option and required environment variables.

## Verification

All slice-level verification checks passed:
- grep -c "UPLOAD" scripts/build-internal.bat → 28 (≥ 3) ✅
- grep -c "CHANNEL" scripts/build-internal.bat → 10 (≥ 3) ✅
- grep "UPDATE_SERVER_URL" found ✅
- grep "UPDATE_SERVER_TOKEN" found ✅
- :UPLOAD subroutine at line 169, called from main flow at line 72 ✅
- No Chinese characters in either BAT file ✅
- dotnet build: 0 errors, 92 warnings (pre-existing) ✅

## Requirements Advanced

- R035 — build-internal.bat now supports channel parameter with auto-upload to Go update server, fulfilling the requirement for one-command build+publish automation

## Requirements Validated

- R035 — grep verification confirms channel param parsing, upload subroutine, env var checks; dotnet build 0 errors

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

None.

## Known Limitations

None.

## Follow-ups

None.

## Files Created/Modified

None.
