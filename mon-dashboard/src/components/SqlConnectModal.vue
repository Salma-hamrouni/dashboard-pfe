<template>
  <div class="sql-overlay" @click.self="$emit('close')">
    <div class="sql-modal">

      <!-- ── Header ───────────────────────────────────────────── -->
      <div class="sql-header">
        <div class="sql-header-icon">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round">
            <ellipse cx="12" cy="5" rx="9" ry="3"/><path d="M3 5v14c0 1.66 4.03 3 9 3s9-1.34 9-3V5"/>
            <path d="M3 12c0 1.66 4.03 3 9 3s9-1.34 9-3"/>
          </svg>
        </div>
        <div>
          <h3 class="sql-header-title">Connexion SQL</h3>
          <p class="sql-header-sub">MySQL / MariaDB</p>
        </div>
        <button class="sql-close" @click="$emit('close')"><i class="pi pi-times"></i></button>
      </div>

      <!-- ── Steps ────────────────────────────────────────────── -->
      <div class="sql-steps">
        <div v-for="(s, i) in steps" :key="i" class="sql-step" :class="{ active: step === i, done: step > i }">
          <div class="step-dot">{{ step > i ? '✓' : i + 1 }}</div>
          <span>{{ s }}</span>
        </div>
        <div class="step-line"></div>
      </div>

      <!-- ── Body ─────────────────────────────────────────────── -->
      <div class="sql-body">

        <!-- Step 0 : Connexion ──────────────────────────────── -->
        <template v-if="step === 0">
          <div class="sql-grid-2">
            <div class="sql-field sql-field--span">
              <label>Nom de la source <span class="req">*</span></label>
              <input v-model="form.name" class="sql-input" placeholder="ex : Base production" maxlength="100" />
            </div>
            <div class="sql-field sql-field--span">
              <label>Hôte / Serveur <span class="req">*</span></label>
              <input v-model="form.server" class="sql-input" placeholder="localhost" />
            </div>
            <div class="sql-field">
              <label>Port</label>
              <input v-model.number="form.port" class="sql-input" type="number" placeholder="3306" min="1" max="65535" />
            </div>
            <div class="sql-field">
              <label>Base de données <span class="req">*</span></label>
              <input v-model="form.database" class="sql-input" placeholder="ma_base" />
            </div>
            <div class="sql-field">
              <label>Utilisateur</label>
              <input v-model="form.username" class="sql-input" placeholder="root" autocomplete="username" />
            </div>
            <div class="sql-field">
              <label>Mot de passe</label>
              <div class="sql-pw-wrap">
                <input
                  v-model="form.password"
                  :type="showPw ? 'text' : 'password'"
                  class="sql-input"
                  placeholder="••••••••"
                  autocomplete="current-password"
                />
                <button class="sql-pw-toggle" @click="showPw = !showPw" type="button" tabindex="-1">
                  <i :class="showPw ? 'pi pi-eye-slash' : 'pi pi-eye'"></i>
                </button>
              </div>
            </div>
          </div>

          <!-- Connection status badge -->
          <div v-if="connStatus !== 'idle'" class="sql-conn-status" :class="connStatus">
            <i :class="connStatus === 'ok' ? 'pi pi-check-circle' : connStatus === 'testing' ? 'pi pi-spin pi-spinner' : 'pi pi-times-circle'"></i>
            <span>{{ connMessage }}</span>
          </div>

          <div class="sql-footer">
            <button class="sql-btn sql-btn--ghost" @click="$emit('close')">Annuler</button>
            <button class="sql-btn sql-btn--test" @click="testConn" :disabled="!canTest || connStatus === 'testing'">
              <i class="pi pi-wifi"></i>
              {{ connStatus === 'testing' ? 'Test…' : 'Tester la connexion' }}
            </button>
            <button class="sql-btn sql-btn--primary" @click="step = 1" :disabled="connStatus !== 'ok'">
              Suivant <i class="pi pi-arrow-right"></i>
            </button>
          </div>
        </template>

        <!-- Step 1 : Requête ────────────────────────────────── -->
        <template v-else-if="step === 1">
          <div class="sql-field">
            <label>Requête SQL <span class="req">*</span> <span class="sql-badge-hint">SELECT uniquement</span></label>
            <div class="sql-editor-wrap">
              <textarea
                v-model="form.query"
                class="sql-editor"
                placeholder="SELECT * FROM ma_table LIMIT 1000"
                rows="8"
                spellcheck="false"
              ></textarea>
              <!-- quick helpers -->
              <div class="sql-quick-btns">
                <button class="sql-quick" @click="appendQuery('SELECT * FROM ')">SELECT *</button>
                <button class="sql-quick" @click="appendQuery('LIMIT 1000')">LIMIT</button>
                <button class="sql-quick" @click="appendQuery('WHERE ')">WHERE</button>
                <button class="sql-quick" @click="appendQuery('ORDER BY ')">ORDER BY</button>
              </div>
            </div>
            <p class="sql-hint"><i class="pi pi-info-circle"></i> Seules les requêtes SELECT sont autorisées. Max 10 000 lignes recommandées.</p>
          </div>

          <!-- Preview table -->
          <template v-if="previewRows.length > 0">
            <div class="sql-preview-header">
              <span class="sql-preview-title">Aperçu — {{ previewRows.length }} lignes sur {{ previewTotal }}</span>
              <span class="sql-preview-cols">{{ previewColumns.length }} colonnes</span>
            </div>
            <div class="sql-table-wrap">
              <table class="sql-table">
                <thead>
                  <tr>
                    <th v-for="col in previewColumns" :key="col.name">
                      <span class="th-type" :class="`th-type--${col.type}`">{{ typeIcon(col.type) }}</span>
                      {{ col.name }}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="(row, ri) in previewRows.slice(0, 10)" :key="ri">
                    <td v-for="col in previewColumns" :key="col.name">{{ formatCell(row[col.name]) }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </template>

          <!-- Error -->
          <div v-if="queryError" class="sql-error"><i class="pi pi-exclamation-triangle"></i> {{ queryError }}</div>

          <div class="sql-footer">
            <button class="sql-btn sql-btn--ghost" @click="step = 0"><i class="pi pi-arrow-left"></i> Retour</button>
            <button class="sql-btn sql-btn--test" @click="runPreview" :disabled="!form.query.trim() || isPreviewing">
              <i class="pi pi-play" :class="{ 'pi-spin pi-spinner': isPreviewing }"></i>
              {{ isPreviewing ? 'Exécution…' : 'Aperçu' }}
            </button>
            <button class="sql-btn sql-btn--primary" @click="doImport" :disabled="previewRows.length === 0 || isImporting">
              <i class="pi pi-database" :class="{ 'pi-spin pi-spinner': isImporting }"></i>
              {{ isImporting ? 'Importation…' : 'Importer' }}
            </button>
          </div>
        </template>

      </div><!-- /sql-body -->
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, computed } from 'vue'
import { sqlService, type SqlColumn, type SqlRow } from '@/services/sqlService'

