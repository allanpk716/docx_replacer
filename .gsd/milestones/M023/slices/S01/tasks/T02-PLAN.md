---
estimated_steps: 7
estimated_files: 4
skills_used: []
---

# T02: Implement upload handler with auto-registration + Bearer auth middleware

Build the upload handler for POST /api/apps/{appId}/channels/{channel}/releases and the Bearer token auth middleware. The upload handler accepts multipart form data with any releases.*.json feed file (not just releases.win.json) plus .nupkg artifacts. Auto-registration validates that the feed's PackageId matches the appId in the URL path — first upload for an appId automatically creates the app directory structure. Channel names are validated by regex (alphanumeric+hyphen) instead of a hardcoded set. The auth middleware protects POST/DELETE API paths, skips auth for GET to non-API paths (Velopack client static serving) and for GET /api/apps/{appId}/channels/{channel}/releases (public version list). Use Go 1.22 r.PathValue() for path parameter extraction.

Steps:
1. Create handler/upload.go: UploadHandler struct with Store, ServeHTTP handling multipart upload, feed merge (detect releases.*.json by pattern), artifact write, auto-cleanup
2. Implement auto-registration: parse uploaded feed, extract PackageId from first asset, validate it matches the appId path parameter (case-insensitive). If no match, return 400
3. Create middleware/auth.go: BearerAuth(token) middleware, skip auth for GET non-API paths and GET /api/*/releases, use subtle.ConstantTimeCompare for token comparison
4. Write handler/upload_test.go: test valid multipart upload, upload with feed, bad channel name, auto-registration validation, missing auth, wrong auth
5. Write middleware/auth_test.go: test auth skip for static GET, auth required for POST, auth required for DELETE, wrong token, empty token config disables auth

## Inputs

- `update-hub/go.mod`
- `update-hub/model/release.go`
- `update-hub/storage/store.go`
- `update-server/handler/upload.go`
- `update-server/middleware/auth.go`

## Expected Output

- `update-hub/handler/upload.go`
- `update-hub/middleware/auth.go`
- `update-hub/handler/upload_test.go`
- `update-hub/middleware/auth_test.go`

## Verification

cd update-hub && go test ./handler/... ./middleware/... -run 'TestUpload|TestAuth' -count=1 -v
