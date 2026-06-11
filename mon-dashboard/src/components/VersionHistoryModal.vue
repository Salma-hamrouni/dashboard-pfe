<template>
  <Teleport to="body">
    <Transition name="vh-backdrop">
      <div v-if="modelValue" class="vh-backdrop" @click.self="close" />
    </Transition>

    <Transition name="vh-panel">
      <aside v-if="modelValue" class="vh-panel">

        <!-- ── Header ─────────────────────────────────────────────────── -->
        <div class="vh-header">
          <div class="vh-header-left">
            <div class="vh-header-icon">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>
              </svg>
            </div>
            <div>
              <p class="vh-header-title">Historique des versions</p>
              <p class="vh-header-sub">{{ dashboardName }}</p>
            </div>
          </div>
          <button class="vh-close-btn" @click="close" title="Fermer">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
          </button>
        </div>

        <!-- ── Save current version ───────────────────────────────────── -->
        <div class="vh-save-bar">
          <input
            v-model="newLabel"
            class="vh-label-input"
            placeholder="Label (optionnel) — ex : Avant refonte"
            maxlength="80"
            @keydown.enter="saveCurrentVersion"
          />
          <button class="vh-save-now-btn" :disabled="isSaving" @click="saveCurrentVersion">
            <svg v-if="!isSaving" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/><polyline points="17 21 17 13 7 13 7 21"/><polyline points="7 3 7 8 15 8"/></svg>
            <svg v-else width="13" height="13" viewBox="0 0 24 24" class="spin" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0z"/></svg>
            {{ isSaving ? 'Sauvegarde…' : 'Sauvegarder version actuelle' }}
          </button>
        </div>

        <!-- ── Main content ───────────────────────────────────────────── -->
        <div class="vh-body">

          <!-- Loading -->
          <div v-if="isLoading" class="vh-loading">
            <svg class="spin" width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 12a9 9 0 1 1-18 0"/></svg>
            <span>Chargement…</span>
          </div>

          <!-- Empty state -->
          <div v-else-if="!isLoading && versions.length === 0" class="vh-empty">
            <svg width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
            <p>Aucune version sauvegardée</p>
            <span>Cliquez sur « Sauvegarder version actuelle » pour créer un point de restauration.</span>
          </div>

          <!-- Version list -->
          <div v-else class="vh-list">
            <div
              v-for="v in versions"
              :key="v.id"
              class="vh-item"
              :class="{ 'vh-item--active': selectedVersion?.id === v.id }"
              @click="selectVersion(v)"
            >
              <div class="vh-item-badge">v{{ v.version }}</div>
              <div class="vh-item-info">
                <p class="vh-item-label">{{ v.label || `Version ${v.version}` }}</p>
                <p class="vh-item-date">{{ formatDate(v.createdAt) }}</p>
              </div>
              <div class="vh-item-meta">
                <span v-if="v.widgetCount !== undefined" class="vh-item-wcount">
                  {{ v.widgetCount }} widget{{ v.widgetCount !== 1 ? 's' : '' }}
                </span>
                <button
                  class="vh-delete-btn"
                  title="Supprimer cette version"
                  @click.stop="deleteVersion(v)"
                >
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14H6L5 6"/><path d="M10 11v6"/><path d="M14 11v6"/><path d="M9 6V4h6v2"/></svg>
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- ── Detail / restore panel ─────────────────────────────────── -->
        <Transition name="vh-detail">
          <div v-if="selectedVersion" class="vh-detail">
            <div class="vh-detail-header">
              <div>
                <p class="vh-detail-title">
                  <span class="vh-detail-badge">v{{ selectedVersion.version }}</span>
                  {{ selectedVersion.label || `Version ${selectedVersion.version}` }}
                </p>
                <p class="vh-detail-date">{{ formatDate(selectedVersion.createdAt) }}</p>
              </div>
              <button
                class="vh-restore-btn"
                :disabled="isRestoring"
                @click="restoreVersion"
              >
                <svg v-if="!isRestoring" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8"/><path d="M3 3v5h5"/></svg>
                <svg v-else width="13" height="13" class="spin" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M21 12a9 9 0 1 1-18 0"/></svg>
                {{ isRestoring ? 'Restauration…' : 'Restaurer cette version' }}
              </button>
            </div>

            <!-- Widget list preview -->
            <div v-if="isLoadingDetail" class="vh-detail-loading">
              <svg class="spin" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 12a9 9 0 1 1-18 0"/></svg>
            </div>
            <div v-else-if="versionDetail" class="vh-widget-list">
              <p class="vh-widget-list-title">{{ versionDetail.snapshot.widgets.length }} widget(s)</p>
              <div
                v-for="w in versionDetail.snapshot.widgets"
                :key="w.id"
                class="vh-widget-row"
              >
                <span class="vh-widget-type" :class="`wtype-${w.type}`">{{ w.type }}</span>
                <span class="vh-widget-title">{{ w.title || '(sans titre)' }}</span>
              </div>
              <div v-if="versionDetail.snapshot.widgets.length === 0" class="vh-widget-empty">
                Aucun widget dans cette version
              </div>
            </div>
          </div>
        </Transition>

      </aside>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import versionService, { type DashboardVersionSummary, type DashboardVersionDetail } from '@/services/versionService'

