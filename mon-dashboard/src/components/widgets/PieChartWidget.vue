<template>
  <div class="pie-widget">
    <!-- ── Empty state ──────────────────────────────────────────── -->
    <div v-if="!hasData" class="pie-empty">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" width="32" height="32" opacity="0.3">
        <circle cx="12" cy="12" r="10"/><path d="M12 2v10l4.5 4.5"/>
      </svg>
      <span>Choisissez une Dimension et une Mesure</span>
    </div>

    <div v-else class="pie-layout" :class="`legend-${legendPos}`">

      <!-- ═══ SVG ════════════════════════════════════════════════ -->
      <div class="donut-wrap">
        <svg :viewBox="`0 0 ${S} ${S}`" class="donut-svg" :key="animKey">
          <defs>
            <!-- Drop-shadow filter reused by all slices on hover -->
            <filter id="pie-glow" x="-40%" y="-40%" width="180%" height="180%">
              <feGaussianBlur in="SourceGraphic" stdDeviation="4" result="blur"/>
              <feComposite in="SourceGraphic" in2="blur" operator="over"/>
            </filter>
          </defs>

          <g :transform="`translate(${C},${C})`">

            <!-- Background track (donut only) -->
            <circle
              v-if="isDoughnut"
              r="75.5"
              fill="none"
              stroke-width="39"
              stroke="rgba(255,255,255,0.025)"
            />

            <!-- ── Slices ────────────────────────────────────── -->
            <g
              v-for="(sl, i) in slices"
              :key="i"
              class="slice-g"
              :transform="hovered === i ? `translate(${sl.tx},${sl.ty})` : ''"
            >
              <path
                :d="sl.path"
                :fill="sl.color"
                :stroke="bgColor"
                :stroke-width="slices.length > 1 ? 2 : 0"
                stroke-linejoin="round"
                class="donut-slice"
                :class="{ 'is-dimmed': hovered !== null && hovered !== i }"
                :style="{
                  animationDelay: `${i * 50}ms`,
                  filter: hovered === i ? `brightness(1.18) drop-shadow(0 0 10px ${sl.color}88)` : '',
                }"
                @mouseenter="hovered = i"
                @mouseleave="hovered = null"
              />
            </g>

            <!-- Donut hole -->
            <circle
              v-if="isDoughnut"
              :r="IR"
              :fill="bgColor"
              style="pointer-events:none"
            />

            <!-- ── Center info (donut only) ─────────────────── -->
            <g v-if="isDoughnut" style="pointer-events:none">
              <!-- Idle state: total + label -->
              <template v-if="hovered === null">
                <text x="0" y="-12" class="c-total" text-anchor="middle" dominant-baseline="middle">
                  {{ fmtNum(total) }}
                </text>
                <text x="0" y="9" class="c-lbl" text-anchor="middle" dominant-baseline="middle">
                  {{ yAxisLabel ? truncate(yAxisLabel, 10).toUpperCase() : 'TOTAL' }}
                </text>
                <text x="0" y="22" class="c-count" text-anchor="middle" dominant-baseline="middle">
                  {{ slices.length }} part{{ slices.length > 1 ? 's' : '' }}
                </text>
              </template>
              <!-- Hover state: category + value + pct -->
              <template v-else>
                <text x="0" :y="-18" class="c-cat" text-anchor="middle" dominant-baseline="middle">
                  {{ truncate(slices[hovered].label, 11) }}
                </text>
                <text x="0" y="2" class="c-val" text-anchor="middle" dominant-baseline="middle"
                  :fill="slices[hovered].color">
                  {{ fmtNum(slices[hovered].value) }}
                </text>
                <text x="0" y="19" class="c-pct" text-anchor="middle" dominant-baseline="middle">
                  {{ slices[hovered].pct }}%
                </text>
              </template>
            </g>

            <!-- ── Slice labels (when showLabels = true) ────── -->
            <template v-if="showLabels">
              <g
                v-for="(sl, i) in slices"
                :key="'L' + i"
                style="pointer-events:none"
                :style="{ opacity: hovered !== null && hovered !== i ? 0.08 : 1, transition: 'opacity 0.2s' }"
              >
                <!-- ≥ 18% → internal pill (name + value + pct) -->
                <g v-if="sl.pct >= 18">
                  <rect
                    :x="sl.lbX - 29" :y="sl.lbY - 24"
                    width="58" height="48" rx="8"
                    fill="rgba(0,0,0,0.5)"
                  />
                  <text :x="sl.lbX" :y="sl.lbY - 13"
                    class="lbl-name" text-anchor="middle" dominant-baseline="middle">
                    {{ truncate(sl.label, 8) }}
                  </text>
                  <text :x="sl.lbX" :y="sl.lbY + 2"
                    class="lbl-val" text-anchor="middle" dominant-baseline="middle"
                    :fill="sl.color">
                    {{ fmtNum(sl.value) }}
                  </text>
                  <text :x="sl.lbX" :y="sl.lbY + 16"
                    class="lbl-pct" text-anchor="middle" dominant-baseline="middle">
                    {{ sl.pct }}%
                  </text>
                </g>

                <!-- 5–18% → leader line + external label -->
                <g v-else-if="sl.pct >= 5">
                  <polyline
                    :points="`${sl.lx1},${sl.ly1} ${sl.lx2},${sl.ly2} ${sl.lx3},${sl.ly3}`"
                    fill="none" :stroke="sl.color" stroke-width="1.2"
                    stroke-linecap="round" stroke-linejoin="round" opacity="0.75"
                  />
                  <circle :cx="sl.lx1" :cy="sl.ly1" r="2.5" :fill="sl.color"/>
                  <text
                    :x="sl.txtX" :y="sl.ly3 - 8"
                    :text-anchor="sl.side > 0 ? 'start' : 'end'"
                    class="lbl-ext-name" dominant-baseline="middle">
                    {{ truncate(sl.label, 10) }}
                  </text>
                  <text
                    :x="sl.txtX" :y="sl.ly3 + 8"
                    :text-anchor="sl.side > 0 ? 'start' : 'end'"
                    class="lbl-ext-val" dominant-baseline="middle" :fill="sl.color">
                    {{ fmtNum(sl.value) }} · {{ sl.pct }}%
                  </text>
                </g>

                <!-- < 5% → just a tiny pct dot at edge -->
                <g v-else>
                  <circle :cx="sl.lx1" :cy="sl.ly1" r="3" :fill="sl.color" opacity="0.8"/>
                </g>
              </g>
            </template>

          </g>
        </svg>

        <!-- Floating info card — pure pie on hover (no center text) -->
        <transition name="pcard">
          <div v-if="!isDoughnut && hovered !== null" class="pie-card">
            <div class="pie-card-bar" :style="{ background: slices[hovered].color }"/>
            <div class="pie-card-body">
              <div class="pie-card-row">
                <span class="pcard-label">{{ slices[hovered].label }}</span>
                <span class="pcard-pct" :style="{ color: slices[hovered].color }">
                  {{ slices[hovered].pct }}%
                </span>
              </div>
              <span class="pcard-val">{{ fmtNum(slices[hovered].value) }}</span>
            </div>
          </div>
        </transition>
      </div>

      <!-- ═══ Legend ════════════════════════════════════════════ -->
      <div v-if="legendPos !== 'none'" class="pie-legend">
        <div
          v-for="(sl, i) in slices"
          :key="i"
          class="leg-row"
          :data-label="sl.label"
          :class="{ 'leg-active': hovered === i, 'leg-dim': hovered !== null && hovered !== i }"
          @mouseenter="hovered = i"
          @mouseleave="hovered = null"
        >
          <!-- Color indicator -->
          <div class="leg-ind" :style="{ background: sl.color }"/>

          <!-- Content -->
          <div class="leg-content">
            <!-- Row 1: name + pct -->
            <div class="leg-row1">
              <span class="leg-name" :title="fullLabel(sl.label)">{{ fullLabel(sl.label) }}</span>
              <span class="leg-pct" :style="{ color: sl.color }">{{ sl.pct }}%</span>
            </div>
            <!-- Progress bar -->
            <div class="leg-track">
              <div
                class="leg-fill"
                :style="{ width: sl.pct + '%', background: sl.color }"
              />
            </div>
            <!-- Row 2: measure label + value -->
            <div class="leg-row2">
              <span class="leg-measure">{{ yAxisLabel || 'valeur' }}</span>
              <span class="leg-val">{{ fmtNum(sl.value) }}</span>
            </div>
          </div>
        </div>
      </div>

    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'