// ── Events ───────────────────────────────────────────────────────────────────
const emit = defineEmits<{
  (e: 'close'): void
  (e: 'imported', payload: {
    name:    string
    columns: { name: string; type: string; min?: number; max?: number; uniqueValues?: string[] }[]
    rows:    Record<string, string>[]
    sourceId: number
  }): void
}>()

// ── State ─────────────────────────────────────────────────────────────────────
const step = ref(0)
const steps = ['Connexion', 'Requête & Aperçu']
const showPw = ref(false)

const form = reactive({
  name:     '',
  server:   'localhost',
  port:     3306,
  database: '',
  username: 'root',
  password: '',
  query:    '',
})

// Connection test
const connStatus  = ref<'idle' | 'testing' | 'ok' | 'error'>('idle')
const connMessage = ref('')

// Query preview
const previewColumns = ref<SqlColumn[]>([])
const previewRows    = ref<SqlRow[]>([])
const previewTotal   = ref(0)
const queryError     = ref('')
const isPreviewing   = ref(false)
const isImporting    = ref(false)

// ── Computed ──────────────────────────────────────────────────────────────────
const canTest = computed(() =>
  form.server.trim() !== '' && form.database.trim() !== ''
)

// ── Methods ───────────────────────────────────────────────────────────────────

