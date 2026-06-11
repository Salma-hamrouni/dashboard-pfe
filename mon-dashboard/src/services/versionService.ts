import api from './api'

export interface DashboardVersionSummary {
  id:          number
  version:     number
  label:       string | null
  createdAt:   string
  widgetCount?: number
}

export interface DashboardVersionDetail extends DashboardVersionSummary {
  snapshot: {
    dashboard: {
      id:                  number
      name:                string
      datasetId:           number
      columnsJson?:        string | null
      insightsJson?:       string | null
      recommendationsJson?: string | null
    }
    widgets: {
      id:          number
      type:        string
      title:       string
      data?:       string | null
      dashboardId: number
    }[]
  }
}

const versionService = {
  /** Sauvegarde un snapshot de la version courante */
  async saveVersion(dashboardId: number, label?: string): Promise<DashboardVersionSummary> {
    const { data } = await api.post(`/dashboard-version/${dashboardId}/save`, { label: label ?? null })
    return data
  },

  /** Liste toutes les versions d'un dashboard (ordre décroissant) */
  async getVersions(dashboardId: number): Promise<DashboardVersionSummary[]> {
    const { data } = await api.get(`/dashboard-version/${dashboardId}`)
    return Array.isArray(data) ? data : (data?.data ?? [])
  },

  /** Récupère le snapshot complet d'une version */
  async getVersion(dashboardId: number, versionNumber: number): Promise<DashboardVersionDetail> {
    const { data } = await api.get(`/dashboard-version/${dashboardId}/${versionNumber}`)
    return data?.data ?? data
  },

  /** Restaure une version (crée un backup de la version actuelle) */
  async restoreVersion(dashboardId: number, versionNumber: number) {
    const { data } = await api.post(`/dashboard-version/${dashboardId}/${versionNumber}/restore`)
    return data?.data ?? data
  },

  /** Supprime une version par son ID */
  async deleteVersion(id: number): Promise<void> {
    await api.delete(`/dashboard-version/${id}`)
  },
}

export default versionService