const props = withDefaults(defineProps<{
  data?:        { label: string; value: number }[]
  colors?:      string[]
  bgColor?:     string
  legendPos?:   'right' | 'bottom' | 'none'
  showLabels?:  boolean
  donut?:       boolean
  maxSlices?:   number
  valuePrefix?: string
  valueSuffix?: string
  xAxisLabel?:  string
  yAxisLabel?:  string
}>(), {
  data:        () => [],
  colors:      () => [
    '#6366f1', '#10b981', '#f59e0b', '#ef4444',
    '#3b82f6', '#ec4899', '#06b6d4', '#84cc16',
  ],
  bgColor:     '#0a1f1a',
  legendPos:   'right',
  showLabels:  false,
  donut:       true,
  maxSlices:   8,
  valuePrefix: '',
  valueSuffix: '',
  xAxisLabel:  '',
  yAxisLabel:  '',
})

// ── Geometry ──────────────────────────────────────────────────────────────────
const S     = 340   // viewBox side — labels up to ±128 from center stay within
const C     = 170   // center
const OR    = 95    // outer radius (donut)
const IR    = 56    // inner radius (donut hole)
const MR    = 75.5  // mid-ring for internal labels
const OP    = 110   // outer radius for full pie (larger since no hole)
// Leader line radii
const LR1   = OR + 4   // start (just outside arc)
const LR2   = OR + 16  // elbow
// lx3 = lx2 + side*13 ; txtX = lx3 + side*3  (computed per slice)

