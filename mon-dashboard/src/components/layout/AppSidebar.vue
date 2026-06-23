<template>
  <aside class="sidebar" :class="{ collapsed }">

    <!-- Toggle tab (always visible on edge) -->
    <button class="sidebar-toggle-tab" @click="$emit('toggle')" :title="collapsed ? 'Ouvrir le menu' : 'Réduire le menu'">
      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
        <polyline v-if="!collapsed" points="9,18 15,12 9,6" />
        <polyline v-else           points="15,18 9,12 15,6" />
      </svg>
    </button>

    <!-- Logo -->
    <div class="sidebar-logo">
      <div class="logo-icon">
        <svg width="26" height="26" viewBox="0 0 48 48" fill="none">
          <rect x="4"  y="24" width="8"  height="16" rx="2" fill="#1B6B3A" opacity="0.6"/>
          <rect x="16" y="16" width="8"  height="24" rx="2" fill="#1B6B3A" opacity="0.85"/>
          <rect x="28" y="8"  width="8"  height="32" rx="2" fill="#1B6B3A"/>
          <rect x="40" y="18" width="8"  height="22" rx="2" fill="#134E2A"/>
          <path d="M6 20 L20 12 L32 18 L44 10" stroke="#1B6B3A" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
      </div>
      <transition name="fade-text">
        <span class="logo-text" v-if="!collapsed">Dash<span class="accent">Gen</span></span>
      </transition>
    </div>

    <!-- Navigation -->
    <nav class="sidebar-nav">
      <span class="nav-label" v-if="!collapsed">Menu</span>
      <router-link
        v-for="item in navItems"
        :key="item.to"
        :to="item.to"
        class="nav-item"
        :class="{ active: $route.path.startsWith(item.to), 'nav-item--admin': item.admin }"
      >
        <span class="nav-icon" v-html="item.icon"></span>
        <transition name="fade-text">
          <span class="nav-text" v-if="!collapsed">{{ item.label }}</span>
        </transition>
        <transition name="fade-text">
          <span class="nav-badge" v-if="item.badge && !collapsed">{{ item.badge }}</span>
        </transition>
        <!-- Tooltip quand collapsed -->
        <div class="nav-tooltip" v-if="collapsed">{{ item.label }}</div>
      </router-link>
    </nav>

    <!-- Footer: user + admin btn + logout -->
    <div class="sidebar-footer">
      <!-- Back to PoubCoin -->
      <a
        class="footer-btn poubcoin-btn"
        href="http://localhost:8081"
        target="_blank"
        rel="noopener"
        :title="collapsed ? 'Ouvrir PoubCoin' : ''"
      >
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/>
          <polyline points="9,22 9,12 15,12 15,22"/>
        </svg>
        <transition name="fade-text">
          <span v-if="!collapsed">PoubCoin</span>
        </transition>
        <div class="nav-tooltip" v-if="collapsed">PoubCoin</div>
      </a>

      <!-- Back to admin -->
      <button
        class="footer-btn admin-btn"
        @click="$emit('back-to-admin')"
        :title="collapsed ? 'Panneau admin' : ''"
      >
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <polyline points="15,18 9,12 15,6"/>
        </svg>
        <transition name="fade-text">
          <span v-if="!collapsed">Admin</span>
        </transition>
        <div class="nav-tooltip" v-if="collapsed">Panneau admin</div>
      </button>

      <div class="sidebar-user">
        <div class="user-avatar">{{ userInitial }}</div>
        <transition name="fade-text">
          <div class="user-info" v-if="!collapsed">
            <span class="user-email">{{ userName }}</span>
            <span class="user-role" :class="{ 'role-admin': isAdmin }">
              {{ isAdmin ? '⚡ Admin' : 'Utilisateur' }}
            </span>
          </div>
        </transition>
        <transition name="fade-text">
          <button class="logout-btn" v-if="!collapsed" @click="handleLogout" title="Déconnexion">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/>
              <polyline points="16,17 21,12 16,7"/>
              <line x1="21" y1="12" x2="9" y2="12"/>
            </svg>
          </button>
        </transition>
        <div class="nav-tooltip" v-if="collapsed">{{ userName }}</div>
      </div>
    </div>

  </aside>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const props = defineProps<{ collapsed: boolean }>()
