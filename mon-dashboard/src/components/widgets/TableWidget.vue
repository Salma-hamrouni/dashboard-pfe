<template>
  <div class="tbl-widget">
    <!-- Empty -->
    <div v-if="!hasData" class="tbl-empty">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" width="28" height="28">
        <rect x="3" y="3" width="18" height="18" rx="2"/>
        <line x1="3" y1="9" x2="21" y2="9"/><line x1="3" y1="15" x2="21" y2="15"/>
        <line x1="9" y1="9" x2="9" y2="21"/><line x1="15" y1="9" x2="15" y2="21"/>
      </svg>
      <span>Aucune donnée</span>
    </div>

    <template v-else>
      <!-- Search -->
      <div class="tbl-toolbar" v-if="showSearch !== false">
        <input
          v-model="search"
          class="tbl-search"
          placeholder="Rechercher…"
          @input="currentPage = 1"
        />
        <span class="tbl-count">{{ filtered.length }} ligne(s)</span>
      </div>

      <!-- Table -->
      <div class="tbl-scroll">
        <table class="tbl-table">
          <thead>
            <tr>
              <th
                v-for="(col, ci) in columns"
                :key="ci"
                class="tbl-th"
                :class="{ sorted: sortCol === ci }"
                @click="toggleSort(ci)"
              >
                {{ col }}
                <span class="sort-icon">
                  {{ sortCol === ci ? (sortAsc ? '↑' : '↓') : '↕' }}
                </span>
              </th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="(row, ri) in paginated"
              :key="ri"
              class="tbl-tr"
              :class="{ 'tbl-tr-even': ri % 2 === 0 }"
            >
              <td
                v-for="(cell, ci) in row"
                :key="ci"
                class="tbl-td"
                :class="{ 'tbl-td-num': isNumeric(cell) }"
              >
                <span
                  v-if="isBadge(cell)"
                  class="tbl-badge"
                  :class="badgeClass(cell)"
                >{{ cell }}</span>
                <span v-else>{{ fmtCell(cell) }}</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pagination -->
      <div v-if="totalPages > 1" class="tbl-pagination">
        <button class="pg-btn" :disabled="currentPage === 1" @click="currentPage--">‹</button>
        <span class="pg-info">{{ currentPage }} / {{ totalPages }}</span>
        <button class="pg-btn" :disabled="currentPage === totalPages" @click="currentPage++">›</button>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'

const props = defineProps<{
  columns?:    string[]
  rows?:       (string | number)[][]
  pageSize?:   number
  showSearch?: boolean
}>()

const search      = ref('')
const sortCol     = ref<number | null>(null)
const sortAsc     = ref(true)
const currentPage = ref(1)
const perPage     = computed(() => Math.max(1, props.pageSize ?? 10))

const hasData = computed(() =>
  (props.columns?.length ?? 0) > 0 && (props.rows?.length ?? 0) > 0
)

const columns = computed(() => props.columns ?? [])

// Filter rows
const filtered = computed(() => {
  const rows = props.rows ?? []
  const q    = search.value.trim().toLowerCase()
  if (!q) return rows
  return rows.filter(row => row.some(cell => String(cell).toLowerCase().includes(q)))
})

// Sort
const sorted = computed(() => {
  if (sortCol.value === null) return filtered.value
  return [...filtered.value].sort((a, b) => {
    const va = a[sortCol.value!]
    const vb = b[sortCol.value!]
    const na = Number(va), nb = Number(vb)
    const cmp = (!isNaN(na) && !isNaN(nb))
      ? na - nb
      : String(va).localeCompare(String(vb), 'fr')
    return sortAsc.value ? cmp : -cmp
  })
})

// Paginate
const totalPages = computed(() => Math.max(1, Math.ceil(sorted.value.length / perPage.value)))
const paginated  = computed(() => {
  const start = (currentPage.value - 1) * perPage.value
  return sorted.value.slice(start, start + perPage.value)
})

function toggleSort(ci: number) {
  if (sortCol.value === ci) sortAsc.value = !sortAsc.value
  else { sortCol.value = ci; sortAsc.value = true }
  currentPage.value = 1
}

function isNumeric(v: string | number) {
  return typeof v === 'number' || (!isNaN(Number(v)) && String(v).trim() !== '')
}

