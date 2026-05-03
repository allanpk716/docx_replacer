---
estimated_steps: 19
estimated_files: 3
skills_used: []
---

# T02: Create Go server E2E portable update test script

Create `scripts/e2e-portable-go-update-test.sh` that automates the full portable update verification against the internal Go update server.

The script should:
1. Build Go server binary from update-server/ source
2. Start server on a high port with temp data dir and test token
3. Build v1.0.0 and v1.1.0 from C# source, pack with vpk
4. Upload v1.1.0 artifacts to Go server's stable channel via curl POST
5. Verify GET /stable/releases.win.json contains the version
6. Extract v1.0.0 Portable.zip
7. Create update-config.json pointing to the Go server
8. Run `DocuFiller.exe update --yes` from the portable directory
9. Parse JSONL output to verify update succeeded
10. Clean up (kill server, remove temp dirs, restore csproj version)
11. Print PASS/FAIL summary

Key constraints:
- Script must run in git-bash on Windows
- Use the upload API pattern from e2e-dual-channel-test.sh as reference
- Go server serves from /{channel}/ subdirectories
- The update-config.json must use the Go server's URL (http://localhost:{port}/)
- Handle the fact that ApplyUpdatesAndRestart may cause the process to not return gracefully

## Inputs

- `scripts/e2e-dual-channel-test.sh`
- `update-server/main.go`
- `scripts/e2e-serve.py`
- `Services/UpdateService.cs`

## Expected Output

- `scripts/e2e-portable-go-update-test.sh`

## Verification

bash -n scripts/e2e-portable-go-update-test.sh && echo 'Syntax OK' || echo 'Syntax error'
grep -c 'PASS\|FAIL' scripts/e2e-portable-go-update-test.sh
