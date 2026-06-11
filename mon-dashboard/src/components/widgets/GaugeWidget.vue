<template>
  <div class="gauge-wrap">
    <!-- Empty state -->
    <div v-if="!hasValue" class="gauge-empty">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" width="28" height="28" opacity="0.4">
        <path d="M12 22C6.477 22 2 17.523 2 12S6.477 2 12 2s10 4.477 10 10"/>
        <path d="M12 6v6l4 2"/>
      </svg>
      <span>Configurez un axe Y numérique</span>
    </div>

    <template v-else>
      <svg viewBox="0 0 200 126" class="gauge-svg" preserveAspectRatio="xMidYMid meet">
        <defs>
          <linearGradient :id="`gg-${uid}`" gradientUnits="userSpaceOnUse" x1="22" y1="100" x2="178" y2="100">
            <stop offset="0%"   :stop-color="resolvedColor" stop-opacity="0.4"/>
            <stop offset="100%" :stop-color="resolvedColor" stop-opacity="1"/>
          </linearGradient>
          <filter :id="`glow-${uid}`">
            <feGaussianBlur stdDeviation="3" result="blur"/>
            <feMerge><feMergeNode in="blur"/><feMergeNode in="SourceGraphic"/></feMerge>
          </filter>
        </defs>

        <!-- Tick marks -->
        <g v-for="t in ticks" :key="t.p">
          <line
            :x1="tickPt(t.p, RO + 6).x" :y1="tickPt(t.p, RO + 6).y"
            :x2="tickPt(t.p, RO + 11).x" :y2="tickPt(t.p, RO + 11).y"
            stroke="rgba(255,255,255,0.2)" stroke-width="1.5" stroke-linecap="round"
          />
        </g>

        <!-- Background track -->
        <path :d="trackPath" class="g-track"/>

        <!-- Colored fill arc -->
        <path v-if="normPct > 0.001" :d="fillPath" :fill="`url(#gg-${uid})`" class="g-fill"/>

        <!-- Glow tip circle -->
        <circle
          v-if="normPct > 0.01 && normPct < 0.999"
          :cx="tipPt.x" :cy="tipPt.y" r="6"
          :fill="resolvedColor" opacity="0.25"
          :filter="`url(#glow-${uid})`"
        />
        <circle
          v-if="normPct > 0.01 && normPct < 0.999"
          :cx="tipPt.x" :cy="tipPt.y" r="4"
          :fill="resolvedColor"
        />

        <!-- Center pivot -->
        <circle cx="100" cy="100" r="6" :fill="resolvedColor" opacity="0.7"/>
        <circle cx="100" cy="100" r="3" fill="rgba(255,255,255,0.9)"/>

        <!-- Value text (shifts up when label is present) -->
        <text :x="100" :y="label ? 72 : 80" class="g-val" text-anchor="middle" dominant-baseline="auto">
          {{ displayValue }}
        </text>
        <text :x="100" :y="label ? 87 : 96" class="g-pct" text-anchor="middle" dominant-baseline="auto">
          {{ pct }}%
        </text>
        <!-- Optional label (metric name) inside the arc hole -->
        <text v-if="label" x="100" y="103" class="g-label" text-anchor="middle" dominant-baseline="auto">
          {{ label.length > 18 ? label.slice(0, 17) + '…' : label }}
        </text>

        <!-- Min / Max labels -->
        <text x="22"  y="118" class="g-range" text-anchor="middle">{{ fmtN(resolvedMin) }}</text>
        <text x="178" y="118" class="g-range" text-anchor="middle">{{ fmtN(resolvedMax) }}</text>
      </svg>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(defineProps<{
  value?:  number | string
  min?:    number
  max?:    number
  color?:  string
  prefix?: string
  suffix?: string
  label?:  string
}>(), {
  value:  undefined,
  min:    0,
  max:    100,
  color:  'var(--chart-1)',
  prefix: '',
  suffix: '',
  label:  '',
})

const uid = Math.random().toString(36).slice(2, 8)

const resolvedColor = computed(() => props.color || 'var(--chart-1)')
const resolvedMin   = computed(() => props.min ?? 0)
const resolvedMax   = computed(() => props.max ?? 100)