// Explode distance (SVG user units) — how much a hovered slice pops out
const EXPLODE = 8

const isDoughnut = computed(() => props.donut !== false)

// ── State ─────────────────────────────────────────────────────────────────────
const hovered = ref<number | null>(null)
const animKey = ref(0)

// Efficient watch: compare a fingerprint string instead of deep-watching objects
const dataFP = computed(() =>
  (props.data ?? []).map(d => `${d.label}:${d.value}`).join('|')
)
watch(dataFP, () => {
  animKey.value++
  hovered.value = null
})

// ── Data preparation ──────────────────────────────────────────────────────────
const hasData = computed(() => (props.data ?? []).some(d => d.value > 0))

const otherCount = ref(0)
const filteredData = computed(() => {
  const valid = (props.data ?? []).filter(d => d.value > 0)
  if (valid.length <= props.maxSlices) { otherCount.value = 0; return valid }
  const top  = valid.slice(0, props.maxSlices - 1)
  const rest = valid.slice(props.maxSlices - 1)
  otherCount.value = rest.length
  return [...top, {
    label: `Autres (${rest.length})`,
    value: rest.reduce((s, d) => s + d.value, 0),
  }]
})

const total = computed(() =>
  filteredData.value.reduce((s, d) => s + d.value, 0)
)

// ── Math helpers ──────────────────────────────────────────────────────────────
function rad(a: number) { return ((a - 90) * Math.PI) / 180 }
function pt(a: number, r: number): [number, number] {
  return [+(Math.cos(rad(a)) * r).toFixed(3), +(Math.sin(rad(a)) * r).toFixed(3)]
}

// ── Path builders ─────────────────────────────────────────────────────────────
function donutPath(sa: number, ea: number): string {
  if (ea - sa >= 359.99)
    return donutPath(sa, sa + 179.99) + ' ' + donutPath(sa + 179.99, ea)
  // >= 180 (not >) so that an exact 50% slice uses the large-arc flag correctly
  const laf = ea - sa >= 180 ? 1 : 0
  const [sx, sy]   = pt(sa, OR);  const [ex, ey]   = pt(ea, OR)
  const [six, siy] = pt(sa, IR);  const [eix, eiy] = pt(ea, IR)
  return `M${sx},${sy} A${OR},${OR} 0 ${laf},1 ${ex},${ey} L${eix},${eiy} A${IR},${IR} 0 ${laf},0 ${six},${siy} Z`
}

