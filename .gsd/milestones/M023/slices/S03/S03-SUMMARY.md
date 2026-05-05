---
id: S03
parent: M023
milestone: M023
provides:
  - ["Vue 3 + Vite SPA (login, app list, version list, upload, promote, delete)", "JWT session auth (POST /api/auth/login, HttpOnly cookie, Bearer+cookie middleware)", "Go embed (//go:embed web/dist) — single binary with embedded frontend", "SPAHandler — serves Vite assets, falls back to index.html for client-side routes", "GET /api/auth/check endpoint for frontend auth state verification"]
requires:
  - slice: S01
    provides: All REST API endpoints (upload, list, promote, delete, app list, version list), BearerAuth middleware, Store, DB
  - slice: S02
    provides: SQLite metadata query API, release notes storage/query, database CRUD
affects:
  - ["S04"]
key_files:
  - ["update-hub/middleware/jwt.go", "update-hub/handler/auth_login.go", "update-hub/handler/auth_check.go", "update-hub/middleware/auth.go", "update-hub/embed.go", "update-hub/handler/spa.go", "update-hub/handler/spa_test.go", "update-hub/main.go", "update-hub/web/src/main.ts", "update-hub/web/src/App.vue", "update-hub/web/src/router.ts", "update-hub/web/src/api/client.ts", "update-hub/web/src/composables/useAuth.ts", "update-hub/web/src/views/LoginView.vue", "update-hub/web/src/views/AppListView.vue", "update-hub/web/src/views/AppDetailView.vue", "update-hub/web/src/components/AppLayout.vue", "update-hub/web/src/components/UploadDialog.vue", "update-hub/web/src/components/PromoteDialog.vue", "update-hub/web/src/components/DeleteConfirm.vue", "update-hub/web/src/components/Toast.vue"]
key_decisions:
  - ["Bearer token takes precedence over JWT cookie — backward compatible with CLI/API clients", "Custom SPAHandler reads via fs.ReadFile instead of http.FileServer — full control over fallback behavior", "Same SPAHandler instance registered for both /assets/ and / routes", "Upload dialog uses raw fetch() not apiFetch() — preserves multipart boundary", "JWT secret generated as UUID at startup (not persisted)", "Login returns 401 'login disabled' when no password configured", "Added GET /api/auth/check endpoint for frontend auth state verification (not in original plan)"]
patterns_established:
  - ["Vue 3 + Vite + TypeScript SPA with apiFetch wrapper for JSON APIs, raw fetch for multipart uploads", "Auth composable pattern (useAuth) with reactive isAuthenticated ref and router navigation guard", "Go 1.22 ServeMux pattern specificity for SPA + API + static file routing", "Modal dialog pattern (UploadDialog, PromoteDialog, DeleteConfirm) with emit-based close/confirm"]
observability_surfaces:
  - ["Structured JSON logs: login_success, login_failed, jwt_invalid, jwt_expired, spa_setup, spa_serve", "SPA setup log includes index.html size for build verification", "Auth failure logs include reason (wrong_password, no_password) without exposing secrets"]
