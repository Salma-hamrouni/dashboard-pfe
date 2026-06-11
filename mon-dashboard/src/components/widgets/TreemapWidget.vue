<template>
  <div class="tm-widget" ref="el">
    <!-- Empty state -->
    <div v-if="!hasData" class="tm-empty">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.4" width="30" height="30">
        <rect x="3" y="3" width="8" height="8" rx="1"/>
        <rect x="13" y="3" width="8" height="8" rx="1"/>
        <rect x="3" y="13" width="5" height="8" rx="1"/>
        <rect x="10" y="13" width="11" height="8" rx="1"/>
      </svg>
      <span>Aucune donnée</span>
      <span class="tm-empty-sub">Configurez les axes X et Y</span>
    </div>

    <!-- Treemap SVG -->
    <svg
      v-else
      class="tm-svg"
      :viewBox="`0 0 ${svgW} ${svgH}`"
      :width="svgW"
      :height="svgH"
      xmlns="http://www.w3.org/2000/svg"
    >
      <g v-for="(node, i) in nodes" :key="i">
        <!-- Cell rect -->
        <rect
          :x="node.x + 1"
          :y="node.y + 1"
          :width="Math.max(node.w - 2, 0)"
          :height="Math.max(node.h - 2, 0)"
          :fill="palette[i % palette.length]"
          rx="4"
          class="tm-rect"
          @mouseenter="hoveredIdx = i"
          @mouseleave="hoveredIdx = null"
        />
        <!-- Label (only if cell is large enough) -->
        <text
          v-if="node.w > 44 && node.h > 20"
          :x="node.x + node.w / 2"
          :y="node.y + node.h / 2 - (node.h > 36 ? 8 : 0)"
          text-anchor="middle"
          dominant-baseline="middle"
          class="tm-label"
          :font-size="labelSize(node)"
          :fill="labelColor(i)"
          clip-path="`url(#clip-${i})`"
        >{{ truncate(node.label, maxChars(node)) }}</text>
        <!-- Value label (only if tall enough) -->
        <text
          v-if="node.w > 44 && node.h > 36"
          :x="node.x + node.w / 2"
          :y="node.y + node.h / 2 + 10"
          text-anchor="middle"
          dominant-baseline="middle"
          class="tm-value"
          :font-size="Math.max(labelSize(node) - 2, 8)"
          :fill="labelColor(i)"
        >{{ fmtVal(node.value) }}</text>
        <!-- Tooltip on hover -->
        <g v-if="hoveredIdx === i">
          <!-- Tooltip background -->
          <rect
            :x="tooltipX(node)"
            :y="tooltipY(node)"
            :width="tooltipW"
            height="44"
            rx="6"
            fill="rgba(10,16,28,0.96)"
            stroke="rgba(255,255,255,0.12)"
            stroke-width="1"
          />
          <text
            :x="tooltipX(node) + tooltipW / 2"
            :y="tooltipY(node) + 15"
            text-anchor="middle"
            font-size="11"
            fill="rgba(255,255,255,0.8)"
          >{{ node.label }}</text>
          <text
            :x="tooltipX(node) + tooltipW / 2"
            :y="tooltipY(node) + 32"
            text-anchor="middle"
            font-size="13"
            font-weight="700"
            fill="white"
          >{{ fmtVal(node.value) }}</text>
        </g>
      </g>
    </svg>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'

interface DataItem {
  label: string
  value: number
}

interface TmNode extends DataItem {
  x: number
  y: number
  w: number
  h: number
}

const props = defineProps<{
  data?: DataItem[]
}>()

const el         = ref<HTMLElement>()
const svgW       = ref(320)
const svgH       = ref(240)
const hoveredIdx = ref<number | null>(null)

let ro: ResizeObserver | null = null
onMounted(() => {
  if (!el.value) return
  updateSize()
  ro = new ResizeObserver(updateSize)
  ro.observe(el.value)
})
onBeforeUnmount(() => ro?.disconnect())

function updateSize() {
  if (!el.value) return
  svgW.value = el.value.clientWidth  || 320
  svgH.value = el.value.clientHeight || 240
}

const hasData = computed(() =>
  Array.isArray(props.data) && props.data.some(d => d.value > 0)
)

// Sorted items (descending)
const items = computed<DataItem[]>(() => {
  if (!props.data?.length) return []
  return [...props.data]
    .filter(d => isFinite(d.value) && d.value > 0)
    .sort((a, b) => b.value - a.value)
})

