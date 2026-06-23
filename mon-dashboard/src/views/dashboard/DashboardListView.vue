<template>
  <AppLayout>
    <div class="dashboard-list">

      <!-- ── Page Header ─────────────────────────────────── -->
      <div class="page-header">
        <div class="header-left">
          <div class="title-row">
            <h1 class="page-title">{{ isViewer ? 'Dashboards partagés' : 'Mes Tableaux de bord' }}</h1>
            <span v-if="!isLoading" class="count-pill">{{ dashboards.length }}</span>
          </div>
          <p class="page-subtitle">{{ isViewer ? 'Dashboards publics accessibles en lecture seule' : 'Créez, visualisez et gérez vos analyses interactives' }}</p>
        </div>
        <button v-if="!isViewer" class="btn-create" @click="openCreateModal">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" width="16" height="16">
            <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
          </svg>
          Nouveau dashboard
        </button>
      </div>

      <!-- ── Stats Bar ───────────────────────────────────── -->
      <div class="stats-bar" v-if="dashboards.length > 0 || !isLoading">
        <div class="stat-item">
          <span class="stat-number">{{ dashboards.length }}</span>
          <span class="stat-label">Total</span>
        </div>
        <div class="stat-sep"></div>
        <div class="stat-item">
          <span class="stat-number green">{{ publishedCount }}</span>
          <span class="stat-label">Publiés</span>
        </div>
        <div class="stat-sep"></div>
        <div class="stat-item">
          <span class="stat-number muted">{{ dashboards.length - publishedCount }}</span>
          <span class="stat-label">Brouillons</span>
        </div>
        <div class="stat-sep"></div>
        <div class="stat-item">
          <span class="stat-number blue">{{ totalWidgets }}</span>
          <span class="stat-label">Widgets</span>
        </div>
      </div>

      <!-- ── Toolbar ─────────────────────────────────────── -->
      <div class="toolbar">
        <div class="search-wrapper">
          <svg class="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="11" cy="11" r="8"/><path d="m21 21-4.35-4.35"/>
          </svg>
          <input
            v-model="searchQuery"
            type="text"
            class="search-input"
            placeholder="Rechercher un dashboard..."
          />
          <button v-if="searchQuery" class="search-clear" @click="searchQuery = ''">✕</button>
        </div>

        <div class="filter-tabs">
          <button
            v-for="tab in tabs"
            :key="tab.value"
            :class="['tab-btn', { active: activeTab === tab.value }]"
            @click="activeTab = tab.value"
          >
            {{ tab.label }}
          </button>
        </div>

        <div class="toolbar-right">
          <button class="filter-toggle-btn" :class="{ active: showFilters }" @click="showFilters = !showFilters">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polygon points="22 3 2 3 10 12.46 10 19 14 21 14 12.46 22 3"/>
            </svg>
            Filtres
            <span v-if="activeFilterCount > 0" class="filter-badge">{{ activeFilterCount }}</span>
          </button>
          <select v-model="sortBy" class="sort-select">
            <option value="date">Plus récents</option>
            <option value="name">Nom A→Z</option>
            <option value="widgets">Nb. widgets</option>
          </select>

          <div class="view-toggle">
            <button :class="['toggle-btn', { active: viewMode === 'grid' }]" @click="viewMode = 'grid'" title="Grille">
              <svg viewBox="0 0 24 24" fill="currentColor" width="15" height="15">
                <rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/>
                <rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/>
              </svg>
            </button>
            <button :class="['toggle-btn', { active: viewMode === 'list' }]" @click="viewMode = 'list'" title="Liste">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="15" height="15">
                <line x1="8" y1="6" x2="21" y2="6"/><line x1="8" y1="12" x2="21" y2="12"/>
                <line x1="8" y1="18" x2="21" y2="18"/>
                <line x1="3" y1="6" x2="3.01" y2="6"/><line x1="3" y1="12" x2="3.01" y2="12"/>
                <line x1="3" y1="18" x2="3.01" y2="18"/>
              </svg>
            </button>
          </div>
        </div>
      </div>

      <!-- ── Panneau Filtres ────────────────────────────────── -->
      <transition name="slide-filters">
        <div v-if="showFilters" class="filter-panel">
          <!-- Date -->
          <div class="fp-group">
            <span class="fp-label">
              <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="3" y="4" width="18" height="18" rx="2"/>
                <line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/>
                <line x1="3" y1="10" x2="21" y2="10"/>
              </svg>
              Date
            </span>
            <input v-model="filterDate.from" type="date" class="fp-input" />
            <span class="fp-sep">→</span>
            <input v-model="filterDate.to" type="date" class="fp-input" />
          </div>

          <!-- Visibilité (liste) -->
          <div class="fp-group">
            <span class="fp-label">
              <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="8" y1="6" x2="21" y2="6"/><line x1="8" y1="12" x2="21" y2="12"/><line x1="8" y1="18" x2="21" y2="18"/>
                <line x1="3" y1="6" x2="3.01" y2="6"/><line x1="3" y1="12" x2="3.01" y2="12"/><line x1="3" y1="18" x2="3.01" y2="18"/>
              </svg>
              Visibilité
            </span>
            <div class="fp-pills">
              <button v-for="opt in visibilityOpts" :key="opt.value"
                class="fp-pill" :class="{ active: filterVisibility === opt.value }"
                @click="filterVisibility = filterVisibility === opt.value ? '' : opt.value">
                {{ opt.label }}
              </button>
            </div>
          </div>

          <!-- Widgets (range) -->
          <div class="fp-group">
            <span class="fp-label">
              <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="2" y="3" width="20" height="14" rx="2"/>
                <line x1="8" y1="21" x2="16" y2="21"/><line x1="12" y1="17" x2="12" y2="21"/>
              </svg>
              Widgets
            </span>
            <input v-model.number="filterWidgets.min" type="number" min="0" class="fp-input fp-num" placeholder="Min" />
            <span class="fp-sep">–</span>
            <input v-model.number="filterWidgets.max" type="number" min="0" class="fp-input fp-num" placeholder="Max" />
          </div>

          <button class="fp-reset" @click="resetFilters">
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polyline points="1 4 1 10 7 10"/><path d="M3.51 15a9 9 0 1 0 .49-4.5"/>
            </svg>
            Réinitialiser
          </button>
        </div>
      </transition>

      <!-- ── Error State ─────────────────────────────────── -->
      <div v-if="storeError" class="alert-error">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="18" height="18">
          <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/>
          <line x1="12" y1="16" x2="12.01" y2="16"/>
        </svg>
        <span>{{ storeError }}</span>
        <button class="retry-btn" @click="dashboardStore.fetchDashboards()">Réessayer</button>
      </div>

      <!-- ── Loading Skeletons ───────────────────────────── -->
      <div v-else-if="isLoading" class="cards-grid">
        <div v-for="i in 6" :key="i" class="dash-card skeleton-card">
          <div class="sk-preview"></div>
          <div class="sk-body">
            <div class="sk-line sm"></div>
            <div class="sk-line lg"></div>
            <div class="sk-line md"></div>
          </div>
          <div class="sk-footer"></div>
        </div>
      </div>

      <!-- ── Hero Empty (first visit, no dashboards) ─────── -->
      <div v-else-if="dashboards.length === 0" class="hero-empty">
        <div class="hero-icon">
          <svg viewBox="0 0 80 80" fill="none" xmlns="http://www.w3.org/2000/svg">
            <rect x="8" y="8" width="28" height="28" rx="5" stroke="#2dd4a0" stroke-width="2"/>
            <rect x="44" y="8" width="28" height="28" rx="5" stroke="#2dd4a0" stroke-width="2" opacity=".5"/>
            <rect x="8" y="44" width="28" height="28" rx="5" stroke="#2dd4a0" stroke-width="2" opacity=".4"/>
            <rect x="44" y="44" width="28" height="28" rx="5" stroke="#2dd4a0" stroke-width="2" opacity=".25"/>
            <line x1="22" y1="18" x2="22" y2="28" stroke="#2dd4a0" stroke-width="2" stroke-linecap="round"/>
            <line x1="17" y1="28" x2="27" y2="28" stroke="#2dd4a0" stroke-width="2" stroke-linecap="round"/>
          </svg>
        </div>
        <h2 class="hero-title">{{ isViewer ? 'Aucun dashboard partagé' : 'Créez votre premier dashboard' }}</h2>
        <p class="hero-sub">{{ isViewer ? 'Aucun dashboard public n\'est disponible pour le moment.' : 'Importez des données, ajoutez des widgets et visualisez vos analyses en quelques clics.' }}</p>
        <button v-if="!isViewer" class="btn-create btn-lg" @click="openCreateModal">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" width="18" height="18">
            <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
          </svg>
          Créer mon premier dashboard
        </button>
      </div>

      <!-- ── No results (search/filter) ─────────────────── -->
      <div v-else-if="filteredDashboards.length === 0" class="empty-state">
        <div class="empty-icon">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" width="40" height="40">
            <circle cx="11" cy="11" r="8"/><path d="m21 21-4.35-4.35"/>
          </svg>
        </div>
        <p class="empty-title">Aucun résultat pour « {{ searchQuery || activeTab }} »</p>
        <p class="empty-sub">Essayez un autre terme ou modifiez le filtre actif</p>
        <button class="btn-ghost" @click="searchQuery = ''; activeTab = 'all'">
          Réinitialiser la recherche
        </button>
      </div>

      <!-- ── Grid View ───────────────────────────────────── -->
      <div v-else-if="viewMode === 'grid'" class="cards-grid">
        <div
          v-for="(dash, index) in filteredDashboards"
          :key="dash.id"
          class="dash-card"
          :style="{ animationDelay: `${index * 50}ms` }"
          @click="goToBuilder(dash.id)"
        >
          <!-- Preview zone -->
          <div class="card-preview" :style="{ background: dash.previewGradient }">
            <div class="preview-chart">
              <div
                v-for="(h, i) in dash.previewBars"
                :key="i"
                class="preview-bar"
                :style="{ height: h + '%', background: dash.accentColor + (i % 2 === 0 ? 'cc' : '66') }"
              ></div>
            </div>
            <div class="preview-overlay"></div>
            <div class="card-badges">
              <span v-if="dash.isPublished" class="badge badge-published">Publié</span>
              <span v-else class="badge badge-draft">Brouillon</span>
            </div>
            <div class="card-accent-dot" :style="{ background: dash.accentColor }"></div>
          </div>

          <!-- Card body -->
          <div class="card-body">
            <div class="card-meta-top">
              <span class="widget-count">
                <svg viewBox="0 0 24 24" fill="currentColor" width="11" height="11">
                  <rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/>
                  <rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/>
                </svg>
                {{ dash.widgetCount }} widget{{ dash.widgetCount !== 1 ? 's' : '' }}
              </span>
              <span class="dash-date">{{ dash.updatedAt }}</span>
            </div>

            <h3 class="card-title" :title="dash.name">{{ dash.name }}</h3>
            <p class="card-desc">{{ dash.description || 'Aucune description' }}</p>

            <!-- Owner (affiché uniquement sur les dashboards publics pour les Viewers) -->
            <div v-if="isViewer && dash.ownerName" class="card-owner">
              <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/>
                <circle cx="12" cy="7" r="4"/>
              </svg>
              <span :title="dash.ownerEmail ?? undefined">{{ dash.ownerName }}</span>
            </div>

            <div v-if="dash.tags.length > 0" class="card-tags">
              <span v-for="tag in dash.tags.slice(0,3)" :key="tag" class="tag">{{ tag }}</span>
            </div>
          </div>

          <!-- Card actions -->
          <div class="card-actions" @click.stop>
            <button class="action-btn" title="Aperçu" @click="goToViewer(dash.id)">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="14" height="14">
                <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                <circle cx="12" cy="12" r="3"/>
              </svg>
            </button>
            <button v-if="!isViewer" class="action-btn" title="Éditer" @click="goToBuilder(dash.id)">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="14" height="14">
                <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
              </svg>
            </button>
            <button v-if="!isViewer" class="action-btn" title="Dupliquer" @click="duplicate(dash)">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="14" height="14">
                <rect x="9" y="9" width="13" height="13" rx="2"/>
                <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/>
              </svg>
            </button>
            <button v-if="!isViewer" class="action-btn action-share" title="Partage" @click="toggleShare(dash)">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="14" height="14">
                <circle cx="18" cy="5" r="3"/><circle cx="6" cy="12" r="3"/><circle cx="18" cy="19" r="3"/>
                <line x1="8.59" y1="13.51" x2="15.42" y2="17.49"/>
                <line x1="15.41" y1="6.51" x2="8.59" y2="10.49"/>
              </svg>
            </button>
            <button v-if="!isViewer" class="action-btn action-delete" title="Supprimer" @click="confirmDelete(dash)">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="14" height="14">
                <polyline points="3 6 5 6 21 6"/>
                <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
                <path d="M10 11v6"/><path d="M14 11v6"/><path d="M9 6V4h6v2"/>
              </svg>
            </button>
          </div>
        </div>
      </div>

      <!-- ── List View ───────────────────────────────────── -->
      <div v-else class="list-view">
        <div class="list-header">
          <span>Nom</span>
          <span>Widgets</span>
          <span>Statut</span>
          <span>Créé le</span>
          <span>Actions</span>
        </div>
        <div
          v-for="(dash, index) in filteredDashboards"
          :key="dash.id"
          class="list-row"
          :style="{ animationDelay: `${index * 35}ms` }"
        >
          <div class="list-name" @click="goToBuilder(dash.id)">
            <div class="list-dot" :style="{ background: dash.accentColor }"></div>
            <div>
              <p class="list-title">{{ dash.name }}</p>
              <p class="list-desc-small">{{ dash.description || '—' }}</p>
            </div>
          </div>
          <span class="list-cell">
            <span class="widget-pill">{{ dash.widgetCount }}</span>
          </span>
          <span class="list-cell">
            <span :class="['badge', dash.isPublished ? 'badge-published' : 'badge-draft']">
              {{ dash.isPublished ? 'Publié' : 'Brouillon' }}
            </span>
          </span>
          <span class="list-cell muted">{{ dash.updatedAt }}</span>
          <div class="list-actions" @click.stop>
            <button class="action-btn icon-only" title="Aperçu" @click="goToViewer(dash.id)">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="13" height="13">
                <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                <circle cx="12" cy="12" r="3"/>
              </svg>
            </button>
            <button v-if="!isViewer" class="action-btn icon-only" title="Éditer" @click="goToBuilder(dash.id)">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="13" height="13">
                <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
              </svg>
            </button>
            <button v-if="!isViewer" class="action-btn icon-only action-delete" title="Supprimer" @click="confirmDelete(dash)">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="13" height="13">
                <polyline points="3 6 5 6 21 6"/>
                <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
                <path d="M10 11v6"/><path d="M14 11v6"/><path d="M9 6V4h6v2"/>
              </svg>
            </button>
          </div>
        </div>
      </div>

    </div>

    <!-- ── Toast ──────────────────────────────────────────── -->
    <Teleport to="body">
      <Transition name="toast">
        <div v-if="toast.show" class="toast" :class="'toast-' + toast.type">
          <svg v-if="toast.type === 'success'" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" width="16" height="16">
            <polyline points="20 6 9 17 4 12"/>
          </svg>
          <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" width="16" height="16">
            <circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/>
          </svg>
          {{ toast.message }}
        </div>
      </Transition>
    </Teleport>

    <!-- ── Create Modal ────────────────────────────────────── -->
    <Teleport to="body">
      <Transition name="modal">
        <div v-if="showCreateModal" class="modal-overlay" @click.self="closeCreateModal">
          <div class="modal">
            <div class="modal-header">
              <div class="modal-title-row">
                <div class="modal-icon-wrap">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="18" height="18">
                    <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
                  </svg>
                </div>
                <h2>Nouveau dashboard</h2>
              </div>
              <button class="modal-close" @click="closeCreateModal">✕</button>
            </div>
            <div class="modal-body">
              <div class="form-group">
                <label>Nom <span class="required">*</span></label>
                <input
                  v-model="newDash.name"
                  type="text"
                  class="form-input"
                  placeholder="Ex : Suivi des ventes Q2 2026"
                  @keydown.enter="createDashboard"
                  autofocus
                />
              </div>
              <div class="form-group">
                <label>Description <span class="opt">(optionnel)</span></label>
                <textarea
                  v-model="newDash.description"
                  class="form-input form-textarea"
                  placeholder="Courte description de ce dashboard…"
                ></textarea>
              </div>
              <div class="form-group">
                <label>Tags <span class="opt">(séparés par virgule)</span></label>
                <input
                  v-model="newDash.tagsRaw"
                  type="text"
                  class="form-input"
                  placeholder="Ex : ventes, finance, Q2"
                />
              </div>
            </div>
            <div class="modal-footer">
              <button class="btn-cancel" @click="closeCreateModal">Annuler</button>
              <button
                class="btn-create"
                :disabled="!newDash.name.trim() || isCreating"
                @click="createDashboard"
              >
                <span v-if="isCreating" class="spinner"></span>
                <span v-else>Créer et ouvrir →</span>
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- ── Delete Confirm Modal ───────────────────────────── -->
    <Teleport to="body">
      <Transition name="modal">
        <div v-if="showDeleteModal" class="modal-overlay" @click.self="showDeleteModal = false">
          <div class="modal modal-sm">
            <div class="modal-header">
              <div class="modal-title-row">
                <div class="modal-icon-wrap danger">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="18" height="18">
                    <polyline points="3 6 5 6 21 6"/>
                    <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
                    <path d="M9 6V4h6v2"/>
                  </svg>
                </div>
                <h2>Supprimer le dashboard</h2>
              </div>
              <button class="modal-close" @click="showDeleteModal = false">✕</button>
            </div>
            <div class="modal-body">
              <p class="delete-warning">
                Vous allez supprimer <strong>« {{ dashToDelete?.name }} »</strong>
                et tous ses widgets. Cette action est <strong>irréversible</strong>.
              </p>
            </div>
            <div class="modal-footer">
              <button class="btn-cancel" @click="showDeleteModal = false">Annuler</button>
              <button class="btn-danger" @click="deleteDashboard">Supprimer définitivement</button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

  </AppLayout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, reactive } from 'vue'
