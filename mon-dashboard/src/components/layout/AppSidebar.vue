<template>
  <aside class="sidebar" :class="{ collapsed }">
    <!-- Logo -->
    <div class="sidebar-logo">
      <div class="logo-icon">
        <svg width="28" height="28" viewBox="0 0 48 48" fill="none">
          <rect x="4" y="24" width="8" height="16" rx="2" fill="#6EE7B7" />
          <rect x="16" y="16" width="8" height="24" rx="2" fill="#34D399" />
          <rect x="28" y="8" width="8" height="32" rx="2" fill="#10B981" />
          <rect x="40" y="18" width="8" height="22" rx="2" fill="#059669" />
          <path
            d="M6 20 L20 12 L32 18 L44 10"
            stroke="#6EE7B7"
            stroke-width="2.5"
            stroke-linecap="round"
            stroke-linejoin="round"
          />
        </svg>
      </div>
      <span class="logo-text" v-show="!collapsed">Dash<span class="accent">Gen</span></span>
      <button class="collapse-btn" @click="$emit('toggle')">
        <svg
          width="16"
          height="16"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2.5"
        >
          <polyline v-if="!collapsed" points="9,18 15,12 9,6" />
          <polyline v-else points="15,18 9,12 15,6" />
        </svg>
      </button>
    </div>

    <!-- Navigation -->
    <nav class="sidebar-nav">
      <div class="nav-section">
        <span class="nav-label" v-show="!collapsed">Menu</span>
        <router-link
          v-for="item in navItems"
          :key="item.to"
          :to="item.to"
          class="nav-item"
          :class="{ active: $route.path.startsWith(item.to), 'nav-item--admin': item.admin }"
        >
          <span class="nav-icon" v-html="item.icon"></span>
          <span class="nav-text" v-show="!collapsed">{{ item.label }}</span>
          <span class="nav-badge" v-if="item.badge && !collapsed">{{ item.badge }}</span>
          <div class="nav-tooltip" v-if="collapsed">{{ item.label }}</div>
        </router-link>
      </div>
    </nav>

    <!-- User section -->
    <div class="sidebar-user">
      <div class="user-avatar">{{ userInitial }}</div>
      <div class="user-info" v-show="!collapsed">
        <span class="user-email">{{ userName }}</span>
        <span class="user-role" :class="{ 'role-admin': isAdmin }">
          {{ isAdmin ? '⚡ Administrateur' : 'Utilisateur' }}
        </span>
      </div>
      <button class="logout-btn" @click="handleLogout" v-show="!collapsed" title="Déconnexion">
        <svg
          width="16"
          height="16"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
        >
          <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
          <polyline points="16,17 21,12 16,7" />
          <line x1="21" y1="12" x2="9" y2="12" />
        </svg>
      </button>
    </div>
  </aside>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const props = defineProps<{ collapsed: boolean }>()
defineEmits(['toggle'])

const router = useRouter()
const authStore = useAuthStore()

// Utilise les données du store d'authentification
const userEmail    = computed(() => authStore.user?.email || 'test@test.com')
const userName     = computed(() => authStore.user?.name || userEmail.value)
const userInitial  = computed(() => {
  const name = authStore.user?.name
  if (name && name.trim()) return name.trim().charAt(0).toUpperCase()
  return userEmail.value.charAt(0).toUpperCase()
})

const isAdmin  = computed(() => authStore.user?.role === 'Admin')
const isViewer = computed(() => authStore.user?.role === 'Viewer')

const navItems = computed(() => [
  {
    to: '/dashboard',
    label: 'Mes Dashboards',
    badge: null,
    admin: false,
    icon: `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
      <rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/>
      <rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/>
    </svg>`,
  },
  ...(!isViewer.value ? [
    {
      to: '/dataset',
      label: 'Importer des données',
      badge: null,
      admin: false,
      icon: `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
        <polyline points="17,8 12,3 7,8"/><line x1="12" y1="3" x2="12" y2="15"/>
      </svg>`,
    },
    {
      to: '/builder',
      label: 'Créer un dashboard',
      badge: 'New',
      admin: false,
      icon: `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
      </svg>`,
    },
  ] : []),
  ...(isAdmin.value ? [{
    to: '/admin',
    label: 'Administration',
    badge: null,
    admin: true,
    icon: `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
      <path d="M12 1l9 4v6c0 5.55-3.84 10.74-9 12-5.16-1.26-9-6.45-9-12V5l9-4z"/>
    </svg>`,
  }] : []),
])

function handleLogout() {
  authStore.logout()
  router.push('/login')
}
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Syne:wght@700;800&family=DM+Sans:wght@300;400;500&display=swap');