async function testConn() {
  connStatus.value  = 'testing'
  connMessage.value = 'Test en cours…'
  const res = await sqlService.testConnection({
    server: form.server, port: form.port,
    database: form.database, username: form.username, password: form.password,
  })
  if (res.ok) {
    connStatus.value  = 'ok'
    connMessage.value = 'Connexion réussie !'
  } else {
    connStatus.value  = 'error'
    connMessage.value = res.message || 'Connexion échouée'
  }
}

async function runPreview() {
  if (!form.query.trim()) return
  isPreviewing.value = true
  queryError.value   = ''
  previewRows.value  = []
  previewColumns.value = []
  try {
    const res = await sqlService.preview(
      { server: form.server, port: form.port, database: form.database, username: form.username, password: form.password },
      form.query,
    )
    previewColumns.value = normalizeColumns(res.columns, res.rows)
    previewRows.value    = res.rows as SqlRow[]
    previewTotal.value   = res.totalRows
  } catch (err: unknown) {
    queryError.value = err instanceof Error ? err.message : 'Erreur lors de l\'exécution'
  } finally {
    isPreviewing.value = false
  }
}

async function doImport() {
  if (!form.name.trim()) { alert('Veuillez saisir un nom pour la source.'); return }
  isImporting.value = true
  try {
    const res = await sqlService.connect(
      { server: form.server, port: form.port, database: form.database, username: form.username, password: form.password },
      form.query,
      form.name.trim(),
    )

    // Convert SQL rows → Record<string, string>[]
    const rawCols = res.columns ?? previewColumns.value
    const rawRows = (res.rows ?? previewRows.value) as SqlRow[]
    const normCols = normalizeColumns(rawCols, rawRows)
    const strRows  = rawRows.map(row =>
      Object.fromEntries(
        rawCols.map(col => [col.name, row[col.name] == null ? '' : String(row[col.name])])
      )
    )

    emit('imported', {
      name:     form.name.trim(),
      columns:  normCols,
      rows:     strRows,
      sourceId: res.id,
    })
  } catch (err: unknown) {
    alert(err instanceof Error ? err.message : 'Erreur lors de l\'importation')
  } finally {
    isImporting.value = false
  }
}

/** Normalise les colonnes SQL et calcule min/max/uniqueValues depuis les rows */
function normalizeColumns(
  cols: SqlColumn[],
  rows: SqlRow[],
): { name: string; type: string; min?: number; max?: number; uniqueValues?: string[] }[] {
  return cols.map(col => {
    // map backend "string" → frontend "category"
    const frontType = col.type === 'string' || col.type === 'category' ? 'category' : col.type

    const allVals = rows.map(r => r[col.name])

    if (frontType === 'number') {
      const nums = allVals
        .map(v => parseFloat(String(v).replace(/[^\d.-]/g, '')))
        .filter(v => !isNaN(v))
      return {
        name: col.name,
        type: 'number',
        min:  nums.length ? Math.min(...nums) : 0,
        max:  nums.length ? Math.max(...nums) : 0,
      }
    }

    if (frontType === 'category' || frontType === 'boolean') {
      const unique = [...new Set(allVals.map(v => String(v ?? '')).filter(v => v !== ''))]
      return {
        name: col.name,
        type: unique.length <= 2 && frontType === 'boolean' ? 'boolean' : unique.length < 30 ? 'category' : 'text',
        uniqueValues: unique.slice(0, 50),
      }
    }

    return { name: col.name, type: frontType }
  })
}