import { useRouter } from 'vue-router'
import AppLayout from '@/components/layout/AppLayout.vue'
import { useDashboardStore } from '@/stores/dashboard'
import { useAuthStore } from '@/stores/auth'
import { dashboardService } from '@/services/dashboardService'
import type { DashboardDetailDto } from '@/services/dashboardService'

const router         = useRouter()
const dashboardStore = useDashboardStore()
const authStore      = useAuthStore()

const isViewer = computed(() => authStore.user?.role === 'Viewer')

// ── Visual palette ────────────────────────────────────
const GRADIENTS = [
  'linear-gradient(135deg, #EEF7F1 0%, #1a4a35 100%)',
  'linear-gradient(135deg, #1a1f2e 0%, #2a3a5c 100%)',
  'linear-gradient(135deg, #1f1a0d 0%, #3d3210 100%)',
  'linear-gradient(135deg, #1f0d1a 0%, #3d1030 100%)',
  'linear-gradient(135deg, #0d1a2b 0%, #1a3a5c 100%)',
  'linear-gradient(135deg, #0d1f1a 0%, #1a3d35 100%)',
  'linear-gradient(135deg, #1a0d2b 0%, #2d1a4a 100%)',
]
const ACCENTS = ['#2dd4a0', '#60a5fa', '#fbbf24', '#f472b6', '#a78bfa', '#1B6B3A', '#fb923c']

