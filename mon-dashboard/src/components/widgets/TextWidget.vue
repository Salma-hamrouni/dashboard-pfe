<template>
  <div class="text-widget" :style="wrapStyle">
    <!-- Empty/placeholder -->
    <div v-if="!trimmed" class="text-placeholder">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" width="20" height="20" opacity="0.3">
        <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
        <polyline points="14 2 14 8 20 8"/>
        <line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/>
        <polyline points="10 9 9 9 8 9"/>
      </svg>
      <span>Ajoutez votre texte dans le panneau Format</span>
    </div>

    <!-- Content lines -->
    <template v-else>
      <p
        v-for="(line, i) in lines"
        :key="i"
        class="text-line"
        :style="{ minHeight: line === '' ? '0.8em' : undefined }"
      >{{ line }}</p>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(defineProps<{
  content?:    string
  fontSize?:   number
  align?:      'left' | 'center' | 'right'
  color?:      string
  fontFamily?: string
}>(), {
  content:    '',
  fontSize:   14,
  align:      'left',
  color:      '',
  fontFamily: '',
})

const trimmed = computed(() => (props.content || '').trim())
const lines   = computed(() => (props.content || '').split('\n'))

const wrapStyle = computed(() => ({
  fontSize:   (props.fontSize || 14) + 'px',
  textAlign:  props.align || 'left',
  color:      props.color || 'var(--color-text)',
  fontFamily: props.fontFamily || 'inherit',
}))
</script>

<style scoped>
.text-widget {
  width: 100%;
  height: 100%;
  overflow: auto;
  padding: 4px 2px;
  display: flex;
  flex-direction: column;
  gap: 0;
  scrollbar-width: thin;
}

.text-placeholder {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  color: rgba(255,255,255,0.25);
  font-size: 11px;
  text-align: center;
}

.text-line {
  margin: 0;
  padding: 1px 0;
  line-height: 1.55;
  white-space: pre-wrap;
  word-break: break-word;
}
</style>