const props = defineProps<{
  modelValue:    boolean
  dashboardId:   number | null
  dashboardName: string
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', v: boolean): void
  (e: 'restored'): void
}>()

// ── State ────────────────────────────────────────────────────────────────────
const versions        = ref<DashboardVersionSummary[]>([])
const selectedVersion = ref<DashboardVersionSummary | null>(null)
const versionDetail   = ref<DashboardVersionDetail | null>(null)
const newLabel        = ref('')
const isLoading       = ref(false)
const isLoadingDetail = ref(false)
const isSaving        = ref(false)
const isRestoring     = ref(false)

// ── Open / close ─────────────────────────────────────────────────────────────
watch(() => props.modelValue, open => {
  if (open) loadVersions()
  else reset()
})

function close() { emit('update:modelValue', false) }

function reset() {
  versions.value        = []
  selectedVersion.value = null
  versionDetail.value   = null
  newLabel.value        = ''
}

// ── Load versions list ────────────────────────────────────────────────────────
async function loadVersions() {
  if (!props.dashboardId) return
  isLoading.value = true
  try {
    const all = await versionService.getVersions(props.dashboardId)
    // Masquer les backups automatiques créés avant chaque restauration
    versions.value = all.filter(v => !v.label?.startsWith('Backup avant restauration'))
  } catch { /* silent */ }
  finally { isLoading.value = false }
}

// ── Select a version → load its detail ───────────────────────────────────────
async function selectVersion(v: DashboardVersionSummary) {
  if (selectedVersion.value?.id === v.id) { selectedVersion.value = null; versionDetail.value = null; return }
  selectedVersion.value = v
  versionDetail.value   = null
  if (!props.dashboardId) return
  isLoadingDetail.value = true
  try {
    versionDetail.value = await versionService.getVersion(props.dashboardId, v.version)
  } catch { /* silent */ }
  finally { isLoadingDetail.value = false }
}

// ── Save current version ──────────────────────────────────────────────────────
async function saveCurrentVersion() {
  if (!props.dashboardId || isSaving.value) return
  isSaving.value = true
  try {
    const saved = await versionService.saveVersion(props.dashboardId, newLabel.value.trim() || undefined)
    versions.value.unshift({ ...saved, widgetCount: saved.widgetCount })
    newLabel.value = ''
  } catch { /* silent */ }
  finally { isSaving.value = false }
}

// ── Restore ────────────────────────────────────────────────────────────────────
async function restoreVersion() {
  if (!props.dashboardId || !selectedVersion.value || isRestoring.value) return
  isRestoring.value = true
  try {
    await versionService.restoreVersion(props.dashboardId, selectedVersion.value.version)
    emit('restored')
    close()
  } catch { /* silent */ }
  finally { isRestoring.value = false }
}

// ── Delete ─────────────────────────────────────────────────────────────────────
async function deleteVersion(v: DashboardVersionSummary) {
  if (!confirm(`Supprimer la version ${v.version} ?`)) return
  try {
    await versionService.deleteVersion(v.id)
    versions.value = versions.value.filter(x => x.id !== v.id)
    if (selectedVersion.value?.id === v.id) { selectedVersion.value = null; versionDetail.value = null }
  } catch { /* silent */ }
}

// ── Helpers ───────────────────────────────────────────────────────────────────
function formatDate(iso: string): string {
  const d = new Date(iso)
  return d.toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' }) +
         ' · ' + d.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' })
}

// Exposé pour que le parent puisse déclencher depuis handleSave
defineExpose({ loadVersions })
</script>

<style scoped>
/* ── Backdrop ───────────────────────────────────────────────────────────────── */
.vh-backdrop {
  position: fixed; inset: 0;
  background: rgba(0,0,0,0.4);
  z-index: 1000;
}
.vh-backdrop-enter-active, .vh-backdrop-leave-active { transition: opacity .25s; }
.vh-backdrop-enter-from, .vh-backdrop-leave-to { opacity: 0; }

