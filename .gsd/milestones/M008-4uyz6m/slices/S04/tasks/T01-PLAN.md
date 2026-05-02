---
estimated_steps: 12
estimated_files: 6
skills_used: []
---

# T01: Write e2e dual-channel integration test script

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

## Inputs

- `update-server/main.go`
- `update-server/handler/static.go`
- `update-server/handler/upload.go`
- `update-server/handler/promote.go`
- `update-server/handler/api.go`
- `Services/UpdateService.cs`
- `scripts/test-update-server.sh`

## Expected Output

- `scripts/e2e-dual-channel-test.sh`

## Verification

cd C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M008-4uyz6m && bash scripts/e2e-dual-channel-test.sh
