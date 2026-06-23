<template>
  <AppLayout>
    <div class="dataset-page">

      <div class="page-header">
        <h1 class="page-title">Importer des données</h1>
        <p class="page-subtitle">Uploadez un fichier CSV pour créer un dataset et générer des visualisations</p>
      </div>

      <!-- Upload zone -->
      <div
        class="upload-zone"
        :class="{ 'drag-over': isDragging, 'has-file': selectedFile }"
        @dragover.prevent="isDragging = true"
        @dragleave.prevent="isDragging = false"
        @drop.prevent="onDrop"
        @click="triggerFileInput"
      >
        <input ref="fileInput" type="file" accept=".csv" class="file-input" @change="onFileChange" />

        <div v-if="!selectedFile" class="upload-placeholder">
          <div class="upload-icon">
            <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
              <polyline points="17,8 12,3 7,8"/><line x1="12" y1="3" x2="12" y2="15"/>
            </svg>
          </div>
          <p class="upload-title">Glissez votre fichier CSV ici</p>
          <p class="upload-sub">ou <span class="upload-link">cliquez pour parcourir</span></p>
          <p class="upload-hint">Format accepté : .csv — Taille max : 10 Mo</p>
        </div>

        <div v-else class="file-selected">
          <div class="file-icon">
            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="#1B6B3A" stroke-width="2">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
              <polyline points="14,2 14,8 20,8"/>
            </svg>
          </div>
          <div class="file-info">
            <p class="file-name">{{ selectedFile.name }}</p>
            <p class="file-size">{{ formatSize(selectedFile.size) }}</p>
          </div>
          <button class="file-remove" @click.stop="clearFile">✕</button>
        </div>
      </div>

      <!-- Error -->
      <div v-if="error" class="alert alert-error">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
        </svg>
        {{ error }}
      </div>

      <!-- Upload button -->
      <div class="upload-actions" v-if="selectedFile && !uploadResult">
        <button class="btn-upload" :disabled="isLoading" @click="uploadFile">
          <span v-if="!isLoading">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
              <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
              <polyline points="17,8 12,3 7,8"/><line x1="12" y1="3" x2="12" y2="15"/>
            </svg>
            Uploader le dataset
          </span>
          <span v-else class="spinner-row"><span class="spinner"></span> Upload en cours…</span>
        </button>
      </div>

      <!-- Result -->
      <div v-if="uploadResult" class="result-section">
        <div class="result-header">
          <div class="result-badge">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#1B6B3A" stroke-width="2.5">
              <polyline points="20,6 9,17 4,12"/>
            </svg>
            Upload réussi
          </div>
          <p class="result-info">
            <strong>{{ uploadResult.totalRows }}</strong> lignes —
            <strong>{{ uploadResult.columns.length }}</strong> colonnes
          </p>
        </div>

        <div class="columns-section">
          <h3 class="section-title">Colonnes détectées</h3>
          <div class="columns-grid">
            <div v-for="col in uploadResult.columns" :key="col.name" class="col-chip">
              <span class="col-dot" :class="`type-${col.type}`"></span>
              {{ col.name }}
              <span class="col-type">{{ col.type }}</span>
            </div>
          </div>
        </div>

        <div class="preview-section">
          <h3 class="section-title">Aperçu (5 premières lignes)</h3>
          <div class="table-wrap">
            <table class="preview-table">
              <thead>
                <tr><th v-for="col in uploadResult.columns" :key="col.name">{{ col.name }}</th></tr>
              </thead>
              <tbody>
                <tr v-for="(row, i) in uploadResult.preview" :key="i">
                  <td v-for="col in uploadResult.columns" :key="col.name">
                    {{ (row as Record<string,string>)[col.name] ?? '—' }}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div class="result-actions">
          <button class="btn-secondary" @click="reset">Importer un autre fichier</button>
          <button class="btn-primary" @click="goToBuilder">
            Créer un dashboard →
          </button>
        </div>
      </div>

    </div>
  </AppLayout>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import AppLayout from '@/components/layout/AppLayout.vue'
