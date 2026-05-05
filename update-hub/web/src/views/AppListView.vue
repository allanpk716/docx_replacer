<template>
  <AppLayout>
    <div class="apps-page">
      <div class="page-header">
        <div>
          <h1 class="page-title">Applications</h1>
          <p class="page-subtitle">Select an application to manage versions</p>
        </div>
      </div>

      <div v-if="loading" class="loading-state">
        <span class="spinner"></span>
        <span>Loading applications…</span>
      </div>

      <div v-else-if="error" class="error-state">
        <p>Failed to load applications</p>
        <p class="error-detail">{{ error }}</p>
        <button class="btn btn-ghost btn-sm" @click="loadApps">Retry</button>
      </div>

      <div v-else-if="apps.length === 0" class="empty-state">
        <p class="empty-icon">📦</p>
        <p class="empty-title">No applications yet</p>
        <p class="empty-hint">Upload a release to register an application</p>
      </div>

      <div v-else class="app-grid">
        <router-link
          v-for="app in apps"
          :key="app.id"
          :to="{ name: 'app-detail', params: { appId: app.id } }"
          class="app-card"
        >
          <div class="app-card-icon">📱</div>
          <div class="app-card-info">
            <h2 class="app-card-name">{{ app.id }}</h2>
            <div class="app-card-channels">
              <span
                v-for="ch in app.channels"
                :key="ch"
                :class="['channel-badge', `channel-badge--${ch}`]"
              >{{ ch }}</span>
            </div>
          </div>
          <span class="app-card-arrow">→</span>
        </router-link>
      </div>
    </div>
  </AppLayout>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { apiFetch } from '@/api/client'
import AppLayout from '@/components/AppLayout.vue'

interface AppInfo {
  id: string
  channels: string[]
}

const apps = ref<AppInfo[]>([])
const loading = ref(true)
const error = ref('')

async function loadApps() {
  loading.value = true
  error.value = ''
  try {
    apps.value = await apiFetch<AppInfo[]>('/apps')
  } catch (err: unknown) {
    error.value = err instanceof Error ? err.message : 'Unknown error'
  } finally {
    loading.value = false
  }
}

onMounted(loadApps)
</script>

<style scoped>
.apps-page {
  padding: 1.5rem;
  max-width: 960px;
  margin: 0 auto;
}

.page-header {
  margin-bottom: 1.5rem;
}

.page-title {
  font-size: 1.5rem;
  font-weight: 600;
  margin-bottom: 0.25rem;
}

.page-subtitle {
  color: var(--color-text-secondary);
  font-size: 0.875rem;
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

@keyframes spin {
  to { transform: rotate(360deg); }
}

.error-state {
  text-align: center;
  padding: 3rem;
}

.error-state p {
  margin-bottom: 0.25rem;
}

.error-detail {
  color: var(--color-text-secondary);
  font-size: 0.875rem;
  margin-bottom: 1rem !important;
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

.app-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1rem;
}

.app-card {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1.25rem;
  background: var(--color-surface);
  border-radius: var(--radius);
  box-shadow: var(--shadow);
  text-decoration: none;
  color: inherit;
  transition: box-shadow 0.15s, transform 0.15s;
}

.app-card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  transform: translateY(-1px);
}

.app-card-icon {
  font-size: 1.75rem;
  flex-shrink: 0;
}

.app-card-info {
  flex: 1;
  min-width: 0;
}

.app-card-name {
  font-size: 1rem;
  font-weight: 600;
  margin-bottom: 0.375rem;
  word-break: break-all;
}

.app-card-channels {
  display: flex;
  flex-wrap: wrap;
  gap: 0.375rem;
}

.channel-badge {
  display: inline-block;
  padding: 0.125rem 0.5rem;
  border-radius: 999px;
  font-size: 0.6875rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.025em;
}

.channel-badge--stable {
  background: #ecfdf5;
  color: #065f46;
}

.channel-badge--beta {
  background: #fef3c7;
  color: #92400e;
}

.channel-badge--alpha {
  background: #ede9fe;
  color: #5b21b6;
}

.channel-badge--dev {
  background: #e0e7ff;
  color: #3730a3;
}

.app-card-arrow {
  color: var(--color-text-secondary);
  font-size: 1.125rem;
  flex-shrink: 0;
}

.btn-sm {
  padding: 0.375rem 0.75rem;
  font-size: 0.8125rem;
}

.btn-ghost {
  background: none;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.15s;
  color: var(--color-text-secondary);
}

.btn-ghost:hover {
  background: var(--color-bg);
  color: var(--color-text);
}
</style>