// ── Binary treemap layout ────────────────────────────────────────────────────
function subdivide(
  list: DataItem[],
  x: number,
  y: number,
  w: number,
  h: number,
  total: number,
): TmNode[] {
  if (list.length === 0) return []
  if (list.length === 1) return [{ ...list[0]!, x, y, w, h }]

  // Split into two halves with closest-to-equal areas
  let half = total / 2
  let acc  = 0
  let split = 1
  for (let i = 0; i < list.length - 1; i++) {
    acc += list[i]!.value
    split = i + 1
    if (acc >= half) break
  }

  const leftItems  = list.slice(0, split)
  const rightItems = list.slice(split)
  const leftTotal  = leftItems.reduce((s, d) => s + d.value, 0)
  const rightTotal = rightItems.reduce((s, d) => s + d.value, 0)
  const ratio      = total > 0 ? leftTotal / total : 0.5

  if (w >= h) {
    const leftW = w * ratio
    return [
      ...subdivide(leftItems,  x,          y, leftW,      h, leftTotal),
      ...subdivide(rightItems, x + leftW,  y, w - leftW,  h, rightTotal),
    ]
  } else {
    const topH = h * ratio
    return [
      ...subdivide(leftItems,  x, y,        w, topH,      leftTotal),
      ...subdivide(rightItems, x, y + topH, w, h - topH,  rightTotal),
    ]
  }
}

const nodes = computed<TmNode[]>(() => {
  if (!items.value.length) return []
  const total = items.value.reduce((s, d) => s + d.value, 0)
  return subdivide(items.value, 0, 0, svgW.value, svgH.value, total)
})

// ── Color palette ─────────────────────────────────────────────────────────────
const palette = [
  '#4A6CF7', '#7C3AED', '#EC4899', '#F59E0B',
  '#10B981', '#3B82F6', '#EF4444', '#8B5CF6',
  '#06B6D4', '#84CC16', '#F97316', '#14B8A6',
]

function labelColor(i: number): string {
  const hex = (palette[i % palette.length] || '#4A6CF7').replace('#', '')
  const r = parseInt(hex.slice(0, 2), 16)
  const g = parseInt(hex.slice(2, 4), 16)
  const b = parseInt(hex.slice(4, 6), 16)
  const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255
  return luminance > 0.55 ? 'rgba(10,20,30,0.85)' : 'rgba(255,255,255,0.92)'
}

function labelSize(node: TmNode): number {
  const base = Math.min(node.w / 6, node.h / 3, 14)
  return Math.max(Math.min(base, 13), 8)
}

function maxChars(node: TmNode): number {
  return Math.floor(node.w / (labelSize(node) * 0.55))
}

function truncate(s: string, n: number): string {
  return s.length > n ? s.slice(0, Math.max(n - 1, 1)) + '…' : s
}

function fmtVal(v: number): string {
  if (v >= 1_000_000) return (v / 1_000_000).toFixed(1) + 'M'
  if (v >= 1_000)     return (v / 1_000).toFixed(1) + 'k'
  return Number.isInteger(v) ? String(v) : v.toFixed(2)
}

// ── Tooltip positioning ───────────────────────────────────────────────────────
const tooltipW = 130

function tooltipX(node: TmNode): number {
  const cx = node.x + node.w / 2 - tooltipW / 2
  return Math.max(4, Math.min(cx, svgW.value - tooltipW - 4))
}

function tooltipY(node: TmNode): number {
  const above = node.y - 52
  return above < 4 ? node.y + node.h + 4 : above
}
</script>

<style scoped>
.tm-widget {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: stretch;
  justify-content: stretch;
  overflow: hidden;
  border-radius: 6px;
}

.tm-svg {
  width: 100%;
  height: 100%;
  display: block;
}

.tm-rect {
  cursor: default;
  transition: filter 0.15s, transform 0.1s;
  transform-origin: center;
}
.tm-rect:hover {
  filter: brightness(1.15);
}

.tm-label {
  pointer-events: none;
  font-weight: 600;
  letter-spacing: 0.01em;
}

.tm-value {
  pointer-events: none;
  font-variant-numeric: tabular-nums;
  opacity: 0.85;
}

.tm-empty {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  color: var(--color-text-muted);
  font-size: 12px;
}
.tm-empty-sub {
  font-size: 10px;
  opacity: 0.6;
}
</style>
