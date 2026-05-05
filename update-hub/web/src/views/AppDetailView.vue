<template>
  <AppLayout>
    <div class="detail-page">
      <div class="page-header">
        <div>
          <router-link to="/" class="back-link">← Applications</router-link>
          <h1 class="page-title">{{ appId }}</h1>
        </div>
        <button class="btn btn-primary" @click="showUpload = true">Upload Release</button>
      </div>

      <div v-if="loading" class="loading-state">
        <span class="spinner"></span>
        <span>Loading…</span>
      </div>

      <div v-else-if="loadError" class="error-state">
        <p>Failed to load application data</p>
        <p class="error-detail">{{ loadError }}</p>
        <button class="btn btn-ghost btn-sm" @click="loadData">Retry</button>
      </div>

      <template v-else>
        <section v-for="channel in channels" :key="channel" class="channel-section">
          <div class="channel-header">
            <h2 :class="['channel-name', `channel-name--${channel}`]">{{ channel }}</h2>
            <span class="channel-count">{{ versionMap[channel]?.length ?? 0 }} version(s)</span>
          </div>

          <div v-if="channelLoading[channel]" class="channel-loading">
            <span class="spinner spinner--sm"></span>
          </div>

          <table v-else-if="versionMap[channel]?.length" class="version-table">
            <thead>
              <tr>
                <th>Version</th>
                <th>Notes</th>
                <th>Uploaded</th>
                <th class="th-actions">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="v in versionMap[channel]" :key="v.version">
                <td class="td-version">
                  <span class="version-tag">{{ v.version }}</span>
                </td>
                <td class="td-notes">
                  <span v-if="v.notes" class="notes-text" :title="v.notes">
                    {{ v.notes.length > 60 ? v.notes.slice(0, 60) + '…' : v.notes }}
                  </span>
                  <span v-else class="notes-empty">—</span>
                </td>
                <td class="td-date">{{ formatDate(v.created_at) }}</td>
                <td class="td-actions">
                  <button
                    class="btn btn-sm btn-outline"
                    title="Promote to another channel"
                    @click="openPromote(channel, v.version)"
                  >Promote</button>
                  <button
                    class="btn btn-sm btn-danger-outline"
                    title="Delete this version"
                    @click="openDelete(channel, v.version)"
                  >Delete</button>
                </td>
              </tr>
            </tbody>
          </table>

          <div v-else class="channel-empty">
            No versions in this channel
          </div>
        </section>

        <div v-if="channels.length === 0" class="empty-state">
          <p class="empty-icon">📦</p>
          <p class="empty-title">No channels yet</p>
          <p class="empty-hint">Upload a release to create the first channel</p>
        </div>
      </template>
    </div>

    <UploadDialog
      :open="showUpload"
      :app-id="appId"
      :channels="channels"
      @close="showUpload = false"
      @success="onUploadSuccess"
    />

    <PromoteDialog
      :open="showPromote"
      :app-id="appId"
      :version="promoteVersion"
      :source-channel="promoteSource"
      :channels="channels"
      @close="showPromote = false"
      @success="onPromoteSuccess"
    />

    <DeleteConfirm
      :open="showDelete"
      :app-id="appId"
      :channel="deleteChannel"
      :version="deleteVersion"
      @close="showDelete = false"
      @success="onDeleteSuccess"
    />

    <Toast ref="toastRef" />
  </AppLayout>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { apiFetch } from '@/api/client'
import AppLayout from '@/components/AppLayout.vue'
import UploadDialog from '@/components/UploadDialog.vue'
import PromoteDialog from '@/components/PromoteDialog.vue'
import DeleteConfirm from '@/components/DeleteConfirm.vue'
import Toast from '@/components/Toast.vue'

interface VersionEntry {
  id: number
  app_id: string
  channel: string
  version: string
  notes: string
  created_at: string
}

const props = defineProps<{
  appId: string
}>()

const loading = ref(true)
const loadError = ref('')
const channels = ref<string[]>([])
const versionMap = reactive<Record<string, VersionEntry[]>>({})
const channelLoading = reactive<Record<string, boolean>>({})

const showUpload = ref(false)
const showPromote = ref(false)
const showDelete = ref(false)
const promoteSource = ref('')
const promoteVersion = ref('')
const deleteChannel = ref('')
const deleteVersion = ref('')

const toastRef = ref<InstanceType<typeof Toast>>()

function toast(message: string, type: 'success' | 'error' = 'success') {
  toastRef.value?.show(message, type)
}

async function loadData() {
  loading.value = true
  loadError.value = ''
  try {
    // Get app channels via the app list endpoint
    const apps = await apiFetch<{ id: string; channels: string[] }[]>('/apps')
    const app = apps.find((a) => a.id === props.appId)
    if (app) {
      channels.value = app.channels
    } else {
      channels.value = []
    }

    // Load versions per channel
    for (const ch of channels.value) {
      await loadChannelVersions(ch)
    }
  } catch (err: unknown) {
    loadError.value = err instanceof Error ? err.message : 'Unknown error'
  } finally {
    loading.value = false
  }
}

async function loadChannelVersions(channel: string) {
  channelLoading[channel] = true
  try {
    const versions = await apiFetch<VersionEntry[]>(
      `/apps/${props.appId}/channels/${channel}/versions`,
    )
    versionMap[channel] = versions
  } catch {
    versionMap[channel] = []
  } finally {
    channelLoading[channel] = false
  }
}

function openPromote(source: string, version: string) {
  promoteSource.value = source
  promoteVersion.value = version
  showPromote.value = true
}

function openDelete(channel: string, version: string) {
  deleteChannel.value = channel
  deleteVersion.value = version
  showDelete.value = true
}

