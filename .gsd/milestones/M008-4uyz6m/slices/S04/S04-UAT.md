# S04: 端到端验证 — UAT

**Milestone:** M008-4uyz6m
**Written:** 2026-04-25T00:56:13.621Z

# S04: 端到端验证 — UAT

**Milestone:** M008-4uyz6m
**Written:** 2026-04-25

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: This slice is a verification slice — its entire output is test scripts and test results. No runtime UI or human experience to test. The e2e script IS the UAT.

## Preconditions

- Go 1.21+ installed and available on PATH
- .NET 8 SDK installed
- Network port 19080 available (used by e2e script)
- curl available on PATH
- Working directory is the repo root

## Smoke Test

```bash
bash scripts/e2e-dual-channel-test.sh
```
Expected: "Results: 13 passed, 0 failed" and exit code 0.

## Test Cases

### 1. Go Server Unit Tests

1. Run `cd update-server && go test ./... -v -count=1`
2. **Expected:** 42 tests PASS, 0 failures. Covers handler package (upload, promote, list, static, auth) and storage package (CRUD, cleanup).

### 2. Go Server Build

1. Run `cd update-server && go build -o bin/update-server.exe .`
2. **Expected:** Exit code 0, binary produced at `update-server/bin/update-server.exe`.

### 3. E2E Dual-Channel Flow

1. Run `bash scripts/e2e-dual-channel-test.sh`
2. Script builds Go server, starts on port 19080 with temp data dir
3. Uploads test release to beta channel via multipart POST with Bearer auth
4. **Expected Step 4:** GET /beta/releases.win.json returns valid JSON containing version 1.0.0 — proves UpdateService URL pattern works
5. **Expected Step 5:** GET /stable/releases.win.json does NOT contain 1.0.0 — channel isolation
6. Promotes 1.0.0 from beta to stable
7. **Expected Step 7:** GET /stable/releases.win.json now contains 1.0.0 and .nupkg served
8. Uploads 11 versions to beta
9. **Expected Step 8:** Auto-cleanup removes oldest version (1.0.0), newest (1.0.10) retained
10. **Expected:** "Results: 13 passed, 0 failed", server killed, temp dir cleaned up

### 4. .NET Build

1. Run `dotnet build`
2. **Expected:** 0 errors (warnings acceptable — pre-existing nullable reference warnings)

### 5. .NET Tests

1. Run `dotnet test`
2. **Expected:** 168 total tests pass (141 DocuFiller.Tests + 27 E2ERegression), 0 failures, 0 skipped

## Edge Cases

### Go Test Path Issue

1. Running `go test ./update-server/...` from repo root
2. **Expected:** Fails with "directory prefix does not contain main module" — this is expected behavior, tests must be run from inside `update-server/` directory

## Failure Signals

- e2e script reports any "FAIL" lines in output
- Go tests report non-zero exit code
- .NET tests report failures in console output
- Server fails to start (port conflict or build error)

## Not Proven By This UAT

- Actual Velopack client update flow (requires running app with Velopack runtime)
- Build script integration with real .nupkg production packages
- HTTPS/TLS (e2e uses HTTP on localhost)
- Authentication with production token (e2e uses test token)
- Concurrent access or load testing

## Notes for Tester

- The e2e script creates a temp directory under /tmp and cleans up after itself
- If the script is interrupted, manually kill any lingering update-server processes
- Server log is captured to a temp file path printed at script start — check this on failure
- .NET test count of 168 is the expected baseline (141 unit + 27 E2E)