// ── Extended type ──────────────────────────────────────
interface DashboardView extends DashboardDetailDto {
  description: string
  tags: string[]
  isPublished: boolean
  updatedAt: string
  previewGradient: string
  previewBars: number[]
  accentColor: string
}

function toView(d: DashboardDetailDto): DashboardView {
  const idx = d.id % GRADIENTS.length
  return {
    ...d,
    description: d.insights?.[0] ?? '',
    tags:        d.columns?.slice(0, 3) ?? [],
    widgetCount: d.widgetCount ?? d.widgets?.length ?? 0,
    isPublished: d.isPublic ?? false,
    updatedAt:   d.createdAt ? new Date(d.createdAt).toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' }) : '—',
    previewGradient: GRADIENTS[idx],
    previewBars:     Array.from({ length: 12 }, (_, i) => 20 + ((d.id * 7 + i * 17) % 65)),
    accentColor:     ACCENTS[idx],
  }
}

// ── State ──────────────────────────────────────────────
const searchQuery    = ref('')
const activeTab      = ref('all')
const sortBy         = ref<'date' | 'name' | 'widgets'>('date')
const viewMode       = ref<'grid' | 'list'>('grid')

// ── Filtres avancés ────────────────────────────────────
const showFilters      = ref(false)
const filterDate       = reactive({ from: '', to: '' })
const filterVisibility = ref('')
const filterWidgets    = reactive({ min: null as number | null, max: null as number | null })

