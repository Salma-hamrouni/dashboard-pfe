<template>
  <div class="app-layout">
    <!-- Mobile overlay -->
    <div v-if="mobileMenuOpen" class="sidebar-overlay" @click="mobileMenuOpen = false" />

    <AppSidebar
      :collapsed="sidebarCollapsed"
      :class="{ 'sidebar-mobile-open': mobileMenuOpen }"
      class="app-sidebar-wrapper"
      @toggle="handleToggle"
      @back-to-admin="goBackToAdmin"
    />

    <div class="layout-main" :class="{ expanded: sidebarCollapsed }">
      <AppNavbar @toggle-menu="mobileMenuOpen = !mobileMenuOpen" />

      <main class="layout-content">
        <slot />
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import AppSidebar from '@/components/layout/AppSidebar.vue'
import AppNavbar from '@/components/layout/AppNavbar.vue'

const router = useRouter()
const sidebarCollapsed = ref(false)
const mobileMenuOpen = ref(false)

function handleToggle() {
  if (window.innerWidth <= 768) {
    mobileMenuOpen.value = false
  } else {
    sidebarCollapsed.value = !sidebarCollapsed.value
  }
}

function goBackToAdmin() {
  router.push('/admin')
}
</script>

<style scoped>
.app-layout {
  display: flex;
  flex-direction: row-reverse;
  min-height: 100vh;
  width: 100%;
  background: var(--color-bg);
  position: relative;
}

.layout-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
  width: 100%;
}

.layout-content {
  flex: 1;
  padding: 0;
  width: 100%;
  overflow-y: auto;
}

/* Mobile overlay */
.sidebar-overlay {
  display: none;
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.4);
  z-index: 99;
}

/* Page transition */
.page-enter-active,
.page-leave-active { transition: all 0.22s ease; }
.page-enter-from { opacity: 0; transform: translateY(10px); }
.page-leave-to   { opacity: 0; transform: translateY(-6px); }

/* ── Mobile ─────────────────────────────────────────────── */
@media (max-width: 768px) {
  .app-layout { flex-direction: column; }

  .app-sidebar-wrapper {
    position: fixed;
    right: -260px;
    top: 0;
    height: 100vh;
    z-index: 100;
    transition: right 0.28s cubic-bezier(0.16, 1, 0.3, 1);
    box-shadow: none;
  }

  .app-sidebar-wrapper.sidebar-mobile-open {
    right: 0;
    box-shadow: -4px 0 24px rgba(0, 0, 0, 0.12);
  }

  .sidebar-overlay { display: block; }

  .layout-main { width: 100%; }
}
</style>
