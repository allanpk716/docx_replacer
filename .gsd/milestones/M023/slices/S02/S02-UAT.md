# S02: SQLite 元数据层 + Release notes — UAT

**Milestone:** M023
**Written:** 2026-05-05T06:49:38.461Z

# S02: SQLite 元数据层 + Release notes — UAT

**Milestone:** M023
**Written:** 2026-05-05

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: This slice adds backend APIs and database operations with no user-facing UI. All behavior is verified through integration tests with real SQLite and real HTTP handlers via httptest.

## Preconditions

- Go 1.22+ toolchain available
- GOCACHE and GOPATH environment variables set
- Working directory: update-hub/

## Smoke Test

Run `go test ./... -v -count=1` — all 65 tests across 4 packages pass with zero failures.

## Test Cases

### 1. Upload with release notes

1. POST multipart form to `/api/apps/{appId}/channels/{channel}/releases` with `feed` and `notes` fields
2. **Expected:** HTTP 200, .nupkg and feed stored on filesystem, version metadata with notes persisted in SQLite

### 2. Query app list

1. GET `/api/apps`
2. **Expected:** JSON array of apps with channels derived from version metadata, e.g. `[{"id":"docufiller","channels":["stable","beta"],"created_at":"..."}]`

### 3. Query version list with notes

1. GET `/api/apps/docufiller/channels/stable/versions`
2. **Expected:** JSON array of versions ordered by id DESC, each with `notes` field containing the release notes text

### 4. Promote carries metadata

1. POST `/api/apps/{appId}/channels/{channel}/versions/{version}/promote?to=stable`
2. GET `/api/apps/{appId}/channels/stable/versions`
3. **Expected:** Promoted version appears in target channel with notes carried over from source

### 5. Delete cleans up metadata

1. DELETE `/api/apps/{appId}/channels/{channel}/versions/{version}`
2. GET `/api/apps/{appId}/channels/{channel}/versions`
3. **Expected:** Deleted version no longer appears in either filesystem feed or SQLite metadata

### 6. Nil DB graceful degradation

1. Call any endpoint with DB field set to nil
2. **Expected:** File operations proceed normally, query endpoints return empty arrays, no panics

## Edge Cases

### Empty notes field
1. Upload without `notes` field
2. **Expected:** Version created with empty string notes, no error

### Unknown app/channel
1. GET `/api/apps/nonexistent/channels/stable/versions`
2. **Expected:** Empty JSON array `[]`, HTTP 200

### SQL injection in notes
1. Upload with notes containing `'; DROP TABLE versions; --`
2. **Expected:** Notes stored verbatim, no injection (parameterized queries)

## Failure Signals

- Any test failure in `go test ./... -count=1`
- `go vet` warnings
- Build compilation errors
- Metadata operations blocking file operations (should never happen — best-effort design)

## Not Proven By This UAT

- Live server deployment (proven in S04)
- Web UI rendering of metadata (proven in S03)
- Data migration seeding SQLite from existing files (proven in S04)
- Concurrent multi-client upload scenarios (only single-client concurrency tested)
- Performance under load with large metadata datasets

## Notes for Tester

- The rtk hook warning in stderr is unrelated to test results and can be ignored
- In worktree environments, GOCACHE must be set explicitly (e.g., `export GOCACHE=C:/Temp/go-cache`)
- SQLite database file is at `data/update-hub.db` — can be inspected with sqlite3 CLI for debugging
