import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authService } from '@/services/authService'

export interface User {
  id: number
  email: string
  role: string
  name?: string
}

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)
  const token = ref<string | null>(localStorage.getItem('token'))
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  const isAuthenticated = computed(() => !!token.value)

  async function login(email: string, password: string) {
    isLoading.value = true
    error.value = null

    try {
      const response = await authService.login(email, password)
      setAuth(
        { id: response.user.id, email: response.user.email, role: response.user.role, name: response.user.name },
        response.token
      )
      return true
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Identifiants invalides'
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function register(email: string, password: string) {
    isLoading.value = true
    error.value = null

    try {
      await authService.register(email, password)
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Erreur lors de l\'inscription'
      throw err
    } finally {
      isLoading.value = false
    }
  }

  function setAuth(userData: User, userToken: string) {
    user.value = userData
    token.value = userToken
    localStorage.setItem('token', userToken)
    localStorage.setItem('user', JSON.stringify(userData))
  }

  function logout() {
    user.value = null
    token.value = null
    error.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('user')
  }

  // Restore state on load
  const savedUser = localStorage.getItem('user')
  if (savedUser) {
    try {
      user.value = JSON.parse(savedUser) as User
    } catch {
      logout()
    }
  }

  function updateName(name: string) {
    if (user.value) {
      user.value = { ...user.value, name }
      localStorage.setItem('user', JSON.stringify(user.value))
    }
  }

  return {
    user,
    token,
    isLoading,
    error,
    isAuthenticated,
    login,
    register,
    logout,
    updateName,
  }
})
