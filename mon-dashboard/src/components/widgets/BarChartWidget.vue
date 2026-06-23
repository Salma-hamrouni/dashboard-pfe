<template>
  <div class="bar-chart" :class="{ 'bar-horizontal': orientation === 'horizontal' }" ref="container">
    <!-- Empty state -->
    <div v-if="!hasData" class="bar-empty">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" width="28" height="28">
        <line x1="18" y1="20" x2="18" y2="10"/><line x1="12" y1="20" x2="12" y2="4"/>
        <line x1="6" y1="20" x2="6" y2="14"/><line x1="2" y1="20" x2="22" y2="20"/>
      </svg>
      <span>Aucune donnée</span>
    </div>

    <!-- ── VERTICAL (default) ──────────────────────────────── -->
    <template v-else-if="orientation !== 'horizontal'">
      <div class="chart-inner">
        <!-- Y axis: optional rotated title + tick labels -->
        <div class="y-col">
          <span v-if="yAxisLabel" class="y-axis-title">{{ yAxisLabel }}</span>
          <div class="y-axis">
            <span v-for="tick in yTicks" :key="tick">{{ fmtTick(tick) }}</span>
          </div>
        </div>

        <!-- Grid + bars -->
        <div class="chart-area">
          <div class="grid-lines">
            <div v-for="tick in yTicks" :key="tick" class="grid-line"/>
          </div>

          <div class="bars-row" :key="dataKey">
            <div
              v-for="(item, i) in data"
              :key="i"
              class="bar-col"
              :style="{ '--bar-delay': `${i * 55}ms` }"
              @mouseenter="hovered = i"
              @mouseleave="hovered = null"
            >
              <!-- Tooltip -->
              <transition name="tt">
                <div
                  v-if="hovered === i"
                  class="bar-tooltip"
                  :class="{ 'tt-right': i < data.length / 2 }"
                >
                  <span class="tt-label">{{ item.label }}</span>
                  <span class="tt-value">{{ fmtValue(item.value) }}</span>
                  <span class="tt-pct">{{ barPct(item.value).toFixed(1) }}% du max</span>
                </div>
              </transition>

              <div class="bar-track">
                <!-- Zero baseline (only shown when dataset has negatives) -->
                <div
                  v-if="dataMin < 0"
                  class="bar-baseline"
                  :style="{ bottom: basePct + '%' }"
                />

                <!-- Value label -->
                <span
                  v-if="showValues"
                  class="bar-value-label"
                  :style="{
                    color: barColor(i),
                    bottom: `calc(${barBottomPct(item.value) + barHeight(item.value)}% + 4px)`,
                  }"
                >{{ fmtValue(item.value) }}</span>

                <div
                  class="bar-fill"
                  :class="{ 'bar-dimmed': hovered !== null && hovered !== i }"
                  :style="{
                    height: barHeight(item.value) + '%',
                    bottom: barBottomPct(item.value) + '%',
                    '--bar-color': barColor(i),
                    background: item.value >= 0
                      ? `linear-gradient(to top, ${barColor(i)}50, ${barColor(i)})`
                      : `linear-gradient(to bottom, ${barColor(i)}50, ${barColor(i)})`,
                    boxShadow: hovered === i
                      ? `0 0 18px ${barColor(i)}55, inset 0 1px 0 rgba(255,255,255,0.15)`
                      : 'inset 0 1px 0 rgba(255,255,255,0.08)',
                    borderRadius: item.value >= 0
                      ? `${resolvedRadius}px ${resolvedRadius}px 0 0`
                      : `0 0 ${resolvedRadius}px ${resolvedRadius}px`,
                    position: 'absolute',
                    left: 0, right: 0,
                  }"
                />
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- X-axis labels -->
      <div class="x-axis" :style="{ paddingLeft: (yAxisLabel ? 52 : 38) + 'px' }">
        <div
          v-for="(item, i) in data"
          :key="i"
          class="x-label"
          :class="{ 'x-active': hovered === i }"
          :title="item.label"
        >{{ truncate(item.label, 8) }}</div>
      </div>
    </template>

    <!-- ── HORIZONTAL ─────────────────────────────────────── -->
    <template v-else>
      <div class="horiz-chart" :key="dataKey">
        <div
          v-for="(item, i) in data"
          :key="i"
          class="horiz-row"
          :style="{ '--bar-delay': `${i * 45}ms` }"
          @mouseenter="hovered = i"
          @mouseleave="hovered = null"
        >
          <!-- Label -->
          <span
            class="horiz-label"
            :class="{ 'x-active': hovered === i }"
            :title="item.label"
          >{{ truncate(item.label, 12) }}</span>

          <!-- Track -->
          <div class="horiz-track">
            <div
              class="horiz-fill"
              :class="{ 'bar-dimmed': hovered !== null && hovered !== i }"
              :style="{
                width: barPct(item.value) + '%',
                background: `linear-gradient(to right, ${barColor(i)}55, ${barColor(i)})`,
                boxShadow: hovered === i ? `0 0 10px ${barColor(i)}55` : 'none',
                borderRadius: `0 ${resolvedRadius}px ${resolvedRadius}px 0`,
              }"
            />
            <span
              v-if="showValues"
              class="horiz-value"
              :style="{ color: barColor(i) }"
            >{{ fmtValue(item.value) }}</span>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'

