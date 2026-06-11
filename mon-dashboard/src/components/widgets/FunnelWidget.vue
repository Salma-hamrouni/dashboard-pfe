<template>
  <div class="funnel-wrap">
    <!-- Empty state -->
    <div v-if="!hasData" class="funnel-empty">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" width="28" height="28" opacity="0.4">
        <path d="M22 3H2l8 9.46V19l4 2v-8.54L22 3z"/>
      </svg>
      <span>Configurez X et Y pour afficher l'entonnoir</span>
    </div>

    <template v-else>
      <svg
        :viewBox="`0 0 300 ${svgH}`"
        class="funnel-svg"
        preserveAspectRatio="xMidYMid meet"
      >
        <g
          v-for="(item, i) in displayData"
          :key="i"
          class="funnel-step"
          @mouseenter="hovered = i"
          @mouseleave="hovered = null"
          style="cursor:pointer"
        >
          <!-- Trapezoid -->
          <polygon
            :points="trapPts(i)"
            :fill="barColor(i)"
            :opacity="hovered !== null && hovered !== i ? 0.3 : 0.85"
            style="transition: opacity 0.15s"
          />

          <!-- Hover glow overlay -->
          <polygon
            v-if="hovered === i"
            :points="trapPts(i)"
            fill="white"
            opacity="0.07"
          />

          <!-- Label inside bar -->
          <text
            :x="CX"
            :y="barMidY(i)"
            text-anchor="middle"
            dominant-baseline="middle"
            class="f-bar-lbl"
          >{{ truncate(item.label, 14) }}</text>

          <!-- Value on the right -->
          <text
            :x="CX + maxHalfW + 12"
            :y="barMidY(i) - (i > 0 ? 7 : 0)"
            text-anchor="start"
            dominant-baseline="middle"
            class="f-val-lbl"
          >{{ fmtVal(item.value) }}</text>

          <!-- Conversion rate (except first step) -->
          <text
            v-if="i > 0"
            :x="CX + maxHalfW + 12"
            :y="barMidY(i) + 8"
            text-anchor="start"
            dominant-baseline="middle"
            class="f-rate-lbl"
            :fill="barColor(i)"
          >↓ {{ convRate(i) }}%</text>
        </g>
      </svg>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'

interface ChartItem { label: string; value: number }

const props = withDefaults(defineProps<{
  data?:        ChartItem[]
  colors?:      string[]
  valuePrefix?: string
  valueSuffix?: string
}>(), {
  data:        () => [],
  colors:      () => [],
  valuePrefix: '',
  valueSuffix: '',
})

const hovered = ref<number | null>(null)

const PALETTE = [
  'var(--chart-1)', 'var(--chart-2)', 'var(--chart-3)',
  'var(--chart-4)', 'var(--chart-5)', 'var(--chart-6)',
]

const hasData = computed(() => props.data.length >= 2 && props.data.some(d => d.value > 0))

/** Sort descending so the biggest step is on top */
const displayData = computed(() =>
  [...props.data].filter(d => d.value > 0).sort((a, b) => b.value - a.value)
)

function barColor(i: number) {
  if (props.colors?.length) return props.colors[i % props.colors.length]!
  return PALETTE[i % PALETTE.length]!
}

function fmtVal(n: number) {
  const pre = props.valuePrefix || ''
  const suf = props.valueSuffix || ''
  if (n >= 1_000_000) return pre + (n / 1_000_000).toFixed(1) + 'M' + suf
  if (n >= 1_000)     return pre + (n / 1_000).toFixed(1) + 'k' + suf
  return pre + n.toLocaleString('fr-FR') + suf
}

function convRate(i: number) {
  if (i === 0 || !displayData.value[i - 1]) return 100
  const prev = displayData.value[i - 1]!.value
  return Math.round((displayData.value[i]!.value / prev) * 100)
}

function truncate(s: string, max: number) {
  return s.length > max ? s.slice(0, max) + '…' : s
}

// ── SVG geometry ─────────────────────────────────────────────────────────────
const CX       = 130    // funnel center x (shifted left for right labels)
const maxHalfW = 100    // half-width of largest bar (= 200px total for 1st bar)
const STEP_H   = 34
const GAP      = 5
const svgH     = computed(() => displayData.value.length * (STEP_H + GAP) + 4)

function itemHalfW(i: number): number {
  const n = displayData.value.length
  if (!n) return 0
  // Guard division by zero if first item has value 0 (filtered out, but defensive)
  const firstVal = displayData.value[0]?.value || 1
  return (displayData.value[i]!.value / firstVal) * maxHalfW
}

function trapPts(i: number): string {
  const n    = displayData.value.length
  const topW = itemHalfW(i)
  const botW = i < n - 1 ? itemHalfW(i + 1) : topW * 0.65
  const y1   = i * (STEP_H + GAP)
  const y2   = y1 + STEP_H
  return [
    `${CX - topW},${y1}`,
    `${CX + topW},${y1}`,
    `${CX + botW},${y2}`,
    `${CX - botW},${y2}`,
  ].join(' ')
}

function barMidY(i: number): number {
  return i * (STEP_H + GAP) + STEP_H / 2
}
</script>

<style scoped>
.funnel-wrap {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

.funnel-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  color: var(--color-text-muted);
  font-size: var(--text-xs);
}

.funnel-svg {
  width: 100%;
  height: 100%;
  overflow: visible;
}

.f-bar-lbl {
  font-size: 10px;
  font-weight: 600;
  fill: rgba(255,255,255,0.9);
  pointer-events: none;
  font-family: var(--font-sans, sans-serif);
}

.f-val-lbl {
  font-size: 11px;
  font-weight: 700;
  fill: rgba(255,255,255,0.85);
  font-family: var(--font-sans, sans-serif);
}

.f-rate-lbl {
  font-size: 9px;
  font-weight: 600;
  font-family: var(--font-sans, sans-serif);
  opacity: 0.85;
}
</style>
