---
id: T04
parent: S01
milestone: M023
key_files:
  - update-hub/main.go
  - update-hub/handler/integration_test.go
key_decisions:
  - Used Go 1.22 ServeMux with method+path patterns (e.g. POST /api/apps/{appId}/...) instead of manual method/path dispatch — cleaner routing with automatic PathValue extraction
  - Logging middleware uses responseRecorder pattern from reference update-server/main.go for structured JSON request logging with status code capture
duration: 
verification_result: passed
completed_at: 2026-05-05T06:25:55.166Z
blocker_discovered: false
---

# T04: Wired Go 1.22 ServeMux routes in main.go and wrote comprehensive integration test proving full Velopack-compatible multi-app workflow

**Wired Go 1.22 ServeMux routes in main.go and wrote comprehensive integration test proving full Velopack-compatible multi-app workflow**

## What Happened

Created `main.go` that wires all handlers using Go 1.22's enhanced ServeMux with method+path patterns (e.g. `"POST /api/apps/{appId}/channels/{channel}/releases"`). CLI flags for `-port` (default 30001), `-data-dir`, and `-token`. Applied auth middleware and structured JSON logging middleware with response status capture. Server configured with 30s/60s/120s timeouts.

Created `handler/integration_test.go` with `TestFullMultiAppWorkflow` covering 15 subtests: upload to DocuFiller/beta (Windows feed + .nupkg), upload to go-tool/stable (Linux feed + .nupkg), static feed serving for both apps, .nupkg binary download, version listing, promote from beta to stable, delete from beta, auth rejection (no token and wrong token), multi-app isolation verification, dynamic channel (nightly), 404 for missing files and missing apps. Also added `TestAuthDisabled` proving empty token disables auth entirely.

All tests pass: the full workflow upload → feed serve → .nupkg serve → list → promote → delete works end-to-end through httptest.NewServer with real handlers and temp directory storage.

## Verification

Ran `cd update-hub && go test ./... -count=1 -v` — all tests pass across handler (29 tests including integration), middleware (12 tests), and storage (19 tests). Ran `go vet ./...` — zero warnings. Ran `go build -o /dev/null .` — compiles successfully.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `cd update-hub && go test ./... -count=1 -v` | 0 | ✅ pass | 2000ms |
| 2 | `cd update-hub && go vet ./...` | 0 | ✅ pass | 500ms |
| 3 | `cd update-hub && go build -o /dev/null .` | 0 | ✅ pass | 500ms |

## Deviations

None. The plan was followed exactly: Go 1.22 ServeMux patterns, all subtests as specified, full verification.

## Known Issues

None.

## Files Created/Modified

- `update-hub/main.go`
- `update-hub/handler/integration_test.go`