function wedgePath(sa: number, ea: number): string {
  if (ea - sa >= 359.99)
    return wedgePath(sa, sa + 179.99) + ' ' + wedgePath(sa + 179.99, ea)
  // >= 180 (not >) so that an exact 50% slice uses the large-arc flag correctly
  const laf = ea - sa >= 180 ? 1 : 0
  const [sx, sy] = pt(sa, OP); const [ex, ey] = pt(ea, OP)
  return `M0,0 L${sx},${sy} A${OP},${OP} 0 ${laf},1 ${ex},${ey} Z`
}

// ── Slices ────────────────────────────────────────────────────────────────────
const slices = computed(() => {
  let angle = 0
  return filteredData.value.map((d, i) => {
    const pct   = total.value > 0 ? (d.value / total.value) * 100 : 0
    const sweep = (pct / 100) * 360
    const sa    = angle
    angle      += sweep
    const ea    = angle

    // Mid-angle direction
    const midA  = (sa + ea) / 2
    const cosM  = Math.cos(rad(midA))
    const sinM  = Math.sin(rad(midA))
    const side  = cosM >= 0 ? 1 : -1

    // Explode translation
    const tx = +(cosM * EXPLODE).toFixed(2)
    const ty = +(sinM * EXPLODE).toFixed(2)

    // Internal label position
    const lblR  = isDoughnut.value ? MR : OP * 0.60
    const lbX   = +(cosM * lblR).toFixed(2)
    const lbY   = +(sinM * lblR).toFixed(2)

    // Leader line points
    const lx1   = +(cosM * LR1).toFixed(2)
    const ly1   = +(sinM * LR1).toFixed(2)
    const lx2   = +(cosM * LR2).toFixed(2)
    const ly2   = +(sinM * LR2).toFixed(2)
    const lx3   = +(lx2 + side * 13).toFixed(2)
    const ly3   = ly2
    const txtX  = +(lx3 + side * 3).toFixed(2)

    return {
      label: d.label,
      value: d.value,
      pct:   Math.round(pct * 10) / 10,
      color: props.colors[i % props.colors.length]!,
      path:  isDoughnut.value ? donutPath(sa, ea) : wedgePath(sa, ea),
      tx, ty,
      lbX, lbY,
      lx1, ly1, lx2, ly2, lx3, ly3, txtX, side,
    }
  })
})

// ── Helpers ───────────────────────────────────────────────────────────────────
function fmtNum(n: number): string {
  const abs  = Math.abs(n)
  const base = abs >= 1_000_000
    ? (n / 1_000_000).toFixed(1) + 'M'
    : abs >= 1_000
      ? (n / 1_000).toFixed(1) + 'k'
      : n.toLocaleString('fr-FR', { maximumFractionDigits: 2 })
  return (props.valuePrefix || '') + base + (props.valueSuffix || '')
}

function truncate(s: string, max: number): string {
  return s && s.length > max ? s.slice(0, max) + '…' : (s ?? '')
}

function fullLabel(label: string): string {
  return props.xAxisLabel ? `${props.xAxisLabel} : ${label}` : label
}
</script>

<style scoped>
/* ── Root ──────────────────────────────────────────────────────── */
.pie-widget {
  width: 100%; height: 100%;
  display: flex; align-items: center; justify-content: center;
  font-family: var(--font-sans, sans-serif);
  overflow: hidden;
}

.pie-empty {
  display: flex; flex-direction: column; align-items: center;
  gap: 10px; color: rgba(255,255,255,0.22); font-size: 11px;
  text-align: center; padding: 24px;
}

