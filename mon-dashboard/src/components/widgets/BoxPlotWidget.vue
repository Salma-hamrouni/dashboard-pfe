<template>
  <div class="bp-wrap" ref="rootEl">
    <!-- Empty state -->
    <div v-if="!hasData" class="bp-empty">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" width="28" height="28" opacity="0.4">
        <rect x="9" y="4" width="6" height="12" rx="1"/>
        <line x1="12" y1="4" x2="12" y2="2"/>
        <line x1="12" y1="16" x2="12" y2="18"/>
        <line x1="9" y1="10" x2="15" y2="10"/>
        <line x1="10" y1="2" x2="14" y2="2"/>
        <line x1="10" y1="18" x2="14" y2="18"/>
      </svg>
      <span>Configurez X (catégorie) et Y (numérique) pour afficher la distribution</span>
    </div>

    <svg
      v-else
      :width="w"
      :height="h"
      class="bp-svg"
      @mousemove="onMouseMove"
      @mouseleave="hoveredIdx = null"
    >
      <!-- ── Y-axis grid lines ─────────────────────────────────── -->
      <g class="bp-grid">
        <line
          v-for="t in yTicks"
          :key="t"
          :x1="ML"
          :y1="sy(t)"
          :x2="w - MR"
          :y2="sy(t)"
          stroke="currentColor"
          stroke-width="0.6"
          opacity="0.07"
          stroke-dasharray="3,4"
        />
      </g>

      <!-- ── Y-axis tick labels ─────────────────────────────────── -->
      <g class="bp-y-labels">
        <text
          v-for="t in yTicks"
          :key="t"
          :x="ML - 6"
          :y="sy(t)"
          text-anchor="end"
          dominant-baseline="middle"
          class="bp-tick-label"
        >{{ fmtTick(t) }}</text>
      </g>

      <!-- ── Zero baseline ─────────────────────────────────────── -->
      <line
        v-if="yMin <= 0 && yMax >= 0"
        :x1="ML"
        :y1="sy(0)"
        :x2="w - MR"
        :y2="sy(0)"
        stroke="currentColor"
        stroke-width="0.8"
        opacity="0.18"
      />

      <!-- ── Box plots ─────────────────────────────────────────── -->
      <g
        v-for="(pt, i) in props.data"
        :key="i"
        class="bp-group"
        @mouseenter="hoveredIdx = i"
        @mouseleave="hoveredIdx = null"
        style="cursor:pointer"
      >
        <!-- Whisker top (max to Q3) -->
        <line
          :x1="cx(i)"
          :y1="sy(pt.max)"
          :x2="cx(i)"
          :y2="sy(pt.q3)"
          :stroke="barColor(i)"
          stroke-width="1.5"
          stroke-dasharray="3,2"
          opacity="0.7"
        />
        <!-- Whisker cap top -->
        <line
          :x1="cx(i) - boxW / 3"
          :y1="sy(pt.max)"
          :x2="cx(i) + boxW / 3"
          :y2="sy(pt.max)"
          :stroke="barColor(i)"
          stroke-width="1.5"
        />

        <!-- IQR Box -->
        <rect
          :x="cx(i) - boxW / 2"
          :y="Math.min(sy(pt.q3), sy(pt.q1))"
          :width="boxW"
          :height="Math.abs(sy(pt.q3) - sy(pt.q1))"
          :fill="barColor(i)"
          :opacity="hoveredIdx === i ? 0.35 : 0.2"
          :stroke="barColor(i)"
          stroke-width="1.5"
          rx="2"
          style="transition: opacity 0.15s"
        />

        <!-- Median line -->
        <line
          :x1="cx(i) - boxW / 2"
          :y1="sy(pt.median)"
          :x2="cx(i) + boxW / 2"
          :y2="sy(pt.median)"
          :stroke="barColor(i)"
          stroke-width="2.5"
          stroke-linecap="round"
        />

        <!-- Whisker bottom (Q1 to min) -->
        <line
          :x1="cx(i)"
          :y1="sy(pt.q1)"
          :x2="cx(i)"
          :y2="sy(pt.min)"
          :stroke="barColor(i)"
          stroke-width="1.5"
          stroke-dasharray="3,2"
          opacity="0.7"
        />
        <!-- Whisker cap bottom -->
        <line
          :x1="cx(i) - boxW / 3"
          :y1="sy(pt.min)"
          :x2="cx(i) + boxW / 3"
          :y2="sy(pt.min)"
          :stroke="barColor(i)"
          stroke-width="1.5"
        />

        <!-- Outliers -->
        <circle
          v-for="(ov, oi) in (pt.outliers ?? [])"
          :key="'o' + oi"
          :cx="cx(i)"
          :cy="sy(ov)"
          r="3"
          :fill="barColor(i)"
          :stroke="barColor(i)"
          stroke-width="1"
          opacity="0.7"
        />

        <!-- X-axis label -->
        <text
          :x="cx(i)"
          :y="h - MB + 14"
          text-anchor="middle"
          dominant-baseline="middle"
          class="bp-x-label"
        >{{ truncate(pt.label, 10) }}</text>

        <!-- Hover tooltip -->
        <g v-if="hoveredIdx === i">
          <rect
            :x="tooltipX(i)"
            :y="tooltipY(i)"
            width="100"
            height="72"
            rx="5"
            fill="rgba(5,20,15,0.92)"
            stroke="rgba(255,255,255,0.08)"
            stroke-width="0.8"
          />
          <text :x="tooltipX(i) + 8" :y="tooltipY(i) + 12" class="bp-tt-label">{{ truncate(pt.label, 14) }}</text>
          <text :x="tooltipX(i) + 8" :y="tooltipY(i) + 26" class="bp-tt-row"><tspan class="bp-tt-key">Max </tspan><tspan :fill="barColor(i)">{{ fmtVal(pt.max) }}</tspan></text>
          <text :x="tooltipX(i) + 8" :y="tooltipY(i) + 38" class="bp-tt-row"><tspan class="bp-tt-key">Q3  </tspan><tspan :fill="barColor(i)">{{ fmtVal(pt.q3) }}</tspan></text>
          <text :x="tooltipX(i) + 8" :y="tooltipY(i) + 50" class="bp-tt-row"><tspan class="bp-tt-key">Med </tspan><tspan :fill="barColor(i)" font-weight="700">{{ fmtVal(pt.median) }}</tspan></text>
          <text :x="tooltipX(i) + 8" :y="tooltipY(i) + 62" class="bp-tt-row"><tspan class="bp-tt-key">Q1  </tspan><tspan :fill="barColor(i)">{{ fmtVal(pt.q1) }}</tspan></text>
        </g>
      </g>
    </svg>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import type { BoxPlotPoint } from '@/types/index'