interface ChartItem { label: string; value: number }

const props = withDefaults(defineProps<{
  data?:         ChartItem[]
  color?:        string
  colors?:       string[]
  orientation?:  'vertical' | 'horizontal'
  showValues?:   boolean
  borderRadius?: number
  valuePrefix?:  string
  valueSuffix?:  string
  yAxisLabel?:   string
}>(), {
  data:        () => [],
  orientation: 'vertical',
  showValues:  false,
  borderRadius: 3,
  valuePrefix: '',
  valueSuffix: '',
  yAxisLabel:  '',
})

const hovered = ref<number | null>(null)

const hasData    = computed(() => props.data.length > 0 && props.data.some(d => d.value !== 0))
const dataMax    = computed(() => Math.max(...props.data.map(d => d.value), 0))
const dataMin    = computed(() => Math.min(...props.data.map(d => d.value), 0))
const totalRange = computed(() => dataMax.value - dataMin.value || 1)
/** Position of the zero baseline from bottom, as a % of chart height */
const basePct    = computed(() => (-dataMin.value / totalRange.value) * 100)

// Compatibility alias used by y-ticks
const maxValue   = computed(() => dataMax.value || 1)

const resolvedRadius = computed(() => props.borderRadius ?? 3)

// Key changes when data changes → forces re-mount of bars → re-triggers CSS animation
const dataKey = computed(() => props.data.map(d => d.label + d.value).join('|'))

const PALETTE = ['var(--chart-1)', 'var(--chart-2)', 'var(--chart-3)', 'var(--chart-4)', 'var(--chart-5)', 'var(--chart-6)']

function barColor(i: number) {
  if (props.color) return props.color
  if (props.colors?.length) return props.colors[i % props.colors.length]
  return PALETTE[i % PALETTE.length]
}

/** Height of a bar as % of the chart area */
function barHeight(v: number): number {
  return (Math.abs(v) / totalRange.value) * 100
}
/** Bottom position of a bar from the chart floor as % */
function barBottomPct(v: number): number {
  return v >= 0 ? basePct.value : basePct.value - barHeight(v)
}

// Keep old name for tooltip
function barPct(v: number): number {
  return dataMax.value ? (v / dataMax.value) * 100 : 0
}

const yTicks = computed(() => {
  const steps = 4
  const result: number[] = []
  for (let i = steps; i >= 0; i--) {
    const v = dataMin.value + (totalRange.value * i) / steps
    result.push(Math.round(v * 100) / 100)
  }
  return result.reverse()
})

function formatTick(n: number) {
  const abs = Math.abs(n)
  const sign = n < 0 ? '-' : ''
  if (abs >= 1_000_000) return sign + (abs / 1_000_000).toFixed(1) + 'M'
  if (abs >= 1_000)     return sign + (abs / 1_000).toFixed(abs >= 10_000 ? 0 : 1) + 'k'
  if (!Number.isInteger(n)) return n.toLocaleString('fr-FR', { maximumFractionDigits: 2 })
  return n.toString()
}

function fmtValue(n: number) {
  return (props.valuePrefix || '') + formatTick(n) + (props.valueSuffix || '')
}

function fmtTick(n: number) {
  return formatTick(n) + (props.valueSuffix || '')
}

function truncate(str: string, max: number) {
  return str.length > max ? str.slice(0, max) + '…' : str
}
</script>

<style scoped>
.bar-chart {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  gap: 4px;
  font-family: var(--font-sans);
}

.bar-empty {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  color: var(--color-text-muted);
  font-size: var(--text-xs);
}

/* ── Vertical ─────────────────────────────────── */
.chart-inner {
  flex: 1;
  display: flex;
  gap: 6px;
  overflow: hidden;
  min-height: 0;
}

.y-col {
  display: flex;
  flex-direction: row;
  align-items: stretch;
  flex-shrink: 0;
  gap: 2px;
}

.y-axis-title {
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
  letter-spacing: 0.04em;
}

.y-axis {
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  width: 32px;
  flex-shrink: 0;
  padding: 2px 0;
}
.y-axis span {
  font-size: 9px;
  color: var(--color-text-muted);
  text-align: right;
  line-height: 1;
}

.chart-area {
  flex: 1;
  position: relative;
  display: flex;
  flex-direction: column;
}

.grid-lines {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  pointer-events: none;
  padding: 2px 0;
}
.grid-line {
  width: 100%;
  height: 1px;
  background: none;
  border-top: 1px dashed rgba(255, 255, 255, 0.07);
}

