import { defineStore } from 'pinia'
import { ref } from 'vue'
import { datasetService, type DatasetItem, type ColumnProfile, type UploadResponse } from '@/services/datasetService'

export type { DatasetItem, ColumnProfile }

// ── Mesure calculée (champ virtuel défini par l'utilisateur) ─────────────────
export interface CustomMeasure {
  id:          string   // identifiant unique (ex: "cm_1717123456789")
  name:        string   // nom affiché / utilisé comme "nom de colonne" dans les widgets
  formula:     'simple' | 'multiply' | 'ratio' | 'difference'
  col1:        string   // colonne A
  col2?:       string   // colonne B  (ratio / difference)
  constant?:   number   // multiplicateur  (multiply)
  aggregation: 'sum' | 'avg' | 'count' | 'max' | 'min'
  pct?:        boolean  // ratio × 100  (affichage en %)
}

// Dataset local chargé depuis un CSV côté browser (avant upload ou en mode offline)
export interface LocalDataset {
  fileName:       string
  columns:        ColumnProfile[]
  data:           Record<string, string>[]
  sourceId?:      number            // id datasource backend si déjà uploadé
  customMeasures: CustomMeasure[]   // mesures calculées définies par l'utilisateur
}

export const useDatasetStore = defineStore('dataset', () => {
  const datasets       = ref<DatasetItem[]>([])
  const currentDataset = ref<LocalDataset | null>(null)
  const isLoading      = ref(false)
  const error          = ref<string | null>(null)

  // ── Chargement depuis le backend ────────────────────────────────────────
  async function fetchDatasets() {
    isLoading.value = true
    error.value = null
    try {
      datasets.value = await datasetService.getAll()
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Erreur lors du chargement'
      datasets.value = []
    } finally {
      isLoading.value = false
    }
  }

  // ── Upload CSV vers le backend ───────────────────────────────────────────
  async function uploadDataset(file: File): Promise<UploadResponse> {
    isLoading.value = true
    error.value = null
    try {
      const result = await datasetService.upload(file)
      await fetchDatasets()
      return result
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : "Erreur lors de l'upload"
      throw err
    } finally {
      isLoading.value = false
    }
  }

  // ── Définit un dataset CSV local (BuilderView — chargement côté browser) ─
  function setDataset(
    fileName: string,
    columns: ColumnProfile[],
    data: Record<string, string>[],
    sourceId?: number,
  ) {
    currentDataset.value = { fileName, columns, data, sourceId, customMeasures: [] }
  }

  // ── Gestion des mesures calculées ────────────────────────────────────────
  function addCustomMeasure(measure: CustomMeasure) {
    if (!currentDataset.value) return
    currentDataset.value.customMeasures.push(measure)
  }

  function removeCustomMeasure(id: string) {
    if (!currentDataset.value) return
    const idx = currentDataset.value.customMeasures.findIndex(m => m.id === id)
    if (idx !== -1) currentDataset.value.customMeasures.splice(idx, 1)
  }

  // ── Analyse des colonnes ─────────────────────────────────────────────────
  async function analyzeColumns(rows: Record<string, string>[]): Promise<ColumnProfile[]> {
    return datasetService.analyze(rows)
  }

  async function getRecommendations(columns: ColumnProfile[]): Promise<object[]> {
    return datasetService.recommend(columns)
  }

  async function aiAnalyze(rows: object[]): Promise<object> {
    return datasetService.aiAnalyze(rows)
  }

  return {
    datasets,
    currentDataset,
    isLoading,
    error,
    fetchDatasets,
    uploadDataset,
    setDataset,
    addCustomMeasure,
    removeCustomMeasure,
    analyzeColumns,
    getRecommendations,
    aiAnalyze,
  }
})
