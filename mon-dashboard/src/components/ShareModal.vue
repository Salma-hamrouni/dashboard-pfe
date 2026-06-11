<template>
  <teleport to="body">
    <transition name="sm-fade">
      <div v-if="modelValue" class="sm-overlay" @click.self="close">
        <div class="sm-card">

          <!-- ── Header ─────────────────────────────────────────────────────── -->
          <div class="sm-header">
            <div class="sm-header-icon">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="18" cy="5" r="3"/><circle cx="6" cy="12" r="3"/><circle cx="18" cy="19" r="3"/>
                <line x1="8.59" y1="13.51" x2="15.42" y2="17.49"/>
                <line x1="15.41" y1="6.51" x2="8.59" y2="10.49"/>
              </svg>
            </div>
            <div>
              <h2 class="sm-title">Partager le dashboard</h2>
              <p class="sm-subtitle">{{ dashboardName }}</p>
            </div>
            <button class="sm-close" @click="close"><i class="pi pi-times"/></button>
          </div>

          <!-- ── Body ──────────────────────────────────────────────────────── -->
          <div class="sm-body">

            <!-- ── Section : accès public ──────────────────────────────────── -->
            <div class="sm-public-section">
              <div class="sm-public-row">
                <div class="sm-public-info">
                  <div class="sm-public-icon">
                    <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <circle cx="12" cy="12" r="10"/>
                      <line x1="2" y1="12" x2="22" y2="12"/>
                      <path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"/>
                    </svg>
                  </div>
                  <div>
                    <p class="sm-public-label">Accès public</p>
                    <p class="sm-public-desc">Tout le monde avec le lien peut consulter</p>
                  </div>
                </div>
                <button
                  class="sm-toggle"
                  :class="{ on: localIsPublic }"
                  :disabled="isTogglingPublic"
                  @click="togglePublic"
                  :title="localIsPublic ? 'Désactiver l\'accès public' : 'Activer l\'accès public'"
                >
                  <span class="sm-toggle-thumb"/>
                </button>
              </div>

              <!-- Lien public (affiché quand isPublic = true) -->
              <transition name="sm-pub-link-slide">
                <div v-if="localIsPublic && localShareToken" class="sm-pub-link-row">
                  <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" style="flex-shrink:0;opacity:.5">
                    <path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/>
                    <path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"/>
                  </svg>
                  <input
                    :value="publicUrl"
                    readonly
                    class="sm-pub-link-input"
                    @focus="(e) => (e.target as HTMLInputElement).select()"
                  />
                  <button class="sm-pub-copy-btn" :class="{ copied: copiedPublic }" @click="copyPublicLink" title="Copier le lien public">
                    <svg v-if="!copiedPublic" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2">
                      <rect x="9" y="9" width="13" height="13" rx="2"/><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/>
                    </svg>
                    <svg v-else width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                      <polyline points="20 6 9 17 4 12"/>
                    </svg>
                  </button>
                </div>
              </transition>
            </div>

            <!-- Résultat (lien généré) -->
            <transition name="sm-result-slide">
              <div v-if="generatedToken" class="sm-result">
                <div class="sm-result-icon">
                  <i class="pi pi-check-circle"/>
                </div>
                <p class="sm-result-label">Lien de partage généré</p>
                <div class="sm-link-row">
                  <input
                    ref="linkInputRef"
                    :value="shareUrl"
                    readonly
                    class="sm-link-input"
                    @focus="(e) => (e.target as HTMLInputElement).select()"
                  />
                  <button class="sm-copy-btn" :class="{ copied }" @click="copyLink">
                    <i :class="copied ? 'pi pi-check' : 'pi pi-copy'"/>
                    <span>{{ copied ? 'Copié !' : 'Copier' }}</span>
                  </button>
                </div>
                <div class="sm-result-meta">
                  <span class="sm-meta-tag" :class="generatedPermission === 1 ? 'perm-edit' : 'perm-view'">
                    <i :class="generatedPermission === 1 ? 'pi pi-pencil' : 'pi pi-eye'"/>
                    {{ generatedPermission === 1 ? 'Édition' : 'Lecture' }}
                  </span>
                  <span class="sm-meta-tag expiry">
                    <i class="pi pi-clock"/>
                    {{ generatedExpiryLabel }}
                  </span>
                </div>
                <button class="sm-new-btn" @click="resetForm">
                  <i class="pi pi-plus"/> Créer un autre lien
                </button>
              </div>
            </transition>

            <!-- Formulaire de création -->
            <div v-if="!generatedToken" class="sm-form">

              <!-- Permission -->
              <div class="sm-field">
                <label class="sm-label">Permission</label>
                <div class="sm-btn-group">
                  <button
                    :class="['sm-perm-btn', { active: permission === 0 }]"
                    @click="permission = 0"
                  >
                    <i class="pi pi-eye"/>
                    <span>Lecture seule</span>
                    <small>Le destinataire peut consulter</small>
                  </button>
                  <button
                    :class="['sm-perm-btn', { active: permission === 1 }]"
                    @click="permission = 1"
                  >
                    <i class="pi pi-pencil"/>
                    <span>Édition</span>
                    <small>Le destinataire peut modifier</small>
                  </button>
                </div>
              </div>

              <!-- Expiration -->
              <div class="sm-field">
                <label class="sm-label">Expiration du lien</label>
                <div class="sm-expiry-grid">
                  <button
                    v-for="opt in expiryOptions"
                    :key="opt.value ?? 'none'"
                    class="sm-expiry-btn"
                    :class="{ active: selectedExpiry === opt.value }"
                    @click="selectExpiry(opt.value)"
                  >{{ opt.label }}</button>
                </div>

                <!-- Custom date picker -->
                <div v-if="selectedExpiry === 'custom'" class="sm-custom-date">
                  <i class="pi pi-calendar sm-date-icon"/>
                  <input
                    type="datetime-local"
                    v-model="customDate"
                    class="sm-date-input"
                    :min="minDateTime"
                  />
                </div>
              </div>

            </div>

            <!-- Liens actifs existants -->
            <div class="sm-existing" v-if="existingShares.length > 0">
              <div class="sm-existing-header">
                <span>Liens actifs ({{ activeShares.length }})</span>
                <button v-if="loadingShares" class="sm-spin"><i class="pi pi-spin pi-spinner"/></button>
              </div>
              <div class="sm-share-list">
                <div
                  v-for="s in existingShares"
                  :key="s.id"
                  class="sm-share-item"
                  :class="{ expired: shareService.isExpired(s) }"
                >
                  <div class="sm-share-info">
                    <span class="sm-share-perm" :class="s.permission === 1 ? 'perm-edit' : 'perm-view'">
                      <i :class="s.permission === 1 ? 'pi pi-pencil' : 'pi pi-eye'"/>
                    </span>
                    <div class="sm-share-details">
                      <span class="sm-share-token">…{{ s.token.slice(-8) }}</span>
                      <span class="sm-share-expiry" :class="{ expired: shareService.isExpired(s) }">
                        {{ shareService.formatExpiry(s.expiresAt) }}
                      </span>
                    </div>
                  </div>
                  <div class="sm-share-actions">
                    <button class="sm-icon-btn" title="Copier ce lien" @click="copyToken(s.token)">
                      <i class="pi pi-copy"/>
                    </button>
                    <button class="sm-icon-btn danger" title="Révoquer" @click="revokeShare(s.id)">
                      <i class="pi pi-trash"/>
                    </button>
                  </div>
                </div>
              </div>
            </div>
            <div v-else-if="!loadingShares && !generatedToken" class="sm-no-shares">
              <i class="pi pi-link" style="opacity:.3; font-size: 22px"/>
              <span>Aucun lien actif pour ce dashboard</span>
            </div>

          </div>

          <!-- ── Footer ─────────────────────────────────────────────────────── -->
          <div class="sm-footer" v-if="!generatedToken">
            <button class="sm-btn-ghost" @click="close">Annuler</button>
            <button
              class="sm-btn-primary"
              :disabled="isCreating || (selectedExpiry === 'custom' && !customDate)"
              @click="createShare"
            >
              <i :class="isCreating ? 'pi pi-spin pi-spinner' : 'pi pi-link'"/>
              {{ isCreating ? 'Génération…' : 'Générer le lien' }}
            </button>
          </div>
          <div class="sm-footer" v-else>
            <button class="sm-btn-primary full" @click="close">
              <i class="pi pi-check"/> Fermer
            </button>
          </div>

        </div>
      </div>
    </transition>
  </teleport>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import shareService from '@/services/shareService'
