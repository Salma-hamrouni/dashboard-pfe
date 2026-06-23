<template>
  <div class="rest-overlay" @click.self="$emit('close')">
    <div class="rest-modal">

      <!-- ── Header ───────────────────────────────────────────── -->
      <div class="rest-header">
        <div class="rest-header-icon">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/>
            <path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"/>
          </svg>
        </div>
        <div>
          <h3 class="rest-header-title">Connexion API REST</h3>
          <p class="rest-header-sub">Importer des données depuis une URL HTTP/HTTPS</p>
        </div>
        <button class="rest-close" @click="$emit('close')"><i class="pi pi-times"></i></button>
      </div>

      <!-- ── Steps ────────────────────────────────────────────── -->
      <div class="rest-steps">
        <div v-for="(s, i) in steps" :key="i" class="rest-step" :class="{ active: step === i, done: step > i }">
          <div class="step-dot">{{ step > i ? '✓' : i + 1 }}</div>
          <span>{{ s }}</span>
        </div>
        <div class="step-line"></div>
      </div>

      <!-- ── Body ─────────────────────────────────────────────── -->
      <div class="rest-body">

        <!-- Step 0 : Configuration ─────────────────────────── -->
        <template v-if="step === 0">
          <div class="rest-grid-2">

            <!-- Nom -->
            <div class="rest-field rest-field--span">
              <label>Nom de la source <span class="req">*</span></label>
              <input v-model="form.name" class="rest-input" placeholder="ex : API Ventes" maxlength="100" />
            </div>

            <!-- Méthode + URL -->
            <div class="rest-field rest-field--method">
              <label>Méthode</label>
              <select v-model="form.method" class="rest-select">
                <option>GET</option>
                <option>POST</option>
                <option>PUT</option>
                <option>PATCH</option>
              </select>
            </div>
            <div class="rest-field rest-field--url">
              <label>URL de l'endpoint <span class="req">*</span></label>
              <input v-model="form.endpoint" class="rest-input" placeholder="https://api.example.com/data" />
            </div>

            <!-- Chemin JSON -->
            <div class="rest-field rest-field--span">
              <label>Chemin JSON <span class="hint-label">(optionnel)</span></label>
              <input v-model="form.dataPath" class="rest-input" placeholder="ex : data.items  ou  results" />
              <p class="rest-hint"><i class="pi pi-info-circle"></i> Chemin vers le tableau dans la réponse JSON. Laisser vide si la réponse est directement un tableau.</p>
            </div>

            <!-- Description -->
            <div class="rest-field rest-field--span">
              <label>Description <span class="hint-label">(optionnel)</span></label>
              <input v-model="form.description" class="rest-input" placeholder="Description de cette source…" maxlength="250" />
            </div>
          </div>

          <div class="rest-footer">
            <button class="rest-btn rest-btn--ghost" @click="$emit('close')">Annuler</button>
            <button class="rest-btn rest-btn--primary" @click="step = 1" :disabled="!canGoStep1">
              Suivant <i class="pi pi-arrow-right"></i>
            </button>
          </div>
        </template>

        <!-- Step 1 : En-têtes & Corps ───────────────────────── -->
        <template v-else-if="step === 1">

          <!-- Headers -->
          <div class="rest-section-title">En-têtes HTTP <span class="hint-label">(optionnel)</span></div>
          <div class="headers-list">
            <div v-for="(h, i) in headers" :key="i" class="header-row">
              <input v-model="h.key"   class="rest-input" placeholder="Clé (ex : Authorization)" />
              <input v-model="h.value" class="rest-input" placeholder="Valeur (ex : Bearer token…)" />
              <button class="header-remove" @click="headers.splice(i, 1)" type="button">✕</button>
            </div>
            <button class="add-header-btn" @click="headers.push({ key: '', value: '' })">
              <i class="pi pi-plus"></i> Ajouter un en-tête
            </button>
          </div>

          <!-- Body (only POST/PUT/PATCH) -->
          <template v-if="form.method !== 'GET'">
            <div class="rest-section-title" style="margin-top:16px">Corps de la requête (JSON) <span class="hint-label">(optionnel)</span></div>
            <textarea
              v-model="form.body"
              class="rest-editor"
              placeholder='{"key": "value"}'
              rows="5"
              spellcheck="false"
            ></textarea>
          </template>

          <div class="rest-footer">
            <button class="rest-btn rest-btn--ghost" @click="step = 0"><i class="pi pi-arrow-left"></i> Retour</button>
            <button class="rest-btn rest-btn--primary" @click="step = 2">
              Suivant <i class="pi pi-arrow-right"></i>
            </button>
          </div>
        </template>

        <!-- Step 2 : Aperçu & Import ────────────────────────── -->
        <template v-else-if="step === 2">

          <!-- Error -->
          <div v-if="fetchError" class="rest-error"><i class="pi pi-exclamation-triangle"></i> {{ fetchError }}</div>

          <!-- Preview table -->
          <template v-if="previewRows.length > 0">
            <div class="rest-preview-header">
              <span class="rest-preview-title">Aperçu — {{ previewRows.length }} lignes sur {{ previewTotal }}</span>
              <span class="rest-preview-cols">{{ previewColumns.length }} colonnes</span>
            </div>
            <div class="rest-table-wrap">
              <table class="rest-table">
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

          <!-- Empty state before fetch -->
          <div v-else-if="!isFetching && !fetchError" class="rest-empty-state">
            <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="rgba(255,255,255,0.2)" stroke-width="1.5">
              <path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/>
              <path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"/>
            </svg>
            <p>Cliquez sur <strong>Tester</strong> pour charger un aperçu de l'API</p>
          </div>

          <div class="rest-footer">
            <button class="rest-btn rest-btn--ghost" @click="step = 1"><i class="pi pi-arrow-left"></i> Retour</button>
            <button class="rest-btn rest-btn--test" @click="doFetch" :disabled="isFetching">
              <i :class="isFetching ? 'pi pi-spin pi-spinner' : 'pi pi-play'"></i>
              {{ isFetching ? 'Chargement…' : 'Tester' }}
            </button>
            <button class="rest-btn rest-btn--primary" @click="doImport" :disabled="previewRows.length === 0 || isImporting">
              <i :class="isImporting ? 'pi pi-spin pi-spinner' : 'pi pi-database'"></i>
              {{ isImporting ? 'Importation…' : 'Importer' }}
            </button>
          </div>
        </template>

      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, computed } from 'vue'
