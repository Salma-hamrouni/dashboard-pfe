import { defineStore } from 'pinia'
import { ref } from 'vue'
import { dashboardService, type DashboardDetailDto } from '@/services/dashboardService'
import api from '@/services/api'
import type { Widget } from '@/types/index'

export type { DashboardDetailDto }
export type { Widget }

export const useDashboardStore = defineStore('dashboard', () => {
  const dashboards       = ref<DashboardDetailDto[]>([])
  const currentDashboard = ref<DashboardDetailDto | null>(null)
  const currentWidgets   = ref<Widget[]>([])
  const isLoading        = ref(false)
  const error            = ref<string | null>(null)

  // Timestamp du dernier fetch réussi — évite de refaire la requête si < 30 s
  let _lastFetchAt = 0
  const CACHE_TTL_MS = 30_000  // 30 secondes

  // ── Fetch all dashboards ──────────────────────────────────────────────────
  async function fetchDashboards(force = false) {
    // Retourne le cache si les données sont fraîches et non vides
    if (!force && dashboards.value.length > 0 && Date.now() - _lastFetchAt < CACHE_TTL_MS) {
      return
    }
    isLoading.value = true
    error.value = null
    try {
      const result = await dashboardService.getAll()
      dashboards.value = Array.isArray(result) ? result : []
      _lastFetchAt = Date.now()
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Erreur lors du chargement'
      dashboards.value = []
    } finally {
      isLoading.value = false
    }
  }

  // ── Fetch widgets for a dashboard ────────────────────────────────────────
  async function fetchWidgets(dashboardId: number) {
    try {
      const { data } = await api.get<any[]>(`/Widget/${dashboardId}`)
      // Map backend format → BuilderView format
      // Handles both camelCase (api interceptor unwrapped) and PascalCase (fallback)
      const list = Array.isArray(data) ? data : []
      currentWidgets.value = list.map((w: any): Widget => {
        const id          = w.id          ?? w.Id          ?? 0
        const dashId      = w.dashboardId ?? w.DashboardId ?? dashboardId
        const type        = w.type        ?? w.Type        ?? 'bar'
        const title       = w.title       ?? w.Title       ?? 'Widget'
        const rawData     = w.data        ?? w.Data        ?? null
        const rawConfig   = w.config      ?? w.Config      ?? null
        let cfg: Record<string, unknown> = {}
        try { cfg = typeof rawConfig === 'string' ? JSON.parse(rawConfig) : (rawConfig ?? {}) } catch {}
        // Ensure data_config is always a JSON string (backend may return a parsed object)
        const dataConfigStr =
          typeof rawConfig === 'string'
            ? rawConfig
            : rawConfig != null ? JSON.stringify(rawConfig) : '{}'

        return {
          id,
          dashboardId: dashId,
          dashboard_id: dashId,
          type,
          title,
          data: rawData,
          data_config: dataConfigStr,
          width:  (cfg.width  as number) ?? 6,
          height: (cfg.height as number) ?? 4,
          x:      (cfg.x      as number) ?? 0,
          y:      (cfg.y      as number) ?? 0,
        }
      })
    } catch {
      currentWidgets.value = []
    }
  }

  // ── Create dashboard — accepte un nom (string) ou un objet complet ────────
  async function createDashboard(
    nameOrPayload: string | Partial<DashboardDetailDto>,
  ): Promise<DashboardDetailDto> {
    const payload =
      typeof nameOrPayload === 'string'
        ? { name: nameOrPayload }
        : nameOrPayload

    const created = await dashboardService.create(payload)
    if (Array.isArray(dashboards.value)) {
      dashboards.value.push(created)
    } else {
      dashboards.value = [created]
    }
    _lastFetchAt = Date.now() // reset cache timestamp — list is now up-to-date
    return created
  }

  // ── Delete dashboard ──────────────────────────────────────────────────────
  async function deleteDashboard(id: number) {
    try {
      await api.delete(`/Dashboard/${id}`)
    } catch {
      // ignore — retire de la liste localement de toute façon
    }
    dashboards.value = dashboards.value.filter((d) => d.id !== id)
    _lastFetchAt = Date.now() // list is up-to-date after local removal
    if (currentDashboard.value?.id === id) {
      currentDashboard.value = null
      currentWidgets.value   = []
    }
  }

  // ── Toggle share ──────────────────────────────────────────────────────────
  async function toggleShare(id: number, isPublic: boolean) {
    return dashboardService.toggleShare(id, isPublic)
  }

  // ── Set current dashboard ─────────────────────────────────────────────────
  function setCurrentDashboard(dashboard: DashboardDetailDto) {
    currentDashboard.value = dashboard
  }

  return {
    dashboards,
    currentDashboard,
    currentWidgets,
    isLoading,
    error,
    fetchDashboards,
    fetchWidgets,
    createDashboard,
    deleteDashboard,
    toggleShare,
    setCurrentDashboard,
  }
})
