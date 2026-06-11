<template>
  <div class="scatter-widget" ref="rootEl">
    <!-- Empty state -->
    <div v-if="!hasData" class="scatter-empty">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" width="28" height="28">
        <circle cx="6" cy="17" r="2"/><circle cx="14" cy="8" r="2"/>
        <circle cx="18" cy="15" r="2"/><circle cx="10" cy="13" r="2"/>
      </svg>
      <span>Configurez X et Y (colonnes numériques) pour afficher le nuage</span>
    </div>

    <!-- Chart SVG — pixel-based, no viewBox distortion -->
    <svg
      v-else
      class="scatter-svg"
      :width="w" :height="h"
      @mousemove="onMouseMove"
      @mouseleave="hoveredIdx = null"
    >
      <!-- ── Grid ──────────────────────────────────────── -->
      <g class="grid">
        <line
          v-for="t in yTickList" :key="'gy'+t"
          :x1="ML" :y1="sy(t)" :x2="w - MR" :y2="sy(t)"
          stroke="currentColor" stroke-width="0.8" opacity="0.07"
          stroke-dasharray="3,4"
        />
        <line
          v-for="t in xTickList" :key="'gx'+t"
          :x1="sx(t)" :y1="MT" :x2="sx(t)" :y2="h - MB"
          stroke="currentColor" stroke-width="0.8" opacity="0.07"
          stroke-dasharray="3,4"
        />
      </g>

      <!-- ── Axes ──────────────────────────────────────── -->
      <line :x1="ML" :y1="MT" :x2="ML" :y2="h-MB" stroke="currentColor" stroke-width="0.8" opacity="0.2"/>
      <line :x1="ML" :y1="h-MB" :x2="w-MR" :y2="h-MB" stroke="currentColor" stroke-width="0.8" opacity="0.2"/>

      <!-- ── Y ticks ─────────────────────────────────── -->
      <text
        v-for="t in yTickList" :key="'ytl'+t"
        :x="ML - 5" :y="sy(t)"
        class="axis-tick" text-anchor="end" dominant-baseline="middle"
      >{{ fmt(t) }}</text>

      <!-- ── X ticks ─────────────────────────────────── -->
      <text
        v-for="t in xTickList" :key="'xtl'+t"
        :x="sx(t)" :y="h - MB + 13"
        class="axis-tick" text-anchor="middle"
      >{{ fmt(t) }}</text>

      <!-- ── Axis labels ─────────────────────────────── -->
      <text
        v-if="yAxisLabel"
        :x="10" :y="MT + plotH / 2"
        class="axis-label"
        text-anchor="middle"
        :transform="`rotate(-90, 10, ${MT + plotH / 2})`"
      >{{ truncate(yAxisLabel, 16) }}</text>

      <text
        v-if="xAxisLabel"
        :x="ML + plotW / 2" :y="h - 2"
        class="axis-label" text-anchor="middle"
      >{{ truncate(xAxisLabel, 20) }}</text>

      <!-- ── Dots ──────────────────────────────────────── -->
      <g v-for="(pt, i) in mappedPts" :key="i" style="cursor:crosshair">
        <!-- glow ring on hover -->
        <circle
          v-if="hoveredIdx === i"
          :cx="pt.sx" :cy="pt.sy"
          :r="dotR + 4"
          :fill="resolvedColor" opacity="0.2"
        />
        <circle
          :cx="pt.sx" :cy="pt.sy"
          :r="dotR"
          :fill="resolvedColor"
          :opacity="hoveredIdx !== null && hoveredIdx !== i ? 0.2 : 0.82"
          stroke="rgba(255,255,255,0.3)" stroke-width="0.8"
          @mouseenter="hoveredIdx = i"
        />
      </g>

      <!-- ── Tooltip ────────────────────────────────────── -->
      <g v-if="hoveredIdx !== null && mappedPts[hoveredIdx]">
        <!-- Compute tooltip box position -->
        <foreignObject
          :x="ttX" :y="ttY"
          width="150" height="54"
          style="overflow:visible"
        >
          <div class="scatter-tt">
            <span class="tt-dot" :style="{ background: resolvedColor }"/>
            <div class="tt-lines">
              <span v-if="mappedPts[hoveredIdx].label" class="tt-label">
                {{ truncate(mappedPts[hoveredIdx].label, 18) }}
              </span>
              <span class="tt-xy">
                <b>X</b> {{ fmtFull(mappedPts[hoveredIdx].x) }}
                &nbsp;·&nbsp;
                <b>Y</b> {{ fmtFull(mappedPts[hoveredIdx].y) }}
              </span>
            </div>
          </div>
        </foreignObject>
      </g>
    </svg>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'

