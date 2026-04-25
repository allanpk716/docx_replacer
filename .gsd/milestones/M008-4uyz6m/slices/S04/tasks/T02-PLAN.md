---
estimated_steps: 9
estimated_files: 3
skills_used: []
---

# T02: Execute full test suite and verify zero regressions

Run the complete test suite across all components to verify zero regressions:

1. Go server tests: `cd update-server && go test ./... -v -count=1` — expect 50 PASS
2. Go server build: `cd update-server && go build -o bin/update-server.exe .` — expect exit 0
3. Execute the e2e dual-channel script: `bash scripts/e2e-dual-channel-test.sh` — expect all tests PASS
4. .NET build: `dotnet build` — expect 0 errors
5. .NET tests: `dotnet test` — expect 168+ PASS (141 unit + 27 E2E)
6. Verify the test count hasn't dropped: grep for total test count in output

If any suite fails, report the specific failure and exit with non-zero code.

This task validates R036: the complete dual-channel flow works end-to-end and all existing tests pass.

## Inputs

- `scripts/e2e-dual-channel-test.sh`
- `update-server/handler/handler_test.go`
- `update-server/storage/store_test.go`
- `update-server/storage/cleanup_test.go`
- `update-server/handler/upload_test.go`
- `Tests/UpdateServiceTests.cs`

## Expected Output

- `scripts/e2e-dual-channel-test.sh`

## Verification

cd C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M008-4uyz6m && go test ./update-server/... -count=1 && dotnet build && dotnet test && bash scripts/e2e-dual-channel-test.sh