/* ── Layout ───────────────────────────────────────────────────── */
.pie-layout {
  display: flex; align-items: center; gap: 10px;
  width: 100%; height: 100%; min-height: 0;
}
.pie-layout.legend-bottom { flex-direction: column; justify-content: center; }
.pie-layout.legend-none   { justify-content: center; }

/* ── SVG wrapper ──────────────────────────────────────────────── */
.donut-wrap {
  position: relative; flex-shrink: 0;
  display: flex; align-items: center; justify-content: center;
  align-self: stretch;          /* fill layout height */
  max-height: 100%;
}
.donut-svg {
  /* Square that fits the shortest dimension, max 280px */
  width:  min(100%, 280px);
  height: min(100%, 280px);
  max-width:  min(100vh, 280px);
  max-height: 100%;
  aspect-ratio: 1 / 1;
  overflow: visible;            /* external labels won't clip */
  flex-shrink: 0;
}

/* ── Slice entrance animation ─────────────────────────────────── */
@keyframes slice-in {
  from { transform: scale(0); opacity: 0; }
  to   { transform: scale(1); opacity: 1; }
}

/* Wrap group: handles the explode translate */
.slice-g {
  transition: transform 0.22s cubic-bezier(0.34, 1.4, 0.64, 1);
}

.donut-slice {
  cursor: pointer;
  transform-origin: 0 0;    /* center of pie (after group translate) */
  animation: slice-in 0.5s cubic-bezier(0.34, 1.1, 0.64, 1) both;
  transition: opacity 0.2s, filter 0.2s;
}
.donut-slice.is-dimmed {
  opacity: 0.22;
  filter: saturate(0.25) !important;
}

/* ── Center text (donut) ──────────────────────────────────────── */
.c-total {
  font-size: 20px; font-weight: 800; letter-spacing: -0.03em;
  fill: rgba(255,255,255,0.95);
  font-family: var(--font-sans, sans-serif);
}
.c-lbl {
  font-size: 8px; font-weight: 700; letter-spacing: 0.09em;
  fill: rgba(255,255,255,0.3);
  font-family: var(--font-sans, sans-serif);
}
.c-count {
  font-size: 8px; font-weight: 500;
  fill: rgba(255,255,255,0.2);
  font-family: var(--font-sans, sans-serif);
}
.c-cat {
  font-size: 11px; font-weight: 600;
  fill: rgba(255,255,255,0.65);
  font-family: var(--font-sans, sans-serif);
}
.c-val {
  font-size: 19px; font-weight: 800; letter-spacing: -0.03em;
  font-family: var(--font-sans, sans-serif);
}
.c-pct {
  font-size: 11px; font-weight: 500;
  fill: rgba(255,255,255,0.38);
  font-family: var(--font-sans, sans-serif);
}

/* ── Internal slice labels ────────────────────────────────────── */
.lbl-name {
  font-size: 10px; font-weight: 700;
  fill: rgba(255,255,255,0.88);
  font-family: var(--font-sans, sans-serif);
  filter: drop-shadow(0 1px 3px rgba(0,0,0,0.95));
}
.lbl-val {
  font-size: 12px; font-weight: 800;
  font-family: var(--font-sans, sans-serif);
  filter: drop-shadow(0 1px 4px rgba(0,0,0,0.95));
}
.lbl-pct {
  font-size: 9px; font-weight: 600;
  fill: rgba(255,255,255,0.55);
  font-family: var(--font-sans, sans-serif);
  filter: drop-shadow(0 1px 3px rgba(0,0,0,0.95));
}

/* ── External labels (leader lines) ──────────────────────────── */
.lbl-ext-name {
  font-size: 9.5px; font-weight: 700;
  fill: rgba(255,255,255,0.82);
  font-family: var(--font-sans, sans-serif);
}
.lbl-ext-val {
  font-size: 8.5px; font-weight: 600;
  fill: rgba(255,255,255,0.5);
  font-family: var(--font-sans, sans-serif);
}

