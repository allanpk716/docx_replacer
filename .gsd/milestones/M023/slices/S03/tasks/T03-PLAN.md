---
estimated_steps: 15
estimated_files: 6
skills_used: []
---

# T03: Build app management pages — list, versions, upload, promote, delete

Build the main management UI pages: app list with channels, version list with notes, upload dialog, promote dialog, delete confirmation. All API calls use the apiFetch client from T02 with JWT cookie auth.

**API endpoints** (all exist from S01/S02):
- `GET /api/apps` → `{"id":"docufiller","channels":["stable","beta"]}[]`
- `GET /api/apps/{appId}/channels/{channel}/versions` → version entries with notes
- `POST /api/apps/{appId}/channels/{channel}/releases` → multipart (files + optional notes)
- `POST /api/apps/{appId}/channels/{target}/promote?from={source}&version={version}`
- `DELETE /api/apps/{appId}/channels/{channel}/versions/{version}`

## Steps

1. Implement `views/AppListView.vue`: fetch GET /api/apps, app cards with channel badges, click → /apps/{appId}, empty state, loading spinner

2. Implement `views/AppDetailView.vue`: appId from route, fetch channels then versions per channel, version table with actions, upload button

3. Create `components/UploadDialog.vue`: modal, file input (.nupkg), channel input, notes textarea, FormData POST, success/error feedback

4. Create `components/PromoteDialog.vue`: modal, source/target channels, version, confirm POST

5. Create `components/DeleteConfirm.vue`: confirmation dialog, DELETE request

6. Create `components/Toast.vue`: success/error toast, auto-dismiss 3s, top-right fixed

7. Verify build

## Inputs

- `update-hub/web/src/views/AppListView.vue`
- `update-hub/web/src/views/AppDetailView.vue`
- `update-hub/web/src/api/client.ts`
- `update-hub/web/src/composables/useAuth.ts`

## Expected Output

- `update-hub/web/src/views/AppListView.vue`
- `update-hub/web/src/views/AppDetailView.vue`
- `update-hub/web/src/components/UploadDialog.vue`
- `update-hub/web/src/components/PromoteDialog.vue`
- `update-hub/web/src/components/DeleteConfirm.vue`
- `update-hub/web/src/components/Toast.vue`
- `update-hub/web/dist/index.html`

## Verification

cd update-hub/web && npm run build && ls dist/index.html && powershell -c "(Get-Content src/views/AppDetailView.vue | Select-String 'UploadDialog|PromoteDialog|DeleteConfirm').Count -ge 3"