const visibilityOpts = [
  { label: '🌐 Public',   value: 'public'  },
  { label: '🔒 Privé',    value: 'private' },
]

const activeFilterCount = computed(() => [
  filterDate.from, filterDate.to, filterVisibility.value,
  filterWidgets.min, filterWidgets.max,
].filter(v => v !== '' && v !== null).length)

function resetFilters() {
  filterDate.from = ''; filterDate.to = ''
  filterVisibility.value = ''
  filterWidgets.min = null; filterWidgets.max = null
}
const showCreateModal = ref(false)
const showDeleteModal = ref(false)
const dashToDelete    = ref<DashboardView | null>(null)
const newDash         = ref({ name: '', description: '', tagsRaw: '' })
const isCreating      = ref(false)

// Toast
const toast = ref({ show: false, message: '', type: 'success' as 'success' | 'error' })
function showToast(message: string, type: 'success' | 'error' = 'success') {
  toast.value = { show: true, message, type }
  setTimeout(() => { toast.value.show = false }, 3200)
}

const tabs = [
  { label: 'Tous',       value: 'all' },
  { label: 'Publiés',    value: 'published' },
  { label: 'Brouillons', value: 'draft' },
]

// ── Store bindings ─────────────────────────────────────
const isLoading  = computed(() => dashboardStore.isLoading)
const storeError = computed(() => dashboardStore.error)

// ── Computed ───────────────────────────────────────────
const dashboards = computed<DashboardView[]>(() =>
  (dashboardStore.dashboards ?? []).map(toView)
)

const publishedCount = computed(() => dashboards.value.filter(d => d.isPublished).length)
const totalWidgets   = computed(() => dashboards.value.reduce((sum, d) => sum + (d.widgetCount ?? 0), 0))

const filteredDashboards = computed(() => {
  let list = dashboards.value.filter(d => {
    const q = searchQuery.value.toLowerCase()
    const matchSearch = !q ||
      d.name.toLowerCase().includes(q) ||
      d.description.toLowerCase().includes(q) ||
      d.tags.some(t => t.toLowerCase().includes(q))
    const matchTab =
      activeTab.value === 'all' ||
      (activeTab.value === 'published' && d.isPublished) ||
      (activeTab.value === 'draft'     && !d.isPublished)

    // Filtre date
    let matchDate = true
    if (filterDate.from || filterDate.to) {
      const ts   = new Date(d.createdAt).getTime()
      const from = filterDate.from ? new Date(filterDate.from).getTime() : -Infinity
      const to   = filterDate.to   ? new Date(filterDate.to).getTime() + 86_400_000 : Infinity
      matchDate  = ts >= from && ts <= to
    }

    // Filtre visibilité
    const matchVis =
      !filterVisibility.value ||
      (filterVisibility.value === 'public'  &&  d.isPublished) ||
      (filterVisibility.value === 'private' && !d.isPublished)

    // Filtre range widgets
    const wc = d.widgetCount ?? 0
    const matchWidgets =
      (filterWidgets.min === null || wc >= filterWidgets.min) &&
      (filterWidgets.max === null || wc <= filterWidgets.max)

    return matchSearch && matchTab && matchDate && matchVis && matchWidgets
  })

  if (sortBy.value === 'name') {
    list = [...list].sort((a, b) => a.name.localeCompare(b.name))
  } else if (sortBy.value === 'widgets') {
    list = [...list].sort((a, b) => (b.widgetCount ?? 0) - (a.widgetCount ?? 0))
  }
  // 'date' keeps the server order (already ordered by createdAt desc)

  return list
})

// ── Lifecycle ──────────────────────────────────────────
onMounted(async () => {
  if (isViewer.value) {
    // Viewer → charger uniquement les dashboards publics
    try {
      const publicDashes = await dashboardService.getPublic()
      dashboardStore.dashboards = publicDashes
    } catch { /* ignore */ }
  } else {
    // Admin / Editor → charger ses propres dashboards
    dashboardStore.fetchDashboards(true)
  }
})

// ── Navigation ─────────────────────────────────────────
const goToBuilder = (id: number) => router.push(`/builder/${id}`)
const goToViewer  = (id: number) => router.push(`/dashboard/${id}`)

// ── Modal helpers ──────────────────────────────────────
function openCreateModal() {
  newDash.value = { name: '', description: '', tagsRaw: '' }
  showCreateModal.value = true
}
const closeCreateModal = () => { showCreateModal.value = false }

// ── Create ─────────────────────────────────────────────
async function createDashboard() {
  if (!newDash.value.name.trim() || isCreating.value) return
  isCreating.value = true
  try {
    const created = await dashboardStore.createDashboard({
      name:            newDash.value.name.trim(),
      columns:         newDash.value.tagsRaw.split(',').map(t => t.trim()).filter(Boolean),
      insights:        newDash.value.description.trim() ? [newDash.value.description.trim()] : [],
      recommendations: [],
      widgets:         [],
    })
    closeCreateModal()
    showToast(`Dashboard « ${created.name} » créé avec succès`)
    router.push(`/builder/${created.id}`)
  } catch (e: any) {
    showToast(e?.message ?? 'Erreur lors de la création', 'error')
  } finally {
    isCreating.value = false
  }
}

