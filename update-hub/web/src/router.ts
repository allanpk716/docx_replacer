import { createRouter, createWebHistory } from 'vue-router'
import { useAuth } from '@/composables/useAuth'
import LoginView from '@/views/LoginView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: LoginView,
      meta: { requiresAuth: false },
    },
    {
      path: '/',
      name: 'apps',
      component: () => import('@/views/AppListView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/apps/:appId',
      name: 'app-detail',
      component: () => import('@/views/AppDetailView.vue'),
      meta: { requiresAuth: true },
      props: true,
    },
  ],
})

router.beforeEach(async (to) => {
  const { isAuthenticated, checking, checkAuth } = useAuth()

  // Wait for initial auth check
  if (checking.value) {
    await checkAuth()
  }

  if (to.meta.requiresAuth !== false && !isAuthenticated.value) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  if (to.name === 'login' && isAuthenticated.value) {
    return { name: 'apps' }
  }
})

export default router
