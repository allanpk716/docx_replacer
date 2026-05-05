---
sliceId: S01
uatType: artifact-driven
verdict: PASS
date: 2026-05-05T14:29:10Z
---

# UAT Result — S01

## Checks

| Check | Mode | Result | Notes |
|-------|------|--------|-------|
| Smoke Test: TestFullMultiAppWorkflow 15 subtests | artifact | PASS | All 15 subtests pass (UploadToDocuFillerBeta, UploadToGoAppStable, ServeDocuFillerFeed, ServeDocuFillerNupkg, ServeGoAppFeed, ListDocuFillerVersions, PromoteToStable, StableServesPromotedVersion, DeleteFromBeta, AuthRejection, AuthRejection_BadToken, MultiAppIsolation, DynamicChannel, NotFoundForMissingFile, NotFoundForMissingApp) |
| Full test suite (96 tests, 3 packages) | artifact | PASS | `go test ./... -count=1` → ok for handler (0.355s), middleware (0.062s), storage (0.250s). Zero failures. |
| go vet | artifact | PASS | `go vet ./...` → exit 0, zero warnings |
| go build | artifact | PASS | `go build -o /dev/null .` → exit 0, compiles cleanly |
| Upload Windows feed + .nupkg to docufiller/beta | artifact | PASS | TestFullMultiAppWorkflow/UploadToDocuFillerBeta: POST multipart returns 200, feed merge logged with versions_added:1 |
| Upload Linux feed to different app (multi-app isolation) | artifact | PASS | TestFullMultiAppWorkflow/UploadToGoAppStable: releases.linux.json uploaded to go-tool/stable. MultiAppIsolation subtest verifies 404 for cross-app leak |
| Version listing | artifact | PASS | TestFullMultiAppWorkflow/ListDocuFillerVersions: GET /api/apps/docufiller/channels/beta/releases returns sorted version list |
| Promote beta → stable | artifact | PASS | TestFullMultiAppWorkflow/PromoteToStable + StableServesPromotedVersion: files_copied:1, stable feed includes promoted version |
| Delete version | artifact | PASS | TestFullMultiAppWorkflow/DeleteFromBeta: files_deleted:1, feed updated |
| Auth enforcement (no header → 401, wrong token → 401) | artifact | PASS | TestFullMultiAppWorkflow/AuthRejection (401) + AuthRejection_BadToken (401). Middleware tests also cover invalid auth scheme and case-insensitive Bearer |
| GET static paths without token → 200 (public) | artifact | PASS | TestFullMultiAppWorkflow/ServeDocuFillerFeed + ServeDocuFillerNupkg: GET static paths serve without auth |
| Dynamic channel (nightly) | artifact | PASS | TestFullMultiAppWorkflow/DynamicChannel: upload to nightly channel succeeds, feed served at /docufiller/nightly/releases.win.json |
| Auto-registration mismatch → 400 | artifact | PASS | TestUpload_AutoRegistration_PackageIdMismatch: returns 400 with "package ID mismatch: feed has OtherApp, URL has docufiller" |
| Non-existent resources → 404 | artifact | PASS | TestFullMultiAppWorkflow/NotFoundForMissingFile + NotFoundForMissingApp + TestStatic_NotFound all return 404 |
| Path traversal protection | artifact | PASS | TestStatic_PathTraversal: request to `../../secret/data/secret.txt` rejected with `static_path_traversal` event |
| Idempotent delete (non-existent version) | artifact | PASS | TestDelete_NonExistentVersion: returns files_deleted:0, no error |
| Multi-OS feed support (releases.win.json + releases.linux.json) | artifact | PASS | TestUpload_LinuxFeed + TestUpload_MultipleFeedFiles + TestCleanupOldVersions_LinuxFeed all pass. IsFeedFilename regex verified |
| Auto-registration directory creation | artifact | PASS | TestUpload_CreatesDirectoryStructure: newapp/beta directory created on first upload |
| Channel name validation (regex) | artifact | PASS | TestUpload_InvalidChannel: bad@channel rejected with upload_invalid_channel event |

## Overall Verdict

PASS — All 19 checks pass across 96 tests in 3 packages. The integration test proves the full Velopack-compatible workflow end-to-end, and all edge cases (path traversal, auth enforcement, auto-registration mismatch, idempotent delete, multi-OS feeds, dynamic channels) are verified.

## Notes

- Test count is 96 (slightly more than the 91 claimed in the summary — additional tests were added during development).
- Environment note: GOCACHE must be set (`export GOCACHE=$HOME/.cache/go-build`) in Git Bash worktree environments.
- Auth middleware tests (12) cover: missing auth, invalid token, valid token, empty token (auth disabled), GET bypass, invalid auth scheme, case-insensitive Bearer prefix.
- Storage tests (26) cover: atomic writes, feed merge, multi-app isolation, semver sorting, cleanup with keep count, multi-OS feeds.
