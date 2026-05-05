---
estimated_steps: 21
estimated_files: 13
skills_used: []
---

# T02: Scaffold Vue 3 + Vite frontend with login page

Initialize the Vue 3 + Vite frontend project in `update-hub/web/`. Set up project structure, API client, auth composable, router with auth guard, and the login page. This task creates the foundation that T03 builds management pages on top of.

**Important**: Vue 3 Composition API with `<script setup>` syntax, Vue Router, and Vite. No Pinia — use simple composables for state. No CSS framework — use clean, minimal CSS with CSS variables for theming. Keep the design professional but simple (internal admin console).

## Steps

1. Initialize Vue 3 + Vite + TypeScript project in `update-hub/web/`:
   - Create `package.json` with dependencies: vue, vue-router, vite, @vitejs/plugin-vue, typescript
   - Create `tsconfig.json` with Vue-friendly config
   - Create `vite.config.ts`: vue() plugin, dev proxy `/api` → `http://localhost:30001`, build output `dist/`
   - Create `index.html` as Vite entry point

2. Create project structure in `web/src/`:
   - `main.ts` — Vue app creation, router mount
   - `App.vue` — root component with `<router-view>`
   - `router.ts` — Vue Router setup with routes and auth guard
   - `api/client.ts` — fetch wrapper for API calls
   - `composables/useAuth.ts` — reactive auth state (isAuthenticated, login, logout)

3. Create `api/client.ts`: apiFetch wrapper with credentials:'include', auto-parse JSON, 401 → unauthenticated

4. Create `composables/useAuth.ts`: isAuthenticated ref, login(password), checkAuth(), logout()

5. Create `router.ts`: routes /login → LoginView, / → AppListView, /apps/:appId → AppDetailView. Navigation guard for auth.

6. Create `views/LoginView.vue`: centered card, password input, submit, error display, loading state

7. Create `components/AppLayout.vue`: header with "Update Hub" title and logout button, main slot

8. Create placeholder views: `views/AppListView.vue` ("Apps" heading), `views/AppDetailView.vue` (route param display)

9. Run `npm install && npm run build`

## Inputs

- `update-hub/handler/auth_login.go`

## Expected Output

- `update-hub/web/package.json`
- `update-hub/web/vite.config.ts`
- `update-hub/web/src/main.ts`
- `update-hub/web/src/views/LoginView.vue`
- `update-hub/web/src/router.ts`
- `update-hub/web/src/api/client.ts`
- `update-hub/web/src/composables/useAuth.ts`
- `update-hub/web/src/views/AppListView.vue`
- `update-hub/web/src/views/AppDetailView.vue`
- `update-hub/web/dist/index.html`

## Verification

cd update-hub/web && npm install && npm run build && ls dist/index.html && ls dist/assets/
