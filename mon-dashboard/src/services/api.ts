import axios from 'axios'
import router from '@/router'

// ─────────────────────────────────────────────
//  Instance axios centrale
//  Toutes les requêtes passent par ici
// ─────────────────────────────────────────────
const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000/api',
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
})

// ─────────────────────────────────────────────
//  Intercepteur REQUEST
//  Injecte le token JWT dans chaque requête
// ─────────────────────────────────────────────
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => Promise.reject(error)
)

// ─────────────────────────────────────────────
//  Intercepteur RESPONSE
//  - Déballe le wrapper ApiResponse { success, data, error }
//  - Redirige vers /login si token expiré (401)
// ─────────────────────────────────────────────
api.interceptors.response.use(
  (response) => {
    const d = response.data
    // Déballe automatiquement ApiResponse<T> → response.data = T
    // Le backend ASP.NET retourne { Success, Data, Error } (PascalCase)
    // ou { success, data, error } (camelCase selon la config JSON).
    // On supporte les deux.
    if (d && typeof d === 'object') {
      const isApiResponse =
        'success' in d || 'Success' in d   // camelCase ou PascalCase
      if (isApiResponse) {
        const ok    = d.success ?? d.Success
        const data  = d.data    ?? d.Data
        const error = d.error   ?? d.Error
        if (ok === false) {
          return Promise.reject(new Error(error || 'Erreur serveur'))
        }
        response.data = data
      }
    }
    return response
  },
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('token')
      router.push('/login')
    }
    // Extrait le message d'erreur du format ApiResponse si disponible
    const d = error.response?.data
    const message =
      d?.error   || d?.Error   ||
      d?.message || d?.Message ||
      d?.title   || d?.Title   ||
      error.message            ||
      'Erreur réseau'
    return Promise.reject(new Error(message))
  }
)

export default api