.bars-row {
  flex: 1;
  display: flex;
  align-items: flex-end;
  gap: 6px;
  padding: 2px 0;
  position: relative;
  z-index: 1;
}

.bar-col {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: flex-end;
  height: 100%;
  position: relative;
  cursor: pointer;
  overflow: visible;
}

.bar-value-label {
  position: absolute;
  left: 50%;
  transform: translateX(-50%);
  font-size: 8px;
  font-weight: 700;
  line-height: 1;
  white-space: nowrap;
  pointer-events: none;
  z-index: 2;
  text-shadow: 0 1px 4px rgba(0,0,0,0.6);
  transition: opacity 0.2s;
}

/* ── Tooltip ── */
.bar-tooltip {
  position: absolute;
  bottom: calc(100% + 8px);
  left: 50%;
  transform: translateX(-50%);
  background: rgba(10, 16, 28, 0.96);
  border: 1px solid rgba(17,23,20,0.10);
  border-radius: 8px;
  padding: 5px 10px 6px;
  white-space: nowrap;
  z-index: 20;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1px;
  pointer-events: none;
  box-shadow: 0 6px 24px rgba(0,0,0,0.55);
  backdrop-filter: blur(8px);
}
.bar-tooltip::after {
  content: '';
  position: absolute;
  top: 100%;
  left: 50%;
  transform: translateX(-50%);
  border: 5px solid transparent;
  border-top-color: rgba(17,23,20,0.10);
}
.bar-tooltip.tt-right { left: 0; transform: none; }
.bar-tooltip.tt-right::after { left: 16px; }

.tt-label { font-size: 9px; color: #94A99A; }
.tt-value { font-size: 12px; font-weight: 700; color: #111714; letter-spacing: -0.02em; }
.tt-pct   { font-size: 9px; color: #94A99A; }

.bar-track {
  width: 100%;
  max-width: 36px;
  height: 100%;
  position: relative;
}

/* Zero baseline line — only visible when negatives are present */
.bar-baseline {
  position: absolute;
  left: 0; right: 0;
  height: 1px;
  background: #C4CFC7;
  pointer-events: none;
  z-index: 2;
}

/* ── Bar fill + entrance animation ── */
@keyframes barRise {
  from { transform: scaleY(0); opacity: 0.3; }
  to   { transform: scaleY(1); opacity: 1; }
}

.bar-fill {
  width: 100%;
  transform-origin: bottom center;
  animation: barRise 0.55s cubic-bezier(0.34, 1.15, 0.64, 1) both;
  animation-delay: var(--bar-delay, 0ms);
  transition: opacity 0.25s, filter 0.25s, box-shadow 0.2s;
  min-height: 2px;
}

.bar-dimmed {
  opacity: 0.18 !important;
  filter: saturate(0.2);
}

.x-axis {
  display: flex;
  gap: 6px;
}
.x-label {
  flex: 1;
  text-align: center;
  font-size: 9px;
  color: var(--color-text-muted);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  transition: color 0.2s;
}
.x-label.x-active {
  color: #4B5E52;
  font-weight: 600;
}

/* ── Horizontal ───────────────────────────────── */
.horiz-chart {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 5px;
  overflow: hidden;
  justify-content: center;
}

.horiz-row {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
}

.horiz-label {
  width: 64px;
  flex-shrink: 0;
  font-size: 9px;
  color: var(--color-text-muted);
  text-align: right;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  transition: color 0.2s;
}
.horiz-label.x-active {
  color: #4B5E52;
  font-weight: 600;
}

.horiz-track {
  flex: 1;
  height: 18px;
  background: rgba(255,255,255,0.04);
  border-radius: 4px;
  overflow: hidden;
  position: relative;
  display: flex;
  align-items: center;
}

/* ── Horizontal bar fill + entrance animation ── */
@keyframes horizGrow {
  from { transform: scaleX(0); opacity: 0.3; }
  to   { transform: scaleX(1); opacity: 1; }
}

.horiz-fill {
  height: 100%;
  min-width: 2px;
  transform-origin: left center;
  animation: horizGrow 0.55s cubic-bezier(0.34, 1.15, 0.64, 1) both;
  animation-delay: var(--bar-delay, 0ms);
  transition: opacity 0.25s, filter 0.25s, box-shadow 0.2s;
}

.horiz-value {
  position: absolute;
  right: 7px;
  font-size: 9px;
  font-weight: 700;
  line-height: 1;
  color: white;
  text-shadow: 0 1px 4px rgba(0,0,0,0.7);
}

/* Tooltip transitions */
.tt-enter-active, .tt-leave-active { transition: opacity 0.15s, transform 0.15s; }
.tt-enter-from, .tt-leave-to { opacity: 0; transform: translateX(-50%) translateY(5px); }
.bar-tooltip.tt-right.tt-enter-from,
.bar-tooltip.tt-right.tt-leave-to { transform: translateY(5px); }
</style>