/* ── Floating info card (pure pie mode) ───────────────────────── */
.pie-card {
  position: absolute;
  bottom: 6px; left: 50%; transform: translateX(-50%);
  background: rgba(8, 14, 24, 0.96);
  border: 1px solid rgba(255,255,255,0.1);
  border-radius: 10px;
  display: flex; align-items: stretch;
  overflow: hidden;
  pointer-events: none;
  box-shadow: 0 8px 28px rgba(0,0,0,0.65);
  backdrop-filter: blur(10px);
  white-space: nowrap;
  z-index: 5;
  min-width: 130px;
}
.pie-card-bar { width: 4px; flex-shrink: 0; }
.pie-card-body {
  padding: 6px 12px;
  display: flex; flex-direction: column; gap: 2px;
}
.pie-card-row {
  display: flex; align-items: baseline;
  justify-content: space-between; gap: 10px;
}
.pcard-label { font-size: 11px; color: rgba(255,255,255,0.55); }
.pcard-pct   { font-size: 13px; font-weight: 800; letter-spacing: -0.02em; }
.pcard-val   { font-size: 14px; font-weight: 800; color: rgba(255,255,255,0.92); letter-spacing: -0.02em; }

.pcard-enter-active, .pcard-leave-active { transition: opacity 0.15s, transform 0.15s; }
.pcard-enter-from, .pcard-leave-to { opacity: 0; transform: translateX(-50%) translateY(6px); }

/* ── Legend ───────────────────────────────────────────────────── */
.pie-legend {
  flex: 1; min-width: 0; min-height: 0;
  display: flex; flex-direction: column; gap: 4px;
  max-height: 100%; overflow-y: auto;
  padding: 2px 2px;
  scrollbar-width: thin;
  scrollbar-color: rgba(255,255,255,0.08) transparent;
}

/* Bottom legend: compact horizontal chips */
.legend-bottom .pie-legend {
  flex-direction: row; flex-wrap: wrap;
  justify-content: center; align-content: flex-start;
  gap: 4px 8px; max-height: 72px;
  overflow: hidden;
}
.legend-bottom .leg-row {
  padding: 3px 8px; gap: 5px;
  border-radius: 20px;
  flex-direction: row; align-items: center;
}
.legend-bottom .leg-content { display: none; }
.legend-bottom .leg-ind { width: 8px; height: 8px; border-radius: 50%; }
.legend-bottom .leg-row::after {
  content: attr(data-label);
  font-size: 10px; color: rgba(255,255,255,0.7); font-weight: 600;
  white-space: nowrap;
}

/* Row (right legend) */
.leg-row {
  display: flex; align-items: flex-start; gap: 8px;
  padding: 5px 7px; border-radius: 9px;
  cursor: pointer;
  transition: background 0.15s, opacity 0.15s, border-color 0.15s;
  border: 1px solid transparent;
}
.leg-row.leg-active, .leg-row:hover {
  background: rgba(255,255,255,0.05);
  border-color: rgba(255,255,255,0.09);
}
.leg-row.leg-dim { opacity: 0.22; }

.leg-ind {
  width: 12px; height: 12px; border-radius: 4px;
  flex-shrink: 0; margin-top: 1px;
}

.leg-content { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 3px; }

.leg-row1 {
  display: flex; align-items: baseline;
  justify-content: space-between; gap: 4px;
}
.leg-name {
  font-size: 11px; font-weight: 600;
  color: rgba(255,255,255,0.8);
  white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
}
.leg-pct {
  font-size: 12px; font-weight: 800;
  flex-shrink: 0; letter-spacing: -0.02em;
}

.leg-track {
  height: 3px;
  background: rgba(255,255,255,0.07);
  border-radius: 2px; overflow: hidden;
}
.leg-fill {
  height: 100%; border-radius: 2px;
  transition: width 0.6s cubic-bezier(0.16, 1, 0.3, 1);
  opacity: 0.85;
}

.leg-row2 {
  display: flex; justify-content: space-between; align-items: center;
}
.leg-measure {
  font-size: 9px; color: rgba(255,255,255,0.28);
  text-transform: uppercase; letter-spacing: 0.04em;
  white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
  max-width: 60%;
}
.leg-val {
  font-size: 10px; font-weight: 700;
  color: rgba(255,255,255,0.55);
}
</style>