async function onUploadSuccess(channel: string) {
  showUpload.value = false
  toast('Upload successful')
  // Refresh channel list and the uploaded channel's versions
  if (!channels.value.includes(channel)) {
    channels.value.push(channel)
  }
  await loadChannelVersions(channel)
}

async function onPromoteSuccess(target: string) {
  showPromote.value = false
  toast(`Version promoted to ${target}`)
  // Refresh target channel
  if (!channels.value.includes(target)) {
    channels.value.push(target)
  }
  await loadChannelVersions(target)
}

async function onDeleteSuccess() {
  showDelete.value = false
  toast('Version deleted')
  // Refresh the affected channel
  await loadChannelVersions(deleteChannel.value)
}

function formatDate(dateStr: string): string {
  if (!dateStr) return '—'
  try {
    const d = new Date(dateStr)
    return d.toLocaleDateString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    })
  } catch {
    return dateStr
  }
}

onMounted(loadData)
</script>

<style scoped>
.detail-page {
  padding: 1.5rem;
  max-width: 960px;
  margin: 0 auto;
}

.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.back-link {
  font-size: 0.8125rem;
  color: var(--color-primary);
  text-decoration: none;
  display: inline-block;
  margin-bottom: 0.375rem;
}

.back-link:hover {
  text-decoration: underline;
}

.page-title {
  font-size: 1.5rem;
  font-weight: 600;
  word-break: break-all;
}

.btn-primary {
  background: var(--color-primary);
  color: #fff;
  border: none;
  border-radius: 6px;
  padding: 0.5rem 1rem;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: background-color 0.15s;
  white-space: nowrap;
  flex-shrink: 0;
}

.btn-primary:hover {
  background: var(--color-primary-hover);
}

.loading-state {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 3rem;
  justify-content: center;
  color: var(--color-text-secondary);
}

.spinner {
  width: 20px;
  height: 20px;
  border: 2px solid var(--color-border);
  border-top-color: var(--color-primary);
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
}

.spinner--sm {
  width: 14px;
  height: 14px;
  border-width: 1.5px;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.error-state {
  text-align: center;
  padding: 3rem;
}

.error-detail {
  color: var(--color-text-secondary);
  font-size: 0.875rem;
  margin-bottom: 1rem;
}

.channel-section {
  margin-bottom: 2rem;
}

.channel-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 0.75rem;
}

.channel-name {
  font-size: 1.125rem;
  font-weight: 600;
}

.channel-name--stable { color: #065f46; }
.channel-name--beta { color: #92400e; }
.channel-name--alpha { color: #5b21b6; }
.channel-name--dev { color: #3730a3; }

.channel-count {
  font-size: 0.75rem;
  color: var(--color-text-secondary);
}

.channel-loading {
  padding: 1.5rem;
  display: flex;
  justify-content: center;
}

.channel-empty {
  padding: 1.5rem;
  text-align: center;
  color: var(--color-text-secondary);
  font-size: 0.875rem;
  background: var(--color-surface);
  border-radius: var(--radius);
}

.version-table {
  width: 100%;
  border-collapse: collapse;
  background: var(--color-surface);
  border-radius: var(--radius);
  overflow: hidden;
  box-shadow: var(--shadow);
}

.version-table th {
  text-align: left;
  padding: 0.75rem 1rem;
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--color-text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.04em;
  background: var(--color-bg);
  border-bottom: 1px solid var(--color-border);
}

.version-table td {
  padding: 0.75rem 1rem;
  border-bottom: 1px solid var(--color-border);
  font-size: 0.875rem;
}

.version-table tr:last-child td {
  border-bottom: none;
}

.td-version {
  width: 160px;
}

.version-tag {
  display: inline-block;
  font-family: 'SF Mono', 'Fira Code', 'Cascadia Code', monospace;
  font-size: 0.8125rem;
  font-weight: 500;
  background: var(--color-bg);
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
}

.td-notes {
  max-width: 240px;
}

.notes-text {
  color: var(--color-text);
}

.notes-empty {
  color: var(--color-text-secondary);
}

.td-date {
  white-space: nowrap;
  color: var(--color-text-secondary);
  font-size: 0.8125rem;
}

.th-actions,
.td-actions {
  width: 180px;
  text-align: right;
}

.td-actions {
  display: flex;
  gap: 0.375rem;
  justify-content: flex-end;
}

.btn-sm {
  padding: 0.3125rem 0.625rem;
  font-size: 0.75rem;
  border-radius: 5px;
  font-weight: 500;
  cursor: pointer;
  border: none;
  transition: all 0.15s;
}

.btn-outline {
  background: none;
  border: 1px solid var(--color-primary);
  color: var(--color-primary);
}

.btn-outline:hover {
  background: var(--color-primary);
  color: #fff;
}

.btn-danger-outline {
  background: none;
  border: 1px solid var(--color-danger);
  color: var(--color-danger);
}

.btn-danger-outline:hover {
  background: var(--color-danger);
  color: #fff;
}

.btn-ghost {
  background: none;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  cursor: pointer;
  color: var(--color-text-secondary);
}

.btn-ghost:hover {
  background: var(--color-bg);
  color: var(--color-text);
}

.empty-state {
  text-align: center;
  padding: 4rem 2rem;
}

.empty-icon {
  font-size: 2.5rem;
  margin-bottom: 0.75rem;
}

.empty-title {
  font-size: 1.125rem;
  font-weight: 500;
  margin-bottom: 0.25rem;
}

.empty-hint {
  color: var(--color-text-secondary);
  font-size: 0.875rem;
}
</style>
