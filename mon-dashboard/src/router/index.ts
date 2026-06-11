import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

declare module 'vue-router' {
  interface RouteMeta {
    requiresAuth?: boolean
    requiresAdmin?: boolean
    requiresEditor?: boolean
    public?: boolean
  }
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', redirect: '/dashboard' },

    {
      path: '/login',
      name: 'Login',
      component: () => import('@/views/auth/LoginView.vue'),
    },

    // ── Routes protégées ───────────────────────────────────────────
    {
      path: '/dashboard',
      name: 'Dashboards',
      component: () => import('@/views/dashboard/DashboardListView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/dashboard/:id(\\d+)',
      name: 'DashboardViewer',
      component: () => import('@/views/dashboard/DashboardViewerView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/builder/:id(\\d+)?',
      name: 'Builder',
      component: () => import('@/views/builder/BuilderView.vue'),
      meta: { requiresAuth: true, requiresEditor: true },
    },
    {
      path: '/dataset',
      name: 'Dataset',
      component: () => import('@/views/dataset/DatasetUploadView.vue'),
      meta: { requiresAuth: true },
    },

    // ── Profil ────────────────────────────────────────────────────
    {
      path: '/profile',
      name: 'Profile',
      component: () => import('@/views/profile/ProfileView.vue'),
      meta: { requiresAuth: true },
    },

    // ── Route Admin ───────────────────────────────────────────────
    {
      path: '/admin',
      name: 'Admin',
      component: () => import('@/views/admin/AdminView.vue'),
      meta: { requiresAuth: true, requiresAdmin: true },
    },

    // ── Route publique de partage ──────────────────────────────────
    {
      path: '/share/:token',
      name: 'SharedDashboard',
      component: () => import('@/views/dashboard/DashboardViewerView.vue'),
      meta: { public: true },
    },

    // ── Fallback 404 ───────────────────────────────────────────────
    {
      path: '/:pathMatch(.*)*',
      redirect: '/dashboard',
    },
  ],
})

router.beforeEach((to) => {
  const authStore = useAuthStore()

  // Route protégée → non authentifié
  if (to.matched.some((r) => r.meta.requiresAuth) && !authStore.isAuthenticated) {
    return { name: 'Login', query: { redirect: to.fullPath } }
  }

  // Route admin → vérifier le rôle
  if (to.matched.some((r) => r.meta.requiresAdmin) && authStore.user?.role !== 'Admin') {
    return { name: 'Dashboards' }
  }

  // Route editor → Viewer n'a pas accès au builder
  if (to.matched.some((r) => r.meta.requiresEditor) && authStore.user?.role === 'Viewer') {
    return { name: 'Dashboards' }
  }

  // Déjà connecté → pas besoin de la page Login
  if (to.name === 'Login' && authStore.isAuthenticated) {
    return { name: 'Dashboards' }
  }
})

export default router
