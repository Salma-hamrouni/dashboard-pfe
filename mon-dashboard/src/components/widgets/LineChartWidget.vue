<template>
  <div class="line-chart-wrap">
    <!-- Empty state -->
    <div v-if="!hasData" class="line-empty">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" width="28" height="28">
        <polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/>
      </svg>
      <span>Aucune donnée</span>
    </div>

    <template v-else>
      <!-- Y-label + SVG side-by-side -->
      <div class="line-body">
        <span v-if="yAxisLabel" class="line-y-label">{{ yAxisLabel }}</span>
        <svg
          ref="svgEl"
          class="line-svg"
          :viewBox="`0 0 ${W} ${H}`"
          preserveAspectRatio="none"
          @mousemove="onMove"
          @mouseleave="activeIdx = null"
        >
          <defs>
            <linearGradient :id="`lg-${uid}`" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" :stop-color="resolvedColor" stop-opacity="0.28"/>
              <stop offset="100%" :stop-color="resolvedColor" stop-opacity="0"/>
            </linearGradient>
            <filter :id="`glow-${uid}`" x="-20%" y="-20%" width="140%" height="140%">
              <feGaussianBlur stdDeviation="2.5" result="blur"/>
              <feMerge><feMergeNode in="blur"/><feMergeNode in="SourceGraphic"/></feMerge>
            </filter>
          </defs>

          <!-- ── Y-axis tick labels ───────────────────────────── -->
          <text
            v-for="(tick, i) in yTicks"
            :key="`yt-${i}`"
            :x="PAD_L - 4"
            :y="yPos(tick)"
            class="y-tick-lbl"
            text-anchor="end"
            dominant-baseline="middle"
          >{{ fmtBase(tick) }}</text>

          <!-- ── Grid lines ─────────────────────────────────────── -->
          <line
            v-for="(tick, i) in yTicks"
            :key="`gl-${i}`"
            :x1="PAD_L" :y1="yPos(tick)"
            :x2="W - PAD_R" :y2="yPos(tick)"
            stroke="rgba(255,255,255,0.06)"
            stroke-width="1"
            stroke-dasharray="3,4"
          />

          <!-- ── Axes ────────────────────────────────────────────── -->
          <line :x1="PAD_L" :y1="PAD_T" :x2="PAD_L" :y2="H-PAD_B"
            stroke="rgba(255,255,255,0.1)" stroke-width="1"/>
          <line :x1="PAD_L" :y1="H-PAD_B" :x2="W-PAD_R" :y2="H-PAD_B"
            stroke="rgba(255,255,255,0.1)" stroke-width="1"/>

          <!-- ── Area fill ───────────────────────────────────────── -->
          <path v-if="fill !== false" :d="areaPath" :fill="`url(#lg-${uid})`" stroke="none"/>

          <!-- ── Line ───────────────────────────────────────────── -->
          <path
            :d="smooth ? smoothLinePath : linePath"
            fill="none"
            :stroke="resolvedColor"
            stroke-width="2.2"
            stroke-linecap="round"
            stroke-linejoin="round"
            :filter="`url(#glow-${uid})`"
          />

          <!-- ── Data point dots ────────────────────────────────── -->
          <template v-if="showDots !== false">
            <circle
              v-for="(p, i) in points"
              :key="`dot-${i}`"
              :cx="p.x" :cy="p.y"
              :r="activeIdx === i ? 5 : 2.5"
              :fill="activeIdx === i ? resolvedColor : 'var(--color-surface, #0d1520)'"
              :stroke="resolvedColor"
              :stroke-width="activeIdx === i ? 0 : 1.8"
              :opacity="activeIdx !== null && activeIdx !== i ? 0.4 : 1"
              style="transition: r 0.12s ease, opacity 0.15s;"
            />
            <!-- Glow ring on active dot -->
            <circle
              v-if="activeIdx !== null"
              :cx="points[activeIdx].x" :cy="points[activeIdx].y"
              r="9"
              :fill="resolvedColor"
              opacity="0.12"
              style="pointer-events:none"
            />
          </template>

          <!-- ── Data labels (optional, above each point) ────────── -->
          <template v-if="showLabels">
            <text
              v-for="(p, i) in points"
              :key="`lbl-${i}`"
              :x="p.x"
              :y="labelY(p.y)"
              class="point-label"
              text-anchor="middle"
              dominant-baseline="auto"
              :fill="resolvedColor"
            >{{ fmtLabel(data[i].value) }}</text>
          </template>

          <!-- ── Crosshair on hover ─────────────────────────────── -->
          <template v-if="activeIdx !== null">
            <!-- vertical -->
            <line
              :x1="points[activeIdx].x" :y1="PAD_T"
              :x2="points[activeIdx].x" :y2="H - PAD_B"
              :stroke="resolvedColor" stroke-width="1"
              stroke-dasharray="3,3" opacity="0.45"
            />
            <!-- horizontal -->
            <line
              :x1="PAD_L" :y1="points[activeIdx].y"
              :x2="W - PAD_R" :y2="points[activeIdx].y"
              stroke="rgba(255,255,255,0.18)" stroke-width="1"
              stroke-dasharray="3,3"
            />
          </template>
        </svg>

        <!-- Tooltip -->
        <transition name="tt">
          <div
            v-if="activeIdx !== null"
            class="line-tooltip"
            :style="tooltipStyle"
          >
            <span class="tt-dot" :style="{ background: resolvedColor }"/>
            <div class="tt-content">
              <span class="tt-lbl">{{ data[activeIdx].label }}</span>
              <span class="tt-val">{{ fmtLabel(data[activeIdx].value) }}</span>
            </div>
          </div>
        </transition>
      </div><!-- /line-body -->

      <!-- X labels -->
      <div
        class="x-labels"
        :style="{ paddingLeft: (PAD_L + (yAxisLabel ? 14 : 0)) + 'px', paddingRight: PAD_R + 'px' }"
      >
        <span
          v-for="(d, i) in data"
          :key="i"
          class="x-lbl"
          :class="{ 'x-active': activeIdx === i }"
        >{{ truncate(d.label, 7) }}</span>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'

