---
id: T04
parent: S03
milestone: M023
key_files:
  - update-hub/embed.go
  - update-hub/handler/spa.go
  - update-hub/handler/spa_test.go
  - update-hub/main.go
key_decisions:
  - SPAHandler reads files directly via fs.ReadFile with mime.TypeByExtension instead of http.FileServer — gives full control over fallback behavior without intercepting FileServer's 404 response
  - Registered same SPAHandler instance for both /assets/ and / since the handler normalizes paths by stripping leading /, so it works identically for both patterns
duration: 
verification_result: passed
completed_at: 2026-05-05T07:28:14.814Z
blocker_discovered: false
---

# T04: Wire Go embed + SPA handler to serve Vue 3 frontend from single binary

**Wire Go embed + SPA handler to serve Vue 3 frontend from single binary**

## What Happened

Created three new files and updated main.go to embed the Vue 3 frontend into the Go binary:

1. **embed.go** — Declares `//go:embed web/dist` directive making the Vite build output available at compile time.

2. **handler/spa.go** — SPAHandler that reads files from an `fs.FS`, serves them with correct MIME types (via `mime.TypeByExtension`), and falls back to `index.html` for any path that doesn't match a real file (SPA client-side routing). Includes path traversal protection by rejecting paths containing `..`. Logs `spa_setup` at startup with the index.html size.

3. **handler/spa_test.go** — Five test cases covering: index.html serving at `/`, asset serving with correct Content-Type (`application/javascript`, `text/css`), SPA fallback for client-side routes (`/login`, `/apps`, `/apps/myapp`, `/settings/profile`), path traversal rejection, and HTTP method enforcement.

4. **main.go** — Added `io/fs` import, created `webDistFS` via `fs.Sub(webDist, "web/dist")`, instantiated SPAHandler, and registered two routes:
   - `/assets/` (literal) — serves Vite build assets, literal pattern beats `/{appId}/` wildcard
   - `/` (catch-all) — serves SPA with index.html fallback

Routing priority in Go 1.22 ServeMux: `/api/*` → API handlers, `/assets/` → SPAHandler, `/{appId}/` → StaticHandler (Velopack), `/` → SPAHandler.

All SPA tests pass, `go vet` is clean, and `go build` produces a single binary with embedded frontend.

## Verification

Ran `go test ./handler/... -run SPA -v -count=1` — all 5 test suites pass (10 sub-tests). Ran `go vet ./...` — clean, no issues. Ran `go build -o update-hub.exe .` — builds successfully producing single binary. Ran full handler test suite `go test ./handler/... -count=1` — all existing tests still pass (no regressions).

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `cd update-hub && go test ./handler/... -run SPA -v -count=1` | 0 | ✅ pass | 12000ms |
| 2 | `cd update-hub && go vet ./...` | 0 | ✅ pass | 5000ms |
| 3 | `cd update-hub && go build -o update-hub.exe .` | 0 | ✅ pass | 3000ms |
| 4 | `cd update-hub && go test ./handler/... -count=1` | 0 | ✅ pass | 500ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `update-hub/embed.go`
- `update-hub/handler/spa.go`
- `update-hub/handler/spa_test.go`
- `update-hub/main.go`