import { useDatasetStore } from '@/stores/dataset'
import type { UploadResponse } from '@/services/datasetService'

const router = useRouter()
const datasetStore = useDatasetStore()

const fileInput = ref<HTMLInputElement | null>(null)
const selectedFile = ref<File | null>(null)
const isDragging = ref(false)
const isLoading = ref(false)
const error = ref('')
const uploadResult = ref<UploadResponse | null>(null)

function triggerFileInput() {
  if (!uploadResult.value) fileInput.value?.click()
}
function onFileChange(e: Event) {
  const f = (e.target as HTMLInputElement).files?.[0]
  if (f) setFile(f)
}
function onDrop(e: DragEvent) {
  isDragging.value = false
  const f = e.dataTransfer?.files[0]
  if (f) setFile(f)
}
function setFile(file: File) {
  if (!file.name.endsWith('.csv')) { error.value = 'Seuls les fichiers .csv sont acceptés.'; return }
  error.value = ''
  selectedFile.value = file
}
function clearFile() {
  selectedFile.value = null
  error.value = ''
  if (fileInput.value) fileInput.value.value = ''
}
function formatSize(b: number) {
  if (b < 1024) return `${b} o`
  if (b < 1048576) return `${(b / 1024).toFixed(1)} Ko`
  return `${(b / 1048576).toFixed(1)} Mo`
}
async function uploadFile() {
  if (!selectedFile.value) return
  isLoading.value = true
  error.value = ''
  try {
    uploadResult.value = await datasetStore.uploadDataset(selectedFile.value)
  } catch (err: unknown) {
    error.value = err instanceof Error ? err.message : "Erreur lors de l'upload."
  } finally {
    isLoading.value = false
  }
}

function goToBuilder() {
  if (!uploadResult.value) return
  datasetStore.setDataset(
    uploadResult.value.name,
    uploadResult.value.columns,
    uploadResult.value.preview as Record<string, string>[],
    uploadResult.value.id,
  )
  router.push('/builder')
}

function reset() {
  selectedFile.value = null
  uploadResult.value = null
  error.value = ''
  if (fileInput.value) fileInput.value.value = ''
}
</script>

