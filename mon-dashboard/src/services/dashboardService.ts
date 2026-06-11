import api from './api'
import type { Widget } from '@/types/index'

export interface DashboardDetailDto {
  id: number
  name: string
  datasetId: number
  isPublic: boolean
  shareToken: string | null
  createdAt: string
  columns: string[]
  insights: string[]
  recommendations: string[]
  widgets: Widget[]
  /** Présent dans la réponse liste (DashboardResponseDto.widgetCount) */
  widgetCount?: number
  /** Propriétaire — renseigné sur les dashboards publics */
  ownerName?: string | null
  ownerEmail?: string | null
}

/** Normalise un objet brut (camelCase ou PascalCase) → DashboardDetailDto */
function normalizeDto(d: any): DashboardDetailDto {
  return {
    id:              d.id              ?? d.Id              ?? 0,
    name:            d.name            ?? d.Name            ?? '',
    datasetId:       d.datasetId       ?? d.DatasetId       ?? 0,
    isPublic:        d.isPublic        ?? d.IsPublic        ?? false,
    shareToken:      d.shareToken      ?? d.ShareToken      ?? null,
    createdAt:       d.createdAt       ?? d.CreatedAt       ?? '',
    columns:         d.columns         ?? d.Columns         ?? [],
    insights:        d.insights        ?? d.Insights        ?? [],
    recommendations: d.recommendations ?? d.Recommendations ?? [],
    widgets:         d.widgets         ?? d.Widgets         ?? [],
    widgetCount:     d.widgetCount     ?? d.WidgetCount     ?? (d.widgets ?? d.Widgets ?? []).length,
    ownerName:       d.ownerName       ?? d.OwnerName       ?? null,
    ownerEmail:      d.ownerEmail      ?? d.OwnerEmail      ?? null,
  }
}

export const dashboardService = {
  async getAll(params?: { page?: number; pageSize?: number; search?: string }): Promise<DashboardDetailDto[]> {
    const { data } = await api.get('/Dashboard', { params: { pageSize: 100, ...params } })
    // Le backend retourne ApiResponse<PagedResponse<DashboardResponseDto>>
    // Après unwrap de l'intercepteur : { items/Items: [...], totalCount, ... }
    if (Array.isArray(data)) return (data as any[]).map(normalizeDto)
    if (data && typeof data === 'object') {
      const raw   = data as any
      const items = raw.items ?? raw.Items
      if (Array.isArray(items)) return items.map(normalizeDto)
    }
    return []
  },

  async getPublic(params?: { page?: number; pageSize?: number; search?: string }): Promise<DashboardDetailDto[]> {
    const { data } = await api.get('/Dashboard/public', { params: { pageSize: 100, ...params } })
    if (Array.isArray(data)) return (data as any[]).map(normalizeDto)
    if (data && typeof data === 'object') {
      const raw   = data as any
      const items = raw.items ?? raw.Items
      if (Array.isArray(items)) return items.map(normalizeDto)
    }
    return []
  },

  async create(payload: Partial<DashboardDetailDto>): Promise<DashboardDetailDto> {
    const { data } = await api.post<any>('/Dashboard', payload)
    return normalizeDto(data)
  },

  async getById(id: number): Promise<DashboardDetailDto> {
    const { data } = await api.get<any>(`/Dashboard/${id}`)
    return normalizeDto(data)
  },

  async getShared(token: string): Promise<DashboardDetailDto> {
    const { data } = await api.get<any>(`/Dashboard/share/${token}`)
    return normalizeDto(data)
  },

  async toggleShare(id: number, isPublic: boolean): Promise<{ shareToken: string; isPublic: boolean }> {
    const { data } = await api.put(`/Dashboard/share/${id}`, null, { params: { isPublic } })
    // Normalize PascalCase (ASP.NET) vs camelCase so shareToken is always accessible
    const raw = data as any
    return {
      shareToken: raw.shareToken ?? raw.ShareToken ?? '',
      isPublic:   raw.isPublic   ?? raw.IsPublic   ?? false,
    }
  },
}
