<template>
  <div class="hm-widget" ref="el">
    <!-- Empty -->
    <div v-if="!hasData" class="hm-empty">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" width="28" height="28">
        <rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/>
        <rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/>
      </svg>
      <span>Aucune donnée</span>
    </div>

    <template v-else>
      <!-- Color scale legend -->
      <div class="hm-legend">
        <span class="hm-leg-label">{{ scaleMin.toFixed(2) }}</span>
        <div class="hm-leg-bar" :style="{ background: legendGradient }"/>
        <span class="hm-leg-label">{{ scaleMax.toFixed(2) }}</span>
      </div>

      <div class="hm-body">
        <!-- Y-axis labels -->
        <div class="hm-y-labels">
          <div v-for="(lbl, i) in labels" :key="i" class="hm-y-lbl">{{ truncate(lbl, 10) }}</div>
        </div>

        <!-- Grid -->
        <div class="hm-grid-wrapper">
          <!-- X-axis labels -->
          <div class="hm-x-labels">
            <div v-for="(lbl, i) in labels" :key="i" class="hm-x-lbl">{{ truncate(lbl, 6) }}</div>
          </div>

          <!-- Cells -->
          <div
            class="hm-grid"
            :style="{ gridTemplateColumns: `repeat(${labels.length}, 1fr)` }"
          >
            <div
              v-for="(val, idx) in flatMatrix"
              :key="idx"
              class="hm-cell"
              :style="{ background: cellColor(val) }"
              @mouseenter="hoveredIdx = idx"
              @mouseleave="hoveredIdx = null"
            >
              <!-- Value label (only if cell big enough) -->
              <span v-if="cellSize >= 28" class="hm-cell-val" :style="{ color: textColor(val) }">
                {{ val.toFixed(2) }}
              </span>
              <!-- Tooltip -->
              <div v-if="hoveredIdx === idx" class="hm-tooltip">
                <strong>{{ labels[Math.floor(idx / labels.length)] }}</strong>
                <span>vs</span>
                <strong>{{ labels[idx % labels.length] }}</strong>
                <span class="hm-tt-val">{{ val.toFixed(3) }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'

const props = defineProps<{
  labels?: string[]
  matrix?: number[][]
}>()

const el         = ref<HTMLElement>()
const hoveredIdx = ref<number | null>(null)
const elWidth    = ref(240)

let ro: ResizeObserver | null = null
onMounted(() => {
  if (!el.value) return
  elWidth.value = el.value.clientWidth || 240
  ro = new ResizeObserver(([e]) => { elWidth.value = e.contentRect.width })
  ro.observe(el.value)
})
onBeforeUnmount(() => ro?.disconnect())

const hasData = computed(() =>
  (props.labels?.length ?? 0) > 1 && (props.matrix?.length ?? 0) > 0
)
const labels     = computed(() => props.labels ?? [])
const flatMatrix = computed(() => (props.matrix ?? []).flat())

const scaleMin = computed(() => {
  const vals = flatMatrix.value
  return vals.length ? Math.min(...vals) : -1
})
const scaleMax = computed(() => {
  const vals = flatMatrix.value
  return vals.length ? Math.max(...vals) :  1
})

// Approximate cell size based on container width
const cellSize = computed(() => {
  const n = labels.value.length
  if (!n) return 0
  const yLabelW = 80
  return Math.floor((elWidth.value - yLabelW) / n)
})

/* Color interpolation: blue (−1) → white (0) → red (+1) */
function cellColor(val: number): string {
  const lo = scaleMin.value, hi = scaleMax.value
  const range = hi - lo || 1
  const t = (val - lo) / range   // 0..1

  // blue → white → red
  const r = t < 0.5 ? Math.round(t * 2 * 255) : 255
  const b = t < 0.5 ? 255 : Math.round((1 - t) * 2 * 255)
  const g = t < 0.5
    ? Math.round(t * 2 * 255)
    : Math.round((1 - t) * 2 * 255)
  return `rgba(${r},${g},${b},0.82)`
}

/* Text color: dark on light cells, light on dark cells */
function textColor(val: number): string {
  const lo = scaleMin.value, hi = scaleMax.value
  const range = hi - lo || 1
  const t = (val - lo) / range
  // middle values are lightest → use dark text; extremes → dark text too
  const brightness = 1 - Math.abs(t - 0.5) * 2   // 0 at extremes, 1 at center
  return brightness > 0.5 ? 'rgba(10,20,30,0.85)' : 'rgba(255,255,255,0.85)'
}

const legendGradient = computed(() => {
  return 'linear-gradient(to right, rgba(0,0,255,0.7), rgba(255,255,255,0.7), rgba(255,0,0,0.7))'
})

function truncate(s: string, n: number) {
  return s.length > n ? s.slice(0, n) + '…' : s
}
</script>

<style scoped>
.hm-widget {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-size: 10px;
  min-height: 0;
  overflow: hidden;
}

.hm-empty {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  color: var(--color-text-muted);
  font-size: var(--text-xs);
}

/* Legend */
.hm-legend {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-shrink: 0;
  padding: 0 2px;
}
.hm-leg-label { font-size: 9px; color: var(--color-text-muted); white-space: nowrap; }
.hm-leg-bar   { flex: 1; height: 6px; border-radius: 3px; }

/* Body */
.hm-body {
  flex: 1;
  display: flex;
  gap: 4px;
  min-height: 0;
  overflow: hidden;
}

/* Y labels */
.hm-y-labels {
  display: flex;
  flex-direction: column;
  justify-content: space-around;
  flex-shrink: 0;
  width: 74px;
  padding-top: 18px; /* offset for x-axis label height */
}
.hm-y-lbl {
  font-size: 9px;
  color: var(--color-text-muted);
  text-align: right;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  line-height: 1;
}

/* Grid wrapper */
.hm-grid-wrapper {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
  overflow: hidden;
}

/* X labels */
.hm-x-labels {
  display: flex;
  height: 18px;
  flex-shrink: 0;
}
.hm-x-lbl {
  flex: 1;
  font-size: 9px;
  color: var(--color-text-muted);
  text-align: center;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  writing-mode: initial;
  display: flex;
  align-items: flex-end;
  justify-content: center;
  padding-bottom: 2px;
}

/* Grid */
.hm-grid {
  flex: 1;
  display: grid;
  gap: 2px;
  min-height: 0;
}

.hm-cell {
  border-radius: 3px;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: default;
  transition: filter 0.15s, transform 0.1s;
  min-width: 0;
}
.hm-cell:hover {
  filter: brightness(1.2);
  transform: scale(1.05);
  z-index: 2;
}

.hm-cell-val {
  font-size: 8px;
  font-weight: 700;
  pointer-events: none;
  font-variant-numeric: tabular-nums;
}

/* Tooltip */
.hm-tooltip {
  position: absolute;
  bottom: calc(100% + 6px);
  left: 50%;
  transform: translateX(-50%);
  background: rgba(10,16,28,0.96);
  border: 1px solid rgba(255,255,255,0.12);
  border-radius: 8px;
  padding: 6px 10px;
  white-space: nowrap;
  z-index: 30;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
  pointer-events: none;
  box-shadow: 0 4px 20px rgba(0,0,0,0.6);
  font-size: 10px;
  color: rgba(255,255,255,0.8);
}
.hm-tt-val {
  font-size: 13px;
  font-weight: 700;
  color: white;
  font-variant-numeric: tabular-nums;
}
</style>



