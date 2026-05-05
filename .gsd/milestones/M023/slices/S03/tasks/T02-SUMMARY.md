---
id: T02
parent: S03
milestone: M023
key_files:
  - update-hub/web/package.json
  - update-hub/web/vite.config.ts
  - update-hub/web/index.html
  - update-hub/web/tsconfig.json
  - update-hub/web/src/main.ts
  - update-hub/web/src/App.vue
  - update-hub/web/src/router.ts
  - update-hub/web/src/api/client.ts
  - update-hub/web/src/composables/useAuth.ts
  - update-hub/web/src/views/LoginView.vue
  - update-hub/web/src/views/AppListView.vue
  - update-hub/web/src/views/AppDetailView.vue
  - update-hub/web/src/components/AppLayout.vue
  - update-hub/handler/auth_check.go
  - update-hub/main.go
key_decisions:
  - Used fileURLToPath+URL for Vite path alias instead of __dirname for ESM compatibility
  - Logout clears session cookie client-side rather than calling a server endpoint — simpler, no additional Go route needed
  - Added GET /api/auth/check Go endpoint to support frontend auth state verification — not in original plan but required for the composable to work
duration: 
verification_result: passed
completed_at: 2026-05-05T07:18:01.302Z
blocker_discovered: false
---

# T02: Scaffold Vue 3 + Vite frontend with login page, auth composable, router with auth guard, and Go auth-check endpoint

**Scaffold Vue 3 + Vite frontend with login page, auth composable, router with auth guard, and Go auth-check endpoint**

## What Happened

Created the complete Vue 3 + Vite + TypeScript frontend in `update-hub/web/` with all planned components:

**Project setup:** package.json with Vue 3, Vue Router, Vite, TypeScript. Vite config with dev proxy for `/api` and `@` path alias using `fileURLToPath` (not `__dirname` for ESM compat).

**API client** (`api/client.ts`): Fetch wrapper with `credentials: 'include'` for JWT cookie, auto-parses JSON, maps 401 to 'unauthenticated' error.

**Auth composable** (`composables/useAuth.ts`): Reactive `isAuthenticated` ref, `checkAuth()` calls `GET /api/auth/check`, `login()` calls `POST /api/auth/login`, `logout()` clears the session cookie client-side.

**Router** (`router.ts`): Three routes — `/login` → LoginView, `/` → AppListView, `/apps/:appId` → AppDetailView. Navigation guard checks auth state via the composable, redirects unauthenticated users to login with return URL.

**Login view** (`views/LoginView.vue`): Centered card layout with password input, error display, loading state. Handles all error types from the backend (invalid password, login disabled, network errors).

**Layout** (`components/AppLayout.vue`): Header with "Update Hub" brand link and sign-out button, main slot for page content.

**Placeholder views**: AppListView with "Applications" heading, AppDetailView displaying the `appId` route param.

**Go backend addition** (`handler/auth_check.go`): Added `GET /api/auth/check` endpoint that validates the JWT session cookie and returns `{"ok": true}` or 401. Registered in main.go. This was needed because the frontend's `checkAuth()` requires a server-side validation endpoint.

**Build environment note:** `npm install` requires `--ignore-scripts` on this machine due to esbuild postinstall failing (ERR_INVALID_ARG_TYPE in Node child_process). The actual build (`npm run build`) works correctly.

TypeScript type checking, Vite production build, and Go compilation all pass.

## Verification

Ran all verification commands:
1. `vue-tsc -b` — TypeScript type checking passes with zero errors
2. `vite build` — Production build succeeds, outputs dist/index.html and 8 JS/CSS asset files
3. `ls dist/index.html` — Confirms dist/index.html exists
4. `ls dist/assets/` — Confirms 8 asset files generated (JS + CSS for index, AppListView, AppDetailView, AppLayout)
5. `go build ./...` with GOCACHE set — Go compilation passes including new auth_check.go
6. `go vet ./...` — Go vet passes

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `cd update-hub/web && npx vue-tsc -b` | 0 | ✅ pass | 3800ms |
| 2 | `cd update-hub/web && npx vite build` | 0 | ✅ pass | 850ms |
| 3 | `cd update-hub/web && ls dist/index.html` | 0 | ✅ pass | 100ms |
| 4 | `cd update-hub/web && ls dist/assets/` | 0 | ✅ pass | 100ms |
| 5 | `cd update-hub && go build ./... (with GOCACHE=/tmp/go-cache)` | 0 | ✅ pass | 950ms |
| 6 | `cd update-hub && go vet ./... (with GOCACHE=/tmp/go-cache)` | 0 | ✅ pass | 550ms |

## Deviations

Added handler/auth_check.go and registered GET /api/auth/check route in main.go — not in the original T02 plan, but the frontend auth composable needs this endpoint to verify JWT session status. Also used --ignore-scripts for npm install due to esbuild postinstall failure on this machine.

## Known Issues

npm install without --ignore-scripts fails due to esbuild postinstall script (ERR_INVALID_ARG_TYPE in Node child_process spawn). Workaround: npm install --ignore-scripts && node node_modules/esbuild/install.js. Go build requires GOCACHE env var to be set explicitly on this machine.

## Files Created/Modified

- `update-hub/web/package.json`
- `update-hub/web/vite.config.ts`
- `update-hub/web/index.html`
- `update-hub/web/tsconfig.json`
- `update-hub/web/src/main.ts`
- `update-hub/web/src/App.vue`
- `update-hub/web/src/router.ts`
- `update-hub/web/src/api/client.ts`
- `update-hub/web/src/composables/useAuth.ts`
- `update-hub/web/src/views/LoginView.vue`
- `update-hub/web/src/views/AppListView.vue`
- `update-hub/web/src/views/AppDetailView.vue`
- `update-hub/web/src/components/AppLayout.vue`
- `update-hub/handler/auth_check.go`
- `update-hub/main.go`