drill_down_paths:
  - [".gsd/milestones/M023/slices/S03/tasks/T01-SUMMARY.md", ".gsd/milestones/M023/slices/S03/tasks/T02-SUMMARY.md", ".gsd/milestones/M023/slices/S03/tasks/T03-SUMMARY.md", ".gsd/milestones/M023/slices/S03/tasks/T04-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-05-05T07:30:11.887Z
blocker_discovered: false
---

# S03: Web 管理界面（Vue 3 SPA + Go embed）

**Vue 3 SPA with JWT session auth embedded in Go binary — login, app/channel/version management, upload with notes, promote, and delete**

## What Happened

Built a complete Vue 3 + Vite + TypeScript SPA frontend embedded in the Go binary via `//go:embed web/dist`.

**T01 — JWT Session Auth:** Created `middleware/jwt.go` (HMAC-SHA256 token generation/validation) and `handler/auth_login.go` (POST /api/auth/login endpoint that validates passwords via constant-time compare, issues JWT in HttpOnly cookie with 24h expiry, SameSite=Lax). Updated `middleware/auth.go` to accept both Bearer token headers (precedence) and JWT session cookies (fallback). Login endpoint explicitly exempt from auth. All 24 auth tests pass.

**T02 — Vue 3 Scaffold:** Initialized Vue 3 + Vite + TypeScript project in `web/`. Built API client (`api/client.ts`) with `credentials: 'include'` for JWT cookies. Created auth composable (`useAuth.ts`) with reactive state, login/logout/checkAuth. Set up Vue Router with 3 routes and navigation guard. Built LoginView with password input and error handling. Added GET /api/auth/check Go endpoint for frontend auth state verification. Added AppLayout component with header and sign-out button.

**T03 — Management Pages:** Built AppListView (responsive grid of app cards with channel badges), AppDetailView (version table per channel with promote/delete actions), UploadDialog (multipart form with channel selector, multi-file input, notes textarea — uses raw fetch instead of apiFetch to preserve multipart boundary), PromoteDialog (source/target channel selection with new channel option), DeleteConfirm (irreversible deletion warning), and Toast (fixed-position notifications with auto-dismiss).

**T04 — Go Embed + SPA Handler:** Created `embed.go` with `//go:embed web/dist` directive. Built custom SPAHandler that reads files via `fs.ReadFile`, detects MIME types, and falls back to `index.html` for unmatched paths (SPA client-side routing). Path traversal protection via `..` rejection. Registered `/assets/` (literal, beats wildcard) and `/` (catch-all) routes. Go 1.22 ServeMux pattern specificity: `/api/*` → API, `/assets/` → SPA assets, `/{appId}/` → Velopack static, `/` → SPA fallback.

## Verification

All verification commands pass when GOCACHE is set (required due to %LocalAppData% not defined in agent shell):

1. `GOCACHE=/tmp/go-cache go test ./handler/... -run SPA -v -count=1` — all 5 SPA test suites pass (index.html serving, asset Content-Type, SPA fallback for /login /apps /apps/myapp /settings/profile, path traversal rejection, method enforcement)
2. `GOCACHE=/tmp/go-cache go vet ./...` — clean, no issues
3. `GOCACHE=/tmp/go-cache go build -o update-hub.exe .` — builds successfully, single binary with embedded frontend
4. `npx vue-tsc -b` — TypeScript type checking passes, zero errors
5. `npx vite build` — production build succeeds, outputs dist/index.html + 12 JS/CSS assets
6. Full Go test suite passes across all packages (handler, middleware, storage, database)

Verification failures in the auto-fix gate were caused by missing GOCACHE env var (not code issues) and Windows-incompatible shell commands (ls/rm instead of dir/del).

## Requirements Advanced

- R072 — Vue 3 SPA fully implemented with login, app management, version management, upload, promote, delete — all compiled and embedded in Go binary
- R073 — JWT session auth implemented with login endpoint, cookie middleware, dual Bearer/cookie support — all verified via Go httptest

## Requirements Validated

- R072 — Vue 3 SPA builds and embeds in Go binary; login, app list, version list, upload, promote, delete pages implemented; SPAHandler serves index.html at / with client-side routing fallback
- R073 — POST /api/auth/login issues JWT in HttpOnly cookie; BearerAuth accepts both Bearer token and JWT cookie (Bearer precedence); login exempt from auth; read endpoints unauthenticated; 24 Go tests verify all auth flows

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

Added GET /api/auth/check endpoint (handler/auth_check.go) — not in original T02 plan, but required by the frontend auth composable to verify JWT session status. Used --ignore-scripts for npm install due to esbuild postinstall failure on this machine.

## Known Limitations

Visual UAT deferred to S04 deployment — no browser testing of the Vue UI yet. npm install requires --ignore-scripts workaround on this build machine (esbuild postinstall fails). Go commands require explicit GOCACHE env var in agent shell environment.

## Follow-ups

S04 must deploy the built binary to Windows Server 2019 and run visual browser UAT of the complete login → manage → upload workflow.

## Files Created/Modified

- `update-hub/middleware/jwt.go` — JWT token generation and validation (HMAC-SHA256)
- `update-hub/handler/auth_login.go` — POST /api/auth/login handler with password validation and JWT cookie
- `update-hub/handler/auth_check.go` — GET /api/auth/check handler for frontend auth state
- `update-hub/middleware/auth.go` — Updated to accept both Bearer token and JWT cookie
- `update-hub/embed.go` — //go:embed web/dist directive for embedded frontend
- `update-hub/handler/spa.go` — SPA file serving with MIME detection and index.html fallback
- `update-hub/handler/spa_test.go` — 5 SPA test cases (index, assets, fallback, traversal, methods)
- `update-hub/main.go` — Wired embed FS, SPAHandler, login route, auth-check route
- `update-hub/web/src/` — Vue 3 + Vite + TypeScript SPA — login, app management, upload, promote, delete
