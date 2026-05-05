---
estimated_steps: 12
estimated_files: 8
skills_used: []
---

# T03: Implement static serving + list + promote + delete handlers

Build the remaining handlers: static file serving for Velopack clients, version list, promote between channels, and delete version. Each handler adapts the existing single-app pattern to multi-app by adding appId to all storage operations and removing the hardcoded channel validation.

Static handler: serves /{appId}/{channel}/releases.{os}.json (Velopack SimpleWebSource compatible) and /{appId}/{channel}/*.nupkg. No channel validation (dynamic channels per D059). Path traversal protection.

List handler: GET /api/apps/{appId}/channels/{channel}/releases returns version list grouped by version number, sorted descending by semver.

Promote handler: POST /api/apps/{appId}/channels/{target}/promote?from={source}&version={version} copies .nupkg files and merges feed entries between channels of the same app.

Delete handler: DELETE /api/apps/{appId}/channels/{channel}/versions/{version} removes matching .nupkg files and removes entries from all OS feed files, then rewrites feeds.

Steps:
1. Create handler/static.go: StaticHandler with ServeHTTP dispatching by path pattern /{appId}/{channel}/{filename}, content-type detection (.json→application/json, .nupkg→application/octet-stream), path traversal prevention
2. Create handler/list.go: ListHandler with ServeHTTP, extract appId/channel from path via r.PathValue(), read feed, group by version, sort descending
3. Create handler/promote.go: PromoteHandler, extract appId/target from path, parse from/version query params, copy .nupkg files, merge feed entries, auto-cleanup target
4. Create handler/delete.go: DeleteHandler, remove .nupkg files matching version, update all releases.*.json feeds to remove matching assets
5. Write handler/static_test.go, handler/list_test.go, handler/promote_test.go, handler/delete_test.go with tests for normal flow + error cases
6. Remove hardcoded validChannels map — all handlers validate channel names by regex only

## Inputs

- `update-hub/go.mod`
- `update-hub/model/release.go`
- `update-hub/storage/store.go`
- `update-hub/handler/upload.go`
- `update-hub/middleware/auth.go`
- `update-server/handler/static.go`
- `update-server/handler/list.go`
- `update-server/handler/promote.go`

## Expected Output

- `update-hub/handler/static.go`
- `update-hub/handler/list.go`
- `update-hub/handler/promote.go`
- `update-hub/handler/delete.go`
- `update-hub/handler/static_test.go`
- `update-hub/handler/list_test.go`
- `update-hub/handler/promote_test.go`
- `update-hub/handler/delete_test.go`

## Verification

cd update-hub && go test ./handler/... -run 'TestStatic|TestList|TestPromote|TestDelete' -count=1 -v