import { dashboardService } from '@/services/dashboardService'
import type { ShareRecord, SharePermission } from '@/services/shareService'

const props = defineProps<{
  modelValue:    boolean
  dashboardId:   number | null
  dashboardName: string
  isPublic?:     boolean
  shareToken?:   string | null
}>()

const emit = defineEmits<{
  (e: 'update:modelValue',  v: boolean): void
  (e: 'update:isPublic',    v: boolean): void
  (e: 'update:shareToken',  v: string | null): void
}>()

// ── Local public state (mirrors prop, updated optimistically) ─────────────────
const localIsPublic   = ref(props.isPublic   ?? false)
const localShareToken = ref(props.shareToken ?? null as string | null)
const isTogglingPublic = ref(false)
const copiedPublic    = ref(false)

watch(() => props.isPublic,   v => { localIsPublic.value   = v ?? false })
watch(() => props.shareToken, v => { localShareToken.value = v ?? null  })

const publicUrl = computed(() =>
  localShareToken.value ? `${window.location.origin}/share/${localShareToken.value}` : ''
)

async function togglePublic() {
  if (!props.dashboardId || isTogglingPublic.value) return
  isTogglingPublic.value = true
  const newVal = !localIsPublic.value
  try {
    const res = await dashboardService.toggleShare(props.dashboardId, newVal)
    localIsPublic.value   = res.isPublic
    localShareToken.value = res.shareToken || null
    emit('update:isPublic',   res.isPublic)
    emit('update:shareToken', res.shareToken || null)
  } catch { /* ignore — toggle reverts visually */ }
  finally { isTogglingPublic.value = false }
}