<style scoped>
.dataset-page { padding: 2rem 2.5rem; max-width: 900px; font-family: 'Segoe UI', system-ui, sans-serif; color: #e2e8f0; }
.page-header { margin-bottom: 2rem; }
.page-title { font-size: 1.6rem; font-weight: 700; color: #f0fdf4; margin: 0 0 .35rem; letter-spacing: -.02em; }
.page-subtitle { font-size: .9rem; color: #6b7280; margin: 0; }

.upload-zone {
  border: 2px dashed rgba(27,107,58,.3); border-radius: 16px; padding: 3rem 2rem;
  text-align: center; cursor: pointer; transition: all .2s; background: rgba(27,107,58,.03); margin-bottom: 1.25rem;
}
.upload-zone:hover, .upload-zone.drag-over { border-color: rgba(27,107,58,.7); background: rgba(27,107,58,.07); }
.upload-zone.has-file { cursor: default; }
.file-input { display: none; }
.upload-icon { margin-bottom: 1rem; color: rgba(27,107,58,.6); }
.upload-title { font-size: 1rem; font-weight: 600; color: #C8D8CC; margin: 0 0 .4rem; }
.upload-sub { font-size: .9rem; color: #6b7280; margin: 0 0 .6rem; }
.upload-link { color: #1B6B3A; text-decoration: underline; }
.upload-hint { font-size: .78rem; color: #4b5563; margin: 0; }
.file-selected { display: flex; align-items: center; gap: 1rem; justify-content: center; }
.file-info { text-align: left; }
.file-name { font-weight: 600; color: #C8D8CC; margin: 0 0 .2rem; }
.file-size { font-size: .8rem; color: #6b7280; margin: 0; }
.file-remove { margin-left: auto; background: rgba(239,68,68,.15); border: 1px solid rgba(239,68,68,.3); color: #f87171; border-radius: 8px; padding: .35rem .6rem; cursor: pointer; font-size: .85rem; transition: all .15s; }
.file-remove:hover { background: rgba(239,68,68,.25); }

.alert { display: flex; align-items: center; gap: .6rem; padding: .85rem 1rem; border-radius: 10px; font-size: .88rem; margin-bottom: 1rem; }
.alert-error { background: rgba(239,68,68,.1); border: 1px solid rgba(239,68,68,.25); color: #fca5a5; }

.upload-actions { margin-bottom: 2rem; }
.btn-upload { display: inline-flex; align-items: center; gap: .6rem; background: linear-gradient(135deg, #134E2A, #1B6B3A); color: #fff; border: none; border-radius: 10px; padding: .75rem 1.75rem; font-size: .95rem; font-weight: 600; cursor: pointer; transition: opacity .2s; }
.btn-upload:disabled { opacity: .6; cursor: not-allowed; }
.spinner-row { display: flex; align-items: center; gap: .5rem; }
.spinner { width: 16px; height: 16px; border: 2px solid rgba(255,255,255,.3); border-top-color: #fff; border-radius: 50%; animation: spin .7s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }

.result-section { margin-top: 1rem; }
.result-header { display: flex; align-items: center; gap: 1.25rem; margin-bottom: 1.75rem; flex-wrap: wrap; }
.result-badge { display: inline-flex; align-items: center; gap: .45rem; background: rgba(27,107,58,.12); border: 1px solid rgba(27,107,58,.3); color: #1B6B3A; border-radius: 20px; padding: .4rem .9rem; font-size: .85rem; font-weight: 600; }
.result-info { font-size: .9rem; color: #9ca3af; margin: 0; }
.result-info strong { color: #C8D8CC; }

.section-title { font-size: .75rem; font-weight: 600; color: #6b7280; letter-spacing: .08em; text-transform: uppercase; margin: 0 0 .75rem; }
.columns-section { margin-bottom: 1.75rem; }
.columns-grid { display: flex; flex-wrap: wrap; gap: .5rem; }
.col-chip { display: inline-flex; align-items: center; gap: .4rem; background: rgba(255,255,255,.05); border: 1px solid rgba(255,255,255,.1); border-radius: 20px; padding: .3rem .8rem; font-size: .82rem; color: #C8D8CC; }
.col-dot { width: 6px; height: 6px; border-radius: 50%; background: #1B6B3A; }

.preview-section { margin-bottom: 2rem; }
.table-wrap { overflow-x: auto; border-radius: 10px; border: 1px solid rgba(255,255,255,.08); }
.preview-table { width: 100%; border-collapse: collapse; font-size: .83rem; }
.preview-table th { background: rgba(27,107,58,.08); color: #1B6B3A; padding: .65rem 1rem; text-align: left; font-weight: 600; border-bottom: 1px solid rgba(255,255,255,.08); white-space: nowrap; }
.preview-table td { padding: .55rem 1rem; color: #cbd5e1; border-bottom: 1px solid rgba(255,255,255,.05); white-space: nowrap; }
.preview-table tr:last-child td { border-bottom: none; }

.result-actions { display: flex; gap: 1rem; flex-wrap: wrap; }
.btn-secondary { background: rgba(255,255,255,.06); border: 1px solid rgba(255,255,255,.12); color: #C8D8CC; border-radius: 10px; padding: .7rem 1.4rem; font-size: .9rem; cursor: pointer; transition: all .15s; }
.btn-secondary:hover { background: rgba(255,255,255,.1); }
.btn-primary { background: linear-gradient(135deg, #134E2A, #1B6B3A); border: none; color: #fff; border-radius: 10px; padding: .7rem 1.6rem; font-size: .9rem; font-weight: 600; cursor: pointer; transition: opacity .15s; }
.btn-primary:hover { opacity: .9; }
</style>