interface ChartItem { label: string; value: number }

const props = withDefaults(defineProps<{
  data?:        ChartItem[]
  color?:       string
  smooth?:      boolean
  fill?:        boolean
  showDots?:    boolean
  showLabels?:  boolean
  valuePrefix?: string
  valueSuffix?: string
  yAxisLabel?:  string
}>(), {
  data:        () => [],
  color:       'var(--chart-1)',
  smooth:      false,
  fill:        true,
  showDots:    true,
  showLabels:  false,
  valuePrefix: '',
  valueSuffix: '',
  yAxisLabel:  '',
})

const uid       = Math.random().toString(36).slice(2, 8)
const svgEl     = ref<SVGSVGElement | null>(null)
const activeIdx = ref<number | null>(null)

const W = 300; const H = 120
const PAD_L = 34   // wider — room for Y-axis labels
const PAD_R = 8
const PAD_T = 16
const PAD_B = 10

const resolvedColor = computed(() => props.color || 'var(--chart-1)')
const hasData       = computed(() => props.data.length >= 2)
// Guard against empty array → Math.max/min returning ±Infinity
const maxValue      = computed(() => props.data.length ? Math.max(...props.data.map(d => d.value)) : 1)
const minValue      = computed(() => props.data.length ? Math.min(...props.data.map(d => d.value)) : 0)

// Add 5% padding so the line never touches the top/bottom edges
const yPad    = computed(() => (maxValue.value - minValue.value) * 0.05 || 1)
const yLow    = computed(() => minValue.value - yPad.value)
const yHigh   = computed(() => maxValue.value + yPad.value)

const yTicks = computed(() => {
  const range = yHigh.value - yLow.value
  return [0, 0.25, 0.5, 0.75, 1].map(f => yLow.value + range * f)
})

function yPos(v: number) {
  const range = yHigh.value - yLow.value || 1
  return H - PAD_B - ((v - yLow.value) / range) * (H - PAD_T - PAD_B)
}

function labelY(py: number): number {
  return Math.max(PAD_T - 2, py - 7)
}

const points = computed(() => {
  const n = props.data.length
  // Guard division by zero when only 1 point (hasData requires >= 2, but be safe)
  return props.data.map((d, i) => ({
    x: n > 1
      ? PAD_L + (i / (n - 1)) * (W - PAD_L - PAD_R)
      : PAD_L + (W - PAD_L - PAD_R) / 2,
    y: yPos(d.value),
  }))
})