async function copyPublicLink() {
  if (!publicUrl.value) return
  await navigator.clipboard.writeText(publicUrl.value)
  copiedPublic.value = true
  setTimeout(() => { copiedPublic.value = false }, 2000)
}

// ── Form state ───────────────────────────────────────────────────────────────
const permission    = ref<SharePermission>(0)
const selectedExpiry = ref<string | null>(null)   // null = no expiry
const customDate    = ref('')
const isCreating    = ref(false)

// ── Result state ─────────────────────────────────────────────────────────────
const generatedToken      = ref('')
const generatedPermission = ref<SharePermission>(0)
const generatedExpiryLabel = ref('')
const copied              = ref(false)

// ── Existing shares ──────────────────────────────────────────────────────────
const existingShares = ref<ShareRecord[]>([])
const loadingShares  = ref(false)

const activeShares = computed(() => existingShares.value.filter(s => !shareService.isExpired(s)))

// ── Expiry options ────────────────────────────────────────────────────────────
const expiryOptions = [
  { label: 'Sans expiration', value: null },
  { label: '24 heures',       value: '1d' },
  { label: '7 jours',         value: '7d' },
  { label: '30 jours',        value: '30d' },
  { label: '90 jours',        value: '90d' },
  { label: 'Date précise',    value: 'custom' },
]

const minDateTime = computed(() => {
  const d = new Date()
  d.setMinutes(d.getMinutes() + 5)
  return d.toISOString().slice(0, 16)
})

function selectExpiry(val: string | null) {
  selectedExpiry.value = val
  if (val !== 'custom') customDate.value = ''
}

// ── Computed share URL ────────────────────────────────────────────────────────
const shareUrl = computed(() =>
  generatedToken.value
    ? `${window.location.origin}/share/${generatedToken.value}`
    : ''
)

// ── Lifecycle ─────────────────────────────────────────────────────────────────
watch(() => props.modelValue, open => {
  if (open) {
    resetForm()
    loadShares()
  }
})

function resetForm() {
  generatedToken.value  = ''
  permission.value      = 0
  selectedExpiry.value  = null
  customDate.value      = ''
  copied.value          = false
}