function appendQuery(snippet: string) {
  form.query = (form.query.trimEnd() + ' ' + snippet).trimStart()
}

function typeIcon(type: string): string {
  if (type === 'number')  return 'Σ'
  if (type === 'date')    return '📅'
  if (type === 'boolean') return '◉'
  return '🔤'
}

function formatCell(val: unknown): string {
  if (val == null)              return '—'
  if (val instanceof Date)      return val.toLocaleDateString()
  if (typeof val === 'boolean') return val ? 'Vrai' : 'Faux'
  const s = String(val)
  return s.length > 40 ? s.slice(0, 40) + '…' : s
}
</script>

<style scoped>
/* ── Overlay ──────────────────────────────────────────────────── */
.sql-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0,0,0,0.55);
  backdrop-filter: blur(4px);
  z-index: 1000;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 16px;
}

/* ── Modal container ──────────────────────────────────────────── */
.sql-modal {
  background: var(--color-surface, #0a1f1a);
  border: 1px solid var(--color-border, rgba(255,255,255,0.1));
  border-radius: 16px;
  width: 100%;
  max-width: 640px;
  max-height: 90vh;
  display: flex;
  flex-direction: column;
  box-shadow: 0 24px 64px rgba(0,0,0,0.5);
  overflow: hidden;
}

/* ── Header ───────────────────────────────────────────────────── */
.sql-header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 20px 24px 16px;
  border-bottom: 1px solid var(--color-border, rgba(255,255,255,0.08));
}