const numValue = computed<number | null>(() => {
  if (typeof props.value === 'number') return props.value
  if (typeof props.value === 'string') {
    const n = parseFloat(props.value.replace(/[^\d.-]/g, ''))
    return isNaN(n) ? null : n
  }
  return null
})

const hasValue  = computed(() => numValue.value !== null)
const normPct   = computed(() => {
  if (numValue.value === null) return 0
  const range = resolvedMax.value - resolvedMin.value || 1
  return Math.max(0, Math.min(1, (numValue.value - resolvedMin.value) / range))
})
const pct = computed(() => Math.round(normPct.value * 100))

const displayValue = computed(() => {
  if (numValue.value === null) return '–'
  const n  = numValue.value
  const pre = props.prefix || ''
  const suf = props.suffix || ''
  if (Math.abs(n) >= 1_000_000) return pre + (n / 1_000_000).toFixed(1) + 'M' + suf
  if (Math.abs(n) >= 1_000)     return pre + (n / 1_000).toFixed(1) + 'k' + suf
  return pre + n.toLocaleString('fr-FR') + suf
})

function fmtN(n: number) {
  if (Math.abs(n) >= 1_000_000) return (n / 1_000_000).toFixed(1) + 'M'
  if (Math.abs(n) >= 1_000)     return (n / 1_000).toFixed(0) + 'k'
  return n.toLocaleString('fr-FR')
}

// ── Arc geometry ─────────────────────────────────────────────────────────────
// Gauge: semi-circle from left (p=0) counterclockwise over top to right (p=1)
// Center (100,100), outer R=78, inner R=58
const CX = 100, CY = 100, RO = 78, RI = 58

/** Cartesian point at fraction p (0=left, 1=right) on radius r */
function pt(r: number, p: number) {
  const a = Math.PI * (1 - Math.max(0, Math.min(0.9999, p)))
  return {
    x: +(CX + r * Math.cos(a)).toFixed(3),
    y: +(CY - r * Math.sin(a)).toFixed(3),
  }
}

/** Annular arc path from fraction `from` to `to` */
function annArc(from: number, to: number): string {
  const p   = Math.min(0.9999, Math.max(0, to))
  if (p - from < 0.0001) return ''
  const laf = (p - from) > 0.5 ? 1 : 0
  const oa  = pt(RO, from), ob = pt(RO, p)
  const ib  = pt(RI, p),    ia = pt(RI, from)
  return [
    `M ${oa.x},${oa.y}`,
    `A ${RO},${RO} 0 ${laf},0 ${ob.x},${ob.y}`,
    `L ${ib.x},${ib.y}`,
    `A ${RI},${RI} 0 ${laf},1 ${ia.x},${ia.y}`,
    'Z',
  ].join(' ')
}

const trackPath = computed(() => annArc(0, 1))
const fillPath  = computed(() => annArc(0, normPct.value))
const tipPt     = computed(() => pt((RO + RI) / 2, normPct.value))

/** Outer tick point */
function tickPt(p: number, r: number) { return pt(r, p) }

const ticks = [0, 0.25, 0.5, 0.75, 1].map(p => ({ p }))
</script>

<style scoped>
.gauge-wrap {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.gauge-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  color: var(--color-text-muted);
  font-size: var(--text-xs);
}

.gauge-svg {
  width: 100%;
  max-width: 220px;
  height: auto;
  overflow: visible;
}

.g-track {
  fill: rgba(255,255,255,0.06);
}

.g-fill {
  animation: gauge-in 0.7s cubic-bezier(0.16, 1, 0.3, 1);
}
@keyframes gauge-in {
  from { opacity: 0; }
  to   { opacity: 1; }
}

.g-val {
  font-size: 22px;
  font-weight: 700;
  fill: rgba(255,255,255,0.92);
  font-family: var(--font-sans, sans-serif);
}
.g-pct {
  font-size: 10px;
  fill: rgba(255,255,255,0.45);
  font-family: var(--font-sans, sans-serif);
  letter-spacing: 0.05em;
}
.g-label {
  font-size: 9px;
  fill: rgba(255,255,255,0.35);
  font-family: var(--font-sans, sans-serif);
  text-transform: uppercase;
  letter-spacing: 0.07em;
}
.g-range {
  font-size: 8.5px;
  fill: rgba(255,255,255,0.3);
  font-family: var(--font-sans, sans-serif);
}
</style>
