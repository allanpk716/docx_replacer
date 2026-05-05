# S03: Web 管理界面（Vue 3 SPA + Go embed）

**Goal:** Build Vue 3 SPA frontend embedded in Go binary — login with JWT session auth, app/channel/version management, upload with notes, promote, and delete operations.
**Demo:** 浏览器打开 http://server:30001/ 看到 Web UI，登录后查看应用列表、上传新版本（带备注）、promote、删除

## Must-Haves

- ## Success Criteria
- POST /api/auth/login accepts password, returns JWT session cookie (HttpOnly, SameSite=Lax, 24h expiry)
- Auth middleware accepts JWT cookie on write operations alongside existing Bearer token
- Login endpoint itself is exempt from auth middleware
- Vue 3 SPA compiles with Vite (`npm run build` succeeds, output in web/dist/)
- Go embed includes web/dist/ in binary
- SPA catch-all serves index.html for non-API, non-static paths (e.g. /login, /apps/docufiller)
- /assets/ serves Vite hashed files from embedded FS (literal match overrides /{appId}/ wildcard)
- Full workflow proven by Go integration test: login → JWT cookie → auth-protected write op → SPA fallback routing

## Proof Level

- This slice proves: Integration — login endpoint and JWT middleware tested via Go httptest; SPA embed verified by building binary and confirming index.html is served at /; frontend compiled by Vite but visual UAT deferred to S04 deployment.

## Integration Closure

- Upstream surfaces consumed: All S01/S02 REST API endpoints (upload, list, promote, delete, app list, version list), BearerAuth middleware pattern, Store, DB
- New wiring introduced: POST /api/auth/login endpoint, JWT session cookie middleware, embed.go with //go:embed web/dist, SPA fallback handler in main.go ServeMux
- What remains before the milestone is truly usable end-to-end: S04 data migration + Windows Server deployment — the Go binary with embedded frontend must be deployed and old data migrated

## Verification

- Runtime signals: Structured JSON logs for login_success, login_failed, jwt_invalid, jwt_expired, spa_serve events
- Inspection surfaces: Log output shows JWT auth events alongside existing auth_missing/auth_invalid_token events
- Failure visibility: Login failures logged with reason (wrong_password, no_password), JWT validation failures logged with token error
- Redaction constraints: JWT secret never logged; passwords never logged; JWT token values truncated in logs

## Tasks

- [ ] **T01: Add JWT session auth — login endpoint and middleware update** `est:1h`
  Add JWT session-based authentication for the Web UI. Create a POST /api/auth/login endpoint that validates a password and issues a JWT session cookie. Update the existing BearerAuth middleware to also accept JWT session cookies on write operations. The login endpoint itself must be exempt from auth.
  - Files: `update-hub/handler/auth_login.go`, `update-hub/middleware/jwt.go`, `update-hub/middleware/auth.go`, `update-hub/main.go`, `update-hub/go.mod`, `update-hub/handler/auth_login_test.go`
  - Verify: cd update-hub && go test ./handler/... ./middleware/... -run "Login|JWT|Cookie" -v -count=1 && go vet ./... && go build ./...

- [ ] **T02: Scaffold Vue 3 + Vite frontend with login page** `est:1.5h`
  Initialize the Vue 3 + Vite frontend project in `update-hub/web/`. Set up project structure, API client, auth composable, router with auth guard, and the login page. This task creates the foundation that T03 builds management pages on top of.
  - Files: `update-hub/web/package.json`, `update-hub/web/vite.config.ts`, `update-hub/web/tsconfig.json`, `update-hub/web/index.html`, `update-hub/web/src/main.ts`, `update-hub/web/src/App.vue`, `update-hub/web/src/router.ts`, `update-hub/web/src/api/client.ts`, `update-hub/web/src/composables/useAuth.ts`, `update-hub/web/src/views/LoginView.vue`, `update-hub/web/src/components/AppLayout.vue`, `update-hub/web/src/views/AppListView.vue`, `update-hub/web/src/views/AppDetailView.vue`
  - Verify: cd update-hub/web && npm install && npm run build && ls dist/index.html && ls dist/assets/

- [ ] **T03: Build app management pages — list, versions, upload, promote, delete** `est:1.5h`
  Build the main management UI pages: app list with channels, version list with notes, upload dialog, promote dialog, delete confirmation. All API calls use the apiFetch client from T02 with JWT cookie auth.
  - Files: `update-hub/web/src/views/AppListView.vue`, `update-hub/web/src/views/AppDetailView.vue`, `update-hub/web/src/components/UploadDialog.vue`, `update-hub/web/src/components/PromoteDialog.vue`, `update-hub/web/src/components/DeleteConfirm.vue`, `update-hub/web/src/components/Toast.vue`
  - Verify: cd update-hub/web && npm run build && ls dist/index.html && powershell -c "(Get-Content src/views/AppDetailView.vue | Select-String 'UploadDialog|PromoteDialog|DeleteConfirm').Count -ge 3"

- [ ] **T04: Wire Go embed + SPA handler and verify full integration** `est:1h`
  Wire the Vue 3 frontend into the Go binary via `embed` and create the SPA serving handler. Routing relies on Go 1.22 ServeMux pattern specificity: literal `/assets/` overrides wildcard `/{appId}/`, which overrides catch-all `/`.
  - Files: `update-hub/embed.go`, `update-hub/handler/spa.go`, `update-hub/handler/spa_test.go`, `update-hub/main.go`
  - Verify: cd update-hub && go test ./handler/... -run SPA -v -count=1 && go vet ./... && go build -o update-hub.exe . && ls update-hub.exe && rm -f update-hub.exe

## Files Likely Touched

- update-hub/handler/auth_login.go
- update-hub/middleware/jwt.go
- update-hub/middleware/auth.go
- update-hub/main.go
- update-hub/go.mod
- update-hub/handler/auth_login_test.go
- update-hub/web/package.json
- update-hub/web/vite.config.ts
- update-hub/web/tsconfig.json
- update-hub/web/index.html
- update-hub/web/src/main.ts
- update-hub/web/src/App.vue
- update-hub/web/src/router.ts
- update-hub/web/src/api/client.ts
- update-hub/web/src/composables/useAuth.ts
- update-hub/web/src/views/LoginView.vue
- update-hub/web/src/components/AppLayout.vue
- update-hub/web/src/views/AppListView.vue
- update-hub/web/src/views/AppDetailView.vue
- update-hub/web/src/components/UploadDialog.vue
- update-hub/web/src/components/PromoteDialog.vue
- update-hub/web/src/components/DeleteConfirm.vue
- update-hub/web/src/components/Toast.vue
- update-hub/embed.go
- update-hub/handler/spa.go
- update-hub/handler/spa_test.go