import { restService, type SqlColumn, type SqlRow } from '@/services/sqlService'

// ── Events ───────────────────────────────────────────────────────────────────
const emit = defineEmits<{
  (e: 'close'): void
  (e: 'imported', payload: {
    name:     string
    columns:  { name: string; type: string; min?: number; max?: number; uniqueValues?: string[] }[]
    rows:     Record<string, string>[]
    sourceId: number
  }): void
}>()

// ── State ─────────────────────────────────────────────────────────────────────
const step  = ref(0)
const steps = ['Configuration', 'En-têtes & Corps', 'Aperçu & Import']

const form = reactive({
  name:        '',
  description: '',
  endpoint:    '',
  method:      'GET' as 'GET' | 'POST' | 'PUT' | 'PATCH',
  body:        '',
  dataPath:    '',
})

const headers = ref<{ key: string; value: string }[]>([])

// Preview
const previewColumns = ref<SqlColumn[]>([])
const previewRows    = ref<SqlRow[]>([])
const previewTotal   = ref(0)
const fetchError     = ref('')
const isFetching     = ref(false)
const isImporting    = ref(false)

// ── Computed ──────────────────────────────────────────────────────────────────
const canGoStep1 = computed(() => form.name.trim() !== '' && form.endpoint.trim() !== '')

const headersMap = computed<Record<string, string>>(() => {
  const map: Record<string, string> = {}
  for (const h of headers.value) {
    if (h.key.trim()) map[h.key.trim()] = h.value
  }
  return map
})