interface ScatterPoint { x: number; y: number; label?: string }

const props = withDefaults(defineProps<{
  data?:        ScatterPoint[]
  color?:       string
  dotSize?:     number
  xAxisLabel?:  string
  yAxisLabel?:  string
  valuePrefix?: string
  valueSuffix?: string
}>(), {
  data:        () => [],
  dotSize:     4,
  xAxisLabel:  '',
  yAxisLabel:  '',
  valuePrefix: '',
  valueSuffix: '',
})

// ── Container size (ResizeObserver) ───────────────────────────────────────
const rootEl    = ref<HTMLElement | null>(null)
const w         = ref(400)
const h         = ref(260)
let   _observer: ResizeObserver | null = null

onMounted(() => {
  if (!rootEl.value) return
  const measure = () => {
    w.value = rootEl.value!.clientWidth  || 400
    h.value = rootEl.value!.clientHeight || 260
  }
  measure()
  _observer = new ResizeObserver(measure)
  _observer.observe(rootEl.value)
})
onBeforeUnmount(() => _observer?.disconnect())

// ── Margins ───────────────────────────────────────────────────────────────
const ML = computed(() => props.yAxisLabel ? 54 : 42)   // left  (y-ticks)
const MR = 12                                             // right
const MT = 10                                             // top
const MB = computed(() => props.xAxisLabel ? 34 : 26)   // bottom (x-ticks)

const plotW = computed(() => Math.max(w.value - ML.value - MR, 1))
const plotH = computed(() => Math.max(h.value - MT - MB.value, 1))

// ── Color ─────────────────────────────────────────────────────────────────
const resolvedColor = computed(() => props.color || '#6366f1')
const dotR          = computed(() => props.dotSize ?? 4)

// ── Data ──────────────────────────────────────────────────────────────────
const hasData = computed(() => (props.data?.length ?? 0) >= 2)

// ── Axis ranges ───────────────────────────────────────────────────────────
function extent(vals: number[]): [number, number] {
  // Guard against empty array → Math.min/max returning ±Infinity
  if (!vals.length) return [0, 1]
  const mn = Math.min(...vals)
  const mx = Math.max(...vals)
  const pad = (mx - mn) * 0.05 || 1
  return [mn - pad, mx + pad]
}
const [xMin, xMax] = [computed(() => extent(props.data.map(d => d.x))[0]),
                      computed(() => extent(props.data.map(d => d.x))[1])]
const [yMin, yMax] = [computed(() => extent(props.data.map(d => d.y))[0]),
                      computed(() => extent(props.data.map(d => d.y))[1])]

// ── Coordinate mapping ────────────────────────────────────────────────────
function sx(v: number) {
  return ML.value + ((v - xMin.value) / (xMax.value - xMin.value)) * plotW.value
}
function sy(v: number) {
  // SVG y=0 is top → invert
  return MT + (1 - (v - yMin.value) / (yMax.value - yMin.value)) * plotH.value
}

// ── Ticks ─────────────────────────────────────────────────────────────────
function niceTicks(min: number, max: number, n = 5): number[] {
  const range = max - min || 1
  const exp   = Math.floor(Math.log10(range / n))
  const step  = [1, 2, 5].map(f => f * Math.pow(10, exp))
    .find(s => (max - min) / s <= n + 1) ?? Math.pow(10, exp)
  const start = Math.ceil(min / step) * step
  const ticks: number[] = []
  for (let v = start; v <= max + step * 0.01; v += step)
    ticks.push(+v.toFixed(10))
  return ticks
}
const xTickList = computed(() => niceTicks(xMin.value, xMax.value, 5))
const yTickList = computed(() => niceTicks(yMin.value, yMax.value, 4))

