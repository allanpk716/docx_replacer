---
id: T01
parent: S04
milestone: M008-4uyz6m
key_files:
  - scripts/e2e-dual-channel-test.sh
key_decisions:
  - Used releases.win.json as the temp feed filename so curl sends the correct multipart filename — the Go upload handler checks fh.Filename == "releases.win.json" to trigger feed merging
duration: 
verification_result: passed
completed_at: 2026-04-25T00:53:10.646Z
blocker_discovered: false
---

# T01: Create e2e dual-channel integration test script proving full beta→promote→stable flow

**Create e2e dual-channel integration test script proving full beta→promote→stable flow**

## What Happened

Created `scripts/e2e-dual-channel-test.sh` that verifies the complete cross-component integration between the Go update server and the C# UpdateService URL construction pattern.

The script builds the Go server from source, starts it on port 19080 with a temp data directory, then performs 8 sequential verification steps:

1. **Build** — Compiles Go server binary from source (`go build`)
2. **Start** — Launches server with temp data dir and test auth token, waits for readiness
3. **Upload to beta** — POSTs a test release (releases.win.json + .nupkg) to beta channel with Bearer auth
4. **Verify beta URL pattern** — GET /beta/releases.win.json returns valid JSON containing the version; this directly proves the UpdateService URL pattern (`UpdateUrl.TrimEnd('/') + "/" + channel + "/"`) resolves correctly through the Go server's static handler. Also verifies the .nupkg file is served statically.
5. **Channel isolation** — Confirms stable feed does NOT contain the beta-only version
6. **Promote** — Promotes version from beta to stable via POST /api/channels/stable/promote?from=beta&version=X
7. **Verify stable** — Confirms GET /stable/releases.win.json now contains the promoted version and its .nupkg is served
8. **Auto-cleanup** — Uploads 11 versions total to beta and verifies the oldest is removed (DefaultMaxKeep=10)

All 13 assertions pass: 0 failures. Script uses `[E2E]` prefix logging, captures server log to temp file for post-mortem, and produces structured PASS/FAIL summary.

Key implementation detail: the multipart upload must use the filename `releases.win.json` (not an arbitrary name) because the Go handler checks `fh.Filename == "releases.win.json"` to trigger feed merging rather than storing as a generic file.

## Verification

Ran `bash scripts/e2e-dual-channel-test.sh` — all 13 checks passed, 0 failed. The script builds the Go binary, starts the server, runs through all 8 test steps (upload, feed verification, channel isolation, promote, cleanup), and cleans up. Exit code 0.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `bash scripts/e2e-dual-channel-test.sh` | 0 | ✅ pass | 15000ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `scripts/e2e-dual-channel-test.sh`