.sidebar {
  width: 240px;
  min-height: 100vh;
  background: #060f0e;
  border-left: 1px solid rgba(16, 185, 129, 0.1);
  display: flex;
  flex-direction: column;
  transition: width 0.3s cubic-bezier(0.16, 1, 0.3, 1);
  position: relative;
  z-index: 100;
  font-family: 'DM Sans', sans-serif;
}

.sidebar.collapsed {
  width: 68px;
}

/* Logo */
.sidebar-logo {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 20px 16px;
  border-bottom: 1px solid rgba(16, 185, 129, 0.08);
  min-height: 68px;
}

.logo-icon {
  width: 38px;
  height: 38px;
  background: rgba(16, 185, 129, 0.1);
  border: 1px solid rgba(16, 185, 129, 0.2);
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.logo-text {
  font-family: 'Syne', sans-serif;
  font-size: 20px;
  font-weight: 800;
  color: #f0fdf9;
  letter-spacing: -0.5px;
  flex: 1;
  white-space: nowrap;
  overflow: hidden;
}

.logo-text .accent {
  color: #10b981;
}

.collapse-btn {
  margin-left: auto;
  background: none;
  border: none;
  cursor: pointer;
  color: rgba(240, 253, 249, 0.3);
  padding: 4px;
  border-radius: 6px;
  display: flex;
  align-items: center;
  transition: all 0.2s;
  flex-shrink: 0;
}
.collapse-btn:hover {
  color: #10b981;
  background: rgba(16, 185, 129, 0.1);
}

/* Nav */
.sidebar-nav {
  flex: 1;
  padding: 16px 10px;
  overflow-y: auto;
}

.nav-section {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.nav-label {
  font-size: 10px;
  font-weight: 500;
  color: rgba(240, 253, 249, 0.25);
  text-transform: uppercase;
  letter-spacing: 1px;
  padding: 0 8px;
  margin-bottom: 6px;
  white-space: nowrap;
}

.nav-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 10px;
  border-radius: 10px;
  text-decoration: none;
  color: rgba(240, 253, 249, 0.45);
  transition: all 0.2s;
  position: relative;
  white-space: nowrap;
  overflow: hidden;
}

.nav-item:hover {
  color: #f0fdf9;
  background: rgba(255, 255, 255, 0.05);
}

.nav-item.active {
  color: #10b981;
  background: rgba(16, 185, 129, 0.1);
  border: 1px solid rgba(16, 185, 129, 0.15);
}

.nav-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  width: 20px;
}

.nav-text {
  font-size: 13.5px;
  font-weight: 400;
  flex: 1;
}

.nav-badge {
  font-size: 10px;
  font-weight: 600;
  background: linear-gradient(135deg, #10b981, #059669);
  color: #fff;
  padding: 2px 7px;
  border-radius: 20px;
}

/* Tooltip when collapsed */
.nav-tooltip {
  display: none;
  position: absolute;
  right: calc(100% + 12px);
  background: #0a1f1a;
  color: #f0fdf9;
  font-size: 12px;
  padding: 6px 10px;
  border-radius: 8px;
  border: 1px solid rgba(16, 185, 129, 0.2);
  white-space: nowrap;
  z-index: 200;
  pointer-events: none;
}

.collapsed .nav-item:hover .nav-tooltip {
  display: block;
}

/* User */
.sidebar-user {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 14px 14px;
  border-top: 1px solid rgba(16, 185, 129, 0.08);
  min-height: 64px;
}

.user-avatar {
  width: 34px;
  height: 34px;
  background: linear-gradient(135deg, #10b981, #059669);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: 'Syne', sans-serif;
  font-weight: 700;
  font-size: 14px;
  color: #fff;
  flex-shrink: 0;
}

.user-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 1px;
  overflow: hidden;
}

.user-email {
  font-size: 12px;
  color: rgba(240, 253, 249, 0.6);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.user-role {
  font-size: 11px;
  color: rgba(16, 185, 129, 0.7);
}

.logout-btn {
  background: none;
  border: none;
  cursor: pointer;
  color: rgba(240, 253, 249, 0.25);
  padding: 6px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  transition: all 0.2s;
  flex-shrink: 0;
}
.logout-btn:hover {
  color: #f87171;
  background: rgba(248, 113, 113, 0.1);
}

/* Admin nav item */
.nav-item--admin {
  color: rgba(248, 113, 113, 0.6);
  margin-top: 8px;
  border-top: 1px solid rgba(255,255,255,0.05);
  padding-top: 14px;
}
.nav-item--admin:hover {
  color: #f87171;
  background: rgba(248, 113, 113, 0.08);
}
.nav-item--admin.active {
  color: #f87171;
  background: rgba(248, 113, 113, 0.1);
  border: 1px solid rgba(248, 113, 113, 0.2);
}

/* Admin role badge */
.role-admin {
  color: #f87171 !important;
}
</style>
