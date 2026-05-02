---
sliceId: S01
uatType: live-runtime
verdict: PASS
date: 2026-04-25T07:43:45+08:00
---

# UAT Result — S01: Go Update Server

## Checks

| Check | Mode | Result | Notes |
|-------|------|--------|-------|
| TC01-3: Empty beta channel returns 404 | runtime | PASS | `curl http://localhost:18090/beta/releases.win.json` → 404 |
| TC01-4: Invalid channel returns 404 | runtime | PASS | `curl http://localhost:18090/invalid/file.txt` → 404 |
| TC02-2: Upload without token returns 401 | runtime | PASS | `{"error":"missing Authorization header"}`, HTTP 401 |
| TC02-3: Upload with bad token returns 401 | runtime | PASS | `{"error":"invalid token"}`, HTTP 401 |
| TC02-4: Upload with correct token returns 200 | runtime | PASS | `{"channel":"beta","files_received":2,"versions_added":["1.0.0"]}`, HTTP 200 |
| TC03-1: Feed JSON downloadable after upload | runtime | PASS | HTTP 200, valid JSON with Assets array containing Version 1.0.0 and FileName DocuFiller-1.0.0-full.nupkg |
| TC03-2: Nupkg file downloadable after upload | runtime | PASS | HTTP 200 for `beta/DocuFiller-1.0.0-full.nupkg` |
| TC04: Version list API (public, no auth required) | runtime | PASS | HTTP 200 without auth token, response contains `versions`, `total_size`, `file_count` |
| TC05-1: Promote beta→stable returns 200 | runtime | PASS | `{"promoted":"1.0.0","from":"beta","to":"stable","files_copied":1}` |
| TC05-2: Stable feed available after promote | runtime | PASS | HTTP 200, feed contains Version 1.0.0 asset |
| TC05-3: Stable version list (public, no auth) | runtime | PASS | HTTP 200, lists version 1.0.0 |
| TC06: Promote non-existent version returns 404 | runtime | PASS | `{"error":"version 99.0.0 not found in beta"}`, HTTP 404 |
| TC07: Auto-cleanup keeps last 10 versions | runtime | PASS | Uploaded 11 versions (1.0.0–1.0.10), version list shows exactly 10 versions (1.0.1–1.0.10), oldest 1.0.0 removed |
| TC08: Cross-channel isolation | runtime | PASS | Beta has 1.1.0 (new upload), stable has only 1.0.0 (promoted), no cross-contamination |

## Fix Applied During UAT

**Auth middleware fix** (`middleware/auth.go`): The GET version list API (`/api/channels/{channel}/releases`) was returning 401 because the auth middleware required authentication on all `/api/*` paths. The UAT specification (and CLAUDE.md) states version listing should be public. Fixed by adding a path check: GET requests to `/api/channels/*/releases` (non-promote paths) now skip auth. All 50 Go unit tests continue to pass after the fix.

## Go Tests

`go test ./...` → 50/50 PASS (handler: 31, storage: 19)

## Overall Verdict

PASS — All 14 UAT checks passed. One auth middleware bug was discovered and fixed during UAT (version list API was incorrectly requiring authentication). The server correctly handles all core operations: static file serving, authenticated upload, public version listing, channel promotion, auto-cleanup, and cross-channel isolation.

## Notes

- Server started cleanly with `-port 18090 -data-dir ./test-data -token test-secret`
- Velopack feed format uses top-level `Assets` array (not `releases` array) — test fixtures needed to match this model
- Upload handler special-cases `releases.win.json` filename for feed merging; other filenames stored as-is
- Cleanup triggers after every upload and promote, keeping last 10 versions by semver descending
- `go vet ./...` reports no issues