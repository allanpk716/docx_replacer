---
id: T03
parent: S03
milestone: M023
key_files:
  - update-hub/web/src/views/AppListView.vue
  - update-hub/web/src/views/AppDetailView.vue
  - update-hub/web/src/components/UploadDialog.vue
  - update-hub/web/src/components/PromoteDialog.vue
  - update-hub/web/src/components/DeleteConfirm.vue
  - update-hub/web/src/components/Toast.vue
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-05-05T07:23:33.150Z
blocker_discovered: false
---

# T03: Build Vue 3 app management pages — AppListView with app cards/channel badges, AppDetailView with version table/actions, UploadDialog, PromoteDialog, DeleteConfirm, and Toast notification component

**Build Vue 3 app management pages — AppListView with app cards/channel badges, AppDetailView with version table/actions, UploadDialog, PromoteDialog, DeleteConfirm, and Toast notification component**

## What Happened

Implemented the complete app management frontend UI in Vue 3:

**AppListView.vue**: Fetches GET /api/apps, renders app cards in a responsive grid. Each card shows the app name, channel badges (with color coding for stable/beta/alpha/dev), and navigates to the detail view on click. Includes loading spinner, error state with retry, and empty state for no applications.

**AppDetailView.vue**: Full version management page. Loads app channels from GET /api/apps, then fetches versions per channel from GET /api/apps/{appId}/channels/{channel}/versions (which returns notes from SQLite). Renders a version table per channel showing version, notes, upload date, and action buttons (Promote, Delete). Upload button opens the upload dialog. Integrates all three dialogs and toast notifications.

**UploadDialog.vue**: Modal dialog with channel selector (existing channels + new channel option), multi-file input for .nupkg files, optional notes textarea. Posts multipart FormData directly via fetch (not apiFetch, since Content-Type must be browser-set for multipart boundary). Shows success/error feedback.

**PromoteDialog.vue**: Modal dialog showing source channel and version, target channel selector (filters out source), supports creating new channels. Uses apiFetch POST to the promote endpoint.

**DeleteConfirm.vue**: Confirmation dialog warning about irreversible deletion. Uses apiFetch DELETE to the version endpoint.

**Toast.vue**: Fixed-position top-right toast notification component with success/error types, auto-dismiss after 3 seconds, animated enter/exit transitions via Vue TransitionGroup.

All components use consistent styling from CSS variables established in App.vue. TypeScript type checking passes (vue-tsc -b), production build succeeds (vite build outputs 12 assets).

## Verification

1. `vue-tsc -b` — TypeScript type checking passes with zero errors across all new components
2. `vite build` — Production build succeeds, outputs dist/index.html plus 12 JS/CSS assets including AppListView, AppDetailView chunks
3. `ls dist/index.html` — Confirms dist/index.html exists
4. PowerShell check confirms AppDetailView.vue imports all three dialogs (UploadDialog, PromoteDialog, DeleteConfirm) — count >= 3

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `cd update-hub/web && npx vue-tsc -b` | 0 | ✅ pass | 5000ms |
| 2 | `cd update-hub/web && npx vite build` | 0 | ✅ pass | 1020ms |
| 3 | `cd update-hub/web && ls dist/index.html` | 0 | ✅ pass | 100ms |
| 4 | `powershell -c "(Get-Content src/views/AppDetailView.vue | Select-String 'UploadDialog|PromoteDialog|DeleteConfirm').Count -ge 3"` | 0 | ✅ pass | 200ms |

## Deviations

UploadDialog uses raw fetch() instead of apiFetch() for the upload POST because apiFetch sets Content-Type: application/json by default, which breaks multipart form uploads (the browser must set the multipart boundary). The unauthenticated check is handled inline. This matches how the Go upload handler expects multipart data.

## Known Issues

None.

## Files Created/Modified

- `update-hub/web/src/views/AppListView.vue`
- `update-hub/web/src/views/AppDetailView.vue`
- `update-hub/web/src/components/UploadDialog.vue`
- `update-hub/web/src/components/PromoteDialog.vue`
- `update-hub/web/src/components/DeleteConfirm.vue`
- `update-hub/web/src/components/Toast.vue`
