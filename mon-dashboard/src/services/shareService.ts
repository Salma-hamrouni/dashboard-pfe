import api from './api'

export type SharePermission = 0 | 1   // 0 = View, 1 = Edit

export interface ShareRecord {
  id:               number
  token:            string
  permission:       SharePermission
  sharedWithUserId: number | null
  createdAt:        string
  expiresAt:        string | null
}

export interface CreateShareResponse {
  id:         number
  token:      string
  permission: SharePermission
  expiresAt:  string | null
}

const shareService = {
  async create(payload: {
    dashboardId:       number
    permission:        SharePermission
    expiresAt?:        string | null
    sharedWithUserId?: number | null
  }): Promise<CreateShareResponse> {
    const { data } = await api.post<any>('/dashboard-share', payload)
    return {
      id:         data.id         ?? data.Id         ?? 0,
      token:      data.token      ?? data.Token      ?? '',
      permission: data.permission ?? data.Permission ?? 0,
      expiresAt:  data.expiresAt  ?? data.ExpiresAt  ?? null,
    }
  },

  async listByDashboard(dashboardId: number): Promise<ShareRecord[]> {
    const { data } = await api.get<any>(`/dashboard-share/${dashboardId}`)
    if (!Array.isArray(data)) return []
    return data.map((s: any) => ({
      id:               s.id               ?? s.Id               ?? 0,
      token:            s.token            ?? s.Token            ?? '',
      permission:       s.permission       ?? s.Permission       ?? 0,
      sharedWithUserId: s.sharedWithUserId ?? s.SharedWithUserId ?? null,
      createdAt:        s.createdAt        ?? s.CreatedAt        ?? '',
      expiresAt:        s.expiresAt        ?? s.ExpiresAt        ?? null,
    }))
  },

  async revoke(id: number): Promise<void> {
    await api.delete(`/dashboard-share/${id}`)
  },

  isExpired(record: ShareRecord): boolean {
    if (!record.expiresAt) return false
    return new Date(record.expiresAt) < new Date()
  },

  formatExpiry(expiresAt: string | null): string {
    if (!expiresAt) return 'Pas d\'expiration'
    const d = new Date(expiresAt)
    const now = new Date()
    const diff = d.getTime() - now.getTime()
    if (diff < 0) return 'Expiré'
    const days  = Math.floor(diff / 86_400_000)
    const hours = Math.floor((diff % 86_400_000) / 3_600_000)
    if (days > 0) return `Expire dans ${days} jour${days > 1 ? 's' : ''}`
    if (hours > 0) return `Expire dans ${hours} heure${hours > 1 ? 's' : ''}`
    return 'Expire bientôt'
  },
}

export default shareService
