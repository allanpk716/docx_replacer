# S01: S01 — UAT

**Milestone:** M008-4uyz6m
**Written:** 2026-04-24T23:39:17.931Z

# UAT: S01 — Go Update Server

## Preconditions
- Go 1.22+ installed
- Built binary: `cd update-server && go build -o bin/update-server.exe .`
- Terminal access (git-bash or CMD)

## Test Cases

### TC01: Server startup and static file serving
1. Start server: `update-server/bin/update-server.exe -port 18090 -data-dir ./test-data -token test-secret`
2. Expected: Server starts, logs `{"msg":"Server listening","port":18090}`
3. `curl -s http://localhost:18090/beta/releases.win.json` → 404 (no files uploaded yet)
4. `curl -s http://localhost:18090/invalid/file.txt` → 404 (invalid channel)

### TC02: Upload API with auth
1. Create a test releases.win.json and a dummy .nupkg file
2. Upload without token: `curl -s -X POST -F "files=@releases.win.json" http://localhost:18090/api/channels/beta/releases` → 401
3. Upload with bad token: `curl -s -X POST -H "Authorization: Bearer wrong" -F "files=@releases.win.json" http://localhost:18090/api/channels/beta/releases` → 401
4. Upload with correct token: `curl -s -X POST -H "Authorization: Bearer test-secret" -F "files=@releases.win.json" -F "files=@dummy.nupkg" http://localhost:18090/api/channels/beta/releases` → 200, JSON response with channel and files_received

### TC03: Static file download after upload
1. `curl -s http://localhost:18090/beta/releases.win.json` → 200, valid JSON with uploaded assets
2. `curl -s -o /dev/null -w "%{http_code}" http://localhost:18090/beta/Dummy.1.0.0.nupkg` → 200

### TC04: Version list API
1. `curl -s http://localhost:18090/api/channels/beta/releases` → 200, JSON with versions array, each version has files, total_size, file_count

### TC05: Promote API
1. `curl -s -X POST "http://localhost:18090/api/channels/stable/promote?from=beta&version=1.0.0" -H "Authorization: Bearer test-secret"` → 200, promoted version response
2. `curl -s http://localhost:18090/stable/releases.win.json` → 200, JSON with promoted assets
3. `curl -s http://localhost:18090/api/channels/stable/releases` → 200, lists promoted version

### TC06: Promote non-existent version
1. `curl -s -X POST "http://localhost:18090/api/channels/stable/promote?from=beta&version=99.0.0" -H "Authorization: Bearer test-secret"` → 404

### TC07: Auto-cleanup (10 versions max)
1. Upload 11 different versions to beta channel (version 1.0.0 through 1.0.10)
2. `curl -s http://localhost:18090/api/channels/beta/releases` → should list exactly 10 versions (1.1.0 through 1.0.1)
3. Version 1.0.0 should be removed (oldest)
4. Server logs should show cleanup event with removed_versions

### TC08: Cross-channel isolation
1. Upload different version to beta after TC07
2. `curl -s http://localhost:18090/api/channels/beta/releases` → shows beta-only versions
3. `curl -s http://localhost:18090/api/channels/stable/releases` → shows only promoted stable version

## Cleanup
- Kill server process
- Remove test-data directory

## Pass Criteria
- All test cases produce expected HTTP status codes and response bodies
- No server panics or errors in logs
- Cleanup removes correct versions and updates feed correctly
