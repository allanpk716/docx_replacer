# S03: Web 管理界面（Vue 3 SPA + Go embed） — UAT

**Milestone:** M023
**Written:** 2026-05-05T07:30:11.888Z

# S03: Web 管理界面（Vue 3 SPA + Go embed） — UAT

**Milestone:** M023
**Written:** 2026-05-05

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: The slice produces a compiled Go binary with embedded SPA. Visual browser testing is deferred to S04 (Windows Server deployment). This UAT verifies the build artifacts, API integration via Go tests, and SPA serving logic.

## Not Proven By This UAT

- Visual rendering of the Vue 3 frontend in a real browser
- End-to-end login → manage apps → upload workflow in a running server
- Responsive layout and CSS styling
- S04 deployment on Windows Server 2019

## Preconditions

- Go 1.22+ toolchain available
- Node.js 18+ and npm available
- GOCACHE env var set (required on this build environment)
- update-hub/web/node_modules installed (`npm install --ignore-scripts`)

## Smoke Test

1. Build the Go binary: `cd update-hub && GOCACHE=/tmp/go-cache go build -o update-hub.exe .`
2. **Expected:** Binary builds successfully without errors

## Test Cases

### 1. SPA serves index.html at root path

1. Run SPA tests: `cd update-hub && GOCACHE=/tmp/go-cache go test ./handler/... -run SPA -v -count=1`
2. **Expected:** TestSPAHandler_ServesIndexHtml passes — GET / returns 200 with index.html content

### 2. SPA serves Vite assets with correct MIME types

1. SPA test suite runs as above
2. **Expected:** TestSPAHandler_ServesAssets passes — GET /assets/index-xxx.js returns application/javascript, GET /assets/index-xxx.css returns text/css

### 3. SPA fallback for client-side routes

1. SPA test suite runs as above
2. **Expected:** TestSPAHandler_SPARouting passes — GET /login, /apps, /apps/myapp, /settings/profile all return index.html with 200

### 4. Path traversal protection

1. SPA test suite runs as above
2. **Expected:** TestSPAHandler_PathTraversal passes — GET /../../etc/passwd returns 400

### 5. JWT login endpoint issues session cookie

1. Run auth tests: `cd update-hub && GOCACHE=/tmp/go-cache go test ./handler/... -run "Login" -v -count=1`
2. **Expected:** TestLoginHandler_Success passes — POST /api/auth/login with correct password returns 200 and Set-Cookie header with HttpOnly JWT

### 6. Auth middleware accepts JWT cookie

1. Run JWT cookie tests: `cd update-hub && GOCACHE=/tmp/go-cache go test ./handler/... -run "JWT|Cookie" -v -count=1`
2. **Expected:** Tests pass — protected endpoints accessible with JWT session cookie, 401 without auth, Bearer token takes precedence over cookie

### 7. Vue 3 frontend compiles

1. Build frontend: `cd update-hub/web && npx vue-tsc -b && npx vite build`
2. **Expected:** TypeScript check passes with zero errors, vite build outputs dist/index.html + assets/

### 8. Full Go test suite passes

1. Run all tests: `cd update-hub && GOCACHE=/tmp/go-cache go test ./... -count=1`
2. **Expected:** All packages pass (handler, middleware, storage, database)

## Edge Cases

- Login with no password configured returns 401 "login disabled" (not 404)
- Expired JWT returns 401 with clear error message
- Bearer token and JWT cookie both present — Bearer takes precedence
- Upload dialog uses raw fetch (not apiFetch) to preserve multipart boundary
