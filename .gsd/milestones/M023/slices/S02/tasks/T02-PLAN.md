---
estimated_steps: 1
estimated_files: 7
skills_used: []
---

# T02: Wire metadata into handlers and add query API endpoints

Wire the database package into existing handlers and add new query API endpoints. (1) Add `*database.DB` field to UploadHandler, PromoteHandler, DeleteHandler — metadata operations run after successful file operations. (2) UploadHandler: accept optional `notes` multipart form field, call UpsertApp + UpsertVersion after file storage. (3) PromoteHandler: call UpsertVersion for target channel after file copy. (4) DeleteHandler: call DeleteVersion after file cleanup. (5) Create new AppListHandler for GET /api/apps returning all apps with channels. (6) Create new VersionListHandler for GET /api/apps/{appId}/channels/{channel}/versions returning versions with notes from SQLite. (7) Update main.go: init DB at data/update-hub.db, pass to handlers, register new routes, defer DB.Close(). (8) Update integration test setup to create DB and pass to handlers. (9) Add integration sub-tests: upload with notes → GET /api/apps → GET versions with notes → promote metadata sync → delete metadata cleanup.

## Inputs

- `update-hub/database/db.go`
- `update-hub/handler/upload.go`
- `update-hub/handler/promote.go`
- `update-hub/handler/delete.go`
- `update-hub/handler/list.go`
- `update-hub/main.go`
- `update-hub/handler/integration_test.go`

## Expected Output

- `update-hub/handler/app_list.go`
- `update-hub/handler/version_list.go`
- `update-hub/handler/upload.go`
- `update-hub/handler/promote.go`
- `update-hub/handler/delete.go`
- `update-hub/main.go`
- `update-hub/handler/integration_test.go`

## Verification

cd update-hub && go test ./... -v -count=1
