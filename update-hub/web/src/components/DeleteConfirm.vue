<template>
  <Teleport to="body">
    <div v-if="open" class="modal-backdrop" @click.self="$emit('close')">
      <div class="modal">
        <div class="modal-header">
          <h2 class="modal-title">Delete Version</h2>
          <button class="modal-close" @click="$emit('close')" aria-label="Close">&times;</button>
        </div>

        <div class="modal-body">
          <p class="confirm-text">
            Delete <strong>{{ version }}</strong> from
            <span class="badge">{{ channel }}</span>?
          </p>
          <p class="confirm-hint">This will remove all package files for this version. This action cannot be undone.</p>

          <div v-if="error" class="modal-error">{{ error }}</div>

          <div class="modal-actions">
            <button class="btn btn-ghost" @click="$emit('close')" :disabled="deleting">Cancel</button>
            <button class="btn btn-danger" :disabled="deleting" @click="handleDelete">
              {{ deleting ? 'Deleting…' : 'Delete' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { apiFetch } from '@/api/client'

const props = defineProps<{
  open: boolean
  appId: string
  channel: string
  version: string
}>()

const emit = defineEmits<{
  close: []
  success: []
}>()

const deleting = ref(false)
const error = ref('')

async function handleDelete() {
  error.value = ''
  deleting.value = true

  try {
    await apiFetch<{ channel: string; version: string; files_deleted: number }>(
      `/apps/${props.appId}/channels/${props.channel}/versions/${encodeURIComponent(props.version)}`,
      { method: 'DELETE' },
    )
    emit('success')
  } catch (err: unknown) {
    error.value = err instanceof Error ? err.message : 'Delete failed'
  } finally {
    deleting.value = false
  }
}
</script>

<style scoped>
.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.4);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 100;
}

.modal {
  background: var(--color-surface);
  border-radius: var(--radius);
  box-shadow: 0 8px 30px rgba(0, 0, 0, 0.12);
  width: 100%;
  max-width: 400px;
  margin: 1rem;
}

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem 1.25rem;
  border-bottom: 1px solid var(--color-border);
}

.modal-title {
  font-size: 1.0625rem;
  font-weight: 600;
}

.modal-close {
  background: none;
  border: none;
  font-size: 1.25rem;
  color: var(--color-text-secondary);
  cursor: pointer;
  padding: 0.25rem;
  line-height: 1;
}

.modal-close:hover {
  color: var(--color-text);
}

.modal-body {
  padding: 1.25rem;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.confirm-text {
  font-size: 0.9375rem;
}

.badge {
  display: inline-block;
  padding: 0.125rem 0.5rem;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 600;
  background: #e8f0fe;
  color: #1a56db;
}

.confirm-hint {
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
}

.modal-error {
  padding: 0.5rem 0.75rem;
  background: #fff0f0;
  border: 1px solid #fcc;
  border-radius: 6px;
  color: var(--color-danger);
  font-size: 0.8125rem;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
  padding-top: 0.5rem;
}

.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.5rem 1rem;
  border: none;
  border-radius: 6px;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: background-color 0.15s;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-danger {
  background: var(--color-danger);
  color: #fff;
}

.btn-danger:hover:not(:disabled) {
  background: var(--color-danger-hover);
}

.btn-ghost {
  background: none;
  border: 1px solid var(--color-border);
  color: var(--color-text-secondary);
}

.btn-ghost:hover:not(:disabled) {
  background: var(--color-bg);
  color: var(--color-text);
}
</style>
