<template>
  <div class="layout">
    <header class="layout-header">
      <div class="header-left">
        <router-link to="/" class="header-brand">Update Hub</router-link>
      </div>
      <div class="header-right">
        <button @click="handleLogout" class="btn btn-ghost">Sign out</button>
      </div>
    </header>
    <main class="layout-main">
      <slot />
    </main>
  </div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuth } from '@/composables/useAuth'

const router = useRouter()
const { logout } = useAuth()

async function handleLogout() {
  await logout()
  router.push({ name: 'login' })
}
</script>

<style scoped>
.layout {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.layout-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 1.5rem;
  height: 52px;
  background: var(--color-surface);
  border-bottom: 1px solid var(--color-border);
}

.header-brand {
  font-size: 1rem;
  font-weight: 600;
  color: var(--color-text);
  text-decoration: none;
}

.header-brand:hover {
  color: var(--color-primary);
}

.btn-ghost {
  background: none;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  padding: 0.375rem 0.75rem;
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
  cursor: pointer;
  transition: all 0.15s;
}

.btn-ghost:hover {
  background: var(--color-bg);
  color: var(--color-text);
}

.layout-main {
  flex: 1;
}
</style>