// ── Duplicate ──────────────────────────────────────────
async function duplicate(dash: DashboardView) {
  try {
    await dashboardStore.createDashboard({
      name:            `${dash.name} (copie)`,
      columns:         dash.columns,
      insights:        dash.insights,
      recommendations: dash.recommendations,
      widgets:         [],
    })
    showToast(`Dashboard dupliqué avec succès`)
  } catch {
    showToast('Erreur lors de la duplication', 'error')
  }
}

// ── Share ──────────────────────────────────────────────
async function toggleShare(dash: DashboardView) {
  try {
    const res = await dashboardStore.toggleShare(dash.id, !dash.isPublished)
    // Mise à jour locale — pas besoin de refaire une requête réseau complète
    const idx = dashboardStore.dashboards.findIndex(d => d.id === dash.id)
    if (idx !== -1) {
      dashboardStore.dashboards[idx]!.isPublic   = res.isPublic
      dashboardStore.dashboards[idx]!.shareToken = res.shareToken
    }
    showToast(dash.isPublished ? 'Dashboard dépublié' : 'Dashboard publié et partageable')
  } catch {
    showToast('Erreur lors du partage', 'error')
  }
}

// ── Delete ─────────────────────────────────────────────
const confirmDelete = (dash: DashboardView) => {
  dashToDelete.value  = dash
  showDeleteModal.value = true
}

async function deleteDashboard() {
  if (!dashToDelete.value) return
  const name = dashToDelete.value.name
  await dashboardStore.deleteDashboard(dashToDelete.value.id)
  showDeleteModal.value = false
  dashToDelete.value    = null
  showToast(`« ${name} » supprimé`)
}
</script>

<style scoped>
/* ── Base ─────────────────────────────────────────────── */
.dashboard-list {
  padding: 2rem 2.5rem;
  min-height: 100%;
  font-family: var(--font-sans, 'Inter', sans-serif);
  color: #e2e8f0;
}

/* ── Header ───────────────────────────────────────────── */
.page-header {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  margin-bottom: 1.5rem;
  gap: 1rem;
}

.title-row {
  display: flex;
  align-items: center;
  gap: .65rem;
  margin-bottom: .3rem;
}

.page-title {
  font-size: 1.65rem;
  font-weight: 800;
  color: #f0fdf4;
  margin: 0;
  letter-spacing: -.025em;
}

.count-pill {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: rgba(45, 212, 160, .15);
  color: #2dd4a0;
  border: 1px solid rgba(45, 212, 160, .25);
  border-radius: 20px;
  font-size: .75rem;
  font-weight: 700;
  padding: .1rem .55rem;
  min-width: 24px;
}

.page-subtitle {
  font-size: .855rem;
  color: #6b7280;
  margin: 0;
}

/* ── Stats Bar ────────────────────────────────────────── */
.stats-bar {
  display: flex;
  align-items: center;
  gap: 0;
  background: rgba(255,255,255,.03);
  border: 1px solid rgba(255,255,255,.07);
  border-radius: 14px;
  padding: .85rem 1.5rem;
  margin-bottom: 1.5rem;
  width: fit-content;
}

.stat-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 0 1.5rem;
}