const linePath = computed(() => {
  if (points.value.length < 2) return ''
  return points.value.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x},${p.y}`).join(' ')
})

// Catmull-Rom → cubic Bezier smooth path
const smoothLinePath = computed(() => {
  const pts = points.value
  if (pts.length < 2) return ''
  let d = `M ${pts[0].x},${pts[0].y}`
  for (let i = 0; i < pts.length - 1; i++) {
    const p0 = pts[i - 1] ?? pts[i]
    const p1 = pts[i]
    const p2 = pts[i + 1]
    const p3 = pts[i + 2] ?? pts[i + 1]
    const cp1x = p1.x + (p2.x - p0.x) / 6
    const cp1y = p1.y + (p2.y - p0.y) / 6
    const cp2x = p2.x - (p3.x - p1.x) / 6
    const cp2y = p2.y - (p3.y - p1.y) / 6
    d += ` C ${cp1x.toFixed(2)},${cp1y.toFixed(2)} ${cp2x.toFixed(2)},${cp2y.toFixed(2)} ${p2.x},${p2.y}`
  }
  return d
})

const areaPath = computed(() => {
  const base = props.smooth ? smoothLinePath.value : linePath.value
  if (!base) return ''
  return `${base} L ${W - PAD_R},${H - PAD_B} L ${PAD_L},${H - PAD_B} Z`
})

const tooltipStyle = computed(() => {
  if (activeIdx.value === null) return {}
  const p   = points.value[activeIdx.value]
  const pct = (p.x / W) * 100
  const left = `clamp(0px, calc(${pct}% - 44px), calc(100% - 88px))`
  // position tooltip above the point
  const bottom = `calc(100% - ${(p.y / H) * 100}% + 14px)`
  return { left, bottom }
})

function onMove(e: MouseEvent) {
  const rect = (e.currentTarget as SVGElement).getBoundingClientRect()
  const xRel = (e.clientX - rect.left) / rect.width
  const idx  = Math.round(xRel * (props.data.length - 1))
  activeIdx.value = Math.max(0, Math.min(props.data.length - 1, idx))
}

function fmtBase(v: number): string {
  const abs = Math.abs(v)
  if (abs >= 1_000_000) return (v / 1_000_000).toFixed(1) + 'M'
  if (abs >= 1_000)     return (v / 1_000).toFixed(1) + 'k'
  return v.toLocaleString('fr-FR', { maximumFractionDigits: 1 })
}

function fmtLabel(v: number): string {
  return (props.valuePrefix || '') + fmtBase(v) + (props.valueSuffix || '')
}

function truncate(str: string, max: number) {
  return str.length > max ? str.slice(0, max) + '…' : str
}
</script>

<style scoped>
.line-chart-wrap {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  position: relative;
  gap: 3px;
}

.line-empty {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  color: var(--color-text-muted);
  font-size: var(--text-xs);
}

.line-body {
  flex: 1;
  display: flex;
  flex-direction: row;
  align-items: stretch;
  min-height: 0;
  gap: 4px;
  position: relative;
}

.line-y-label {
  writing-mode: vertical-rl;
  transform: rotate(180deg);
  font-size: 8px;
  color: var(--color-text-muted);
  opacity: 0.55;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-height: 90px;
  align-self: center;
  flex-shrink: 0;
  letter-spacing: 0.04em;
}

.line-svg {
  flex: 1;
  width: 100%;
  overflow: visible;
  cursor: crosshair;
}

/* Y-axis tick labels inside SVG */
.y-tick-lbl {
  font-size: 7.5px;
  fill: rgba(255,255,255,0.28);
  font-family: var(--font-sans, sans-serif);
}

.point-label {
  font-size: 8px;
  font-weight: 700;
  pointer-events: none;
  letter-spacing: 0;
}

/* Tooltip */
.line-tooltip {
  position: absolute;
  background: rgba(10, 16, 28, 0.96);
  border: 1px solid rgba(255,255,255,0.1);
  border-radius: 8px;
  padding: 5px 9px;
  display: flex;
  align-items: center;
  gap: 6px;
  pointer-events: none;
  z-index: 10;
  box-shadow: 0 6px 24px rgba(0,0,0,0.55);
  backdrop-filter: blur(8px);
  white-space: nowrap;
}
.tt-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  flex-shrink: 0;
}
.tt-content {
  display: flex;
  flex-direction: column;
  gap: 1px;
}
.tt-lbl { font-size: 9px;  color: rgba(255,255,255,0.4); line-height: 1.2; }
.tt-val { font-size: 11px; font-weight: 700; color: rgba(255,255,255,0.92); letter-spacing: -0.01em; }

/* X labels */
.x-labels {
  display: flex;
  justify-content: space-between;
}
.x-lbl {
  font-size: 9px;
  color: var(--color-text-muted);
  text-align: center;
  flex: 1;
  overflow: hidden;
  transition: color 0.15s, font-weight 0.15s;
}
.x-lbl.x-active {
  color: rgba(255,255,255,0.7);
  font-weight: 600;
}

/* Tooltip transition */
.tt-enter-active, .tt-leave-active { transition: opacity 0.12s; }
.tt-enter-from, .tt-leave-to { opacity: 0; }
</style>
