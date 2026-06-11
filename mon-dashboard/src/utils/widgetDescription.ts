/**
 * Generates a human-readable description for a widget based on its type and
 * stored config JSON.  Used by both the builder caption strip and the viewer
 * widget cards so descriptions are consistent everywhere.
 *
 * Priority:
 *  1. Manual description set by the user (`cfg.description`)
 *  2. Auto-generated from axes + aggregation (falls back to empty string)
 */

const AGG_SYMBOLS: Record<string, string> = {
  sum:            'Σ',
  avg:            'Ø',
  count:          '#',
  count_distinct: '#',
  min:            '↓',
  max:            '↑',
  median:         'Med',
  std:            'σ',
  none:           '—',
}

export function generateWidgetDescription(
  type:       string,
  configJson: string | null | undefined,
): string {
  if (!configJson) return ''
  let cfg: Record<string, any> = {}
  try { cfg = JSON.parse(configJson) } catch { return '' }

  // Manual description always wins
  if (cfg.description?.trim()) return cfg.description.trim()

  // Text widgets ARE their content — no caption needed
  if (type === 'text') return ''

  const y   = (cfg.yAxis  || '').trim()
  const x   = (cfg.xAxis  || '').trim()
  const agg = cfg.aggregation || 'sum'
  const suf = (cfg.valueSuffix || cfg.kpiSuffix || '').trim()
  const sym = AGG_SYMBOLS[agg] ?? 'Σ'

  if (!y && !x) return ''

  // ── Widget-type-specific descriptions ──────────────────────────────────────

  if (type === 'boxplot') {
    if (y && x) return `Distribution de ${y} · par ${x}`
    if (y)      return `Distribution de ${y}`
    return ''
  }

  if (type === 'scatter') {
    if (x && y) return `Corrélation ${x} vs ${y}`
    return ''
  }

  if (type === 'pie' || type === 'doughnut') {
    const measure = y ? ` (${sym} ${y})` : ''
    return x ? `Répartition par ${x}${measure}` : ''
  }

  if (type === 'funnel') {
    if (x && y) return `Entonnoir — ${sym} ${y} · par ${x}`
    if (y)      return `Entonnoir — ${sym} ${y}`
    return ''
  }

  if (type === 'radar') {
    if (y && x) return `Radar ${y} · par ${x}`
    return ''
  }

  // ── Generic bar / line / area / table / kpi / gauge ────────────────────────
  const useCount = !y && x
  const valLabel = useCount
    ? '# Éléments'
    : y ? `${sym} ${y}${suf ? ` (${suf})` : ''}`
        : ''

  if (type === 'kpi' || type === 'gauge') return valLabel
  if (valLabel && x) return `${valLabel} · par ${x}`
  if (valLabel)      return valLabel
  if (x)             return `Par ${x}`
  return ''
}