.sql-header-icon {
  width: 38px; height: 38px;
  border-radius: 10px;
  background: rgba(var(--primary-color-rgb, 16,185,129), 0.15);
  color: var(--primary-color, #10b981);
  display: flex; align-items: center; justify-content: center;
  flex-shrink: 0;
}

.sql-header-title {
  margin: 0;
  font-size: 15px;
  font-weight: 600;
  color: var(--color-text, #f0fdf9);
}

.sql-header-sub {
  margin: 2px 0 0;
  font-size: 11px;
  color: var(--color-text-muted, rgba(240,253,249,0.4));
}

.sql-close {
  margin-left: auto;
  background: none; border: none; cursor: pointer;
  color: var(--color-text-muted, rgba(240,253,249,0.4));
  font-size: 14px; padding: 4px;
  border-radius: 6px;
  transition: color .15s, background .15s;
}
.sql-close:hover { color: var(--color-text, #f0fdf9); background: rgba(255,255,255,0.08); }

/* ── Steps ────────────────────────────────────────────────────── */
.sql-steps {
  display: flex;
  align-items: center;
  gap: 0;
  padding: 14px 24px;
  position: relative;
}
.step-line {
  position: absolute;
  top: 50%; left: 44px; right: 44px;
  height: 1px;
  background: var(--color-border, rgba(255,255,255,0.08));
  z-index: 0;
}
.sql-step {
  display: flex;
  align-items: center;
  gap: 8px;
  flex: 1;
  position: relative;
  z-index: 1;
}
.sql-step:last-child { justify-content: flex-end; }
.step-dot {
  width: 24px; height: 24px;
  border-radius: 50%;
  border: 2px solid var(--color-border, rgba(255,255,255,0.15));
  background: var(--color-surface, #0a1f1a);
  display: flex; align-items: center; justify-content: center;
  font-size: 10px; font-weight: 700;
  color: var(--color-text-muted, rgba(240,253,249,0.4));
  transition: all .2s;
  flex-shrink: 0;
}
.sql-step.active .step-dot {
  border-color: var(--primary-color, #10b981);
  color: var(--primary-color, #10b981);
  box-shadow: 0 0 0 3px rgba(var(--primary-color-rgb,16,185,129),.2);
}
.sql-step.done .step-dot {
  border-color: var(--primary-color, #10b981);
  background: var(--primary-color, #10b981);
  color: #000;
}
.sql-step span {
  font-size: 11px;
  color: var(--color-text-muted, rgba(240,253,249,0.4));
}
.sql-step.active span, .sql-step.done span {
  color: var(--color-text, #f0fdf9);
  font-weight: 500;
}

/* ── Body ─────────────────────────────────────────────────────── */
.sql-body {
  flex: 1;
  overflow-y: auto;
  padding: 0 24px 8px;
}

/* ── Form grid ────────────────────────────────────────────────── */
.sql-grid-2 {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 14px;
}
.sql-field { display: flex; flex-direction: column; gap: 5px; }
.sql-field--span { grid-column: 1 / -1; }

label {
  font-size: 11px;
  font-weight: 600;
  color: var(--color-text-secondary, rgba(240,253,249,0.6));
  letter-spacing: .03em;
  text-transform: uppercase;
}
.req { color: var(--primary-color, #10b981); }

.sql-input {
  background: rgba(255,255,255,0.05);
  border: 1px solid var(--color-border, rgba(255,255,255,0.1));
  border-radius: 8px;
  padding: 8px 12px;
  color: var(--color-text, #f0fdf9);
  font-size: 13px;
  outline: none;
  transition: border-color .15s, box-shadow .15s;
  width: 100%;
  box-sizing: border-box;
}
.sql-input:focus {
  border-color: var(--primary-color, #10b981);
  box-shadow: 0 0 0 3px rgba(var(--primary-color-rgb,16,185,129),.15);
}

.sql-pw-wrap { position: relative; }
.sql-pw-wrap .sql-input { padding-right: 38px; }
.sql-pw-toggle {
  position: absolute; right: 10px; top: 50%; transform: translateY(-50%);
  background: none; border: none; cursor: pointer;
  color: var(--color-text-muted, rgba(240,253,249,0.4));
  font-size: 13px;
}
.sql-pw-toggle:hover { color: var(--color-text, #f0fdf9); }

/* ── Connection status ────────────────────────────────────────── */
.sql-conn-status {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 14px;
  border-radius: 8px;
  font-size: 12px;
  font-weight: 500;
  margin-top: 14px;
}
.sql-conn-status.ok      { background: rgba(16,185,129,.12); color: #10b981; border: 1px solid rgba(16,185,129,.3); }
.sql-conn-status.error   { background: rgba(239,68,68,.1);   color: #ef4444; border: 1px solid rgba(239,68,68,.25); }
.sql-conn-status.testing { background: rgba(245,158,11,.1);  color: #f59e0b; border: 1px solid rgba(245,158,11,.25); }

/* ── SQL editor ───────────────────────────────────────────────── */
.sql-editor-wrap { display: flex; flex-direction: column; gap: 4px; }
.sql-editor {
  background: rgba(0,0,0,0.3);
  border: 1px solid var(--color-border, rgba(255,255,255,0.1));
  border-radius: 8px;
  padding: 12px;
  color: #a5f3d0;
  font-size: 12px;
  font-family: 'Fira Code', 'Cascadia Code', 'Consolas', monospace;
  line-height: 1.6;
  resize: vertical;
  min-height: 120px;
  outline: none;
  transition: border-color .15s;
  width: 100%;
  box-sizing: border-box;
}
.sql-editor:focus { border-color: var(--primary-color, #10b981); }

.sql-quick-btns {
  display: flex; gap: 6px; flex-wrap: wrap;
}
.sql-quick {
  background: rgba(255,255,255,0.05);
  border: 1px solid rgba(255,255,255,0.1);
  border-radius: 5px;
  padding: 3px 9px;
  font-size: 10px;
  font-family: monospace;
  color: var(--color-text-muted, rgba(240,253,249,0.5));
  cursor: pointer;
  transition: background .12s, color .12s;
}
.sql-quick:hover { background: rgba(255,255,255,0.1); color: var(--color-text, #f0fdf9); }

.sql-hint {
  font-size: 10px;
  color: var(--color-text-muted, rgba(240,253,249,0.4));
  margin: 2px 0 0;
  display: flex; align-items: center; gap: 4px;
}
.sql-badge-hint {
  background: rgba(245,158,11,0.12);
  color: #f59e0b;
  border-radius: 4px;
  padding: 1px 6px;
  font-size: 9px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: .04em;
}

/* ── Preview table ────────────────────────────────────────────── */
.sql-preview-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin: 16px 0 8px;
}
.sql-preview-title {
  font-size: 11px;
  font-weight: 600;
  color: var(--color-text-secondary, rgba(240,253,249,0.6));
}
.sql-preview-cols {
  font-size: 10px;
  background: rgba(var(--primary-color-rgb,16,185,129),.1);
  color: var(--primary-color, #10b981);
  border-radius: 10px;
  padding: 2px 8px;
}

.sql-table-wrap {
  overflow-x: auto;
  border: 1px solid var(--color-border, rgba(255,255,255,0.08));
  border-radius: 8px;
  max-height: 200px;
  overflow-y: auto;
}
.sql-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 11px;
}
.sql-table th {
  background: rgba(255,255,255,0.04);
  padding: 7px 10px;
  text-align: left;
  color: var(--color-text-secondary, rgba(240,253,249,0.6));
  font-weight: 600;
  white-space: nowrap;
  position: sticky; top: 0;
  border-bottom: 1px solid var(--color-border, rgba(255,255,255,0.08));
}
.sql-table td {
  padding: 5px 10px;
  color: var(--color-text, #f0fdf9);
  border-bottom: 1px solid rgba(255,255,255,0.04);
  max-width: 180px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.sql-table tr:last-child td { border-bottom: none; }
.sql-table tr:hover td { background: rgba(255,255,255,0.03); }

.th-type { margin-right: 4px; font-size: 9px; }
.th-type--number  { color: #10b981; }
.th-type--date    { color: #3b82f6; }
.th-type--boolean { color: #8b5cf6; }

/* ── Error ────────────────────────────────────────────────────── */
.sql-error {
  display: flex; align-items: flex-start; gap: 8px;
  background: rgba(239,68,68,.1);
  border: 1px solid rgba(239,68,68,.25);
  border-radius: 8px;
  padding: 10px 14px;
  color: #ef4444;
  font-size: 12px;
  margin-top: 14px;
  line-height: 1.4;
}

/* ── Footer ───────────────────────────────────────────────────── */
.sql-footer {
  display: flex;
  gap: 8px;
  justify-content: flex-end;
  padding: 16px 0 12px;
  border-top: 1px solid var(--color-border, rgba(255,255,255,0.07));
  margin-top: 16px;
}
.sql-btn {
  display: flex; align-items: center; gap: 6px;
  padding: 8px 16px;
  border-radius: 8px;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  border: none;
  transition: all .15s;
}
.sql-btn:disabled { opacity: .4; cursor: not-allowed; }

.sql-btn--ghost {
  background: rgba(255,255,255,0.06);
  color: var(--color-text-muted, rgba(240,253,249,0.5));
}
.sql-btn--ghost:hover:not(:disabled) { background: rgba(255,255,255,0.1); color: var(--color-text, #f0fdf9); }

.sql-btn--test {
  background: rgba(245,158,11,0.12);
  border: 1px solid rgba(245,158,11,0.3);
  color: #f59e0b;
}
.sql-btn--test:hover:not(:disabled) { background: rgba(245,158,11,0.2); }

.sql-btn--primary {
  background: var(--primary-color, #10b981);
  color: #000;
  font-weight: 700;
}
.sql-btn--primary:hover:not(:disabled) { filter: brightness(1.1); }
</style>