defineEmits(['toggle', 'back-to-admin'])

const router = useRouter()
const authStore = useAuthStore()

const userEmail  = computed(() => authStore.user?.email || 'test@test.com')
const userName   = computed(() => authStore.user?.name || userEmail.value)
const userInitial = computed(() => {
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

/* ── Sidebar shell ─────────────────────────────────────── */
.sidebar {
  width: 220px;
  min-height: 100vh;
  background: #FFFFFF;
  border-left: 1px solid #E4E8E4;
  display: flex;
  flex-direction: column;
  transition: width 0.28s cubic-bezier(0.16, 1, 0.3, 1);
  position: relative;
  z-index: 100;
  font-family: 'DM Sans', sans-serif;
  overflow: visible;
}
.sidebar.collapsed {
  width: 64px;
}

/* ── Toggle tab (floating on the left edge) ────────────── */
.sidebar-toggle-tab {
  position: absolute;
  left: -14px;
  top: 24px;
  width: 28px;
  height: 28px;
  background: #FFFFFF;
  border: 1px solid #E4E8E4;
  border-radius: 50%;
  box-shadow: 0 2px 8px rgba(0,0,0,0.10);
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  color: #4B5E52;
  z-index: 200;
  transition: all 0.2s;
}
.sidebar-toggle-tab:hover {
  background: #EEF7F1;
  border-color: rgba(27,107,58,0.35);
  color: #1B6B3A;
  box-shadow: 0 4px 12px rgba(27,107,58,0.15);
}

/* ── Logo ───────────────────────────────────────────────── */
.sidebar-logo {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 18px 14px 16px;
  border-bottom: 1px solid #E4E8E4;
  min-height: 64px;
  overflow: hidden;
}

.logo-icon {
  width: 36px;
  height: 36px;
  background: rgba(27,107,58,0.08);
  border: 1px solid rgba(27,107,58,0.18);
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.logo-text {
  font-family: 'Syne', sans-serif;
  font-size: 18px;
  font-weight: 800;
  color: #111714;
  letter-spacing: -0.5px;
  white-space: nowrap;
}
.logo-text .accent { color: #1B6B3A; }

/* ── Nav ────────────────────────────────────────────────── */
.sidebar-nav {
  flex: 1;
  padding: 14px 8px;
  display: flex;
  flex-direction: column;
  gap: 2px;
  overflow: hidden;
}

.nav-label {
  font-size: 10px;
  font-weight: 600;
  color: #94A99A;
  text-transform: uppercase;
  letter-spacing: 1px;
  padding: 0 8px;
  margin-bottom: 8px;
  display: block;
  white-space: nowrap;
}

.nav-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 9px 10px;
  border-radius: 10px;
  text-decoration: none;
  color: #4B5E52;
  transition: all 0.18s;
  position: relative;
  white-space: nowrap;
  overflow: hidden;
}
.nav-item:hover  { color: #111714; background: rgba(27,107,58,0.06); }
.nav-item.active {
  color: #1B6B3A;
  background: rgba(27,107,58,0.10);
  border: 1px solid rgba(27,107,58,0.15);
  font-weight: 500;
}

.nav-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  width: 20px;
}
.nav-text  { font-size: 13px; font-weight: 400; flex: 1; }
.nav-badge {
  font-size: 9px;
  font-weight: 700;
  background: linear-gradient(135deg, #1B6B3A, #134E2A);
  color: #fff;
  padding: 2px 6px;
  border-radius: 20px;
  letter-spacing: 0.03em;
}

/* Tooltip when collapsed */
.nav-tooltip {
  display: none;
  position: absolute;
  right: calc(100% + 10px);
  top: 50%;
  transform: translateY(-50%);
  background: #111714;
  color: #F5F6F5;
  font-size: 12px;
  padding: 5px 10px;
  border-radius: 7px;
  white-space: nowrap;
  z-index: 300;
  pointer-events: none;
  box-shadow: 0 4px 12px rgba(0,0,0,0.15);
}
.sidebar.collapsed .nav-item:hover .nav-tooltip,
.sidebar.collapsed .footer-btn:hover .nav-tooltip,
.sidebar.collapsed .sidebar-user:hover .nav-tooltip {
  display: block;
}

/* ── Admin nav item ─────────────────────────────────────── */
.nav-item--admin {
  color: rgba(185,28,28,0.65);
  margin-top: 6px;
  border-top: 1px solid #E4E8E4;
  padding-top: 12px;
  border-radius: 0 0 10px 10px;
}
.nav-item--admin:hover  { color: #B91C1C; background: rgba(185,28,28,0.06); }
.nav-item--admin.active { color: #B91C1C; background: rgba(185,28,28,0.08); border-color: rgba(185,28,28,0.2); }

/* ── Footer ─────────────────────────────────────────────── */
.sidebar-footer {
  border-top: 1px solid #E4E8E4;
  padding: 10px 8px;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.footer-btn {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 7px 10px;
  border-radius: 8px;
  border: none;
  background: none;
  cursor: pointer;
  font-size: 12.5px;
  font-weight: 500;
  transition: all 0.18s;
  white-space: nowrap;
  overflow: hidden;
  position: relative;
  width: 100%;
}

.poubcoin-btn {
  color: #4B5E52;
  background: #F5F6F5;
  border: 1px solid #E4E8E4;
  text-decoration: none;
}
.poubcoin-btn:hover {
  background: rgba(27,107,58,0.08);
  border-color: rgba(27,107,58,0.3);
  color: #1B6B3A;
}

.admin-btn {
  color: #4B5E52;
  background: #F5F6F5;
  border: 1px solid #E4E8E4;
}
.admin-btn:hover {
  background: #EEF7F1;
  border-color: rgba(27,107,58,0.3);
  color: #1B6B3A;
}

.sidebar-user {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 10px;
  border-radius: 10px;
  position: relative;
  overflow: hidden;
  cursor: default;
  transition: background 0.18s;
}
.sidebar-user:hover { background: rgba(27,107,58,0.04); }

.user-avatar {
  width: 32px;
  height: 32px;
  background: linear-gradient(135deg, #1B6B3A, #134E2A);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: 'Syne', sans-serif;
  font-weight: 700;
  font-size: 13px;
  color: #fff;
  flex-shrink: 0;
}

.user-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 1px;
  overflow: hidden;
  min-width: 0;
}
.user-email {
  font-size: 11.5px;
  color: #4B5E52;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.user-role  { font-size: 10.5px; color: #94A99A; }
.role-admin { color: #B91C1C !important; }

.logout-btn {
  background: none;
  border: none;
  cursor: pointer;
  color: #94A99A;
  padding: 5px;
  border-radius: 7px;
  display: flex;
  align-items: center;
  flex-shrink: 0;
  transition: all 0.18s;
}
.logout-btn:hover { color: #B91C1C; background: rgba(185,28,28,0.08); }

/* ── Fade transition ────────────────────────────────────── */
.fade-text-enter-active,
.fade-text-leave-active { transition: opacity 0.18s ease, transform 0.18s ease; }
.fade-text-enter-from   { opacity: 0; transform: translateX(6px); }
.fade-text-leave-to     { opacity: 0; transform: translateX(6px); }

/* ── Mobile ─────────────────────────────────────────────── */
@media (max-width: 768px) {
  .sidebar         { width: 260px; }
  .sidebar.collapsed { width: 260px; }
  .sidebar-toggle-tab { display: none; }
}
</style>
