<template>
  <div class="img-widget" ref="el">
    <!-- Empty state -->
    <div v-if="!src" class="img-empty">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.4" width="32" height="32">
        <rect x="3" y="3" width="18" height="18" rx="2"/>
        <circle cx="8.5" cy="8.5" r="1.5"/>
        <polyline points="21 15 16 10 5 21"/>
      </svg>
      <span>Aucune image</span>
      <span class="img-empty-sub">Configurez une URL ou importez un fichier</span>
    </div>

    <!-- Image -->
    <template v-else>
      <img
        :src="src"
        :alt="altText || 'Image'"
        class="img-content"
        :style="{ objectFit: fit || 'contain' }"
        @error="onError"
      />
      <!-- Caption -->
      <div v-if="altText" class="img-caption">{{ altText }}</div>
    </template>

    <!-- Load error fallback -->
    <div v-if="hasError" class="img-error">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" width="28" height="28">
        <circle cx="12" cy="12" r="10"/>
        <line x1="12" y1="8" x2="12" y2="12"/>
        <line x1="12" y1="16" x2="12.01" y2="16"/>
      </svg>
      <span>Impossible de charger l'image</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'

const props = defineProps<{
  src?: string
  fit?: 'contain' | 'cover' | 'fill' | 'none'
  altText?: string
}>()

const hasError = ref(false)

watch(() => props.src, () => { hasError.value = false })

function onError() {
  hasError.value = true
}
</script>

<style scoped>
.img-widget {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  position: relative;
  overflow: hidden;
  border-radius: 6px;
}

.img-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  color: var(--color-text-muted);
  text-align: center;
}
.img-empty span { font-size: 12px; }
.img-empty-sub { font-size: 10px; opacity: 0.6; }

.img-content {
  width: 100%;
  height: 100%;
  display: block;
  border-radius: 4px;
}

.img-caption {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  padding: 4px 10px;
  background: rgba(0,0,0,0.55);
  font-size: 10px;
  color: rgba(255,255,255,0.85);
  text-align: center;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  backdrop-filter: blur(4px);
}

.img-error {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  background: rgba(0,0,0,0.4);
  color: #f87171;
  font-size: 11px;
  border-radius: 6px;
}
</style>



