---
estimated_steps: 26
estimated_files: 3
skills_used: []
---

# T02: Implement upload API with token authentication

Add the upload API endpoint and Bearer token authentication middleware.

Steps:
1. Create update-server/middleware/auth.go:
   - Bearer token validation middleware
   - Extract token from Authorization header
   - Compare against configured token (from -token flag)
   - Return 401 if missing/invalid, pass through for valid tokens
   - Skip auth for GET requests to static file paths (/{channel}/*) and GET /api/channels/{channel}/releases
2. Create update-server/handler/upload.go:
   - POST /api/channels/{channel}/releases
   - Accept multipart form upload with files (releases.win.json, .nupkg, Setup.exe, Portable.zip, etc.)
   - Validate channel name (only alphanumeric + hyphen, e.g. stable, beta)
   - For each uploaded file, write to {data-dir}/{channel}/ directory
   - When releases.win.json is uploaded, parse it to extract version info, then merge with existing feed:
     a. Read existing releases.win.json from channel dir (if exists)
     b. Parse the uploaded releases.win.json
     c. Merge new assets into existing feed (append, avoiding duplicates by FileName)
     d. Write updated releases.win.json
   - Return 200 with JSON summary: {"channel": "beta", "files_received": 3, "versions_added": ["1.2.0"]}
   - Return 400 for invalid multipart or missing channel
   - Return 404 for non-existent channel path (should auto-create on first upload)
3. Wire routes in main.go:
   - Apply auth middleware to /api/* paths
   - Register upload handler
   - Static file handler continues to serve without auth
4. Verify: start server, use curl to upload a test .nupkg + releases.win.json, confirm files appear in channel dir

## Inputs

- `update-server/main.go`
- `update-server/storage/store.go`
- `update-server/model/release.go`

## Expected Output

- `update-server/middleware/auth.go`
- `update-server/handler/upload.go`
- `update-server/main.go`

## Verification

cd update-server && go build -o bin/update-server.exe . && echo BUILD_OK

## Observability Impact

Signals added: structured log lines for upload (channel, filename, size, version extracted)
How a future agent inspects: check server stdout logs or GET /api/channels/{channel}/releases
Failure state exposed: 401 on bad token, 400 on bad multipart, 500 on write errors with error message
