# S03: 发布脚本改造 — UAT

**Milestone:** M008-4uyz6m
**Written:** 2026-04-25T00:43:42.423Z

# S03: 发布脚本改造 — UAT

**Milestone:** M008-4uyz6m
**Written:** 2026-04-25

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: The build script is a BAT file — its behavior is fully inspectable via source code review and grep checks. Real upload requires a running Go server (S01 dependency), which is deferred to S04 E2E validation.

## Preconditions

- Go update server from S01 must be running (for actual upload tests)
- UPDATE_SERVER_URL and UPDATE_SERVER_TOKEN environment variables must be set (for upload tests)
- Velopack CLI (vpk) must be available on PATH
- .NET 8 SDK must be installed

## Smoke Test

1. Run `build-internal.bat standalone` (no channel) — build should complete without attempting upload, no errors.

## Test Cases

### 1. Backward compatibility — no channel parameter

1. Run `build-internal.bat standalone`
2. Build and pack phases complete normally
3. **Expected:** No [UPLOAD] messages appear. Build succeeds with exit code 0.

### 2. Beta channel upload

1. Set UPDATE_SERVER_URL=http://localhost:8080 and UPDATE_SERVER_TOKEN=<valid-token>
2. Start Go update server
3. Run `build-internal.bat standalone beta`
4. **Expected:** After VPK_PACK, [UPLOAD] messages appear showing upload of releases.win.json and .nupkg files. HTTP 200 status for each. Exit code 0.

### 3. Invalid channel value

1. Run `build-internal.bat standalone gamma`
2. **Expected:** Script prints error about invalid channel value and exits with code 1.

### 4. Missing environment variables

1. Run `build-internal.bat standalone beta` without setting UPDATE_SERVER_URL
2. **Expected:** [UPLOAD] error message listing required environment variables (UPDATE_SERVER_URL, UPDATE_SERVER_TOKEN) and exits with code 1.

### 5. build.bat passes channel through

1. Run `build.bat standalone beta`
2. **Expected:** Channel is passed to build-internal.bat and upload flow executes (if env vars are set).

## Edge Cases

### Server unreachable during upload

1. Set UPDATE_SERVER_URL to an unreachable address
2. Run `build-internal.bat standalone beta`
3. **Expected:** curl times out (--max-time 60), [UPLOAD] FAILED message with error, exit code 1.

### No .nupkg files produced

1. If VPK_PACK produces no output files (unlikely but possible with build errors)
2. **Expected:** Upload subroutine attempts to upload but finds no files; behavior depends on glob pattern handling.

## Failure Signals

- [UPLOAD] FAILED messages with HTTP status code in console output
- Exit code 1 from build-internal.bat
- Missing [UPLOAD] messages when channel is specified (indicates upload was skipped unexpectedly)

## Not Proven By This UAT

- Actual successful upload to a running Go server (requires S01 server, validated in S04)
- End-to-end flow of build → upload → client update (S04 scope)
- Promote workflow (S01 scope)

## Notes for Tester

- The upload uses curl with --max-time 60, so a server timeout takes up to 60 seconds
- Chinese characters must NOT appear in BAT files — verify with grep -Pc '[\x{4e00}-\x{9fff}]' if editing
- The script uses Windows BAT syntax: -o nul (not /dev/null), %%{http_code} (double percent)
