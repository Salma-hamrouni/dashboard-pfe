<template>
  <div class="kpi-card" :style="{ '--kpi-color': resolvedColor }">
    <!-- Top accent bar -->
    <div class="kpi-accent"/>

    <div class="kpi-header">
      <div class="kpi-title-group">
        <span class="kpi-title">{{ title }}</span>
        <span v-if="description" class="kpi-description">{{ description }}</span>
      </div>
      <div class="kpi-icon">
        <svg v-if="iconType === 'trending'" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="16" height="16">
          <polyline points="22 7 13.5 15.5 8.5 10.5 2 17"/><polyline points="16 7 22 7 22 13"/>
        </svg>
        <svg v-else-if="iconType === 'users'" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="16" height="16">
          <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/>
          <path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/>
        </svg>
        <svg v-else-if="iconType === 'dollar'" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="16" height="16">
          <line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/>
        </svg>
        <svg v-else-if="iconType === 'bar'" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="16" height="16">
          <line x1="18" y1="20" x2="18" y2="10"/><line x1="12" y1="20" x2="12" y2="4"/>
          <line x1="6" y1="20" x2="6" y2="14"/><line x1="2" y1="20" x2="22" y2="20"/>
        </svg>
        <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="16" height="16">
          <polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/>
        </svg>
      </div>
    </div>

    <!-- Animated value -->
    <div class="kpi-value">{{ displayValue }}</div>

    <!-- Trend badge -->
    <div v-if="trend !== undefined" class="kpi-trend" :class="trend >= 0 ? 'up' : 'down'">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" width="11" height="11">
        <polyline v-if="trend >= 0" points="18 15 12 9 6 15"/>
        <polyline v-else points="6 9 12 15 18 9"/>
      </svg>
      <span>{{ trend >= 0 ? '+' : '' }}{{ trend }}%</span>
      <span class="trend-label">vs mois préc.</span>
    </div>

    <!-- Sparkline -->
    <div class="kpi-sparkline" v-if="sparkline && sparkline.length > 1">
      <svg :viewBox="`0 0 100 32`" preserveAspectRatio="none" class="sparkline-svg">
        <defs>
          <linearGradient :id="`spark-grad-${uid}`" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" :stop-color="resolvedColor" stop-opacity="0.35"/>
            <stop offset="100%" :stop-color="resolvedColor" stop-opacity="0"/>
          </linearGradient>
        </defs>
        <!-- Area fill -->
        <path :d="sparklineArea" :fill="`url(#spark-grad-${uid})`" stroke="none"/>
        <!-- Line -->
        <path
          :d="sparklineLine"
          fill="none"
          :stroke="resolvedColor"
          stroke-width="1.8"
          stroke-linecap="round"
          stroke-linejoin="round"
        />
        <!-- Last dot -->
        <circle
          v-if="sparkLastPt"
          :cx="sparkLastPt.x"
          :cy="sparkLastPt.y"
          r="2.5"
          :fill="resolvedColor"
        />
      </svg>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch, onMounted, onBeforeUnmount } from 'vue'

const props = defineProps<{
  title:        string
  value:        string | number
  trend?:       number
  color?:       string
  icon?:        string
  prefix?:      string
  suffix?:      string
  description?: string
  sparkline?:   number[]
}>()

const uid = Math.random().toString(36).slice(2, 8)

// ── Color ─────────────────────────────────────────────────────────────────
const resolvedColor = computed(() => props.color || 'var(--color-primary, #6366f1)')

// ── Icon type ────────────────────────────────────────────────────────────
const iconType = computed(() => {
  const ic = (props.icon || '').toLowerCase()
  if (ic.includes('user') || ic.includes('people')) return 'users'
  if (ic.includes('dollar') || ic.includes('money') || ic.includes('euro')) return 'dollar'
  if (ic.includes('bar') || ic.includes('chart')) return 'bar'
  if (ic.includes('trend') || ic.includes('up')) return 'trending'
  return 'line'
})

// ── Count-up animation ────────────────────────────────────────────────────
const animNum = ref(0)
let raf = 0

function runAnim(target: number) {
  cancelAnimationFrame(raf)
  const from = animNum.value
  const dist = target - from
  if (Math.abs(dist) < 0.5) { animNum.value = target; return }
  const dur  = Math.min(900, 200 + Math.abs(dist) * 0.4)
  const t0   = performance.now()

  function tick(now: number) {
    const p = Math.min((now - t0) / dur, 1)
    const eased = 1 - Math.pow(1 - p, 3)  // ease-out cubic
    animNum.value = from + dist * eased
    if (p < 1) raf = requestAnimationFrame(tick)
    else animNum.value = target
  }
  raf = requestAnimationFrame(tick)
}

