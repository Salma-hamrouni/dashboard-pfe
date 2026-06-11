import api from './api'

// ── DTOs ────────────────────────────────────────────────────────────────────

export interface WidgetDto {
  id: number
  dashboardId: number
  type: string
  title: string
  data: string | null
  config: string | null
}

export interface CreateWidgetPayload {
  dashboardId: number
  type: string
  title: string
  data?: string | null
  config?: string | null
}

export interface UpdateWidgetPayload {
  type?: string
  title?: string
  data?: string | null
  config?: string | null
}

// ── Service ─────────────────────────────────────────────────────────────────

export const widgetService = {
  /** GET /api/Widget/{dashboardId} */
  async getByDashboard(dashboardId: number): Promise<WidgetDto[]> {
    const { data } = await api.get<WidgetDto[]>(`/Widget/${dashboardId}`)
    return Array.isArray(data) ? data : []
  },

  /** POST /api/Widget */
  async create(payload: CreateWidgetPayload): Promise<WidgetDto> {
    const { data } = await api.post<any>('/Widget', payload)
    // Normalize PascalCase (ASP.NET default) vs camelCase
    return {
      id:          data.id          ?? data.Id          ?? 0,
      dashboardId: data.dashboardId ?? data.DashboardId ?? payload.dashboardId,
      type:        data.type        ?? data.Type        ?? payload.type,
      title:       data.title       ?? data.Title       ?? payload.title,
      data:        data.data        ?? data.Data        ?? null,
      config:      data.config      ?? data.Config      ?? null,
    }
  },

  /** PUT /api/Widget/{id} */
  async update(id: number, payload: UpdateWidgetPayload): Promise<void> {
    // Send both camelCase and PascalCase to be safe with any backend JSON config
    await api.put(`/Widget/${id}`, {
      type:   payload.type,
      title:  payload.title,
      data:   payload.data,
      config: payload.config,
      Type:   payload.type,
      Title:  payload.title,
      Data:   payload.data,
      Config: payload.config,
    })
  },

  /** DELETE /api/Widget/{id} */
  async remove(id: number): Promise<void> {
    await api.delete(`/Widget/${id}`)
  },
}
