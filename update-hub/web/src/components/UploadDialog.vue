<template>
  <Teleport to="body">
    <div v-if="open" class="modal-backdrop" @click.self="$emit('close')">
      <div class="modal">
        <div class="modal-header">
          <h2 class="modal-title">Upload Release</h2>
          <button class="modal-close" @click="$emit('close')" aria-label="Close">&times;</button>
        </div>

        <form @submit.prevent="handleSubmit" class="modal-body">
          <div class="form-group">
            <label class="form-label">Channel</label>
            <select v-model="channel" class="form-input" required>
              <option value="" disabled>Select channel</option>
              <option v-for="ch in channels" :key="ch" :value="ch">{{ ch }}</option>
              <option value="__new">+ New channel…</option>
            </select>
            <input
              v-if="channel === '__new'"
              v-model="newChannel"
              class="form-input form-input--mt"
              placeholder="channel-name"
              pattern="[a-zA-Z0-9-]+"
              required
            />
          </div>

          <div class="form-group">
            <label class="form-label">Package files (.nupkg)</label>
            <input
              type="file"
              class="form-input form-file"
              accept=".nupkg"
              multiple
              required
              :disabled="uploading"
              @change="onFileChange"
            />
            <span v-if="files.length" class="form-hint">
              {{ files.length }} file(s) selected
            </span>
          </div>

          <div class="form-group">
            <label class="form-label">Notes <span class="form-optional">(optional)</span></label>
            <textarea
              v-model="notes"
              class="form-input form-textarea"
              rows="3"
              placeholder="Release notes or comments…"
              :disabled="uploading"
            />
          </div>

          <div v-if="error" class="modal-error">{{ error }}</div>

          <div class="modal-actions">
            <button type="button" class="btn btn-ghost" @click="$emit('close')" :disabled="uploading">Cancel</button>
            <button type="submit" class="btn btn-primary" :disabled="uploading || !effectiveChannel || files.length === 0">
              {{ uploading ? 'Uploading…' : 'Upload' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'

const props = defineProps<{
  open: boolean
  appId: string
  channels: string[]
}>()

const emit = defineEmits<{
  close: []
  success: [channel: string]
}>()

const channel = ref('')
const newChannel = ref('')
const files = ref<File[]>([])
const notes = ref('')
const uploading = ref(false)
const error = ref('')

const effectiveChannel = computed(() =>
  channel.value === '__new' ? newChannel.value : channel.value,
)

function onFileChange(e: Event) {
  const input = e.target as HTMLInputElement
  files.value = input.files ? Array.from(input.files) : []
}

async function handleSubmit() {
  if (!effectiveChannel.value || files.value.length === 0) return

  error.value = ''
  uploading.value = true

  try {
    const formData = new FormData()
    for (const f of files.value) {
      formData.append('files', f)
    }
    if (notes.value) {
      formData.append('notes', notes.value)
    }

    const res = await fetch(`/api/apps/${props.appId}/channels/${effectiveChannel.value}/releases`, {
      method: 'POST',
      credentials: 'include',
      body: formData,
      // Deliberately no Content-Type — browser sets multipart boundary
    })

    if (res.status === 401) {
      throw new Error('unauthenticated')
    }

    if (!res.ok) {
      let message = `Upload failed: ${res.status}`
      try {
        const body = await res.json()
        if (body.error) message = body.error
      } catch { /* use default */ }
      throw new Error(message)
    }

    emit('success', effectiveChannel.value)
    resetForm()
  } catch (err: unknown) {
    error.value = err instanceof Error ? err.message : 'Upload failed'
  } finally {
    uploading.value = false
  }
}

function resetForm() {
  channel.value = ''
  newChannel.value = ''
  files.value = []
  notes.value = ''
  error.value = ''
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
  max-width: 480px;
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
  gap: 1rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
}

.form-label {
  font-size: 0.8125rem;
  font-weight: 500;
  color: var(--color-text-secondary);
}

.form-optional {
  font-weight: 400;
  color: #999;
}

.form-input {
  padding: 0.625rem 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  font-size: 0.9375rem;
  outline: none;
  transition: border-color 0.15s;
  background: var(--color-surface);
  color: var(--color-text);
  font-family: inherit;
}

.form-input:focus {
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px rgba(0, 102, 204, 0.1);
}

.form-input:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.form-input--mt {
  margin-top: 0.25rem;
}

.form-textarea {
  resize: vertical;
  min-height: 60px;
}

.form-file {
  padding: 0.375rem 0.5rem;
  font-size: 0.8125rem;
}

.form-hint {
  font-size: 0.75rem;
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

.btn-primary {
  background: var(--color-primary);
  color: #fff;
}

.btn-primary:hover:not(:disabled) {
  background: var(--color-primary-hover);
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
