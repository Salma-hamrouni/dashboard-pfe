import api from './api'

export interface AdminUser {
  id: number
  email: string
  role: string
  createdAt: string
  dashboardCount: number
  datasetCount: number
}

export interface AdminDashboard {
  id: number
  name: string
  isPublic: boolean
  createdAt: string
  userId: number
  widgetCount: number
  ownerEmail: string
}

export interface AdminWidget {
  id: number
  title: string
  type: string
  dashboardId: number
  dashboardName: string
  ownerEmail: string
}

export interface AdminDataset {
  id: number
  fileName: string
  rowCount: number
  uploadedAt: string
  userId: number
  ownerEmail: string
}

export interface AdminNotification {
  id: number
  action: string
  userId: number
  userEmail: string
  details: string
  createdAt: string
  ipAddress: string
}

const adminService = {
  // Users
  getUsers: ()                           => api.get<any>('/admin/users').then(r => (r.data?.data ?? r.data ?? []) as AdminUser[]),
  createUser: async (email: string, password: string, role: string): Promise<AdminUser> => {
    const r = await api.post<any>('/admin/users', { email, password, role })
    return (r.data?.data ?? r.data) as AdminUser
  },
  deleteUser: (id: number)               => api.delete(`/admin/users/${id}`),
  changeRole:     (id: number, role: string)     => api.put(`/admin/users/${id}/role`,     { role }),
  changePassword: (id: number, password: string) => api.put(`/admin/users/${id}/password`, { password }),

  // Dashboards
  getDashboards: ()                      => api.get<any>('/admin/dashboards').then(r => (r.data?.data ?? r.data ?? []) as AdminDashboard[]),
  deleteDashboard: (id: number)          => api.delete(`/admin/dashboards/${id}`),

  // Widgets
  getWidgets: ()                         => api.get<any>('/admin/widgets').then(r => (r.data?.data ?? r.data ?? []) as AdminWidget[]),
  deleteWidget: (id: number)             => api.delete(`/admin/widgets/${id}`),

  // Datasets
  getDatasets: ()                        => api.get<any>('/admin/datasets').then(r => (r.data?.data ?? r.data ?? []) as AdminDataset[]),
  deleteDataset: (id: number)            => api.delete(`/admin/datasets/${id}`),

  // Notifications (demandes mot de passe oublié)
  getNotifications: ()                   => api.get<any>('/admin/notifications').then(r => (r.data?.data ?? r.data ?? []) as AdminNotification[]),
  dismissNotification: (id: number)      => api.delete(`/admin/notifications/${id}`),

  // Stats
  getStatsOverview:    () => api.get<any>('/stats/overview').then(r => r.data?.data ?? r.data),
  getStatsUsers:       () => api.get<any>('/stats/users').then(r => r.data?.data ?? r.data),
  getStatsDashboards:  () => api.get<any>('/stats/dashboards').then(r => r.data?.data ?? r.data),
  getStatsDataSources: () => api.get<any>('/stats/datasources').then(r => r.data?.data ?? r.data),
  getStatsAudit:       () => api.get<any>('/stats/audit').then(r => r.data?.data ?? r.data),
  getStatsWidgets:     () => api.get<any>('/stats/widgets').then(r => r.data?.data ?? r.data),
}

export default adminService