/* ── Side panel ─────────────────────────────────────────────────────────────── */
.vh-panel {
  position: fixed;
  top: 0; right: 0;
  width: 380px;
  height: 100vh;
  background: #0b1118;
  border-left: 1px solid rgba(16,185,129,0.12);
  display: flex;
  flex-direction: column;
  z-index: 1001;
  font-family: 'DM Sans', sans-serif;
  overflow: hidden;
}
.vh-panel-enter-active, .vh-panel-leave-active {
  transition: transform .28s cubic-bezier(0.16,1,0.3,1), opacity .2s;
}
.vh-panel-enter-from, .vh-panel-leave-to { transform: translateX(100%); opacity: 0; }

/* ── Header ─────────────────────────────────────────────────────────────────── */
.vh-header {
  display: flex; align-items: center; justify-content: space-between;
  padding: 16px 16px 14px;
  border-bottom: 1px solid rgba(255,255,255,0.06);
  flex-shrink: 0;
}
.vh-header-left { display: flex; align-items: center; gap: 10px; }
.vh-header-icon {
  width: 32px; height: 32px;
  border-radius: 8px;
  background: rgba(16,185,129,0.12);
  border: 1px solid rgba(16,185,129,0.2);
  display: flex; align-items: center; justify-content: center;
  color: #10b981;
  flex-shrink: 0;
}
.vh-header-title { font-size: 13px; font-weight: 700; color: #e2e8f0; line-height: 1.3; }
.vh-header-sub   { font-size: 11px; color: #64748b; margin-top: 1px; }
.vh-close-btn {
  width: 28px; height: 28px; border-radius: 6px;
  border: 1px solid rgba(255,255,255,0.08);
  background: rgba(255,255,255,0.04);
  color: rgba(255,255,255,0.4); cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  transition: background .15s, color .15s;
}
.vh-close-btn:hover { background: rgba(239,68,68,0.12); color: #f87171; border-color: rgba(239,68,68,0.2); }

/* ── Save bar ────────────────────────────────────────────────────────────────── */
.vh-save-bar {
  display: flex; gap: 8px; padding: 12px 14px;
  border-bottom: 1px solid rgba(255,255,255,0.06);
  flex-shrink: 0;
}
.vh-label-input {
  flex: 1; min-width: 0;
  padding: 6px 10px;
  background: rgba(255,255,255,0.04);
  border: 1px solid rgba(255,255,255,0.08);
  border-radius: 7px;
  color: #e2e8f0; font-size: 12px;
  outline: none; transition: border-color .15s;
}
.vh-label-input:focus { border-color: rgba(16,185,129,0.4); }
.vh-label-input::placeholder { color: #475569; }
.vh-save-now-btn {
  display: inline-flex; align-items: center; gap: 5px;
  padding: 6px 11px;
  background: rgba(16,185,129,0.12);
  border: 1px solid rgba(16,185,129,0.22);
  border-radius: 7px;
  color: #10b981; font-size: 11px; font-weight: 600;
  cursor: pointer; white-space: nowrap;
  transition: background .15s, color .15s;
}
.vh-save-now-btn:hover:not(:disabled) { background: rgba(16,185,129,0.22); }
.vh-save-now-btn:disabled { opacity: .5; cursor: not-allowed; }

/* ── Body ─────────────────────────────────────────────────────────────────── */
.vh-body {
  flex: 1; overflow-y: auto; min-height: 0;
}
.vh-body::-webkit-scrollbar { width: 3px; }
.vh-body::-webkit-scrollbar-track { background: transparent; }
.vh-body::-webkit-scrollbar-thumb { background: rgba(255,255,255,0.08); border-radius: 2px; }

/* Loading / empty */
.vh-loading, .vh-empty {
  display: flex; flex-direction: column; align-items: center; justify-content: center;
  gap: 10px; padding: 48px 24px; color: #475569; text-align: center;
}
.vh-empty p   { font-size: 13px; font-weight: 600; color: #64748b; }
.vh-empty span { font-size: 11px; color: #334155; line-height: 1.5; }

/* Version list */
.vh-list { padding: 8px 0; }
.vh-item {
  display: flex; align-items: center; gap: 10px;
  padding: 10px 14px;
  cursor: pointer;
  border-bottom: 1px solid rgba(255,255,255,0.04);
  transition: background .12s;
}
.vh-item:hover { background: rgba(255,255,255,0.04); }
.vh-item--active { background: rgba(16,185,129,0.08) !important; border-left: 2px solid #10b981; }

.vh-item-badge {
  width: 32px; height: 20px; flex-shrink: 0;
  border-radius: 5px;
  background: rgba(99,102,241,0.12);
  border: 1px solid rgba(99,102,241,0.2);
  color: #818cf8;
  font-size: 10px; font-weight: 700;
  display: flex; align-items: center; justify-content: center;
}
.vh-item-info { flex: 1; min-width: 0; }
.vh-item-label {
  font-size: 12px; font-weight: 600; color: #cbd5e1;
  white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
}
.vh-item-date { font-size: 10px; color: #475569; margin-top: 1px; }
.vh-item-meta { display: flex; align-items: center; gap: 6px; flex-shrink: 0; }
.vh-item-wcount { font-size: 10px; color: #334155; }
.vh-delete-btn {
  width: 22px; height: 22px; border-radius: 5px;
  background: transparent; border: 1px solid transparent;
  color: #475569; cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  transition: all .12s;
}
.vh-delete-btn:hover { background: rgba(239,68,68,0.12); color: #f87171; border-color: rgba(239,68,68,0.2); }

/* ── Detail panel ────────────────────────────────────────────────────────────── */
.vh-detail {
  border-top: 1px solid rgba(16,185,129,0.12);
  background: rgba(16,185,129,0.04);
  flex-shrink: 0;
  max-height: 40vh;
  display: flex; flex-direction: column;
  overflow: hidden;
}
.vh-detail-enter-active, .vh-detail-leave-active { transition: max-height .25s ease, opacity .2s; }
.vh-detail-enter-from, .vh-detail-leave-to { max-height: 0; opacity: 0; }

.vh-detail-header {
  display: flex; align-items: flex-start; justify-content: space-between;
  padding: 12px 14px 8px;
  gap: 8px; flex-shrink: 0;
}
.vh-detail-title {
  display: flex; align-items: center; gap: 6px;
  font-size: 12px; font-weight: 700; color: #e2e8f0;
}
.vh-detail-badge {
  font-size: 10px; font-weight: 700; color: #818cf8;
  background: rgba(99,102,241,0.12);
  border: 1px solid rgba(99,102,241,0.2);
  padding: 1px 6px; border-radius: 4px;
}
.vh-detail-date { font-size: 10px; color: #475569; margin-top: 2px; }

.vh-restore-btn {
  display: inline-flex; align-items: center; gap: 5px;
  padding: 6px 11px; flex-shrink: 0;
  background: rgba(16,185,129,0.15);
  border: 1px solid rgba(16,185,129,0.28);
  border-radius: 7px;
  color: #10b981; font-size: 11px; font-weight: 600;
  cursor: pointer; white-space: nowrap;
  transition: background .15s;
}
.vh-restore-btn:hover:not(:disabled) { background: rgba(16,185,129,0.28); }
.vh-restore-btn:disabled { opacity: .5; cursor: not-allowed; }

.vh-detail-loading {
  display: flex; justify-content: center; padding: 16px;
  color: #475569;
}

.vh-widget-list { overflow-y: auto; flex: 1; padding: 0 14px 12px; }
.vh-widget-list::-webkit-scrollbar { width: 3px; }
.vh-widget-list::-webkit-scrollbar-thumb { background: rgba(255,255,255,0.08); border-radius: 2px; }
.vh-widget-list-title { font-size: 10px; font-weight: 600; color: #475569; text-transform: uppercase; letter-spacing: .06em; margin-bottom: 6px; }
.vh-widget-row {
  display: flex; align-items: center; gap: 8px;
  padding: 5px 0;
  border-bottom: 1px solid rgba(255,255,255,0.04);
  font-size: 12px;
}
.vh-widget-type {
  font-size: 9px; font-weight: 700; text-transform: uppercase; letter-spacing: .05em;
  padding: 2px 6px; border-radius: 4px;
  background: rgba(16,185,129,0.1); color: #10b981;
  flex-shrink: 0;
}
.vh-widget-type.wtype-bar   { background: rgba(16,185,129,0.1);  color: #10b981; }
.vh-widget-type.wtype-line  { background: rgba(99,102,241,0.1);  color: #818cf8; }
.vh-widget-type.wtype-area  { background: rgba(6,182,212,0.1);   color: #22d3ee; }
.vh-widget-type.wtype-pie,
.vh-widget-type.wtype-doughnut { background: rgba(245,158,11,0.1); color: #fbbf24; }
.vh-widget-type.wtype-kpi   { background: rgba(139,92,246,0.1);  color: #a78bfa; }
.vh-widget-type.wtype-table { background: rgba(14,165,233,0.1);  color: #38bdf8; }
.vh-widget-title { color: #94a3b8; flex: 1; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.vh-widget-empty { font-size: 11px; color: #334155; text-align: center; padding: 12px 0; }

/* ── Spin animation ──────────────────────────────────────────────────────────── */
.spin { animation: spin .8s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }
</style>
