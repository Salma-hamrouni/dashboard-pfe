import api from './api'

export interface UserInfo {
  id: number
  email: string
  role: string
  createdAt: string
}

export interface LoginResponse {
  token: string
  user: UserInfo
}

export const authService = {
  async login(email: string, password: string): Promise<LoginResponse> {
    const { data } = await api.post<LoginResponse>('/auth/login', { email, password })
    return data
  },

  async register(email: string, password: string): Promise<void> {
    await api.post('/auth/register', { email, password })
  },
}