// ── Mapped points ─────────────────────────────────────────────────────────
const mappedPts = computed(() =>
  props.data.map(d => ({ ...d, sx: sx(d.x), sy: sy(d.y) }))
)

// ── Hover & tooltip ───────────────────────────────────────────────────────
const hoveredIdx = ref<number | null>(null)

function onMouseMove(_e: MouseEvent) { /* dots handle mouseenter */ }

const ttX = computed(() => {
  if (hoveredIdx.value === null) return 0
  const pt = mappedPts.value[hoveredIdx.value]
  return pt.sx + dotR.value + 6 + 150 > w.value
    ? pt.sx - 160
    : pt.sx + dotR.value + 6
})
const ttY = computed(() => {
  if (hoveredIdx.value === null) return 0
  const pt = mappedPts.value[hoveredIdx.value]
  return Math.max(MT, Math.min(pt.sy - 24, h.value - MB.value - 54))
})

// ── Formatters ────────────────────────────────────────────────────────────
function fmt(n: number): string {
  if (!isFinite(n)) return '–'
  const abs = Math.abs(n)
  if (abs >= 1_000_000) return (n / 1_000_000).toFixed(1) + 'M'
  if (abs >= 10_000)    return (n / 1_000).toFixed(0) + 'k'
  if (abs >= 1_000)     return (n / 1_000).toFixed(1) + 'k'
  return +n.toFixed(2) + ''
}
function fmtFull(n: number): string {
  return (props.valuePrefix || '') + n.toLocaleString('fr-FR') + (props.valueSuffix || '')
}
function truncate(s: string, max: number) {
  return s && s.length > max ? s.slice(0, max) + '…' : s
}
</script>

<style scoped>
.scatter-widget {
  width: 100%; height: 100%;
  display: flex; align-items: center; justify-content: center;
  overflow: hidden;
  font-family: var(--font-sans, sans-serif);
}

/* Empty */
.scatter-empty {
  display: flex; flex-direction: column; align-items: center;
  gap: 8px; color: var(--color-text-muted, rgba(255,255,255,0.3));
  font-size: 11px; text-align: center; padding: 16px;
}

/* SVG */
.scatter-svg {
  display: block;
  overflow: visible;
  color: var(--color-text, rgba(255,255,255,0.8));
}

/* Axis ticks */
.axis-tick {
  font-size: 9px;
  fill: rgba(255,255,255,0.35);
  font-family: var(--font-sans, sans-serif);
}
.axis-label {
  font-size: 8px;
  fill: rgba(255,255,255,0.3);
  font-family: var(--font-sans, sans-serif);
  letter-spacing: 0.03em;
}

/* Tooltip (HTML inside foreignObject) */
.scatter-tt {
  background: rgba(10, 16, 28, 0.97);
  border: 1px solid rgba(255,255,255,0.1);
  border-radius: 8px;
  padding: 5px 10px 6px;
  display: flex; align-items: flex-start; gap: 7px;
  white-space: nowrap;
  font-size: 11px;
  box-shadow: 0 6px 24px rgba(0,0,0,0.6);
  backdrop-filter: blur(8px);
  pointer-events: none;
  width: max-content;
}
.tt-dot {
  width: 7px; height: 7px; border-radius: 50%; flex-shrink: 0; margin-top: 3px;
}
.tt-lines {
  display: flex; flex-direction: column; gap: 2px;
}
.tt-label {
  font-size: 10px;
  color: rgba(255,255,255,0.5);
  line-height: 1.2;
}
.tt-xy {
  font-size: 11px;
  color: rgba(255,255,255,0.88);
  font-weight: 500;
}
.tt-xy b { color: rgba(255,255,255,0.4); font-weight: 400; font-size: 10px; }
</style>
