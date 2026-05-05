<template>
  <Teleport to="body">
    <div v-if="open" class="modal-backdrop" @click.self="$emit('close')">
      <div class="modal">
        <div class="modal-header">
          <h2 class="modal-title">Promote Version</h2>
          <button class="modal-close" @click="$emit('close')" aria-label="Close">&times;</button>
        </div>

        <div class="modal-body">
          <div class="promote-summary">
            Promote <strong>{{ version }}</strong> from
            <span class="badge badge--source">{{ sourceChannel }}</span> to
          </div>

          <div class="form-group">
            <label class="form-label">Target channel</label>
            <select v-model="targetChannel" class="form-input" required>
              <option value="" disabled>Select target</option>
              <option
                v-for="ch in targetOptions"
                :key="ch"
                :value="ch"
              >{{ ch }}</option>
              <option value="__new">+ New channel…</option>
            </select>
            <input
              v-if="targetChannel === '__new'"
              v-model="newChannel"
              class="form-input form-input--mt"
              placeholder="channel-name"
              pattern="[a-zA-Z0-9-]+"
              required
            />
          </div>

          <div v-if="error" class="modal-error">{{ error }}</div>

          <div class="modal-actions">
            <button class="btn btn-ghost" @click="$emit('close')" :disabled="promoting">Cancel</button>
            <button
              class="btn btn-primary"
              :disabled="promoting || !effectiveTarget"
              @click="handlePromote"
            >
              {{ promoting ? 'Promoting…' : 'Promote' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { apiFetch } from '@/api/client'

const props = defineProps<{
  open: boolean
  appId: string
  version: string
  sourceChannel: string
  channels: string[]
}>()

const emit = defineEmits<{
  close: []
  success: [target: string]
}>()

const targetChannel = ref('')
const newChannel = ref('')
const promoting = ref(false)
const error = ref('')

const targetOptions = computed(() =>
  props.channels.filter((ch) => ch !== props.sourceChannel),
)

const effectiveTarget = computed(() =>
  targetChannel.value === '__new' ? newChannel.value : targetChannel.value,
)

async function handlePromote() {
  if (!effectiveTarget.value) return

  error.value = ''
  promoting.value = true

  try {
    await apiFetch<{ promoted: string; from: string; to: string; files_copied: number }>(
      `/apps/${props.appId}/channels/${effectiveTarget.value}/promote?from=${encodeURIComponent(props.sourceChannel)}&version=${encodeURIComponent(props.version)}`,
      { method: 'POST' },
    )
    emit('success', effectiveTarget.value)
    targetChannel.value = ''
    newChannel.value = ''
  } catch (err: unknown) {
    error.value = err instanceof Error ? err.message : 'Promote failed'
  } finally {
    promoting.value = false
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
  max-width: 420px;
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

.promote-summary {
  font-size: 0.9375rem;
  line-height: 1.6;
}

.badge {
  display: inline-block;
  padding: 0.125rem 0.5rem;
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 600;
}

.badge--source {
  background: #e8f0fe;
  color: #1a56db;
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

.form-input {
  padding: 0.625rem 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  font-size: 0.9375rem;
  outline: none;
  transition: border-color 0.15s;
  background: var(--color-surface);
  color: var(--color-text);
}

.form-input:focus {
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px rgba(0, 102, 204, 0.1);
}

.form-input--mt {
  margin-top: 0.25rem;
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
