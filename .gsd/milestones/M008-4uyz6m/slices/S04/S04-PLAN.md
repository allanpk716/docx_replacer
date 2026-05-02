# S04: 端到端验证

**Goal:** Verify the complete dual-channel update flow end-to-end: Go server running → upload to beta → client URL resolves to beta feed → promote to stable → stable client URL resolves to stable feed. All existing tests (168+) pass with zero regressions.
**Demo:** 完整流程：Go 服务器运行 → 上传 beta → 客户端检测到更新 → promote stable → stable 客户端检测到更新

## Must-Haves

- Not provided.

## Proof Level

- This slice proves: final-assembly

## Integration Closure

- Upstream surfaces consumed: Go server static file handler (/{channel}/releases.win.json), upload API (POST /api/channels/{channel}/releases), promote API (POST /api/channels/{target}/promote), UpdateService URL construction ({UpdateUrl}/{Channel}/)
- New wiring introduced: e2e dual-channel test script exercises all 3 slices' outputs together
- What remains before milestone is truly usable end-to-end: nothing — this is the final verification slice

## Verification

- e2e script logs every step with [E2E] prefix and reports structured PASS/FAIL counts
- Server log captured to temp file for post-mortem on failure
- Test results from all 3 suites (Go unit, Go e2e, .NET) reported with counts

## Tasks

- [x] **T01: Write e2e dual-channel integration test script** `est:1h`
  Create `scripts/e2e-dual-channel-test.sh` that verifies the complete cross-component integration between the Go update server and the C# client's URL construction pattern.

The script must prove:
1. Build Go server binary from source
2. Start server on a high port with temp data dir and test token
3. Upload a test release (releases.win.json + .nupkg) to the beta channel via curl POST with Bearer auth
4. Verify GET /beta/releases.win.json returns valid JSON containing the uploaded version — this proves the URL pattern UpdateService constructs (`{UpdateUrl}/beta/`) resolves correctly
5. Verify stable feed does NOT contain the beta version (channel isolation)
6. Promote the version from beta to stable via POST /api/channels/stable/promote?from=beta&version=X
7. Verify GET /stable/releases.win.json now contains the promoted version — this proves a stable-channel UpdateService would detect the update
8. Upload 11 versions to beta and verify auto-cleanup removes the oldest
9. Clean up (kill server, remove temp dir)

Script must produce structured PASS/FAIL output like the existing test-update-server.sh.
  - Files: `scripts/e2e-dual-channel-test.sh`, `update-server/main.go`, `update-server/handler/static.go`, `update-server/handler/upload.go`, `update-server/handler/promote.go`, `Services/UpdateService.cs`
  - Verify: cd C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M008-4uyz6m && bash scripts/e2e-dual-channel-test.sh

- [x] **T02: Execute full test suite and verify zero regressions** `est:30m`
  Run the complete test suite across all components to verify zero regressions:

1. Go server tests: `cd update-server && go test ./... -v -count=1` — expect 50 PASS
2. Go server build: `cd update-server && go build -o bin/update-server.exe .` — expect exit 0
3. Execute the e2e dual-channel script: `bash scripts/e2e-dual-channel-test.sh` — expect all tests PASS
4. .NET build: `dotnet build` — expect 0 errors
5. .NET tests: `dotnet test` — expect 168+ PASS (141 unit + 27 E2E)
6. Verify the test count hasn't dropped: grep for total test count in output

If any suite fails, report the specific failure and exit with non-zero code.

This task validates R036: the complete dual-channel flow works end-to-end and all existing tests pass.
  - Files: `update-server/`, `Tests/`, `scripts/e2e-dual-channel-test.sh`
  - Verify: cd C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M008-4uyz6m && go test ./update-server/... -count=1 && dotnet build && dotnet test && bash scripts/e2e-dual-channel-test.sh

## Files Likely Touched

- scripts/e2e-dual-channel-test.sh
- update-server/main.go
- update-server/handler/static.go
- update-server/handler/upload.go
- update-server/handler/promote.go
- Services/UpdateService.cs
- update-server/
- Tests/
