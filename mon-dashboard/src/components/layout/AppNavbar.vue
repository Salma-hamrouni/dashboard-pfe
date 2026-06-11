<template>
  <header class="navbar">
    <div class="navbar-left">
      <div class="page-info">
        <h1 class="page-title">{{ title }}</h1>
        <span v-if="breadcrumb" class="page-breadcrumb">{{ breadcrumb }}</span>
      </div>
    </div>

    <div class="navbar-right">
      <!-- Date -->
      <div class="date-chip">
        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <rect x="3" y="4" width="18" height="18" rx="2"/><line x1="16" y1="2" x2="16" y2="6"/>
          <line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/>
        </svg>
        <span>{{ currentDate }}</span>
      </div>

      <!-- User avatar + dropdown -->
      <div class="user-menu" v-if="authStore.user">
        <button class="avatar-btn" @click="dropdownOpen = !dropdownOpen" @blur="onBlur">
          <div class="avatar" :style="{ background: avatarColor }">
            {{ initials }}
          </div>
          <div class="user-info">
            <span class="user-email">{{ shortEmail }}</span>
            <span class="user-role ds-badge" :class="roleBadge">{{ authStore.user.role }}</span>
          </div>
          <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"
               :style="{ transform: dropdownOpen ? 'rotate(180deg)' : 'none', transition: 'transform 0.2s' }">
            <polyline points="6 9 12 15 18 9"/>
          </svg>
        </button>

        <transition name="dropdown">
          <div v-if="dropdownOpen" class="dropdown" @mousedown.prevent>
            <div class="dropdown-header">
              <div class="avatar avatar-lg" :style="{ background: avatarColor }">{{ initials }}</div>
              <div>
                <p class="d-name">{{ displayName }}</p>
                <p class="d-role">{{ authStore.user.role }}</p>
              </div>
            </div>
            <div class="dropdown-divider"/>
            <button class="dropdown-item" @click="goToProfile">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/>
              </svg>
              Mon profil
            </button>
            <button class="dropdown-item" @click="goToDashboard">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/>
                <rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/>
              </svg>
              Mes dashboards
            </button>
            <div class="dropdown-divider"/>
            <button class="dropdown-item danger" @click="handleLogout">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/>
                <polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/>
              </svg>
              Déconnexion
            </button>
          </div>
        </transition>
      </div>
    </div>
  </header>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const dropdownOpen = ref(false)

function onBlur() {
  setTimeout(() => { dropdownOpen.value = false }, 150)
}

const title = computed(() => {
  const p = route.path
  if (p.startsWith('/builder')) return 'Builder'
  if (p.startsWith('/dashboard/')) return 'Visualisation'
  if (p === '/dashboard') return 'Mes Dashboards'
  if (p === '/dataset') return 'Importer des données'
  return 'DashGen'
})

const breadcrumb = computed(() => {
  const p = route.path
  if (p.startsWith('/builder')) return 'Éditer'
  if (p.startsWith('/dashboard/')) return 'Aperçu'
  if (p === '/dashboard') return 'Accueil'
  if (p === '/dataset') return 'Données'
  return ''
})

const currentDate = computed(() =>
  new Date().toLocaleDateString('fr-FR', { weekday: 'short', day: 'numeric', month: 'short' })
)

const initials = computed(() => {
  const name = authStore.user?.name
  if (name && name.trim()) return name.trim().slice(0, 2).toUpperCase()
  const email = authStore.user?.email ?? ''
  return email.slice(0, 2).toUpperCase()
})

const displayName = computed(() => {
  const name = authStore.user?.name
  if (name && name.trim()) return name.trim().length > 18 ? name.trim().slice(0, 15) + '…' : name.trim()
  const email = authStore.user?.email ?? ''
  return email.length > 18 ? email.slice(0, 15) + '…' : email
})

const shortEmail = computed(() => {
  const email = authStore.user?.email ?? ''
  return email.length > 18 ? email.slice(0, 15) + '…' : email
})

const avatarColor = computed(() => {
  const colors = ['#10b981', '#3b82f6', '#8b5cf6', '#f59e0b', '#ef4444', '#06b6d4']
  const i = (authStore.user?.id ?? 0) % colors.length
  return colors[i]
})

