
// ─────────────────────────────────────────────
export interface User {
  id: number
  email: string
  role: 'User' | 'Admin'
  createdAt: string
}

// ─────────────────────────────────────────────
//  Correspond à la table `datasets`
// ─────────────────────────────────────────────
export interface Dataset {
  id: number
  fileName: string
  rowCount: number
  columnsJson: string[]   // ex: ["mois", "ventes", "region"]
  uploadedAt: string
  userId: number
}

// ─────────────────────────────────────────────
//  Correspond à la table `widgets`
// ─────────────────────────────────────────────
export type WidgetType = 'bar' | 'line' | 'pie' | 'doughnut' | 'area' | 'scatter' | 'radar' | 'kpi' | 'table' | 'heatmap' | 'treemap' | 'boxplot' | 'funnel' | 'gauge' | 'text'

// ── Box Plot ──────────────────────────────────────────────────────────────────
export interface BoxPlotPoint {
  label:    string
  min:      number
  q1:       number
  median:   number
  q3:       number
  max:      number
  outliers?: number[]
}

export interface Widget {
  id: number
  type: string              // WidgetType ou string arbitraire
  title: string
  // Backend (camelCase)
  dashboardId?: number
  data?: string | null      // JSON des points [{label,value}[]] sérialisé
  config?: string | null    // JSON de la config (axes, color, width, height…)
  // BuilderView (snake_case + champs étendus)
  dashboard_id?: number
  data_config?: string | null
  width?: number
  height?: number
  x?: number
  y?: number
}

// Widget sur le canvas builder (avec position/taille)
export interface WidgetLayout extends Widget {
  w: number
  h: number
}

// ─────────────────────────────────────────────
//  Correspond à la table `dashboards`
// ─────────────────────────────────────────────
export interface Dashboard {
  id: number
  name: string
  shareToken: string
  createdAt: string
  userId: number
  datasetId: number | null
  columnsJson: string[]       // colonnes sélectionnées du dataset
  insightsJson: string[]      // insights générés par IA
  recommendationsJson: string[] // recommandations générées par IA
  isPublic: boolean
  widgets?: Widget[]
}

// ─────────────────────────────────────────────
//  Correspond à la table `insights`
// ─────────────────────────────────────────────
export interface Insight {
  id: number
  content: string
  generatedAt: string
  datasetId: number
}

// ─────────────────────────────────────────────
//  Payloads pour les appels API
// ─────────────────────────────────────────────
export interface LoginPayload {
  email: string
  password: string
}

export interface LoginResponse {
  token: string
  user: User
}

export interface CreateDashboardPayload {
  name: string
  datasetId: number
  columnsJson: string[]
  isPublic?: boolean
}

export interface UpdateDashboardPayload {
  name?: string
  columnsJson?: string[]
  insightsJson?: string[]
  recommendationsJson?: string[]
  isPublic?: boolean
}

export interface CreateWidgetPayload {
  type: WidgetType
  title: string
  data?: string
  dashboardId: number
}

// ─────────────────────────────────────────────
//  Réponse paginée générique (si besoin)
// ─────────────────────────────────────────────
export interface PaginatedResponse<T> {
  items: T[]
  total: number
  page: number
  pageSize: number}