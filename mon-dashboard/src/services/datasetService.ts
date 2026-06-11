import api from './api'

// ── Interfaces ──────────────────────────────────────────────────────────────

export interface ColumnProfile {
  name: string
  type: string
  min?: number
  max?: number
  uniqueValues?: string[]
}

/** Réponse de POST /datasource/upload-csv (vrai endpoint qui persiste) */
export interface UploadResponse {
  id: number          // datasourceId en base
  name: string
  type: string
  columns: ColumnProfile[]
  preview: Record<string, string>[]
  totalRows: number
}

/** Item pour la liste des sources de données */
export interface DatasetItem {
  id: number
  name: string
  type: string
  lastRefreshedAt: string
  cachedDataJson?: string
}

export interface PaginatedDatasets {
  items: DatasetItem[]
  total: number
  page: number
  pageSize: number
}

// ── Type normalizer ──────────────────────────────────────────────────────────
// The backend CsvParserService returns "string" for text/categorical columns.
// We map it to "category" so the frontend dropdowns and pie-chart checks work.
function normColType(raw: string): string {
  switch (raw.toLowerCase()) {
    case 'string':   return 'category'   // backend string → frontend category
    case 'integer':
    case 'float':
    case 'decimal':
    case 'double':   return 'number'
    case 'datetime': return 'date'
    default:         return raw.toLowerCase() // 'number','date','boolean','category','text' pass-through
  }
}

// ── Service ─────────────────────────────────────────────────────────────────

export const datasetService = {
  /**
   * Upload un CSV via le vrai endpoint persistant : POST /datasource/upload-csv
   * Retourne l'id de la DataSource créée + colonnes + aperçu.
   */
  async upload(file: File, name?: string): Promise<UploadResponse> {
    const formData = new FormData()
    formData.append('file', file)
    formData.append('name', name || file.name.replace(/\.csv$/i, ''))
    formData.append('delimiter', ',')

    // NOTE: Do NOT set Content-Type manually — axios auto-sets
    // "multipart/form-data; boundary=..." when it detects FormData.
    // Setting it manually would omit the boundary and break parsing.
    const { data } = await api.post<any>('/datasource/upload-csv', formData, {
      timeout: 60_000, // 60 s — large files can take time
    })

    // Normalize PascalCase (ASP.NET default) vs camelCase + map backend types
    const raw = data as any
    const normCols = (raw.columns ?? raw.Columns ?? []).map((c: any) => ({
      name: c.name ?? c.Name ?? '',
      type: normColType(c.type ?? c.Type ?? 'string'),
    }))
    return {
      id:        raw.id        ?? raw.Id        ?? 0,
      name:      raw.name      ?? raw.Name      ?? '',
      type:      raw.type      ?? raw.Type      ?? '',
      columns:   normCols,
      preview:   raw.preview   ?? raw.Preview   ?? [],
      totalRows: raw.totalRows ?? raw.TotalRows ?? 0,
    }
  },

  /**
   * Liste des sources de données de l'utilisateur.
   * GET /datasource
   */
  async getAll(): Promise<DatasetItem[]> {
    const { data } = await api.get<DatasetItem[]>('/datasource')
    return Array.isArray(data) ? data : []
  },

  /**
   * Détecte le type de chaque colonne côté backend.
   * POST /datasets/analyze
   */
  async analyze(rows: Record<string, string>[]): Promise<ColumnProfile[]> {
    const { data } = await api.post<ColumnProfile[]>('/datasets/analyze', rows)
    return data
  },

  /**
   * Recommande des types de graphiques selon les colonnes.
   * POST /datasets/recommend
   */
  async recommend(columns: ColumnProfile[]): Promise<object[]> {
    const { data } = await api.post<object[]>('/datasets/recommend', columns)
    return data
  },

  /**
   * Analyse IA — génère insights + dashboard.
   * POST /datasets/ai-analyze
   */
  async aiAnalyze(rows: object[]): Promise<object> {
    const { data } = await api.post<object>('/datasets/ai-analyze', rows)
    return data
  },

  /**
   * Prévisualise les données d'une datasource existante.
   * POST /datasource/{id}/preview
   */
  async preview(id: number): Promise<{ columns: ColumnProfile[]; rows: Record<string, unknown>[] }> {
    const { data } = await api.post(`/datasource/${id}/preview`)
    return data
  },
}