const roleBadge = computed(() => {
  const r = (authStore.user?.role ?? '').toLowerCase()
  if (r === 'admin') return 'ds-badge-red'
  if (r === 'editor') return 'ds-badge-blue'
  return 'ds-badge-gray'
})

function goToProfile() { dropdownOpen.value = false; router.push('/profile') }
function goToDashboard() { dropdownOpen.value = false; router.push('/dashboard') }

function handleLogout() {
  dropdownOpen.value = false
  authStore.logout()
  router.push('/login')
}
</script>

<style scoped>
.navbar {
  height: var(--navbar-height);
  background: var(--color-surface);
  border-bottom: 1px solid var(--color-border);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 var(--space-6);
  position: sticky;
  top: 0;
  z-index: 50;
  flex-shrink: 0;
}

.navbar-left { display: flex; align-items: center; gap: var(--space-4); }
.page-info { display: flex; flex-direction: column; gap: 1px; }
.page-title {
  font-size: var(--text-lg);
  font-weight: var(--weight-bold);
  color: var(--color-text);
  line-height: 1;
}
.page-breadcrumb {
  font-size: var(--text-xs);
  color: var(--color-text-muted);
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.navbar-right { display: flex; align-items: center; gap: var(--space-2); }

/* Date */
.date-chip {
  display: flex; align-items: center; gap: 5px;
  padding: 5px 10px;
  background: var(--color-surface-2);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-full);
  font-size: var(--text-xs);
  color: var(--color-text-muted);
  white-space: nowrap;
}

/* User menu */
.user-menu { position: relative; }

.avatar-btn {
  display: flex; align-items: center; gap: var(--space-2);
  background: var(--color-surface-2);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: 5px 10px 5px 5px;
  cursor: pointer;
  transition: border-color var(--transition-fast), background var(--transition-fast);
  color: var(--color-text-secondary);
}
.avatar-btn:hover { border-color: var(--color-border-hover); background: var(--color-surface-3); }

.avatar {
  width: 28px; height: 28px;
  border-radius: var(--radius-sm);
  display: flex; align-items: center; justify-content: center;
  font-size: var(--text-xs); font-weight: var(--weight-bold);
  color: #fff; flex-shrink: 0;
}
.avatar-lg { width: 36px; height: 36px; border-radius: var(--radius-md); font-size: var(--text-sm); }

.user-info { display: flex; flex-direction: column; gap: 1px; text-align: left; }
.user-email { font-size: var(--text-xs); color: var(--color-text); font-weight: var(--weight-medium); }
.user-role { font-size: 10px; padding: 1px 5px; }

/* Dropdown */
.dropdown {
  position: absolute;
  top: calc(100% + 8px);
  right: 0;
  background: var(--color-surface-2);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  min-width: 220px;
  box-shadow: var(--shadow-lg);
  z-index: 200;
  overflow: hidden;
}

.dropdown-header {
  display: flex; align-items: center; gap: var(--space-3);
  padding: var(--space-4);
  background: var(--color-surface-3);
}
.d-email, .d-name { font-size: var(--text-sm); color: var(--color-text); font-weight: var(--weight-medium); margin: 0 0 2px; }
.d-role  { font-size: var(--text-xs); color: var(--color-text-muted); margin: 0; text-transform: capitalize; }

.dropdown-divider { height: 1px; background: var(--color-border); margin: 2px 0; }

.dropdown-item {
  width: 100%; display: flex; align-items: center; gap: var(--space-3);
  padding: var(--space-3) var(--space-4);
  background: none; border: none; cursor: pointer;
  color: var(--color-text-secondary); font-size: var(--text-sm);
  font-family: var(--font-sans); text-align: left;
  transition: background var(--transition-fast), color var(--transition-fast);
}
.dropdown-item:hover { background: var(--color-surface-3); color: var(--color-text); }
.dropdown-item.danger { color: var(--color-danger); }
.dropdown-item.danger:hover { background: rgba(239, 68, 68, 0.1); }

/* Transition */
.dropdown-enter-active, .dropdown-leave-active { transition: opacity 0.15s, transform 0.15s; }
.dropdown-enter-from, .dropdown-leave-to { opacity: 0; transform: translateY(-6px); }
</style>
