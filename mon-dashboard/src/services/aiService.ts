import api from './api'

export interface AiColumn { name: string; type: string }

export interface AiChartRecommendation {
  recommendedChart: string
  reason: string
  alternatives: string[]
  xAxis: string
  yAxis: string
}

export interface AiKpi {
  title: string
  column: string
  aggregation: 'sum' | 'avg' | 'count' | 'min' | 'max'
  format: 'number' | 'percent' | 'currency'
  description: string
}

export interface AiAnalysis {
  summary: string
  insights: string[]
  trends: string[]
  anomalies: string[]
  recommendations: string[]
}

export interface AiWidgetConfig {
  type: string
  title: string
  xAxis: string | null
  yAxis: string | null
  aggregation: string
  filters: string[]
  explanation: string
}

export interface AiGeneratedWidget {
  type:        string
  title:       string
  xAxis:       string | null
  yAxis:       string | null
  aggregation: string
  color:       string
  width:       number
  height:      number
  x:           number
  y:           number
}

export interface AiDashboardPlan {
  widgets: AiGeneratedWidget[]
}

const AI_TIMEOUT = 180_000  // 3 min — Gemini 2.5 peut être lent sur les gros datasets

const aiService = {
  recommendChart(columns: AiColumn[]) {
    return api.post<AiChartRecommendation>('/ai/recommend-chart', { columns }, { timeout: AI_TIMEOUT })
      .then(r => r.data)
  },

  suggestKpis(columns: AiColumn[]) {
    return api.post<{ kpis: AiKpi[] }>('/ai/suggest-kpis', { columns }, { timeout: AI_TIMEOUT })
      .then(r => r.data)
  },

  analyze(columns: AiColumn[], preview?: Record<string, unknown>[], question?: string) {
    return api.post<AiAnalysis>('/ai/analyze', { columns, preview, question }, { timeout: AI_TIMEOUT })
      .then(r => r.data)
  },

  chat(message: string, columns?: AiColumn[]) {
    return api.post<AiWidgetConfig>('/ai/chat', { message, columns }, { timeout: AI_TIMEOUT })
      .then(r => r.data)
  },

  generateDashboard(columns: AiColumn[], preview?: Record<string, unknown>[]) {
    return api.post<AiDashboardPlan>(
      '/ai/generate-dashboard',
      { columns, preview },
      { timeout: AI_TIMEOUT },
    ).then(r => r.data)
  },

  narrative(columns: AiColumn[], preview?: Record<string, unknown>[], focus?: string) {
    const colDesc = columns.map(c => `${c.name} (${c.type})`).join(', ')
    const previewText = preview?.length
      ? `\nAperçu des données (${preview.length} lignes) :\n${JSON.stringify(preview.slice(0, 8), null, 2)}`
      : ''
    const focusText = focus?.trim() ? `\nAngle d'analyse souhaité : ${focus.trim()}` : ''

    const message =
      `Génère une analyse narrative complète et professionnelle de ce dataset.\n\n` +
      `Colonnes : ${colDesc}${previewText}${focusText}\n\n` +
      `Structure attendue :\n` +
      `1. **Présentation du dataset** — nature, domaine métier supposé, nombre de dimensions\n` +
      `2. **Observations clés** — ce que les données révèlent immédiatement\n` +
      `3. **Tendances et patterns** — évolutions, corrélations, saisonnalités détectées\n` +
      `4. **Points d'attention** — valeurs aberrantes, données manquantes, incohérences\n` +
      `5. **Recommandations BI** — types de visualisations adaptées, KPIs pertinents\n\n` +
      `Rédige en français, en prose fluide et professionnelle. Utilise **gras** pour les termes importants.`

    return api.post<{ reply: string }>(
      '/ai/ask',
      { message, dashboardName: 'Analyse de données', context: `Colonnes : ${colDesc}` },
      { timeout: AI_TIMEOUT },
    ).then(r => r.data.reply ?? r.data)
  },
}

export default aiService