const BADGES = ['placed', 'not placed', 'yes', 'no', 'true', 'false', 'active', 'inactive']
function isBadge(v: string | number) {
  return BADGES.includes(String(v).toLowerCase())
}

function badgeClass(v: string | number) {
  const s = String(v).toLowerCase()
  if (['placed', 'yes', 'true', 'active'].includes(s)) return 'badge-green'
  if (['not placed', 'no', 'false', 'inactive'].includes(s)) return 'badge-red'
  return 'badge-gray'
}

function fmtCell(v: string | number) {
  if (typeof v === 'number') {
    return Number.isInteger(v) ? v.toString() : v.toFixed(2)
  }
  return v
}
</script>

<style scoped>
.tbl-widget {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-size: 11px;
  min-height: 0;
}

.tbl-empty {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  color: var(--color-text-muted);
  font-size: var(--text-xs);
}

/* Toolbar */
.tbl-toolbar {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.tbl-search {
  flex: 1;
  background: rgba(27,107,58,0.06);
  border: 1px solid rgba(17,23,20,0.10);
  border-radius: 6px;
  padding: 4px 8px;
  font-size: 10px;
  color: var(--color-text);
  outline: none;
  transition: border-color 0.2s;
}
.tbl-search::placeholder { color: #C4CFC7; }
.tbl-search:focus { border-color: var(--color-primary); }

.tbl-count {
  font-size: 9px;
  color: var(--color-text-muted);
  white-space: nowrap;
  flex-shrink: 0;
}

/* Scroll wrapper */
.tbl-scroll {
  flex: 1;
  overflow: auto;
  min-height: 0;
  border-radius: 6px;
  border: 1px solid rgba(255,255,255,0.07);
}

.tbl-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 10px;
}

.tbl-th {
  position: sticky;
  top: 0;
  background: rgba(27,107,58,0.06);
  padding: 5px 8px;
  text-align: left;
  font-weight: 600;
  color: var(--color-text-secondary);
  letter-spacing: 0.04em;
  text-transform: uppercase;
  font-size: 9px;
  cursor: pointer;
  white-space: nowrap;
  border-bottom: 1px solid rgba(17,23,20,0.10);
  user-select: none;
  transition: color 0.15s;
}
.tbl-th:hover { color: var(--color-primary); }
.tbl-th.sorted { color: var(--color-primary); }

.sort-icon {
  margin-left: 4px;
  opacity: 0.5;
  font-size: 8px;
}
.tbl-th.sorted .sort-icon { opacity: 1; }

.tbl-tr { transition: background 0.15s; }
.tbl-tr:hover { background: rgba(255,255,255,0.04); }
.tbl-tr-even { background: rgba(255,255,255,0.015); }

.tbl-td {
  padding: 4px 8px;
  color: var(--color-text-secondary);
  border-bottom: 1px solid rgba(255,255,255,0.04);
  max-width: 140px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.tbl-td-num {
  text-align: right;
  font-variant-numeric: tabular-nums;
  color: var(--color-text);
}

/* Badges */
.tbl-badge {
  display: inline-block;
  padding: 1px 6px;
  border-radius: 10px;
  font-size: 9px;
  font-weight: 600;
  letter-spacing: 0.03em;
}
.badge-green { background: rgba(27,107,58,0.15); color: #1B6B3A; border: 1px solid rgba(27,107,58,0.25); }
.badge-red   { background: rgba(239,68,68,0.15);  color: #f87171; border: 1px solid rgba(239,68,68,0.25); }
.badge-gray  { background: rgba(255,255,255,0.08); color: rgba(255,255,255,0.5); border: 1px solid rgba(255,255,255,0.12); }

/* Pagination */
.tbl-pagination {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  flex-shrink: 0;
}
.pg-btn {
  width: 24px; height: 24px;
  background: rgba(27,107,58,0.06);
  border: 1px solid rgba(17,23,20,0.10);
  border-radius: 6px;
  color: var(--color-text);
  cursor: pointer;
  font-size: 13px;
  display: flex; align-items: center; justify-content: center;
  transition: background 0.15s;
}
.pg-btn:hover:not(:disabled) { background: rgba(255,255,255,0.12); }
.pg-btn:disabled { opacity: 0.3; cursor: not-allowed; }
.pg-info { font-size: 10px; color: var(--color-text-muted); }
</style>