async function loadShares() {
  if (!props.dashboardId) return
  loadingShares.value = true
  try {
    existingShares.value = await shareService.listByDashboard(props.dashboardId)
    // Sort: active first, then by creation date
    existingShares.value.sort((a, b) => {
      const aExp = shareService.isExpired(a)
      const bExp = shareService.isExpired(b)
      if (aExp !== bExp) return aExp ? 1 : -1
      return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    })
  } catch { /* ignore */ }
  finally { loadingShares.value = false }
}

// ── Compute ExpiresAt from selected option ────────────────────────────────────
function computeExpiresAt(): string | null {
  if (!selectedExpiry.value) return null
  if (selectedExpiry.value === 'custom') {
    return customDate.value ? new Date(customDate.value).toISOString() : null
  }
  const map: Record<string, number> = { '1d': 1, '7d': 7, '30d': 30, '90d': 90 }
  const days = map[selectedExpiry.value]
  if (!days) return null
  const d = new Date()
  d.setDate(d.getDate() + days)
  return d.toISOString()
}

// ── Create share ──────────────────────────────────────────────────────────────
async function createShare() {
  if (!props.dashboardId) return
  isCreating.value = true
  try {
    const expiresAt = computeExpiresAt()
    const res = await shareService.create({
      dashboardId: props.dashboardId,
      permission:  permission.value,
      expiresAt,
    })
    generatedToken.value       = res.token
    generatedPermission.value  = res.permission
    generatedExpiryLabel.value = shareService.formatExpiry(res.expiresAt)
    // Refresh the list
    await loadShares()
  } catch (err: any) {
    // Bubble up as alert for now — parent can handle via toast
    alert(err?.message ?? 'Erreur lors de la création du lien.')
  } finally {
    isCreating.value = false
  }
}

// ── Copy ──────────────────────────────────────────────────────────────────────
async function copyLink() {
  if (!shareUrl.value) return
  await navigator.clipboard.writeText(shareUrl.value)
  copied.value = true
  setTimeout(() => { copied.value = false }, 2500)
}

async function copyToken(token: string) {
  const url = `${window.location.origin}/share/${token}`
  await navigator.clipboard.writeText(url)
}

// ── Revoke ────────────────────────────────────────────────────────────────────
async function revokeShare(id: number) {
  try {
    await shareService.revoke(id)
    existingShares.value = existingShares.value.filter(s => s.id !== id)
  } catch { /* ignore */ }
}

// ── Close ─────────────────────────────────────────────────────────────────────
function close() {
  emit('update:modelValue', false)
}
</script>

