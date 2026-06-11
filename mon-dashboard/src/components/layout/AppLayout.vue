<template>
  <div class="app-layout">
    <AppSidebar :collapsed="sidebarCollapsed" @toggle="sidebarCollapsed = !sidebarCollapsed" />

    <div class="layout-main" :class="{ expanded: sidebarCollapsed }">
      <AppNavbar />

      <main class="layout-content">
        <slot />
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import AppSidebar from '@/components/layout/AppSidebar.vue'
import AppNavbar from '@/components/layout/AppNavbar.vue'

const sidebarCollapsed = ref(false)
</script>

<style scoped>
.app-layout {
  display: flex;
  flex-direction: row-reverse;
  min-height: 100vh;
  width: 100%;
  background: var(--color-bg);
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

/* Page transition */
.page-enter-active,
.page-leave-active {
  transition: all 0.22s ease;
}

.page-enter-from {
  opacity: 0;
  transform: translateY(10px);
}

.page-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}
</style>