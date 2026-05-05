import { ref } from 'vue'
import { apiFetch } from '@/api/client'

const isAuthenticated = ref(false)
const checking = ref(true)

export function useAuth() {
  async function checkAuth(): Promise<boolean> {
    try {
      await apiFetch<{ ok: boolean }>('/auth/check')
      isAuthenticated.value = true
      return true
    } catch {
      isAuthenticated.value = false
      return false
    } finally {
      checking.value = false
    }
  }

  async function login(password: string): Promise<void> {
    await apiFetch<{ ok: boolean }>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ password }),
    })
    isAuthenticated.value = true
  }

  async function logout(): Promise<void> {
    // Clear session cookie client-side (no server logout endpoint needed)
    document.cookie = 'session=; path=/; max-age=0'
    isAuthenticated.value = false
  }

  return {
    isAuthenticated,
    checking,
    checkAuth,
    login,
    logout,
  }
}