// ── Methods ───────────────────────────────────────────────────────────────────

async function doFetch() {
  isFetching.value = true
  fetchError.value = ''
  previewRows.value    = []
  previewColumns.value = []
  try {
    const res = await restService.connect({
      name:        form.name.trim(),
      description: form.description,
      endpoint:    form.endpoint.trim(),
      method:      form.method,
      headers:     headersMap.value,
      body:        form.body || undefined,
      dataPath:    form.dataPath || undefined,
    })
    previewColumns.value = res.columns ?? []
    previewRows.value    = (res.rows ?? []) as SqlRow[]
    previewTotal.value   = res.totalRows ?? previewRows.value.length
  } catch (err: unknown) {
    fetchError.value = err instanceof Error ? err.message : 'Erreur lors de l\'appel REST'
  } finally {
    isFetching.value = false
  }
}

async function doImport() {
  if (previewRows.value.length === 0) return
  isImporting.value = true
  try {
    const normCols = normalizeColumns(previewColumns.value, previewRows.value)
    const strRows  = previewRows.value.map(row =>
      Object.fromEntries(
        previewColumns.value.map(col => [col.name, row[col.name] == null ? '' : String(row[col.name])])
      )
    )
    emit('imported', {
      name:     form.name.trim(),
      columns:  normCols,
      rows:     strRows,
      sourceId: 0, // sourceId already created in doFetch → pass 0 or re-call; backend handles idempotency
    })
  } finally {
    isImporting.value = false
  }
}