<style scoped>
/* ── Public access section ─────────────────────────────────────────────── */
.sm-public-section {
  background: rgba(255,255,255,0.03);
  border: 1px solid rgba(255,255,255,0.08);
  border-radius: 12px;
  padding: 14px 16px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.sm-public-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.sm-public-info {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.sm-public-icon {
  width: 32px; height: 32px; flex-shrink: 0;
  border-radius: 8px;
  background: rgba(74,108,247,0.1);
  border: 1px solid rgba(74,108,247,0.2);
  display: flex; align-items: center; justify-content: center;
  color: #7a9cf9;
}

.sm-public-label {
  margin: 0; font-size: 13px; font-weight: 600; color: rgba(255,255,255,0.85);
}
.sm-public-desc {
  margin: 0; font-size: 10px; color: rgba(255,255,255,0.35);
}

/* Toggle switch */
.sm-toggle {
  position: relative; flex-shrink: 0;
  width: 42px; height: 24px;
  background: rgba(255,255,255,0.1);
  border: 1px solid rgba(255,255,255,0.15);
  border-radius: 12px;
  cursor: pointer;
  transition: background .25s, border-color .25s;
  padding: 0;
}
.sm-toggle.on {
  background: #4A6CF7;
  border-color: #4A6CF7;
}
.sm-toggle:disabled { opacity: .5; cursor: not-allowed; }
.sm-toggle-thumb {
  position: absolute;
  top: 3px; left: 3px;
  width: 16px; height: 16px;
  background: #fff;
  border-radius: 50%;
  transition: transform .25s;
  display: block;
}
.sm-toggle.on .sm-toggle-thumb { transform: translateX(18px); }

/* Public link row */
.sm-pub-link-row {
  display: flex; align-items: center; gap: 6px;
}
.sm-pub-link-input {
  flex: 1; min-width: 0;
  background: rgba(255,255,255,0.05);
  border: 1px solid rgba(255,255,255,0.1);
  border-radius: 7px;
  padding: 6px 10px;
  font-size: 10px; font-family: monospace;
  color: rgba(255,255,255,0.65);
  outline: none;
}
.sm-pub-copy-btn {
  flex-shrink: 0;
  width: 28px; height: 28px;
  border-radius: 7px; border: 1px solid rgba(255,255,255,0.1);
  background: rgba(255,255,255,0.06);
  color: rgba(255,255,255,0.5);
  display: flex; align-items: center; justify-content: center;
  cursor: pointer; transition: background .15s, color .15s;
}
.sm-pub-copy-btn:hover { background: rgba(255,255,255,0.12); color: #fff; }
.sm-pub-copy-btn.copied { background: rgba(16,185,129,0.15); color: #10b981; border-color: rgba(16,185,129,0.3); }

.sm-pub-link-slide-enter-active { transition: all .25s ease; }
.sm-pub-link-slide-leave-active { transition: all .2s ease; }
.sm-pub-link-slide-enter-from,
.sm-pub-link-slide-leave-to { opacity: 0; transform: translateY(-6px); }

/* Overlay */
.sm-overlay {
  position: fixed; inset: 0; z-index: 9000;
  display: flex; align-items: center; justify-content: center;
  background: rgba(0,0,0,0.55);
  backdrop-filter: blur(5px);
}

/* Card */
.sm-card {
  width: min(500px, 94vw);
  max-height: 90vh;
  background: #0f1923;
  border: 1px solid rgba(255,255,255,0.1);
  border-radius: 16px;
  display: flex; flex-direction: column;
  box-shadow: 0 20px 60px rgba(0,0,0,0.6);
  overflow: hidden;
}

/* Header */
.sm-header {
  display: flex; align-items: center; gap: 12px;
  padding: 20px 22px 16px;
  border-bottom: 1px solid rgba(255,255,255,0.07);
  flex-shrink: 0;
}
.sm-header-icon {
  width: 40px; height: 40px; border-radius: 10px;
  background: rgba(74,108,247,0.15);
  border: 1px solid rgba(74,108,247,0.3);
  display: flex; align-items: center; justify-content: center;
  color: #4A6CF7; flex-shrink: 0;
}
.sm-title   { margin: 0 0 2px; font-size: 15px; font-weight: 700; color: #fff; }
.sm-subtitle { margin: 0; font-size: 11px; color: rgba(255,255,255,0.4); }
.sm-close {
  margin-left: auto; background: none; border: none;
  color: rgba(255,255,255,0.4); cursor: pointer; padding: 4px;
  border-radius: 6px; transition: color .15s, background .15s;
}
.sm-close:hover { color: #fff; background: rgba(255,255,255,0.08); }

/* Body */
.sm-body {
  flex: 1; overflow-y: auto; padding: 20px 22px;
  display: flex; flex-direction: column; gap: 20px;
  scrollbar-width: thin;
  min-height: 0;
}

/* Result block */
.sm-result {
  display: flex; flex-direction: column; align-items: center;
  gap: 12px; text-align: center;
  padding: 16px; border-radius: 12px;
  background: rgba(16,185,129,0.06);
  border: 1px solid rgba(16,185,129,0.2);
}
.sm-result-icon { font-size: 28px; color: #10b981; }
.sm-result-label { margin: 0; font-size: 13px; font-weight: 600; color: rgba(255,255,255,0.85); }

.sm-link-row {
  display: flex; gap: 8px; width: 100%;
}
.sm-link-input {
  flex: 1; background: rgba(255,255,255,0.06);
  border: 1px solid rgba(255,255,255,0.12);
  border-radius: 8px; padding: 8px 12px;
  font-size: 11px; color: rgba(255,255,255,0.75);
  font-family: monospace; outline: none;
  min-width: 0;
}
.sm-copy-btn {
  display: flex; align-items: center; gap: 5px;
  padding: 8px 14px; border-radius: 8px; border: none;
  background: #4A6CF7; color: #fff; font-size: 12px; font-weight: 600;
  cursor: pointer; white-space: nowrap; transition: background .2s;
  flex-shrink: 0;
}
.sm-copy-btn.copied { background: #10b981; }
.sm-copy-btn:hover  { opacity: .85; }

.sm-result-meta { display: flex; gap: 8px; flex-wrap: wrap; justify-content: center; }
.sm-meta-tag {
  display: flex; align-items: center; gap: 5px;
  padding: 3px 10px; border-radius: 20px; font-size: 11px; font-weight: 600;
}
.perm-view  { background: rgba(74,108,247,0.15); color: #7a9cf9;  border: 1px solid rgba(74,108,247,0.25); }
.perm-edit  { background: rgba(245,158,11,0.12); color: #fbbf24;  border: 1px solid rgba(245,158,11,0.22); }
.expiry     { background: rgba(255,255,255,0.06); color: rgba(255,255,255,0.5); border: 1px solid rgba(255,255,255,0.1); }

.sm-new-btn {
  background: none; border: 1px solid rgba(255,255,255,0.12);
  color: rgba(255,255,255,0.5); border-radius: 8px;
  padding: 6px 14px; font-size: 11px; cursor: pointer;
  display: flex; align-items: center; gap: 5px;
  transition: border-color .15s, color .15s;
}
.sm-new-btn:hover { border-color: rgba(255,255,255,0.3); color: rgba(255,255,255,0.8); }

/* Form */
.sm-field { display: flex; flex-direction: column; gap: 8px; }
.sm-label { font-size: 11px; font-weight: 600; color: rgba(255,255,255,0.55); letter-spacing: .05em; text-transform: uppercase; }

/* Permission buttons */
.sm-btn-group { display: flex; gap: 8px; }
.sm-perm-btn {
  flex: 1; display: flex; flex-direction: column; align-items: center;
  gap: 4px; padding: 12px 10px; border-radius: 10px;
  background: rgba(255,255,255,0.04); border: 1.5px solid rgba(255,255,255,0.08);
  color: rgba(255,255,255,0.55); cursor: pointer; transition: all .2s;
}
.sm-perm-btn i   { font-size: 16px; }
.sm-perm-btn span { font-size: 12px; font-weight: 600; }
.sm-perm-btn small { font-size: 9px; opacity: .6; }
.sm-perm-btn:hover { border-color: rgba(74,108,247,0.4); color: rgba(255,255,255,0.8); }
.sm-perm-btn.active { border-color: #4A6CF7; background: rgba(74,108,247,0.12); color: #fff; }

/* Expiry grid */
.sm-expiry-grid {
  display: grid; grid-template-columns: repeat(3, 1fr); gap: 6px;
}
.sm-expiry-btn {
  padding: 7px 4px; border-radius: 8px; font-size: 11px; font-weight: 600;
  background: rgba(255,255,255,0.04); border: 1px solid rgba(255,255,255,0.08);
  color: rgba(255,255,255,0.55); cursor: pointer; transition: all .15s;
  text-align: center;
}
.sm-expiry-btn:hover  { border-color: rgba(255,255,255,0.2); color: rgba(255,255,255,0.85); }
.sm-expiry-btn.active { border-color: #4A6CF7; background: rgba(74,108,247,0.12); color: #fff; }

/* Custom date */
.sm-custom-date {
  position: relative; margin-top: 4px;
}
.sm-date-icon {
  position: absolute; left: 10px; top: 50%; transform: translateY(-50%);
  font-size: 12px; color: rgba(255,255,255,0.3); pointer-events: none;
}
.sm-date-input {
  width: 100%; padding: 8px 10px 8px 32px; border-radius: 8px;
  background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1);
  color: rgba(255,255,255,0.8); font-size: 12px; outline: none;
  transition: border-color .2s;
  color-scheme: dark;
}
.sm-date-input:focus { border-color: #4A6CF7; }

/* Existing shares */
.sm-existing-header {
  display: flex; align-items: center; justify-content: space-between;
  font-size: 11px; font-weight: 600; color: rgba(255,255,255,0.4);
  letter-spacing: .05em; text-transform: uppercase;
  padding-bottom: 8px;
  border-bottom: 1px solid rgba(255,255,255,0.06);
}
.sm-share-list { display: flex; flex-direction: column; gap: 4px; margin-top: 6px; }
.sm-share-item {
  display: flex; align-items: center; justify-content: space-between;
  padding: 8px 10px; border-radius: 8px;
  background: rgba(255,255,255,0.03); border: 1px solid rgba(255,255,255,0.06);
  transition: background .15s;
}
.sm-share-item:hover { background: rgba(255,255,255,0.06); }
.sm-share-item.expired { opacity: .45; }

.sm-share-info { display: flex; align-items: center; gap: 10px; min-width: 0; }
.sm-share-perm {
  width: 26px; height: 26px; border-radius: 6px;
  display: flex; align-items: center; justify-content: center;
  font-size: 11px; flex-shrink: 0;
}
.sm-share-perm.perm-view { background: rgba(74,108,247,0.15); color: #7a9cf9; }
.sm-share-perm.perm-edit { background: rgba(245,158,11,0.12); color: #fbbf24; }

.sm-share-details { display: flex; flex-direction: column; gap: 1px; min-width: 0; }
.sm-share-token {
  font-size: 11px; font-family: monospace; color: rgba(255,255,255,0.7);
}
.sm-share-expiry {
  font-size: 9px; color: rgba(255,255,255,0.35);
}
.sm-share-expiry.expired { color: #f87171; }

.sm-share-actions { display: flex; gap: 4px; flex-shrink: 0; }
.sm-icon-btn {
  width: 26px; height: 26px; border-radius: 6px; border: none;
  background: rgba(255,255,255,0.06); color: rgba(255,255,255,0.5);
  cursor: pointer; display: flex; align-items: center; justify-content: center;
  font-size: 11px; transition: background .15s, color .15s;
}
.sm-icon-btn:hover { background: rgba(255,255,255,0.12); color: #fff; }
.sm-icon-btn.danger:hover { background: rgba(239,68,68,0.15); color: #f87171; }

.sm-no-shares {
  display: flex; flex-direction: column; align-items: center; gap: 8px;
  padding: 20px 0; color: rgba(255,255,255,0.3); font-size: 11px;
}
.sm-spin { background: none; border: none; color: rgba(255,255,255,0.4); }

/* Footer */
.sm-footer {
  display: flex; gap: 10px; justify-content: flex-end;
  padding: 14px 22px 18px;
  border-top: 1px solid rgba(255,255,255,0.07);
  flex-shrink: 0;
}
.sm-btn-ghost {
  padding: 8px 18px; border-radius: 8px; font-size: 13px; font-weight: 600;
  background: rgba(255,255,255,0.06); border: 1px solid rgba(255,255,255,0.1);
  color: rgba(255,255,255,0.6); cursor: pointer; transition: opacity .15s;
}
.sm-btn-ghost:hover { opacity: .8; }
.sm-btn-primary {
  padding: 8px 20px; border-radius: 8px; font-size: 13px; font-weight: 600;
  background: #4A6CF7; border: none; color: #fff; cursor: pointer;
  display: flex; align-items: center; gap: 7px; transition: opacity .15s;
}
.sm-btn-primary.full { flex: 1; justify-content: center; }
.sm-btn-primary:hover:not(:disabled) { opacity: .85; }
.sm-btn-primary:disabled { opacity: .4; cursor: not-allowed; }

/* Transitions */
.sm-fade-enter-active,
.sm-fade-leave-active { transition: opacity .22s, transform .22s; }
.sm-fade-enter-from,
.sm-fade-leave-to { opacity: 0; }
.sm-fade-enter-from .sm-card,
.sm-fade-leave-to  .sm-card  { transform: scale(.95) translateY(6px); }

.sm-result-slide-enter-active { transition: all .3s ease; }
.sm-result-slide-enter-from   { opacity: 0; transform: translateY(-8px); }
</style>