.stat-number {
  font-size: 1.5rem;
  font-weight: 800;
  color: #f0fdf4;
  line-height: 1;
  font-variant-numeric: tabular-nums;
}
.stat-number.green  { color: #2dd4a0; }
.stat-number.muted  { color: #6b7280; }
.stat-number.blue   { color: #60a5fa; }

.stat-label {
  font-size: .72rem;
  color: #6b7280;
  margin-top: .2rem;
  text-transform: uppercase;
  letter-spacing: .06em;
  font-weight: 600;
}

.stat-sep {
  width: 1px;
  height: 32px;
  background: rgba(255,255,255,.07);
  flex-shrink: 0;
}

/* ── Toolbar ──────────────────────────────────────────── */
/* ── Filtre toggle btn ── */
.filter-toggle-btn {
  display: flex; align-items: center; gap: 6px;
  padding: 8px 14px;
  background: var(--color-surface-2);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  color: var(--color-text-muted);
  font-size: 13px; font-weight: 600;
  cursor: pointer; white-space: nowrap;
  transition: all .2s; position: relative;
}
.filter-toggle-btn:hover,
.filter-toggle-btn.active { border-color: var(--color-primary); color: var(--color-primary); }
.filter-badge {
  background: var(--color-primary);
  color: #fff; font-size: 10px; font-weight: 700;
  border-radius: 999px; padding: 1px 6px;
  min-width: 18px; text-align: center;
}

/* ── Filter panel ── */
.filter-panel {
  display: flex; align-items: center; flex-wrap: wrap;
  gap: 12px 24px;
  background: var(--color-surface-2);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: 14px 18px;
  margin-bottom: 1.25rem;
}
.fp-group { display: flex; align-items: center; gap: 8px; }
.fp-label {
  display: flex; align-items: center; gap: 5px;
  font-size: 11px; font-weight: 700;
  color: var(--color-text-muted);
  text-transform: uppercase; letter-spacing: .05em;
  white-space: nowrap;
}
.fp-input {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  color: var(--color-text);
  font-size: 12px; padding: 6px 10px;
  outline: none; transition: border-color .2s;
}
.fp-input:focus { border-color: var(--color-primary); }
.fp-input[type="date"] { width: 136px; color-scheme: dark; }
.fp-num { width: 80px; }
.fp-sep { font-size: 11px; color: var(--color-text-muted); }
.fp-pills { display: flex; gap: 6px; }
.fp-pill {
  padding: 5px 12px; border-radius: 999px;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  color: var(--color-text-muted);
  font-size: 12px; font-weight: 500; cursor: pointer;
  transition: all .2s;
}
.fp-pill:hover { border-color: var(--color-primary); color: var(--color-primary); }
.fp-pill.active { background: var(--color-primary); border-color: var(--color-primary); color: #fff; }
.fp-reset {
  display: flex; align-items: center; gap: 5px;
  margin-left: auto;
  background: none; border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  color: var(--color-text-muted);
  font-size: 12px; padding: 6px 12px; cursor: pointer;
  transition: all .2s;
}
.fp-reset:hover { border-color: #f87171; color: #f87171; }

/* ── Slide transition ── */
.slide-filters-enter-active,
.slide-filters-leave-active { transition: all .25s ease; }
.slide-filters-enter-from,
.slide-filters-leave-to { opacity: 0; transform: translateY(-8px); }

.toolbar {
  display: flex;
  align-items: center;
  gap: .85rem;
  margin-bottom: 1.5rem;
  flex-wrap: wrap;
}

.search-wrapper {
  position: relative;
  flex: 1;
  min-width: 200px;
  max-width: 340px;
}

.search-icon {
  position: absolute;
  left: .85rem;
  top: 50%;
  transform: translateY(-50%);
  width: 14px;
  height: 14px;
  color: #6b7280;
  pointer-events: none;
}

.search-input {
  width: 100%;
  padding: .6rem 2.1rem .6rem 2.35rem;
  background: rgba(255,255,255,.04);
  border: 1px solid rgba(255,255,255,.08);
  border-radius: 10px;
  color: #e2e8f0;
  font-size: .875rem;
  outline: none;
  transition: border-color .2s;
  box-sizing: border-box;
}
.search-input::placeholder { color: #4b5563; }
.search-input:focus { border-color: rgba(45,212,160,.4); box-shadow: 0 0 0 3px rgba(45,212,160,.06); }

.search-clear {
  position: absolute;
  right: .5rem;
  top: 50%;
  transform: translateY(-50%);
  background: none;
  border: none;
  color: #6b7280;
  cursor: pointer;
  font-size: .8rem;
  padding: .2rem .3rem;
  border-radius: 4px;
  line-height: 1;
}
.search-clear:hover { color: #e2e8f0; background: rgba(255,255,255,.06); }

.filter-tabs {
  display: flex;
  background: rgba(255,255,255,.04);
  border: 1px solid rgba(255,255,255,.07);
  border-radius: 10px;
  padding: 3px;
  gap: 2px;
}

.tab-btn {
  padding: .42rem .95rem;
  border: none;
  background: transparent;
  color: #9ca3af;
  border-radius: 7px;
  font-size: .82rem;
  cursor: pointer;
  transition: all .18s;
  white-space: nowrap;
}
.tab-btn.active {
  background: rgba(45, 212, 160, .15);
  color: #2dd4a0;
  font-weight: 600;
}

.toolbar-right {
  display: flex;
  align-items: center;
  gap: .6rem;
  margin-left: auto;
}

.sort-select {
  padding: .48rem .8rem;
  background: rgba(255,255,255,.04);
  border: 1px solid rgba(255,255,255,.08);
  border-radius: 9px;
  color: #9ca3af;
  font-size: .82rem;
  cursor: pointer;
  outline: none;
  appearance: none;
  padding-right: 1.8rem;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' viewBox='0 0 24 24' fill='none' stroke='%236b7280' stroke-width='2'%3E%3Cpolyline points='6 9 12 15 18 9'/%3E%3C/svg%3E");
  background-repeat: no-repeat;
  background-position: right .55rem center;
}
.sort-select option { background: #F5F6F5; }

.view-toggle {
  display: flex;
  background: rgba(255,255,255,.04);
  border: 1px solid rgba(255,255,255,.07);
  border-radius: 9px;
  padding: 3px;
  gap: 2px;
}

.toggle-btn {
  padding: .42rem .55rem;
  border: none;
  background: transparent;
  color: #6b7280;
  border-radius: 6px;
  cursor: pointer;
  display: flex;
  align-items: center;
  transition: all .18s;
}
.toggle-btn.active { background: rgba(45,212,160,.15); color: #2dd4a0; }

/* ── Create Button ────────────────────────────────────── */
.btn-create {
  display: inline-flex;
  align-items: center;
  gap: .5rem;
  padding: .6rem 1.25rem;
  background: linear-gradient(135deg, #2dd4a0, #1B6B3A);
  color: #134E2A;
  border: none;
  border-radius: 10px;
  font-size: .875rem;
  font-weight: 700;
  cursor: pointer;
  transition: opacity .18s, transform .15s, box-shadow .18s;
  white-space: nowrap;
  box-shadow: 0 4px 14px rgba(45, 212, 160, .25);
}
.btn-create:hover { opacity: .92; transform: translateY(-1px); box-shadow: 0 6px 20px rgba(45,212,160,.35); }
.btn-create:active { transform: translateY(0); }
.btn-create:disabled { opacity: .4; cursor: not-allowed; transform: none; box-shadow: none; }
.btn-create.btn-lg { padding: .75rem 1.75rem; font-size: .95rem; }

/* ── Alert Error ──────────────────────────────────────── */
.alert-error {
  display: flex;
  align-items: center;
  gap: .75rem;
  padding: .9rem 1.25rem;
  background: rgba(239, 68, 68, .08);
  border: 1px solid rgba(239, 68, 68, .2);
  border-radius: 10px;
  color: #f87171;
  font-size: .875rem;
  margin-bottom: 1.5rem;
}
.retry-btn {
  margin-left: auto;
  padding: .3rem .8rem;
  background: rgba(239,68,68,.15);
  border: 1px solid rgba(239,68,68,.25);
  border-radius: 6px;
  color: #f87171;
  font-size: .8rem;
  cursor: pointer;
}
.retry-btn:hover { background: rgba(239,68,68,.25); }

/* ── Loading Skeletons ────────────────────────────────── */
.skeleton-card {
  background: rgba(255,255,255,.025);
  border: 1px solid rgba(255,255,255,.05);
  border-radius: 14px;
  overflow: hidden;
  cursor: default;
}
.sk-preview {
  height: 110px;
  background: linear-gradient(90deg, rgba(255,255,255,.04) 25%, rgba(255,255,255,.07) 50%, rgba(255,255,255,.04) 75%);
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
}
.sk-body {
  padding: 1rem;
  display: flex;
  flex-direction: column;
  gap: .55rem;
}
.sk-line {
  height: 10px;
  border-radius: 5px;
  background: linear-gradient(90deg, rgba(255,255,255,.04) 25%, rgba(255,255,255,.07) 50%, rgba(255,255,255,.04) 75%);
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
}
.sk-line.sm { width: 40%; }
.sk-line.md { width: 70%; }
.sk-line.lg { width: 90%; }
.sk-footer {
  height: 44px;
  border-top: 1px solid rgba(255,255,255,.04);
  background: rgba(255,255,255,.015);
}
@keyframes shimmer {
  0%   { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

/* ── Hero Empty ───────────────────────────────────────── */
.hero-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  padding: 5rem 2rem 6rem;
  gap: 1.1rem;
}
.hero-icon {
  width: 100px;
  height: 100px;
  background: rgba(45, 212, 160, .06);
  border: 1px solid rgba(45, 212, 160, .15);
  border-radius: 24px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: .5rem;
}
.hero-title {
  font-size: 1.3rem;
  font-weight: 700;
  color: #f0fdf4;
  margin: 0;
}
.hero-sub {
  font-size: .9rem;
  color: #6b7280;
  margin: 0;
  max-width: 400px;
  line-height: 1.6;
}

/* ── Filter Empty ─────────────────────────────────────── */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  padding: 4rem 2rem;
  gap: .85rem;
}
.empty-icon { color: #374151; }
.empty-title { font-size: 1rem; font-weight: 600; color: #d1d5db; margin: 0; }
.empty-sub { font-size: .85rem; color: #6b7280; margin: 0; }
.btn-ghost {
  padding: .55rem 1.2rem;
  background: transparent;
  border: 1px solid rgba(255,255,255,.1);
  border-radius: 9px;
  color: #9ca3af;
  font-size: .85rem;
  cursor: pointer;
  transition: all .18s;
}
.btn-ghost:hover { background: rgba(255,255,255,.05); color: #e2e8f0; border-color: rgba(255,255,255,.18); }

/* ── Cards Grid ───────────────────────────────────────── */
.cards-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(290px, 1fr));
  gap: 1.1rem;
}

.dash-card {
  background: rgba(255,255,255,.03);
  border: 1px solid rgba(255,255,255,.07);
  border-radius: 14px;
  overflow: hidden;
  cursor: pointer;
  transition: transform .22s, border-color .22s, box-shadow .22s;
  animation: cardIn .35s ease both;
  position: relative;
}
.dash-card:hover {
  transform: translateY(-4px);
  border-color: rgba(45, 212, 160, .28);
  box-shadow: 0 12px 36px rgba(0,0,0,.35), 0 0 0 1px rgba(45,212,160,.08);
}
@keyframes cardIn {
  from { opacity: 0; transform: translateY(14px); }
  to   { opacity: 1; transform: translateY(0); }
}

/* Card Preview */
.card-preview {
  height: 112px;
  position: relative;
  overflow: hidden;
  display: flex;
  align-items: flex-end;
  padding: 0 1rem .8rem;
}
.preview-chart {
  display: flex;
  align-items: flex-end;
  gap: 3px;
  height: 65px;
  width: 100%;
  position: relative;
  z-index: 1;
}
.preview-bar {
  flex: 1;
  border-radius: 3px 3px 0 0;
  min-height: 4px;
}
.preview-overlay {
  position: absolute;
  inset: 0;
  background: linear-gradient(to bottom, transparent 30%, rgba(0,0,0,.45) 100%);
}
.card-badges {
  position: absolute;
  top: .65rem;
  right: .65rem;
  z-index: 2;
}
.card-accent-dot {
  position: absolute;
  top: .65rem;
  left: .65rem;
  width: 8px;
  height: 8px;
  border-radius: 50%;
  z-index: 2;
  box-shadow: 0 0 8px currentColor;
}

/* Card Body */
.card-body { padding: .9rem 1rem .5rem; }

.card-meta-top {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: .5rem;
}
.widget-count {
  display: flex;
  align-items: center;
  gap: .3rem;
  font-size: .72rem;
  color: #6b7280;
}
.dash-date { font-size: .7rem; color: #4b5563; }

.card-title {
  font-size: .92rem;
  font-weight: 700;
  color: #f0fdf4;
  margin: 0 0 .3rem;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.card-owner {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  font-size: .72rem;
  color: var(--color-primary, #1B6B3A);
  background: rgba(27, 107, 58, 0.08);
  border: 1px solid rgba(27, 107, 58, 0.15);
  border-radius: 999px;
  padding: 2px 9px 2px 6px;
  margin: 0 0 .55rem;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.card-owner svg { flex-shrink: 0; color: var(--color-primary, #1B6B3A); }

.card-desc {
  font-size: .78rem;
  color: #6b7280;
  margin: 0 0 .6rem;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  line-height: 1.5;
  font-style: italic;
}
.card-tags { display: flex; flex-wrap: wrap; gap: .3rem; }

/* Card Actions */
.card-actions {
  display: flex;
  gap: .2rem;
  padding: .5rem .65rem .7rem;
  border-top: 1px solid rgba(255,255,255,.05);
  justify-content: flex-end;
}

/* ── Badges ───────────────────────────────────────────── */
.badge {
  font-size: .65rem;
  font-weight: 700;
  padding: .18rem .5rem;
  border-radius: 20px;
  letter-spacing: .04em;
  text-transform: uppercase;
}
.badge-published {
  background: rgba(45, 212, 160, .15);
  color: #2dd4a0;
  border: 1px solid rgba(45, 212, 160, .25);
}
.badge-draft {
  background: rgba(107, 114, 128, .12);
  color: #9ca3af;
  border: 1px solid rgba(107, 114, 128, .18);
}
.tag {
  font-size: .68rem;
  padding: .12rem .48rem;
  background: rgba(255,255,255,.05);
  border: 1px solid rgba(255,255,255,.07);
  border-radius: 20px;
  color: #9ca3af;
}

/* ── Action Buttons ───────────────────────────────────── */
.action-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: .25rem;
  padding: .35rem .55rem;
  background: rgba(255,255,255,.04);
  border: 1px solid rgba(255,255,255,.07);
  border-radius: 7px;
  color: #6b7280;
  font-size: .72rem;
  cursor: pointer;
  transition: all .15s;
}
.action-btn:hover { background: rgba(255,255,255,.09); color: #e2e8f0; border-color: rgba(255,255,255,.12); }
.action-btn.icon-only { padding: .35rem .45rem; }
.action-share:hover { background: rgba(96,165,250,.1); color: #60a5fa; border-color: rgba(96,165,250,.2); }
.action-delete:hover { background: rgba(239,68,68,.1); color: #f87171; border-color: rgba(239,68,68,.2); }

/* ── Widget Pill ──────────────────────────────────────── */
.widget-pill {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: rgba(255,255,255,.06);
  border-radius: 20px;
  padding: .1rem .5rem;
  font-size: .75rem;
  color: #9ca3af;
  min-width: 22px;
}

/* ── List View ────────────────────────────────────────── */
.list-view {
  border: 1px solid rgba(255,255,255,.07);
  border-radius: 14px;
  overflow: hidden;
}
.list-header {
  display: grid;
  grid-template-columns: 2.5fr 1fr 1fr 1fr 1fr;
  gap: 1rem;
  padding: .7rem 1.25rem;
  background: rgba(255,255,255,.03);
  font-size: .72rem;
  color: #6b7280;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: .06em;
}
.list-row {
  display: grid;
  grid-template-columns: 2.5fr 1fr 1fr 1fr 1fr;
  gap: 1rem;
  align-items: center;
  padding: .85rem 1.25rem;
  border-top: 1px solid rgba(255,255,255,.04);
  animation: cardIn .28s ease both;
  transition: background .15s;
}
.list-row:hover { background: rgba(255,255,255,.02); }
.list-name {
  display: flex;
  align-items: center;
  gap: .7rem;
  cursor: pointer;
}
.list-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  flex-shrink: 0;
}
.list-title {
  font-size: .875rem;
  font-weight: 600;
  color: #f0fdf4;
  margin: 0 0 .12rem;
}
.list-desc-small {
  font-size: .73rem;
  color: #6b7280;
  margin: 0;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 240px;
  font-style: italic;
}
.list-cell { font-size: .82rem; color: #9ca3af; }
.muted { color: #4b5563 !important; }
.list-actions { display: flex; gap: .25rem; }

/* ── Toast ────────────────────────────────────────────── */
.toast {
  position: fixed;
  bottom: 2rem;
  right: 2rem;
  display: flex;
  align-items: center;
  gap: .6rem;
  padding: .75rem 1.1rem;
  border-radius: 10px;
  font-size: .875rem;
  font-weight: 500;
  z-index: 9999;
  box-shadow: 0 8px 32px rgba(0,0,0,.4);
  backdrop-filter: blur(8px);
}
.toast-success {
  background: rgba(16, 40, 28, .95);
  border: 1px solid rgba(45, 212, 160, .3);
  color: #2dd4a0;
}
.toast-error {
  background: rgba(40, 16, 16, .95);
  border: 1px solid rgba(239, 68, 68, .3);
  color: #f87171;
}
.toast-enter-from { opacity: 0; transform: translateY(12px); }
.toast-enter-active { transition: all .25s ease; }
.toast-leave-to { opacity: 0; transform: translateY(8px); }
.toast-leave-active { transition: all .2s ease; }

/* ── Modal ────────────────────────────────────────────── */
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0,0,0,.72);
  backdrop-filter: blur(6px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}
.modal-enter-from .modal,
.modal-leave-to .modal { opacity: 0; transform: translateY(18px) scale(.97); }
.modal-enter-active .modal,
.modal-leave-active .modal { transition: all .22s ease; }
.modal-enter-from, .modal-leave-to { opacity: 0; }
.modal-enter-active, .modal-leave-active { transition: opacity .22s ease; }

.modal {
  background: #F0F7F2;
  border: 1px solid rgba(45, 212, 160, .18);
  border-radius: 18px;
  width: 100%;
  max-width: 490px;
  margin: 1rem;
  box-shadow: 0 24px 60px rgba(0,0,0,.5);
}
.modal-sm { max-width: 390px; }

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1.3rem 1.5rem 0;
}
.modal-title-row {
  display: flex;
  align-items: center;
  gap: .75rem;
}
.modal-icon-wrap {
  width: 36px;
  height: 36px;
  background: rgba(45, 212, 160, .12);
  border: 1px solid rgba(45, 212, 160, .2);
  border-radius: 9px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #2dd4a0;
  flex-shrink: 0;
}
.modal-icon-wrap.danger {
  background: rgba(239, 68, 68, .1);
  border-color: rgba(239, 68, 68, .2);
  color: #f87171;
}
.modal-header h2 {
  font-size: 1rem;
  font-weight: 700;
  color: #f0fdf4;
  margin: 0;
}
.modal-close {
  background: none;
  border: none;
  color: #6b7280;
  font-size: 1rem;
  cursor: pointer;
  padding: .3rem .45rem;
  border-radius: 6px;
  transition: all .15s;
}
.modal-close:hover { background: rgba(255,255,255,.07); color: #e2e8f0; }

.modal-body { padding: 1.25rem 1.5rem; }
.form-group { margin-bottom: 1.1rem; }
.form-group label {
  display: block;
  font-size: .8rem;
  color: #9ca3af;
  margin-bottom: .4rem;
  font-weight: 500;
}
.required { color: #f87171; }
.opt { color: #4b5563; font-weight: 400; font-size: .75rem; }

.form-input {
  width: 100%;
  padding: .68rem .9rem;
  background: rgba(255,255,255,.045);
  border: 1px solid rgba(255,255,255,.1);
  border-radius: 9px;
  color: #e2e8f0;
  font-size: .875rem;
  outline: none;
  transition: border-color .2s, box-shadow .2s;
  box-sizing: border-box;
  font-family: inherit;
}
.form-input:focus { border-color: rgba(45,212,160,.4); box-shadow: 0 0 0 3px rgba(45,212,160,.06); }
.form-input::placeholder { color: #374151; }
.form-textarea { resize: vertical; min-height: 75px; line-height: 1.5; }

.modal-footer {
  display: flex;
  gap: .75rem;
  justify-content: flex-end;
  padding: .75rem 1.5rem 1.3rem;
  border-top: 1px solid rgba(255,255,255,.06);
}

.btn-cancel {
  padding: .6rem 1.1rem;
  background: transparent;
  border: 1px solid rgba(255,255,255,.1);
  border-radius: 9px;
  color: #9ca3af;
  font-size: .875rem;
  cursor: pointer;
  transition: all .15s;
}
.btn-cancel:hover { background: rgba(255,255,255,.05); color: #e2e8f0; }

.btn-danger {
  padding: .6rem 1.1rem;
  background: rgba(239,68,68,.12);
  border: 1px solid rgba(239,68,68,.28);
  border-radius: 9px;
  color: #f87171;
  font-size: .875rem;
  font-weight: 600;
  cursor: pointer;
  transition: all .15s;
}
.btn-danger:hover { background: rgba(239,68,68,.22); }

.delete-warning {
  font-size: .875rem;
  color: #9ca3af;
  line-height: 1.65;
  margin: 0;
}
.delete-warning strong { color: #f0fdf4; }

/* ── Spinner ──────────────────────────────────────────── */
.spinner {
  display: inline-block;
  width: 14px;
  height: 14px;
  border: 2px solid rgba(10,31,22,.4);
  border-top-color: #134E2A;
  border-radius: 50%;
  animation: spin .7s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }

/* ── Responsive ───────────────────────────────────────── */
@media (max-width: 640px) {
  .dashboard-list { padding: 1.25rem 1rem; }
  .page-header { flex-direction: column; align-items: stretch; }
  .stats-bar { overflow-x: auto; }
  .toolbar { flex-direction: column; align-items: stretch; }
  .search-wrapper { max-width: none; }
  .toolbar-right { justify-content: space-between; }
  .cards-grid { grid-template-columns: 1fr; }
  .list-header, .list-row { grid-template-columns: 1.5fr 1fr 1fr; }
  .list-header span:nth-child(4),
  .list-header span:nth-child(5),
  .list-row > span:nth-child(4),
  .list-row > div:last-child { display: none; }
}
</style>