function normalizeColumns(
  cols: SqlColumn[],
  rows: SqlRow[],
): { name: string; type: string; min?: number; max?: number; uniqueValues?: string[] }[] {
  return cols.map(col => {
    const frontType = col.type === 'string' || col.type === 'category' ? 'category' : col.type
    const allVals   = rows.map(r => r[col.name])

    if (frontType === 'number') {
      const nums = allVals
        .map(v => parseFloat(String(v).replace(/[^\d.-]/g, '')))
        .filter(v => !isNaN(v))
      return { name: col.name, type: 'number', min: nums.length ? Math.min(...nums) : 0, max: nums.length ? Math.max(...nums) : 0 }
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

function typeIcon(type: string): string {
  if (type === 'number')  return 'Σ'
  if (type === 'date')    return '📅'
  if (type === 'boolean') return '◉'
  return '🔤'
}

function formatCell(val: unknown): string {
  if (val == null)              return '—'
  if (typeof val === 'boolean') return val ? 'Vrai' : 'Faux'
  const s = String(val)
  return s.length > 40 ? s.slice(0, 40) + '…' : s
}
</script>

<style scoped>
/* ── Overlay ──────────────────────────────────────────────────── */
.rest-overlay {
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

/* ── Modal ────────────────────────────────────────────────────── */
.rest-modal {
  background: var(--color-surface, #EEF7F1);
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
.rest-header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 20px 24px 16px;
  border-bottom: 1px solid var(--color-border, rgba(255,255,255,0.08));
}
.rest-header-icon {
  width: 38px; height: 38px;
  border-radius: 10px;
  background: rgba(124, 58, 237, 0.15);
  color: #7c3aed;
  display: flex; align-items: center; justify-content: center;
  flex-shrink: 0;
}
.rest-header-title { margin: 0; font-size: 15px; font-weight: 600; color: var(--color-text, #f0fdf9); }
.rest-header-sub   { margin: 2px 0 0; font-size: 11px; color: var(--color-text-muted, #94A99A); }
.rest-close {
  margin-left: auto;
  background: none; border: none; cursor: pointer;
  color: var(--color-text-muted, #94A99A);
  font-size: 14px; padding: 4px; border-radius: 6px;
  transition: color .15s, background .15s;
}
.rest-close:hover { color: var(--color-text, #f0fdf9); background: rgba(255,255,255,0.08); }

/* ── Steps ────────────────────────────────────────────────────── */
.rest-steps {
  display: flex;
  align-items: center;
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
.rest-step {
  display: flex; align-items: center; gap: 8px;
  flex: 1; position: relative; z-index: 1;
}
.rest-step:last-child { justify-content: flex-end; }
.step-dot {
  width: 24px; height: 24px; border-radius: 50%;
  border: 2px solid var(--color-border, rgba(255,255,255,0.15));
  background: var(--color-surface, #EEF7F1);
  display: flex; align-items: center; justify-content: center;
  font-size: 10px; font-weight: 700;
  color: var(--color-text-muted, #94A99A);
  transition: all .2s; flex-shrink: 0;
}
.rest-step.active .step-dot { border-color: #7c3aed; color: #7c3aed; box-shadow: 0 0 0 3px rgba(124,58,237,.2); }
.rest-step.done   .step-dot { border-color: #7c3aed; background: #7c3aed; color: #fff; }
.rest-step span { font-size: 11px; color: var(--color-text-muted, #94A99A); }
.rest-step.active span, .rest-step.done span { color: var(--color-text, #f0fdf9); font-weight: 500; }

/* ── Body ─────────────────────────────────────────────────────── */
.rest-body { flex: 1; overflow-y: auto; padding: 0 24px 8px; }

/* ── Form ─────────────────────────────────────────────────────── */
.rest-grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
.rest-field   { display: flex; flex-direction: column; gap: 5px; }
.rest-field--span   { grid-column: 1 / -1; }
.rest-field--method { grid-column: span 1; }
.rest-field--url    { grid-column: span 1; }

label {
  font-size: 11px; font-weight: 600;
  color: var(--color-text-secondary, #4B5E52);
  letter-spacing: .03em; text-transform: uppercase;
}
.req        { color: #7c3aed; }
.hint-label { color: var(--color-text-muted, #94A99A); font-weight: 400; text-transform: none; letter-spacing: 0; }

.rest-input, .rest-select {
  background: rgba(27,107,58,0.06);
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
.rest-input:focus, .rest-select:focus {
  border-color: #7c3aed;
  box-shadow: 0 0 0 3px rgba(124,58,237,.15);
}
.rest-select option { background: #1a2744; }

.rest-hint {
  font-size: 10px;
  color: var(--color-text-muted, #94A99A);
  margin: 2px 0 0;
  display: flex; align-items: center; gap: 4px;
}

/* ── Headers list ─────────────────────────────────────────────── */
.rest-section-title {
  font-size: 11px; font-weight: 600;
  color: var(--color-text-secondary, #4B5E52);
  letter-spacing: .05em; text-transform: uppercase;
  margin: 16px 0 8px;
}
.headers-list { display: flex; flex-direction: column; gap: 8px; }
.header-row {
  display: grid;
  grid-template-columns: 1fr 1fr 32px;
  gap: 8px;
  align-items: center;
}
.header-remove {
  background: rgba(239,68,68,.15); border: 1px solid rgba(239,68,68,.3); color: #f87171;
  border-radius: 6px; padding: 4px 6px; cursor: pointer; font-size: 12px; transition: all .15s;
}
.header-remove:hover { background: rgba(239,68,68,.25); }

.add-header-btn {
  display: inline-flex; align-items: center; gap: 6px;
  background: rgba(124,58,237,.1); border: 1px dashed rgba(124,58,237,.3);
  color: #7c3aed; border-radius: 8px; padding: 7px 14px;
  font-size: 12px; cursor: pointer; transition: all .15s; width: fit-content;
}
.add-header-btn:hover { background: rgba(124,58,237,.18); }

/* ── REST body editor ─────────────────────────────────────────── */
.rest-editor {
  background: rgba(0,0,0,0.3);
  border: 1px solid var(--color-border, rgba(255,255,255,0.1));
  border-radius: 8px;
  padding: 12px;
  color: #c4b5fd;
  font-size: 12px;
  font-family: 'Fira Code', 'Cascadia Code', 'Consolas', monospace;
  line-height: 1.6;
  resize: vertical;
  min-height: 100px;
  outline: none;
  transition: border-color .15s;
  width: 100%;
  box-sizing: border-box;
}
.rest-editor:focus { border-color: #7c3aed; }

/* ── Preview ──────────────────────────────────────────────────── */
.rest-preview-header {
  display: flex; align-items: center; justify-content: space-between;
  margin: 16px 0 8px;
}
.rest-preview-title { font-size: 11px; font-weight: 600; color: var(--color-text-secondary, #4B5E52); }
.rest-preview-cols  {
  font-size: 10px;
  background: rgba(124,58,237,.1); color: #7c3aed;
  border-radius: 10px; padding: 2px 8px;
}
.rest-table-wrap {
  overflow-x: auto;
  border: 1px solid var(--color-border, rgba(255,255,255,0.08));
  border-radius: 8px;
  max-height: 220px; overflow-y: auto;
}
.rest-table { width: 100%; border-collapse: collapse; font-size: 11px; }
.rest-table th {
  background: rgba(255,255,255,0.04);
  padding: 7px 10px; text-align: left;
  color: var(--color-text-secondary, #4B5E52); font-weight: 600;
  white-space: nowrap; position: sticky; top: 0;
  border-bottom: 1px solid var(--color-border, rgba(255,255,255,0.08));
}
.rest-table td {
  padding: 5px 10px; color: var(--color-text, #f0fdf9);
  border-bottom: 1px solid rgba(255,255,255,0.04);
  max-width: 180px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
}
.rest-table tr:last-child td { border-bottom: none; }
.rest-table tr:hover td { background: rgba(255,255,255,0.03); }
.th-type { margin-right: 4px; font-size: 9px; }
.th-type--number  { color: #1B6B3A; }
.th-type--date    { color: #3b82f6; }
.th-type--boolean { color: #8b5cf6; }

/* ── Empty state ──────────────────────────────────────────────── */
.rest-empty-state {
  display: flex; flex-direction: column; align-items: center; gap: 12px;
  padding: 40px 20px; text-align: center;
  color: #94A99A; font-size: 13px;
}
.rest-empty-state strong { color: #4B5E52; }

/* ── Error ────────────────────────────────────────────────────── */
.rest-error {
  display: flex; align-items: flex-start; gap: 8px;
  background: rgba(239,68,68,.1); border: 1px solid rgba(239,68,68,.25);
  border-radius: 8px; padding: 10px 14px;
  color: #ef4444; font-size: 12px; margin-top: 14px; line-height: 1.4;
}

/* ── Footer ───────────────────────────────────────────────────── */
.rest-footer {
  display: flex; gap: 8px; justify-content: flex-end;
  padding: 16px 0 12px;
  border-top: 1px solid var(--color-border, rgba(255,255,255,0.07));
  margin-top: 16px;
}
.rest-btn {
  display: flex; align-items: center; gap: 6px;
  padding: 8px 16px; border-radius: 8px;
  font-size: 12px; font-weight: 600; cursor: pointer;
  border: none; transition: all .15s;
}
.rest-btn:disabled { opacity: .4; cursor: not-allowed; }
.rest-btn--ghost {
  background: rgba(27,107,58,0.06);
  color: var(--color-text-muted, rgba(240,253,249,0.5));
}
.rest-btn--ghost:hover:not(:disabled) { background: rgba(255,255,255,0.1); color: var(--color-text, #f0fdf9); }
.rest-btn--test {
  background: rgba(245,158,11,0.12); border: 1px solid rgba(245,158,11,0.3); color: #f59e0b;
}
.rest-btn--test:hover:not(:disabled) { background: rgba(245,158,11,0.2); }
.rest-btn--primary {
  background: #7c3aed; color: #fff; font-weight: 700;
}
.rest-btn--primary:hover:not(:disabled) { filter: brightness(1.1); }
</style>