const props = withDefaults(defineProps<{
  data?:        BoxPlotPoint[]
  colors?:      string[]
  valuePrefix?: string
  valueSuffix?: string
}>(), {
  data:        () => [],
  colors:      () => [],
  valuePrefix: '',
  valueSuffix: '',
})

// ── Container size (ResizeObserver) ──────────────────────────────────────────
const rootEl = ref<HTMLElement | null>(null)
const w = ref(300)
const h = ref(200)

const ro = new ResizeObserver((entries) => {
  const e = entries[0]
  if (!e) return
  w.value = Math.max(100, e.contentRect.width)
  h.value = Math.max(60, e.contentRect.height)
})

import { onMounted, onUnmounted } from 'vue'

onMounted(() => {
  if (rootEl.value) {
    ro.observe(rootEl.value)
    w.value = rootEl.value.clientWidth  || 300
    h.value = rootEl.value.clientHeight || 200
  }
})
onUnmounted(() => ro.disconnect())

// ── Palette ───────────────────────────────────────────────────────────────────
const PALETTE = [
  'var(--chart-1)', 'var(--chart-2)', 'var(--chart-3)',
  'var(--chart-4)', 'var(--chart-5)', 'var(--chart-6)',
]

function barColor(i: number): string {
  if (props.colors?.length) return props.colors[i % props.colors.length]!
  return PALETTE[i % PALETTE.length]!
}

// ── Derived state ─────────────────────────────────────────────────────────────
const hasData = computed(() => props.data.length > 0)

// ── Margins ───────────────────────────────────────────────────────────────────
const MT = 10   // top
const MR = 10   // right
const MB = 28   // bottom (x labels)
const ML = 40   // left (y axis)

