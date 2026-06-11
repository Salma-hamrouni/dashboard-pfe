import api from './api'

// ── Types ────────────────────────────────────────────────────────────────────

export interface RestConnParams {
  name:         string
  description?: string
  endpoint:     string
  method:       'GET' | 'POST' | 'PUT' | 'PATCH'
  headers?:     Record<string, string>
  body?:        string
  dataPath?:    string
}

export interface RestConnectResult {
  id:        number
  name:      string
  type:      string
  columns:   SqlColumn[]
  rows:      SqlRow[]
  totalRows: number
}

export interface SqlConnParams {
  server:   string
  port:     number
  database: string
  username: string
  password: string
}

export interface SqlColumn {
  name:  string
  type:  string   // "number" | "date" | "boolean" | "string"
  index: number
}

export type SqlRow = Record<string, unknown>

export interface SqlPreviewResult {
  columns:   SqlColumn[]
  rows:      SqlRow[]
  totalRows: number
}

export interface SqlConnectResult extends SqlPreviewResult {
  id:   number
  name: string
  type: string
}

// ── Service ──────────────────────────────────────────────────────────────────

export const sqlService = {

  /**
   * Teste la connexion sans exécuter de requête.
   * POST /api/datasource/test-sql
   */
  async testConnection(p: SqlConnParams): Promise<{ ok: boolean; message?: string }> {
    try {
      await api.post('/datasource/test-sql', {
        server:   p.server,
        database: p.database,
        username: p.username,
        password: p.password,
        port:     p.port,
      })
      return { ok: true }
    } catch (err: unknown) {
      return { ok: false, message: err instanceof Error ? err.message : 'Connexion échouée' }
    }
  },

  /**
   * Exécute un SELECT et retourne jusqu'à 200 lignes, sans persistance.
   * POST /api/datasource/preview-sql
   */
  async preview(p: SqlConnParams, query: string): Promise<SqlPreviewResult> {
    const { data } = await api.post<SqlPreviewResult>('/datasource/preview-sql', {
      server:   p.server,
      database: p.database,
      username: p.username,
      password: p.password,
      port:     p.port,
      query,
    })
    return data as SqlPreviewResult
  },

  /**
   * Exécute, sauvegarde comme DataSource et retourne toutes les lignes.
   * POST /api/datasource/connect-sql
   */
  async connect(
    p: SqlConnParams,
    query: string,
    name: string,
    description?: string,
  ): Promise<SqlConnectResult> {
    const { data } = await api.post<SqlConnectResult>('/datasource/connect-sql', {
      server:      p.server,
      database:    p.database,
      username:    p.username,
      password:    p.password,
      port:        p.port,
      query,
      name,
      description: description ?? '',
    })
    return data as SqlConnectResult
  },
}

// ── REST Connector Service ────────────────────────────────────────────────────

export const restService = {

  /**
   * Appelle l'URL REST, sauvegarde comme DataSource et retourne les données.
   * POST /api/datasource/connect-rest
   */
  async connect(p: RestConnParams): Promise<RestConnectResult> {
    const { data } = await api.post<any>('/datasource/connect-rest', {
      name:        p.name,
      description: p.description ?? '',
      endpoint:    p.endpoint,
      method:      p.method,
      headers:     p.headers ?? {},
      body:        p.body ?? '',
      dataPath:    p.dataPath ?? '',
    })
    // Le backend renvoie "preview" ; on le normalise en "rows" pour le frontend
    return {
      id:        data.id,
      name:      data.name,
      type:      data.type,
      columns:   data.columns  ?? [],
      rows:      data.rows     ?? data.preview ?? [],
      totalRows: data.totalRows ?? 0,
    } as RestConnectResult
  },
}