onMounted(() => {
  if (typeof props.value === 'number') runAnim(props.value)
})
watch(() => props.value, v => {
  if (typeof v === 'number') runAnim(v)
})
onBeforeUnmount(() => cancelAnimationFrame(raf))

// ── Formatted display ─────────────────────────────────────────────────────
const displayValue = computed(() => {
  const pre = props.prefix || ''
  const suf = props.suffix || ''
  if (typeof props.value !== 'number') return pre + props.value + suf
  const n   = animNum.value
  const abs = Math.abs(n)
  if (abs >= 1_000_000) return pre + (n / 1_000_000).toFixed(1) + 'M' + suf
  if (abs >= 10_000)    return pre + (n / 1_000).toFixed(1) + 'k' + suf
  if (abs >= 1_000)     return pre + (n / 1_000).toFixed(abs >= 10_000 ? 1 : 2).replace(/\.?0+$/, '') + 'k' + suf
  // Show up to 2 decimal places for non-integers, integers as-is
  const decimals = Number.isInteger(Math.round(n * 100) / 100) ? 0 : 2
  return pre + n.toLocaleString('fr-FR', { minimumFractionDigits: 0, maximumFractionDigits: decimals }) + suf
})

// ── Sparkline paths ────────────────────────────────────────────────────────
const sparkPoints = computed(() => {
  const data = props.sparkline
  if (!data || data.length < 2) return []
  const max = Math.max(...data)
  const min = Math.min(...data)
  const range = max - min || 1
  return data.map((v, i) => ({
    x: (i / (data.length - 1)) * 100,
    y: 29 - ((v - min) / range) * 26,
  }))
})

const sparklineLine = computed(() => {
  return sparkPoints.value.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(' ')
})

const sparklineArea = computed(() => {
  const line = sparklineLine.value
  if (!line) return ''
  return `${line} L 100,32 L 0,32 Z`
})

const sparkLastPt = computed(() => {
  const pts = sparkPoints.value
  return pts.length ? pts[pts.length - 1] : null
})
</script>

<style scoped>
.kpi-card {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  gap: 5px;
  padding: 4px 2px 2px;
  position: relative;
  overflow: hidden;
}

/* Colored top accent bar */
.kpi-accent {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 2.5px;
  background: var(--kpi-color);
  border-radius: 2px 2px 0 0;
  opacity: 0.8;
  box-shadow: 0 0 8px var(--kpi-color);
}

.kpi-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-top: 4px;
}

.kpi-title-group {
  display: flex;
  flex-direction: column;
  gap: 1px;
  min-width: 0;
}

.kpi-title {
  font-size: var(--text-xs);
  font-weight: var(--weight-medium);
  color: var(--color-text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.06em;
  line-height: 1.3;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.kpi-description {
  font-size: 9px;
  color: var(--color-text-muted, rgba(255,255,255,0.35));
  font-weight: 400;
  letter-spacing: 0;
  text-transform: none;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  line-height: 1.2;
  font-style: italic;
}

/* Icon box — uses the KPI color for bg + glow */
.kpi-icon {
  width: 30px;
  height: 30px;
  border-radius: var(--radius-md);
  background: color-mix(in srgb, var(--kpi-color) 15%, transparent);
  border: 1px solid color-mix(in srgb, var(--kpi-color) 30%, transparent);
  box-shadow: 0 0 10px color-mix(in srgb, var(--kpi-color) 20%, transparent);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--kpi-color);
  flex-shrink: 0;
}

/* Animated value */
.kpi-value {
  font-size: clamp(22px, 4vw, 32px);
  font-weight: var(--weight-bold);
  color: var(--color-text);
  line-height: 1;
  letter-spacing: -0.02em;
  flex: 1;
  display: flex;
  align-items: center;
  font-variant-numeric: tabular-nums;
}

/* Trend badge */
.kpi-trend {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: var(--text-xs);
  font-weight: var(--weight-semibold);
  padding: 2px 7px;
  border-radius: var(--radius-full);
  width: fit-content;
}
.kpi-trend.up {
  color: var(--color-success);
  background: rgba(16, 185, 129, 0.12);
  border: 1px solid rgba(16, 185, 129, 0.2);
}
.kpi-trend.down {
  color: var(--color-danger);
  background: rgba(239, 68, 68, 0.12);
  border: 1px solid rgba(239, 68, 68, 0.2);
}
.trend-label {
  font-weight: var(--weight-normal);
  color: var(--color-text-muted);
}

/* Sparkline */
.kpi-sparkline {
  height: 32px;
  margin-top: 2px;
}
.sparkline-svg {
  width: 100%;
  height: 100%;
}
</style>