// ── Scale ─────────────────────────────────────────────────────────────────────
const yMin = computed((): number => {
  if (!props.data.length) return 0
  const allVals = props.data.flatMap(d => [d.min, ...(d.outliers ?? [])])
  return Math.min(...allVals)
})

const yMax = computed((): number => {
  if (!props.data.length) return 1
  const allVals = props.data.flatMap(d => [d.max, ...(d.outliers ?? [])])
  return Math.max(...allVals)
})

const yRange = computed((): number => {
  const range = yMax.value - yMin.value
  return range === 0 ? 1 : range
})

// Y-axis ticks (5 ticks)
const yTicks = computed((): number[] => {
  const n     = 5
  const step  = yRange.value / (n - 1)
  return Array.from({ length: n }, (_, i) => yMin.value + i * step)
})

function sy(val: number): number {
  const chartH = h.value - MT - MB
  return MT + chartH * (1 - (val - yMin.value) / yRange.value)
}

// X position of each group
const boxW = computed((): number => {
  const n = props.data.length
  if (!n) return 20
  const available = w.value - ML - MR
  const slot = available / n
  return Math.min(slot * 0.5, 40)
})

function cx(i: number): number {
  const n = props.data.length
  const available = w.value - ML - MR
  const slot = available / n
  return ML + slot * i + slot / 2
}

// ── Tooltip position ──────────────────────────────────────────────────────────
const hoveredIdx = ref<number | null>(null)

function tooltipX(i: number): number {
  const raw = cx(i) + 8
  return raw + 100 > w.value ? cx(i) - 108 : raw
}
function tooltipY(i: number): number {
  const pt = props.data[i]!
  const raw = sy((pt.q1 + pt.q3) / 2) - 36
  return Math.max(MT, Math.min(raw, h.value - MB - 80))
}

function onMouseMove(e: MouseEvent) {
  const rect  = (e.currentTarget as SVGElement).getBoundingClientRect()
  const mx    = e.clientX - rect.left
  let closest = -1
  let minDist = Infinity
  props.data.forEach((_, i) => {
    const d = Math.abs(cx(i) - mx)
    if (d < minDist) { minDist = d; closest = i }
  })
  if (minDist < (w.value / props.data.length) * 0.6) {
    hoveredIdx.value = closest
  } else {
    hoveredIdx.value = null
  }
}

// ── Formatting ────────────────────────────────────────────────────────────────
function fmtTick(n: number): string {
  if (!isFinite(n)) return '–'
  const abs = Math.abs(n)
  if (abs >= 1_000_000) return (n / 1_000_000).toFixed(1) + 'M'
  if (abs >= 10_000)    return (n / 1_000).toFixed(0) + 'k'
  if (abs >= 1_000)     return (n / 1_000).toFixed(1) + 'k'
  return +n.toFixed(1) + ''
}

function fmtVal(n: number): string {
  const pre = props.valuePrefix || ''
  const suf = props.valueSuffix || ''
  return pre + fmtTick(n) + suf
}

function truncate(s: string, max: number): string {
  return s.length > max ? s.slice(0, max - 1) + '…' : s
}
</script>

<style scoped>
.bp-wrap {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

.bp-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  color: var(--color-text-muted, #94A99A);
  font-size: 11px;
  text-align: center;
  padding: 0 16px;
}

.bp-svg {
  overflow: visible;
  display: block;
}

.bp-tick-label {
  font-size: 9px;
  fill: #4B5E52;
  font-family: var(--font-mono, 'JetBrains Mono', monospace);
  pointer-events: none;
}

.bp-x-label {
  font-size: 10px;
  fill: rgba(17,23,20,0.55);
  font-family: var(--font-sans, 'DM Sans', sans-serif);
  pointer-events: none;
}

.bp-tt-label {
  font-size: 10px;
  font-weight: 600;
  fill: #111714;
  font-family: var(--font-sans, sans-serif);
  pointer-events: none;
}

.bp-tt-row {
  font-size: 9.5px;
  font-family: var(--font-mono, monospace);
  fill: rgba(17,23,20,0.70);
  pointer-events: none;
}

.bp-tt-key {
  fill: #94A99A;
}
</style>



