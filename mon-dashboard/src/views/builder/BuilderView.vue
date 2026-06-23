<template>
  <div class="builder-layout">
    <!-- Sidebar LEFT -->
    <aside class="sidebar" :style="{ width: sidebarWidth + 'px' }">
      <div class="sidebar-resize-handle" @mousedown="startSidebarResize"></div>
      <div class="sidebar-header">
        <div class="logo">
          <button class="back-admin-btn" @click="goBackToAdmin" title="Retour au panneau admin">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
              <polyline points="15,18 9,12 15,6"/>
            </svg>
          </button>
          <svg width="28" height="28" viewBox="0 0 48 48" fill="none">
            <rect x="4"  y="24" width="8"  height="16" rx="2" fill="#1B6B3A" opacity="0.6"/>
            <rect x="16" y="16" width="8"  height="24" rx="2" fill="#1B6B3A" opacity="0.8"/>
            <rect x="28" y="8"  width="8"  height="32" rx="2" fill="#1B6B3A"/>
          </svg>
          <span class="logo-text">DashGen</span>
        </div>
        <div class="header-theme-dots">
          <span
            v-for="theme in themes" :key="theme.name"
            class="theme-dot"
            :class="{ active: currentTheme === theme.name }"
            :style="{ background: theme.primary }"
            @click="setTheme(theme.name)"
            :title="theme.label"
          />
        </div>
      </div>

      <nav class="sidebar-nav">

        <!-- Dataset status -->
        <div class="sidebar-dataset-status" :class="{ loaded: !!datasetStore.currentDataset }">
          <div class="ds-icon">
            <i :class="datasetStore.currentDataset ? 'pi pi-database' : 'pi pi-cloud-upload'"></i>
          </div>
          <div class="ds-info">
            <span class="ds-name">{{ datasetStore.currentDataset?.fileName || 'Aucun dataset' }}</span>
            <span class="ds-meta" v-if="datasetStore.currentDataset">
              {{ datasetStore.currentDataset.data.length }} lignes · {{ datasetStore.currentDataset.columns.length }} colonnes
            </span>
            <span class="ds-meta" v-else>Importez un CSV pour visualiser</span>
          </div>
          <button class="ds-import-btn" @click="triggerCsvImport" title="Importer CSV">
            <i class="pi pi-file"></i>
          </button>
          <button class="ds-import-btn ds-import-btn--sql" @click="showSqlModal = true" title="Connexion SQL">
            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">
              <ellipse cx="12" cy="5" rx="9" ry="3"/><path d="M3 5v14c0 1.66 4.03 3 9 3s9-1.34 9-3V5"/>
              <path d="M3 12c0 1.66 4.03 3 9 3s9-1.34 9-3"/>
            </svg>
          </button>
          <button class="ds-import-btn ds-import-btn--rest" @click="showRestModal = true" title="Connexion API REST">
            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/>
              <path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"/>
            </svg>
          </button>
          <input type="file" ref="csvInput" style="display:none" accept=".csv" @change="handleCsvFile" />
        </div>

        <div class="nav-section">
          <div class="section-title">Tableaux de bord</div>
          <div class="dashboard-list">
            <div
              v-for="dash in dashboardStore.dashboards"
              :key="dash.id"
              class="dashboard-item"
              :class="{ active: dashboardStore.currentDashboard?.id === dash.id }"
              @click="selectDashboard(dash)"
            >
              <div class="dash-dot" :style="{ background: currentThemeObj.primary }"></div>
              <span class="dash-name">{{ dash.name }}</span>
              <button class="delete-dash-btn" @click.stop="confirmDeleteDashboard(dash)" title="Supprimer">
                <i class="pi pi-trash"></i>
              </button>
            </div>
            <button class="add-dash-btn" @click="showNewDashModal = true">
              <i class="pi pi-plus"></i> Nouveau dashboard
            </button>
          </div>
        </div>

        <div class="nav-section" v-if="dashboardStore.currentDashboard">
          <div class="section-title">Widgets disponibles</div>
          <div class="widget-palette">
            <div
              v-for="type in widgetTypes"
              :key="type.id"
              class="palette-item"
              :class="{ disabled: ['pie','doughnut'].includes(type.id) && availableColumns.length > 0 && !availableColumns.some(c => c.type === 'number') }"
              :draggable="!['pie','doughnut'].includes(type.id) || availableColumns.length === 0 || availableColumns.some(c => c.type === 'number')"
              @dragstart="handleDragStart($event, type)"
              @click="quickAddWidget(type)"
              :title="type.label"
            >
              <div class="palette-preview">
                <!-- Mini SVG chart preview per type -->
                <svg v-if="type.id === 'bar'" viewBox="0 0 36 20" class="mini-chart">
                  <rect x="2" y="10" width="5" height="10" rx="1" fill="#1B6B3A" opacity="0.9"/>
                  <rect x="10" y="5"  width="5" height="15" rx="1" fill="#1B6B3A" opacity="0.7"/>
                  <rect x="18" y="12" width="5" height="8"  rx="1" fill="#1B6B3A" opacity="0.8"/>
                  <rect x="26" y="2"  width="5" height="18" rx="1" fill="#1B6B3A" opacity="0.6"/>
                </svg>
                <svg v-else-if="type.id === 'line'" viewBox="0 0 36 20" class="mini-chart">
                  <polyline points="2,16 10,8 18,12 26,4 34,7" fill="none" stroke="#1B6B3A" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                  <polyline points="2,16 10,8 18,12 26,4 34,7" fill="none" stroke="#1B6B3A" stroke-width="6" opacity="0.08"/>
                </svg>
                <svg v-else-if="['pie','doughnut'].includes(type.id)" viewBox="0 0 36 36" class="mini-chart">
                  <circle cx="18" cy="18" r="14" fill="none" stroke="#1B6B3A" stroke-width="14" stroke-dasharray="44 44" stroke-dashoffset="11"/>
                  <circle cx="18" cy="18" r="14" fill="none" stroke="#3b82f6" stroke-width="14" stroke-dasharray="22 66" stroke-dashoffset="-33"/>
                  <circle cx="18" cy="18" r="7" fill="var(--card-bg, #FFFFFF)" v-if="type.id==='doughnut'"/>
                </svg>
                <svg v-else-if="type.id === 'area'" viewBox="0 0 36 20" class="mini-chart">
                  <path d="M2,16 L10,8 L18,12 L26,4 L34,7 L34,20 L2,20 Z" fill="#1B6B3A" opacity="0.25"/>
                  <polyline points="2,16 10,8 18,12 26,4 34,7" fill="none" stroke="#1B6B3A" stroke-width="1.5" stroke-linecap="round"/>
                </svg>
                <svg v-else-if="type.id === 'kpi'" viewBox="0 0 36 20" class="mini-chart">
                  <text x="18" y="15" text-anchor="middle" font-size="13" font-weight="700" fill="#1B6B3A">42K</text>
                </svg>
                <svg v-else-if="type.id === 'scatter'" viewBox="0 0 36 20" class="mini-chart">
                  <circle cx="6"  cy="14" r="2" fill="#1B6B3A" opacity="0.8"/>
                  <circle cx="12" cy="8"  r="2" fill="#1B6B3A" opacity="0.7"/>
                  <circle cx="20" cy="11" r="2" fill="#1B6B3A" opacity="0.9"/>
                  <circle cx="28" cy="5"  r="2" fill="#1B6B3A" opacity="0.6"/>
                  <circle cx="32" cy="14" r="2" fill="#1B6B3A" opacity="0.75"/>
                </svg>
                <svg v-else-if="type.id === 'radar'" viewBox="0 0 36 36" class="mini-chart">
                  <polygon points="18,4 30,13 26,28 10,28 6,13" fill="#1B6B3A" opacity="0.15" stroke="#1B6B3A" stroke-width="1.2"/>
                  <polygon points="18,9 25,15 23,24 13,24 11,15" fill="#1B6B3A" opacity="0.4"/>
                </svg>
                <svg v-else-if="type.id === 'table'" viewBox="0 0 36 20" class="mini-chart">
                  <rect x="1" y="1" width="34" height="4"  rx="1" fill="#1B6B3A" opacity="0.4"/>
                  <rect x="1" y="7" width="34" height="3"  rx="1" fill="#1B6B3A" opacity="0.2"/>
                  <rect x="1" y="12" width="34" height="3" rx="1" fill="#1B6B3A" opacity="0.15"/>
                  <rect x="1" y="17" width="34" height="3" rx="1" fill="#1B6B3A" opacity="0.1"/>
                </svg>
                <!-- Gauge mini preview -->
                <svg v-else-if="type.id === 'gauge'" viewBox="0 0 36 24" class="mini-chart">
                  <path d="M4,20 A14,14 0 0,1 32,20" fill="none" stroke="rgba(255,255,255,0.1)" stroke-width="4" stroke-linecap="round"/>
                  <path d="M4,20 A14,14 0 0,1 24,9"  fill="none" :stroke="'#1B6B3A'" stroke-width="4" stroke-linecap="round"/>
                  <circle cx="18" cy="20" r="2.5" fill="#1B6B3A"/>
                </svg>
                <!-- Funnel mini preview -->
                <svg v-else-if="type.id === 'funnel'" viewBox="0 0 36 24" class="mini-chart">
                  <polygon points="4,2 32,2 26,9 10,9"   :fill="'#1B6B3A'" opacity="0.85"/>
                  <polygon points="10,11 26,11 22,17 14,17" :fill="'#1B6B3A'" opacity="0.65"/>
                  <polygon points="14,19 22,19 20,24 16,24" :fill="'#1B6B3A'" opacity="0.45"/>
                </svg>
                <!-- Text mini preview -->
                <svg v-else-if="type.id === 'text'" viewBox="0 0 36 20" class="mini-chart">
                  <rect x="2"  y="3"  width="32" height="3" rx="1.5" fill="#1B6B3A" opacity="0.8"/>
                  <rect x="2"  y="9"  width="26" height="2.5" rx="1.2" fill="#1B6B3A" opacity="0.5"/>
                  <rect x="2"  y="14" width="20" height="2.5" rx="1.2" fill="#1B6B3A" opacity="0.35"/>
                </svg>
                <!-- Box plot mini preview -->
                <svg v-else-if="type.id === 'boxplot'" viewBox="0 0 36 28" class="mini-chart">
                  <!-- groupe 1 -->
                  <line x1="7"  y1="4"  x2="7"  y2="8"  stroke="#1B6B3A" stroke-width="1.4" opacity="0.7"/>
                  <line x1="5"  y1="4"  x2="9"  y2="4"  stroke="#1B6B3A" stroke-width="1.4"/>
                  <rect x="4"  y="8"  width="6" height="8" rx="1" fill="#1B6B3A" opacity="0.25" stroke="#1B6B3A" stroke-width="1.4"/>
                  <line x1="4"  y1="12" x2="10" y2="12" stroke="#1B6B3A" stroke-width="2"/>
                  <line x1="7"  y1="16" x2="7"  y2="21" stroke="#1B6B3A" stroke-width="1.4" opacity="0.7"/>
                  <line x1="5"  y1="21" x2="9"  y2="21" stroke="#1B6B3A" stroke-width="1.4"/>
                  <!-- groupe 2 -->
                  <line x1="18" y1="6"  x2="18" y2="10" stroke="#1B6B3A" stroke-width="1.4" opacity="0.7"/>
                  <line x1="16" y1="6"  x2="20" y2="6"  stroke="#1B6B3A" stroke-width="1.4"/>
                  <rect x="15" y="10" width="6" height="10" rx="1" fill="#1B6B3A" opacity="0.25" stroke="#1B6B3A" stroke-width="1.4"/>
                  <line x1="15" y1="14" x2="21" y2="14" stroke="#1B6B3A" stroke-width="2"/>
                  <line x1="18" y1="20" x2="18" y2="24" stroke="#1B6B3A" stroke-width="1.4" opacity="0.7"/>
                  <line x1="16" y1="24" x2="20" y2="24" stroke="#1B6B3A" stroke-width="1.4"/>
                  <!-- groupe 3 -->
                  <line x1="29" y1="3"  x2="29" y2="7"  stroke="#1B6B3A" stroke-width="1.4" opacity="0.7"/>
                  <line x1="27" y1="3"  x2="31" y2="3"  stroke="#1B6B3A" stroke-width="1.4"/>
                  <rect x="26" y="7"  width="6" height="12" rx="1" fill="#1B6B3A" opacity="0.25" stroke="#1B6B3A" stroke-width="1.4"/>
                  <line x1="26" y1="11" x2="32" y2="11" stroke="#1B6B3A" stroke-width="2"/>
                  <line x1="29" y1="19" x2="29" y2="23" stroke="#1B6B3A" stroke-width="1.4" opacity="0.7"/>
                  <line x1="27" y1="23" x2="31" y2="23" stroke="#1B6B3A" stroke-width="1.4"/>
                </svg>
                <i v-else :class="type.icon" style="font-size:16px;color:#1B6B3A"></i>
              </div>
              <span class="palette-label">{{ type.label }}</span>
              <span class="palette-add-hint">+ clic / drag</span>
            </div>
          </div>
        </div>
      </nav>

      <div class="sidebar-footer">
        <div class="user-info">
          <div class="avatar">{{ (authStore.user?.email || 'U')[0].toUpperCase() }}</div>
          <div class="details">
            <span class="name">{{ authStore.user?.email?.split('@')[0] }}</span>
            <span class="email">{{ authStore.user?.role || 'User' }}</span>
          </div>
        </div>
        <button class="logout-btn" @click="handleLogout" title="Déconnexion">
          <i class="pi pi-sign-out"></i>
        </button>
      </div>
    </aside>

    <!-- Main Content: Canvas Area -->
    <main class="canvas-container">
      <header class="canvas-header" v-if="dashboardStore.currentDashboard">
        <div class="header-info">
          <div class="header-title-row">
            <h1>{{ dashboardStore.currentDashboard.name }}</h1>
            <span class="status-badge">
              <span class="status-dot"></span>
              Mode Édition
            </span>
          </div>
          <div class="header-meta">
            <span class="header-meta-item">
              <i class="pi pi-clone" style="font-size:10px"></i>
              {{ dashboardStore.currentWidgets.length }} widget(s)
            </span>
            <span class="header-meta-item" :class="{ 'meta-ok': !!datasetStore.currentDataset }">
              <i :class="datasetStore.currentDataset ? 'pi pi-database' : 'pi pi-exclamation-triangle'" style="font-size:10px"></i>
              {{ datasetStore.currentDataset ? datasetStore.currentDataset.fileName : 'Pas de dataset' }}
            </span>
            <span v-if="lastSaved" class="header-meta-item">
              <i class="pi pi-check-circle" style="font-size:10px;color:#1B6B3A"></i>
              Sauvegardé {{ lastSaved }}
            </span>
          </div>
        </div>
        <div class="header-actions">
          <!-- Auto-generate dashboard button -->
          <button
            class="autogen-btn"
            :disabled="!datasetStore.currentDataset || isAutoGenerating"
            :title="datasetStore.currentDataset ? 'Générer automatiquement tout le dashboard avec l\'IA' : 'Importez d\'abord un dataset'"
            @click="autoGenerate"
          >
            <i :class="isAutoGenerating ? 'pi pi-spin pi-spinner' : 'pi pi-bolt'"/>
            <span>{{ isAutoGenerating ? 'Génération…' : 'Générer' }}</span>
          </button>
          <!-- Share button -->
          <button
            class="share-topbar-btn"
            :disabled="!dashboardStore.currentDashboard"
            title="Partager ce dashboard"
            @click="showShareModal = true"
          >
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" style="flex-shrink:0">
              <circle cx="18" cy="5" r="3"/><circle cx="6" cy="12" r="3"/><circle cx="18" cy="19" r="3"/>
              <line x1="8.59" y1="13.51" x2="15.42" y2="17.49"/>
              <line x1="15.41" y1="6.51" x2="8.59" y2="10.49"/>
            </svg>
            <span>Partager</span>
          </button>
          <!-- AI Assistant button -->
          <button class="ai-topbar-btn" :class="{ 'ai-topbar-btn--open': aiOpen }" @click="aiOpen = !aiOpen">
            <span class="ai-topbar-pulse" v-if="!aiOpen"/>
            <svg width="15" height="15" viewBox="0 0 24 24" fill="currentColor" style="flex-shrink:0">
              <path d="M12 2L15.09 8.26L22 9.27L17 14.14L18.18 21.02L12 17.77L5.82 21.02L7 14.14L2 9.27L8.91 8.26L12 2Z"/>
            </svg>
            <span>{{ aiOpen ? 'Fermer IA' : 'Assistant IA' }}</span>
          </button>
          <!-- Version history button -->
          <button
            class="version-topbar-btn"
            :disabled="!dashboardStore.currentDashboard"
            title="Historique des versions"
            @click="showVersionModal = true"
          >
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" style="flex-shrink:0">
              <circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>
            </svg>
            <span>Historique</span>
          </button>
          <div class="header-sep"/>
          <!-- Undo -->
          <button class="icon-btn" @click="undo" :disabled="undoStack.length === 0" title="Annuler (Ctrl+Z)">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
              <polyline points="1 4 1 10 7 10"/><path d="M3.51 15a9 9 0 1 0 .49-4.9"/>
            </svg>
          </button>
          <!-- Templates -->
          <button class="icon-btn" @click="showTemplatesModal = true" title="Templates pré-configurés">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
              <rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/><rect x="14" y="14" width="7" height="7"/>
            </svg>
          </button>
          <!-- Mobile preview -->
          <button class="icon-btn" :class="{ active: mobilePreview }" @click="mobilePreview = !mobilePreview" title="Prévisualisation mobile">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
              <rect x="7" y="2" width="10" height="20" rx="2"/><line x1="12" y1="18" x2="12" y2="18.01"/>
            </svg>
          </button>
          <!-- Auto-refresh -->
          <div style="position:relative">
            <button class="icon-btn" :class="{ active: autoRefreshInterval > 0 }" @click="showAutoRefreshMenu = !showAutoRefreshMenu" title="Auto-refresh">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                <polyline points="23 4 23 10 17 10"/><polyline points="1 20 1 14 7 14"/>
                <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"/>
              </svg>
              <span v-if="autoRefreshInterval > 0" style="font-size:9px;font-weight:700;color:#1B6B3A">{{ autoRefreshInterval }}s</span>
            </button>
            <div v-if="showAutoRefreshMenu" class="auto-refresh-menu">
              <div class="arm-title">Auto-refresh</div>
              <button v-for="opt in [0,15,30,60,300]" :key="opt" class="arm-opt" :class="{ active: autoRefreshInterval === opt }" @click="autoRefreshInterval = opt; showAutoRefreshMenu = false">
                {{ opt === 0 ? 'Désactivé' : opt < 60 ? opt + ' sec' : (opt/60) + ' min' }}
              </button>
            </div>
          </div>
          <div class="header-sep"/>
          <button class="icon-btn" @click="showDashSettings = !showDashSettings" title="Paramètres" :class="{ active: showDashSettings }">
            <i class="pi pi-sliders-h"></i>
          </button>
          <button class="export-btn json" @click="handleExportJson" :disabled="isExporting" title="Exporter en JSON">
            <i class="pi pi-code"></i>
            <span>JSON</span>
          </button>
          <button class="export-btn pdf" @click="handleExport('pdf')" :disabled="isExporting" title="Exporter en PDF">
            <i class="pi pi-file-pdf"></i>
            <span>PDF</span>
          </button>
          <button class="save-btn" @click="handleSave" :disabled="isSaving">
            <i :class="isSaving ? 'pi pi-spin pi-spinner' : 'pi pi-cloud-upload'"></i>
            {{ isSaving ? 'En cours…' : 'Enregistrer' }}
          </button>
        </div>
      </header>

      <!-- Filters Toolbar -->
      <div class="filters-toolbar" v-if="dashboardStore.currentDashboard">
        <div class="filters-label">
          <i class="pi pi-filter"></i>
          <span>Filtres actifs :</span>
        </div>
        <div class="filter-tags">
          <div
            v-for="(filter, index) in activeFilters"
            :key="index"
            class="filter-tag"
            :class="filter.type"
          >
            <i :class="getFilterIcon(filter.type)"></i>
            <span>{{ filter.label }}: {{ filter.value }}</span>
            <button class="remove-filter" @click="removeFilter(index)">
              <i class="pi pi-times"></i>
            </button>
          </div>
          <button class="add-filter-btn" @click="showAddFilterModal = true">
            <i class="pi pi-plus-circle"></i>
            <span>Ajouter</span>
          </button>
        </div>
        <button v-if="activeFilters.length > 0" class="clear-filters" @click="activeFilters = []">
          Effacer tout
        </button>
      </div>

      <!-- ── Format Toolbar contextuelle (sélection widget) ── -->
      <transition name="ftb-slide">
        <div v-if="showConfigPanel && selectedWidget" class="format-toolbar">
          <div class="ftb-info">
            <div class="ftb-type-badge" :class="`wtype-${selectedWidget.type}`">
              <i :class="getWidgetIcon(selectedWidget.type)"></i>
            </div>
            <span class="ftb-title">{{ widgetConfig.title || selectedWidget.title }}</span>
          </div>
          <div class="ftb-sep"></div>
          <!-- Quick colors -->
          <div class="ftb-colors">
            <button v-for="c in COLOR_PALETTE.slice(0, 7)" :key="c"
              class="ftb-color" :class="{ active: widgetConfig.color === c }"
              :style="{ background: c }" :title="c"
              @click="widgetConfig.color = c">
            </button>
          </div>
          <div class="ftb-sep"></div>
          <!-- Tab shortcuts -->
          <button class="ftb-btn" :class="{ active: ppTab === 'build' }"  @click="ppTab = 'build'"  title="Construire"><i class="pi pi-database"></i></button>
          <button class="ftb-btn" :class="{ active: ppTab === 'format' }" @click="ppTab = 'format'" title="Format"><i class="pi pi-palette"></i></button>
          <button class="ftb-btn" :class="{ active: ppTab === 'fields' }" @click="ppTab = 'fields'" title="Champs"><i class="pi pi-list"></i></button>
          <div class="ftb-sep"></div>
          <!-- Actions -->
          <button class="ftb-btn" @click="duplicateWidget(selectedWidget)" title="Dupliquer"><i class="pi pi-copy"></i></button>
          <button class="ftb-btn ftb-btn--danger" @click="removeWidget(selectedWidget.id); showConfigPanel = false" title="Supprimer"><i class="pi pi-trash"></i></button>
          <div class="ftb-sep"></div>
          <button class="ftb-apply-btn" @click="applyWidgetConfig"><i class="pi pi-check"></i> Appliquer</button>
          <button class="ftb-close-btn" @click="showConfigPanel = false" title="Fermer"><i class="pi pi-times"></i></button>
        </div>
      </transition>

      <div
        ref="canvasGridRef"
        class="canvas-grid"
        :class="{ 'canvas-grid--mobile': mobilePreview }"
        :style="{
          gap: dashSettings.gridGap + 'px',
          backgroundImage: dashSettings.showGridDots
            ? 'radial-gradient(circle, rgba(0,0,0,0.12) 1px, transparent 1px)'
            : 'radial-gradient(circle, rgba(0,0,0,0.06) 1px, transparent 1px)',
          backgroundSize: dashSettings.showGridDots ? '28px 28px' : '28px 28px',
        }"
        @dragover.prevent
        @drop="handleDrop"
        v-if="dashboardStore.currentDashboard"
      >
        <div
          v-for="(widget, index) in dashboardStore.currentWidgets"
          :key="widget.id"
          class="widget-card"
          :ref="el => builderWidgetRefs[widget.id] = el as HTMLElement"
          :class="{
            dragging: draggedWidgetIndex === index,
            resizing: resizingWidget?.id === widget.id,
          }"
          :style="getWidgetStyle(widget)"
          draggable="true"
          @dragstart="handleWidgetDragStart($event, index)"
          @dragend="draggedWidgetIndex = null"
          @dragover.prevent
          @drop="handleWidgetDrop($event, index)"
        >
          <div class="widget-header">
            <div class="widget-type-badge" :class="`wtype-${widget.type}`">
              <i :class="getWidgetIcon(widget.type)"></i>
            </div>
            <div class="widget-title-group">
              <span class="widget-title">{{ widget.title }}</span>
              <span v-if="getWidgetCfg(widget).description" class="widget-description">{{ getWidgetCfg(widget).description }}</span>
            </div>
            <div class="widget-actions">
              <!-- Bouton libellés (uniquement pour les types qui le supportent) -->
              <button
                v-if="['bar','line','area','pie','doughnut','funnel'].includes(widget.type)"
                @click.stop="toggleWidgetLabels(widget)"
                class="wbtn-labels"
                :class="{ active: widgetLabelsOn(widget) }"
                title="Libellés de valeurs"
              >
                <svg width="12" height="12" viewBox="0 0 12 12" fill="none">
                  <rect x="1" y="3" width="10" height="6" rx="1.5" stroke="currentColor" stroke-width="1.2"/>
                  <line x1="3.5" y1="6" x2="8.5" y2="6" stroke="currentColor" stroke-width="1.2" stroke-linecap="round"/>
                  <line x1="3.5" y1="7.5" x2="6.5" y2="7.5" stroke="currentColor" stroke-width="1.2" stroke-linecap="round"/>
                </svg>
              </button>
              <button @click.stop="exportBuilderWidgetPng(widget.id, widget.title)" title="Exporter en PNG" class="wbtn-export">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
                  <polyline points="7 10 12 15 17 10"/>
                  <line x1="12" y1="15" x2="12" y2="3"/>
                </svg>
              </button>
              <button @click="openWidgetConfig(widget)" title="Configurer" class="wbtn-config">
                <i class="pi pi-cog"></i>
              </button>
              <button @click="removeWidget(widget.id)" title="Supprimer" class="wbtn-delete">
                <i class="pi pi-trash"></i>
              </button>
            </div>
          </div>
          <div class="widget-body">
            <!-- Dynamic Content based on widget type -->
            <KpiCardWidget
              v-if="widget.type === 'kpi'"
              :title="widget.title"
              :value="getKpiValue(widget)"
              :trend="getWidgetCfg(widget).kpiShowTrend ? getWidgetCfg(widget).kpiTrendValue : undefined"
              :color="getWidgetCfg(widget).color || '#1B6B3A'"
              :icon="getWidgetCfg(widget).kpiIcon"
              :prefix="getWidgetCfg(widget).valuePrefix || ''"
              :suffix="getWidgetCfg(widget).kpiSuffix || getWidgetCfg(widget).valueSuffix || ''"
              :description="getWidgetCfg(widget).description || ''"
              :sparkline="getKpiSparkline(widget)"
            />

            <!-- Pie / Donut — our SVG custom widget -->
            <PieChartWidget
              v-else-if="['pie','doughnut'].includes(widget.type)"
              :data="getWidgetData(widget)"
              :bg-color="getWidgetCfg(widget).backgroundColor || currentThemeObj.card"
              :legend-pos="getWidgetCfg(widget).pieLegendPosition || 'right'"
              :show-labels="!!getWidgetCfg(widget).pieShowLabels"
              :donut="widget.type === 'doughnut'"
              :max-slices="getWidgetCfg(widget).pieMaxSlices || 8"
              :value-prefix="getWidgetCfg(widget).valuePrefix || ''"
              :value-suffix="getWidgetCfg(widget).valueSuffix || ''"
              :x-axis-label="getWidgetCfg(widget).xAxis || ''"
              :y-axis-label="getWidgetCfg(widget).yAxis || ''"
            />

            <!-- Bar — our SVG custom widget -->
            <BarChartWidget
              v-else-if="widget.type === 'bar'"
              :data="getWidgetData(widget)"
              :color="getWidgetCfg(widget).color || '#1B6B3A'"
              :orientation="getWidgetCfg(widget).barOrientation || 'vertical'"
              :show-values="!!getWidgetCfg(widget).barShowValues"
              :border-radius="getWidgetCfg(widget).barBorderRadius ?? 3"
              :value-prefix="getWidgetCfg(widget).valuePrefix || ''"
              :value-suffix="getWidgetCfg(widget).valueSuffix || ''"
              :y-axis-label="getWidgetCfg(widget).yAxisLabel || ''"
            />

            <!-- Line / Area — our SVG custom widget -->
            <LineChartWidget
              v-else-if="['line','area'].includes(widget.type)"
              :data="getWidgetData(widget)"
              :color="getWidgetCfg(widget).color || '#1B6B3A'"
              :smooth="!!getWidgetCfg(widget).lineSmooth"
              :fill="getWidgetCfg(widget).lineFill !== false"
              :show-dots="getWidgetCfg(widget).lineShowDots !== false"
              :show-labels="!!getWidgetCfg(widget).lineShowLabels"
              :value-prefix="getWidgetCfg(widget).valuePrefix || ''"
              :value-suffix="getWidgetCfg(widget).valueSuffix || ''"
              :y-axis-label="getWidgetCfg(widget).yAxisLabel || ''"
            />

            <!-- Gauge -->
            <GaugeWidget
              v-else-if="widget.type === 'gauge'"
              :value="getKpiValue(widget)"
              :min="getWidgetCfg(widget).gaugeMin ?? 0"
              :max="getWidgetCfg(widget).gaugeMax ?? 100"
              :color="getWidgetCfg(widget).color || '#1B6B3A'"
              :prefix="getWidgetCfg(widget).valuePrefix || ''"
              :suffix="getWidgetCfg(widget).valueSuffix || ''"
              :label="getWidgetCfg(widget).yAxisLabel || getWidgetCfg(widget).yAxis || ''"
            />

            <!-- Funnel -->
            <FunnelWidget
              v-else-if="widget.type === 'funnel'"
              :data="getWidgetData(widget)"
              :value-prefix="getWidgetCfg(widget).valuePrefix || ''"
              :value-suffix="getWidgetCfg(widget).valueSuffix || ''"
            />

            <!-- Text / Note -->
            <TextWidget
              v-else-if="widget.type === 'text'"
              :content="getWidgetCfg(widget).textContent || ''"
              :font-size="getWidgetCfg(widget).textFontSize ?? 14"
              :align="getWidgetCfg(widget).textAlign || 'left'"
              :color="getWidgetCfg(widget).color || ''"
              :font-family="getWidgetCfg(widget).textFontFamily || 'inherit'"
            />

            <!-- Scatter — custom SVG widget -->
            <ScatterWidget
              v-else-if="widget.type === 'scatter'"
              :data="getWidgetData(widget)"
              :color="getWidgetCfg(widget).color || '#1B6B3A'"
              :dot-size="getWidgetCfg(widget).scatterDotSize ?? 4"
              :x-axis-label="getWidgetCfg(widget).xAxis || ''"
              :y-axis-label="getWidgetCfg(widget).yAxis || ''"
              :value-prefix="getWidgetCfg(widget).valuePrefix || ''"
              :value-suffix="getWidgetCfg(widget).valueSuffix || ''"
            />

            <!-- BoxPlot -->
            <BoxPlotWidget
              v-else-if="widget.type === 'boxplot'"
              :data="getBoxPlotData(widget)"
              :value-prefix="getWidgetCfg(widget).valuePrefix || ''"
              :value-suffix="getWidgetCfg(widget).valueSuffix || ''"
            />

            <BaseChart
              v-else-if="widget.type === 'radar'"
              :type="widget.type"
              :data="getWidgetData(widget)"
              :color="getWidgetCfg(widget).color || '#1B6B3A'"
              :theme="currentTheme"
              :text-color="themes.find((t) => t.name === currentTheme)?.text"
              :muted-color="currentTheme === 'white' ? 'rgba(10,31,26,0.5)' : '#94A99A'"
            />

            <TableWidget
              v-else-if="widget.type === 'table'"
              v-bind="parseTableData(JSON.stringify(getWidgetData(widget)))"
              :page-size="getWidgetCfg(widget).tablePageSize ?? 10"
              :show-search="getWidgetCfg(widget).tableShowSearch !== false"
            />

            <HeatmapWidget
              v-else-if="widget.type === 'heatmap'"
              v-bind="parseHeatmapData(widget.data)"
            />

            <ImageWidget
              v-else-if="widget.type === 'image'"
              :src="parseImageData(widget.data).src"
              :fit="parseImageData(widget.data).fit"
              :alt-text="parseImageData(widget.data).altText"
            />

            <TreemapWidget
              v-else-if="widget.type === 'treemap'"
              :data="getWidgetData(widget)"
            />

            <MapWidget
              v-else-if="widget.type === 'map'"
              :data="getMapData(widget)"
              :marker-color="getWidgetCfg(widget).markerColor || '#3b82f6'"
              :zoom="getWidgetCfg(widget).mapZoom || 5"
              :center-lat="getWidgetCfg(widget).mapCenterLat || 46.5"
              :center-lon="getWidgetCfg(widget).mapCenterLon || 2.3"
            />

            <div v-else class="placeholder-content">
              <i :class="getWidgetIcon(widget.type)"></i>
              <span>Contenu {{ widget.type }}</span>
            </div>
          </div>

          <!-- ── Widget caption strip ──────────────────────────── -->
          <div
            v-if="getWidgetCaption(widget)"
            class="widget-caption"
            :class="{ 'widget-caption--manual': !!getWidgetCfg(widget).description?.trim() }"
            :title="getWidgetCaption(widget)"
          >
            <!-- icon hint depending on type -->
            <i
              class="widget-caption-icon pi"
              :class="{
                'pi-chart-bar':  ['bar','funnel'].includes(widget.type),
                'pi-chart-line': ['line','area'].includes(widget.type),
                'pi-chart-pie':  ['pie','doughnut'].includes(widget.type),
                'pi-circle':     widget.type === 'gauge',
                'pi-info-circle':widget.type === 'kpi',
                'pi-list':       widget.type === 'table',
                'pi-compass':    widget.type === 'radar',
                'pi-th-large':   widget.type === 'scatter',
                'pi-chart-bar':  widget.type === 'boxplot' || ['bar','funnel'].includes(widget.type),
              }"
            ></i>
            <span class="widget-caption-text">{{ getWidgetCaption(widget) }}</span>
          </div>

          <!-- Resize Handles (4 Corners) -->
          <div
            v-for="corner in ['top-left', 'top-right', 'bottom-left', 'bottom-right']"
            :key="corner"
            class="resize-handle"
            :class="corner"
            @mousedown.stop.prevent="startResizing($event, widget, corner)"
          >
            <div class="resize-icon"></div>
          </div>
        </div>

        <div v-if="dashboardStore.currentWidgets.length === 0" class="empty-state">
          <div class="empty-state-visual">
            <svg viewBox="0 0 120 80" width="120" height="80" fill="none">
              <rect x="5" y="5" width="50" height="35" rx="6" stroke="#1B6B3A" stroke-width="1.5" stroke-dasharray="4 3" opacity="0.4"/>
              <rect x="65" y="5" width="50" height="35" rx="6" stroke="#1B6B3A" stroke-width="1.5" stroke-dasharray="4 3" opacity="0.4"/>
              <rect x="5" y="47" width="110" height="28" rx="6" stroke="#1B6B3A" stroke-width="1.5" stroke-dasharray="4 3" opacity="0.4"/>
              <text x="30" y="27" text-anchor="middle" font-size="16" fill="#1B6B3A" opacity="0.3">+</text>
              <text x="90" y="27" text-anchor="middle" font-size="16" fill="#1B6B3A" opacity="0.3">+</text>
              <text x="60" y="65" text-anchor="middle" font-size="16" fill="#1B6B3A" opacity="0.3">+</text>
            </svg>
          </div>
          <h3>Votre dashboard est vide</h3>
          <p>Cliquez sur un widget dans la barre latérale, ou glissez-le ici</p>
          <div class="empty-quick-add">
            <button v-for="t in widgetTypes.slice(0, 4)" :key="t.id" class="quick-add-btn" @click="quickAddWidget(t)">
              <i :class="t.icon"></i>{{ t.label }}
            </button>
          </div>
        </div>
      </div>

      <div v-else class="no-dashboard-state">
        <div class="empty-hero">
          <div class="empty-hero-icon">
            <i class="pi pi-chart-bar"></i>
          </div>
          <h2>Créez votre premier tableau de bord</h2>
          <p>Sélectionnez ou créez un dashboard dans la barre latérale gauche pour commencer.</p>
          <button class="hero-create-btn" @click="showNewDashModal = true">
            <i class="pi pi-plus"></i> Nouveau dashboard
          </button>
        </div>
      </div>
    </main>

    <!-- ══════════════════════════════════════════════════
         PANNEAU PROPRIÉTÉS — style Power BI
         (Construire | Mettre en forme | Champs)
         ══════════════════════════════════════════════════ -->
    <aside
      class="pp"
      :class="{ 'pp--collapsed': ppCollapsed, 'pp--resizing': ppResizing }"
      :style="ppCollapsed ? undefined : { width: ppWidth + 'px' }"
    >
      <!-- Drag-to-resize handle (left edge) -->
      <div
        v-if="!ppCollapsed"
        class="pp-resize-handle"
        @mousedown.prevent="startPpResize"
        title="Redimensionner"
      ></div>

      <!-- Toggle handle -->
      <button class="pp-toggle" @click="ppCollapsed = !ppCollapsed"
        :title="ppCollapsed ? 'Ouvrir les propriétés' : 'Réduire'">
        <i :class="ppCollapsed ? 'pi pi-chevron-left' : 'pi pi-chevron-right'"></i>
      </button>

      <!-- ── COLLAPSED ── -->
      <template v-if="ppCollapsed">
        <div class="pp-collapsed-hint">
          <i class="pi pi-sliders-v"></i>
          <span>Propriétés</span>
        </div>
      </template>

      <!-- ── EXPANDED ── -->
      <template v-else>

        <!-- ── Header ── -->
        <div v-if="showConfigPanel && selectedWidget" class="pp-header pp-header--widget">
          <div class="pp-widget-badge" :class="`wtype-${selectedWidget.type}`">
            <i :class="getWidgetIcon(selectedWidget.type)"></i>
          </div>
          <input v-model="widgetConfig.title" class="pp-title-input" placeholder="Titre du widget" />
          <button class="pp-close-btn" @click="showConfigPanel = false" title="Fermer">
            <i class="pi pi-times"></i>
          </button>
        </div>
        <div v-else class="pp-header pp-header--idle">
          <i class="pi pi-database" style="color:rgba(27,107,58,0.6);font-size:14px"></i>
          <span class="pp-idle-title">Propriétés</span>
          <button class="fields-import-btn" @click="showSqlModal = true" title="Connexion SQL" style="margin-right:4px">
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">
              <ellipse cx="12" cy="5" rx="9" ry="3"/><path d="M3 5v14c0 1.66 4.03 3 9 3s9-1.34 9-3V5"/>
              <path d="M3 12c0 1.66 4.03 3 9 3s9-1.34 9-3"/>
            </svg>
          </button>
          <button class="fields-import-btn fields-import-btn--rest" @click="showRestModal = true" title="Connexion API REST" style="margin-right:4px">
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/>
              <path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"/>
            </svg>
          </button>
          <button class="fields-import-btn" @click="triggerCsvImport" title="Importer CSV"><i class="pi pi-upload"></i></button>
        </div>

        <!-- ── Tabs (quand widget sélectionné) ── -->
        <div v-if="showConfigPanel && selectedWidget" class="pp-tabs">
          <button class="pp-tab" :class="{ active: ppTab === 'build' }"  @click="ppTab = 'build'">
            <i class="pi pi-database"></i><span>Construire</span>
          </button>
          <button class="pp-tab" :class="{ active: ppTab === 'format' }" @click="ppTab = 'format'">
            <i class="pi pi-palette"></i><span>Format</span>
          </button>
          <button class="pp-tab" :class="{ active: ppTab === 'fields' }" @click="ppTab = 'fields'">
            <i class="pi pi-list"></i><span>Champs</span>
          </button>
        </div>

        <!-- ── Corps scrollable ── -->
        <div class="pp-body">

          <!-- ═══ TAB CONSTRUIRE ══════════════════════════════ -->
          <template v-if="showConfigPanel && selectedWidget && ppTab === 'build'">

            <!-- Axes (non-KPI / non-gauge / non-text / non-table / non-heatmap) -->
            <div class="pp-section" v-if="!['kpi','gauge','text','table','heatmap','image','map'].includes(selectedWidget.type)">
              <div class="pp-section-title">Champs du visuel</div>

              <!-- 📦 BoxPlot explainer banner -->
              <div v-if="selectedWidget.type === 'boxplot'" class="pie-fields-banner" style="border-color:rgba(99,102,241,0.3);background:rgba(99,102,241,0.07)">
                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="color:#a5b4fc;flex-shrink:0">
                  <rect x="9" y="4" width="6" height="12" rx="1"/><line x1="12" y1="4" x2="12" y2="2"/><line x1="12" y1="16" x2="12" y2="18"/>
                  <line x1="10" y1="2" x2="14" y2="2"/><line x1="10" y1="18" x2="14" y2="18"/><line x1="9" y1="10" x2="15" y2="10"/>
                </svg>
                <span style="color:#c7d2fe">Choisissez un <strong>Groupe (X)</strong> — colonne catégorie qui divise les boîtes — et une <strong>Variable (Y)</strong> numérique dont la distribution sera calculée (min, Q1, médiane, Q3, max).</span>
              </div>

              <!-- 🥧 Pie/Donut explainer banner -->
              <div v-if="['pie','doughnut'].includes(selectedWidget.type)" class="pie-fields-banner">
                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M12 2a10 10 0 0 1 10 10H12V2z" opacity="0.8"/><path d="M12 2v10l-7 7A10 10 0 0 1 12 2z" opacity="0.5"/><circle cx="12" cy="12" r="10" fill="none"/></svg>
                <span>Un camembert n'a <strong>pas d'axes</strong>. Choisissez une <strong>Dimension</strong> (ce qui divise les parts) et une <strong>Mesure</strong> (la valeur de chaque part).</span>
              </div>

              <div class="axis-wells">

                <!-- ── Dimension / X ─────────────────────────── -->
                <div class="axis-well" :class="{ 'axis-well--over': wellHover === 'x' }"
                  @dragover.prevent="wellHover = 'x'" @dragleave="wellHover = null"
                  @drop.prevent="wellDrop($event, 'x')">
                  <div class="axis-well-header">
                    <!-- Badge: pie/donut → column type icon ; scatter → X ; others → X -->
                    <span class="axis-badge axis-badge--x" v-if="['pie','doughnut'].includes(selectedWidget.type)">
                      {{ widgetConfig.xAxis ? getColumnTypeIcon(widgetConfig.xAxis) : '🔤' }}
                    </span>
                    <span class="axis-badge axis-badge--x" v-else>X</span>
                    <span class="axis-well-label">
                      {{ ['pie','doughnut'].includes(selectedWidget.type) ? 'Dimension (catégories = parts)'
                        : selectedWidget.type === 'scatter'  ? 'Axe X (numérique)'
                        : selectedWidget.type === 'boxplot'  ? 'Groupe (colonne catégorie)'
                        : 'Axe des abscisses' }}
                    </span>
                    <button v-if="widgetConfig.xAxis" class="axis-clear" @click="widgetConfig.xAxis = ''"><i class="pi pi-times"></i></button>
                  </div>
                  <div class="axis-well-body">
                    <div v-if="widgetConfig.xAxis" class="axis-chip axis-chip--dim">
                      <span class="aChip-icon">{{ getColumnTypeIcon(widgetConfig.xAxis) }}</span>
                      <span class="aChip-name">{{ widgetConfig.xAxis }}</span>
                    </div>
                    <div v-else class="axis-well-placeholder">
                      <i class="pi pi-arrows-alt"></i>
                      <span>{{ ['pie','doughnut','boxplot'].includes(selectedWidget.type) ? 'Colonne catégorie…' : 'Glisser ici ou choisir' }}</span>
                    </div>
                  </div>
                  <!-- Scatter: numeric only -->
                  <template v-if="selectedWidget.type === 'scatter'">
                    <select v-model="widgetConfig.xAxis" class="axis-fallback-select">
                      <option value="">— Sélectionner —</option>
                      <option v-for="col in availableColumns.filter(c => c.type === 'number')" :key="col.name" :value="col.name">{{ col.name }}</option>
                      <option v-if="availableColumns.length === 0 && widgetConfig.xAxis" :value="widgetConfig.xAxis">{{ widgetConfig.xAxis }}</option>
                    </select>
                  </template>
                  <!-- BoxPlot: prefer category columns (same logic as pie/donut) -->
                  <template v-else-if="selectedWidget.type === 'boxplot'">
                    <select v-model="widgetConfig.xAxis" class="axis-fallback-select">
                      <option value="">— Sélectionner un groupe —</option>
                      <optgroup label="🔤 Catégories (recommandé)" v-if="availableColumns.some(c => ['category','boolean','text'].includes(c.type))">
                        <option v-for="col in availableColumns.filter(c => ['category','boolean','text'].includes(c.type))" :key="col.name" :value="col.name">{{ col.name }}</option>
                      </optgroup>
                      <optgroup label="📅 Dates" v-if="availableColumns.some(c => c.type === 'date')">
                        <option v-for="col in availableColumns.filter(c => c.type === 'date')" :key="col.name" :value="col.name">{{ col.name }}</option>
                      </optgroup>
                      <option v-if="availableColumns.length === 0 && widgetConfig.xAxis" :value="widgetConfig.xAxis">{{ widgetConfig.xAxis }}</option>
                      <option v-if="availableColumns.length === 0 && !widgetConfig.xAxis" disabled>— Aucun CSV chargé —</option>
                    </select>
                  </template>
                  <!-- Pie / Donut: prefer category columns, but allow dates and all -->
                  <template v-else-if="['pie','doughnut'].includes(selectedWidget.type)">
                    <select v-model="widgetConfig.xAxis" class="axis-fallback-select">
                      <option value="">— Sélectionner —</option>
                      <optgroup label="🔤 Catégories (recommandé)" v-if="availableColumns.some(c => ['category','boolean','text'].includes(c.type))">
                        <option v-for="col in availableColumns.filter(c => ['category','boolean','text'].includes(c.type))" :key="col.name" :value="col.name">{{ col.name }}</option>
                      </optgroup>
                      <optgroup label="📅 Dates" v-if="availableColumns.some(c => c.type === 'date')">
                        <option v-for="col in availableColumns.filter(c => c.type === 'date')" :key="col.name" :value="col.name">{{ col.name }}</option>
                      </optgroup>
                      <optgroup label="# Numériques (groupement)" v-if="availableColumns.some(c => c.type === 'number')">
                        <option v-for="col in availableColumns.filter(c => c.type === 'number')" :key="col.name" :value="col.name">{{ col.name }}</option>
                      </optgroup>
                      <option v-if="availableColumns.length === 0 && widgetConfig.xAxis" :value="widgetConfig.xAxis">{{ widgetConfig.xAxis }}</option>
                      <option v-if="availableColumns.length === 0 && !widgetConfig.xAxis" disabled>— Aucun CSV chargé —</option>
                    </select>
                  </template>
                  <!-- Bar / Line / Area / etc. -->
                  <template v-else>
                    <select v-model="widgetConfig.xAxis" class="axis-fallback-select">
                      <option value="">— Valeur totale —</option>
                      <optgroup label="📅 Dates" v-if="availableColumns.some(c => c.type === 'date')">
                        <option v-for="col in availableColumns.filter(c => c.type === 'date')" :key="col.name" :value="col.name">{{ col.name }}</option>
                      </optgroup>
                      <optgroup label="🔤 Catégories / Texte" v-if="availableColumns.some(c => ['category','boolean','text'].includes(c.type))">
                        <option v-for="col in availableColumns.filter(c => ['category','boolean','text'].includes(c.type))" :key="col.name" :value="col.name">{{ col.name }}</option>
                      </optgroup>
                      <optgroup label="# Numériques (groupement)" v-if="availableColumns.some(c => c.type === 'number')">
                        <option v-for="col in availableColumns.filter(c => c.type === 'number')" :key="col.name" :value="col.name">{{ col.name }}</option>
                      </optgroup>
                      <option v-if="availableColumns.length === 0 && widgetConfig.xAxis" :value="widgetConfig.xAxis">{{ widgetConfig.xAxis }}</option>
                      <option v-if="availableColumns.length === 0 && !widgetConfig.xAxis" disabled>— Aucun CSV chargé —</option>
                    </select>
                  </template>
                </div>

                <!-- ── Mesure / Y ─────────────────────────────── -->
                <div class="axis-well" :class="{ 'axis-well--over': wellHover === 'y' }"
                  @dragover.prevent="wellHover = 'y'" @dragleave="wellHover = null"
                  @drop.prevent="wellDrop($event, 'y')">
                  <div class="axis-well-header">
                    <span class="axis-badge axis-badge--y">{{ selectedWidget.type === 'scatter' ? '#' : selectedWidget.type === 'boxplot' ? 'σ' : aggSymbol(widgetConfig.aggregation) }}</span>
                    <span class="axis-well-label">
                      {{ ['pie','doughnut'].includes(selectedWidget.type) ? 'Mesure (taille des parts)'
                        : selectedWidget.type === 'scatter'  ? 'Axe Y (numérique)'
                        : selectedWidget.type === 'boxplot'  ? 'Variable mesurée (numérique)'
                        : 'Mesure (valeur)' }}
                    </span>
                    <button v-if="widgetConfig.yAxis" class="axis-clear" @click="widgetConfig.yAxis = ''"><i class="pi pi-times"></i></button>
                  </div>
                  <div class="axis-well-body">
                    <div v-if="widgetConfig.yAxis" class="axis-chip axis-chip--measure">
                      <span class="aChip-icon">{{ selectedWidget.type === 'scatter' ? '#' : aggSymbol(widgetConfig.aggregation) }}</span>
                      <span class="aChip-name">{{ widgetConfig.yAxis }}</span>
                    </div>
                    <div v-else class="axis-well-placeholder">
                      <i class="pi pi-arrows-alt"></i>
                      <span>{{ ['pie','doughnut'].includes(selectedWidget.type) ? 'Colonne numérique…' : 'Glisser ici ou choisir' }}</span>
                    </div>
                  </div>
                  <select v-model="widgetConfig.yAxis" class="axis-fallback-select">
                    <option value="">— {{ ['pie','doughnut'].includes(selectedWidget.type) ? 'Compter les lignes (auto)' : 'Sélectionner' }} —</option>
                    <optgroup label="# Colonnes numériques" v-if="availableColumns.some(c => c.type === 'number')">
                      <option v-for="col in availableColumns.filter(c => c.type === 'number')" :key="col.name" :value="col.name">{{ selectedWidget.type === 'scatter' ? '' : aggSymbol(widgetConfig.aggregation) + ' ' }}{{ col.name }}</option>
                    </optgroup>
                    <optgroup label="ƒ Mesures calculées" v-if="selectedWidget.type !== 'scatter' && (datasetStore.currentDataset?.customMeasures ?? []).length > 0">
                      <option v-for="m in (datasetStore.currentDataset?.customMeasures ?? [])" :key="m.id" :value="m.name">ƒ {{ m.name }}</option>
                    </optgroup>
                    <option v-if="availableColumns.length === 0 && widgetConfig.yAxis" :value="widgetConfig.yAxis">{{ widgetConfig.yAxis }}</option>
                    <option v-if="availableColumns.length === 0 && !widgetConfig.yAxis" disabled>— Aucun CSV chargé —</option>
                  </select>
                </div>

              </div>

              <!-- Scatter: label column + dot size + max points -->
              <template v-if="selectedWidget.type === 'scatter'">
                <div class="cfg-row-2" style="margin-top:8px">
                  <div class="cfg-field">
                    <label>Étiquette (tooltip)</label>
                    <select v-model="widgetConfig.labelAxis" class="cfg-select">
                      <option value="">— Aucune —</option>
                      <option v-for="col in availableColumns" :key="col.name" :value="col.name">{{ col.name }}</option>
                    </select>
                  </div>
                  <div class="cfg-field">
                    <label>Taille point</label>
                    <input type="number" v-model.number="widgetConfig.scatterDotSize" min="2" max="12" class="cfg-input" />
                  </div>
                </div>
                <div class="cfg-row-2">
                  <div class="cfg-field">
                    <label>Max points</label>
                    <input type="number" v-model.number="widgetConfig.maxItems" min="10" max="2000" class="cfg-input" />
                  </div>
                </div>
              </template>

              <!-- BoxPlot: max groups uniquement (pas d'agrégation) -->
              <template v-else-if="selectedWidget.type === 'boxplot'">
                <div class="cfg-row-2" style="margin-top:8px">
                  <div class="cfg-field">
                    <label>Max groupes affichés</label>
                    <input type="number" v-model.number="widgetConfig.maxItems" min="2" max="30" class="cfg-input" />
                    <span class="cfg-hint">Limite le nombre de boîtes (défaut : 15)</span>
                  </div>
                </div>
              </template>

              <!-- Aggregation + date/maxItems (non-scatter, non-boxplot) -->
              <template v-else>
              <div class="cfg-row-2">
                <div class="cfg-field">
                  <label>Agrégation</label>
                  <select v-model="widgetConfig.aggregation" class="cfg-select">
                    <option value="none">— Aucune agrégation</option>
                    <option value="sum">Σ Somme</option>
                    <option value="avg">⌀ Moyenne</option>
                    <option value="median">~ Médiane</option>
                    <option value="count"># Nombre de lignes</option>
                    <option value="count_distinct">⊞ Valeurs distinctes</option>
                    <option value="min">↓ Minimum</option>
                    <option value="max">↑ Maximum</option>
                    <option value="std">σ Écart-type</option>
                  </select>
                  <span class="cfg-hint">{{ aggHint(widgetConfig.aggregation) }}</span>
                </div>
                <div class="cfg-field" v-if="availableColumns.find(c => c.name === widgetConfig.xAxis)?.type === 'date'">
                  <label>Granularité</label>
                  <select v-model="widgetConfig.dateGranularity" class="cfg-select">
                    <option value="day">Jour</option><option value="month">Mois</option><option value="year">Année</option>
                  </select>
                </div>
                <div class="cfg-field" v-else>
                  <label>Max éléments</label>
                  <input type="number" v-model.number="widgetConfig.maxItems" min="2" max="50" class="cfg-input" />
                </div>
              </div>
              <!-- Sort -->
              <div class="cfg-row-2" v-if="widgetConfig.xAxis">
                <div class="cfg-field">
                  <label>Trier par</label>
                  <select v-model="widgetConfig.sortBy" class="cfg-select">
                    <option value="value">Valeur</option><option value="label">Libellé</option>
                  </select>
                </div>
                <div class="cfg-field">
                  <label>Ordre</label>
                  <div class="cfg-btn-group">
                    <button :class="{ active: widgetConfig.sortDir === 'desc' }" @click="widgetConfig.sortDir = 'desc'">↓ Déc.</button>
                    <button :class="{ active: widgetConfig.sortDir === 'asc' }"  @click="widgetConfig.sortDir = 'asc'">↑ Cro.</button>
                  </div>
                </div>
              </div>
              </template><!-- end v-else non-scatter -->
            </div>

            <!-- Text widget — content editor -->
            <div class="pp-section" v-if="selectedWidget.type === 'text'">
              <div class="pp-section-title">Contenu</div>
              <div class="cfg-field">
                <textarea
                  v-model="widgetConfig.textContent"
                  class="cfg-textarea"
                  rows="7"
                  placeholder="Saisissez votre texte ici…&#10;Chaque ligne = nouveau paragraphe"
                />
              </div>
            </div>

            <!-- KPI / Gauge — valeur unique -->
            <div class="pp-section" v-if="['kpi','gauge'].includes(selectedWidget.type)">
              <div class="pp-section-title">Valeur KPI</div>

              <!-- ── Mesure (Y) ──────────────────────────────── -->
              <div class="axis-well" :class="{ 'axis-well--over': wellHover === 'y' }"
                @dragover.prevent="wellHover = 'y'" @dragleave="wellHover = null" @drop.prevent="wellDrop($event, 'y')">
                <div class="axis-well-header">
                  <span class="axis-badge axis-badge--y">{{ aggSymbol(widgetConfig.aggregation) }}</span>
                  <span class="axis-well-label">Mesure (colonne numérique)</span>
                  <button v-if="widgetConfig.yAxis" class="axis-clear" @click="widgetConfig.yAxis = ''"><i class="pi pi-times"></i></button>
                </div>
                <div class="axis-well-body">
                  <div v-if="widgetConfig.yAxis" class="axis-chip axis-chip--measure">
                    <span class="aChip-icon">{{ aggSymbol(widgetConfig.aggregation) }}</span>
                    <span class="aChip-name">{{ widgetConfig.yAxis }}</span>
                  </div>
                  <div v-else class="axis-well-placeholder"><i class="pi pi-arrows-alt"></i><span>Glisser une mesure ici</span></div>
                </div>
                <select v-model="widgetConfig.yAxis" class="axis-fallback-select">
                  <option value="">— Sélectionner —</option>
                  <optgroup label="# Colonnes numériques" v-if="availableColumns.some(c => c.type === 'number')">
                    <option v-for="col in availableColumns.filter(c => c.type === 'number')" :key="col.name" :value="col.name">{{ aggSymbol(widgetConfig.aggregation) }} {{ col.name }}</option>
                  </optgroup>
                  <optgroup label="ƒ Mesures calc." v-if="(datasetStore.currentDataset?.customMeasures ?? []).length > 0">
                    <option v-for="m in (datasetStore.currentDataset?.customMeasures ?? [])" :key="m.id" :value="m.name">ƒ {{ m.name }}</option>
                  </optgroup>
                  <option v-if="availableColumns.length === 0 && widgetConfig.yAxis" :value="widgetConfig.yAxis">{{ widgetConfig.yAxis }}</option>
                </select>
              </div>

              <!-- ── Dimension X (sparkline / tendance) — KPI uniquement ── -->
              <div
                v-if="selectedWidget.type === 'kpi'"
                class="axis-well"
                style="margin-top:8px"
                :class="{ 'axis-well--over': wellHover === 'x' }"
                @dragover.prevent="wellHover = 'x'" @dragleave="wellHover = null" @drop.prevent="wellDrop($event, 'x')">
                <div class="axis-well-header">
                  <span class="axis-badge axis-badge--x">X</span>
                  <span class="axis-well-label">Dimension (sparkline / regroupement)</span>
                  <button v-if="widgetConfig.xAxis" class="axis-clear" @click="widgetConfig.xAxis = ''"><i class="pi pi-times"></i></button>
                </div>
                <div class="axis-well-body">
                  <div v-if="widgetConfig.xAxis" class="axis-chip axis-chip--dim">
                    <span class="aChip-icon">{{ getColumnTypeIcon(widgetConfig.xAxis) }}</span>
                    <span class="aChip-name">{{ widgetConfig.xAxis }}</span>
                  </div>
                  <div v-else class="axis-well-placeholder"><i class="pi pi-arrows-alt"></i><span>Optionnel — active la sparkline</span></div>
                </div>
                <select v-model="widgetConfig.xAxis" class="axis-fallback-select">
                  <option value="">— Aucun (valeur globale) —</option>
                  <optgroup label="📅 Dates" v-if="availableColumns.some(c => c.type === 'date')">
                    <option v-for="col in availableColumns.filter(c => c.type === 'date')" :key="col.name" :value="col.name">{{ col.name }}</option>
                  </optgroup>
                  <optgroup label="🔤 Catégories" v-if="availableColumns.some(c => ['category','boolean','text'].includes(c.type))">
                    <option v-for="col in availableColumns.filter(c => ['category','boolean','text'].includes(c.type))" :key="col.name" :value="col.name">{{ col.name }}</option>
                  </optgroup>
                  <option v-if="availableColumns.length === 0 && widgetConfig.xAxis" :value="widgetConfig.xAxis">{{ widgetConfig.xAxis }}</option>
                </select>
              </div>

              <div class="cfg-row-2" style="margin-top:10px">
                <div class="cfg-field">
                  <label>Agrégation</label>
                  <select v-model="widgetConfig.aggregation" class="cfg-select">
                    <option value="none">— Aucune agrégation</option>
                    <option value="sum">Σ Somme</option>
                    <option value="avg">⌀ Moyenne</option>
                    <option value="median">~ Médiane</option>
                    <option value="count"># Nombre de lignes</option>
                    <option value="count_distinct">⊞ Valeurs distinctes</option>
                    <option value="min">↓ Minimum</option>
                    <option value="max">↑ Maximum</option>
                    <option value="std">σ Écart-type</option>
                  </select>
                </div>
                <div class="cfg-field">
                  <label>Suffixe</label>
                  <input type="text" v-model="widgetConfig.kpiSuffix" placeholder="€, %, …" class="cfg-input" maxlength="8" />
                </div>
              </div>
              <div class="cfg-row-2">
                <div class="cfg-field">
                  <label>Tendance %</label>
                  <input type="number" v-model.number="widgetConfig.kpiTrendValue" step="0.1" class="cfg-input" />
                </div>
              </div>
            </div>

            <!-- Taille sur la grille -->
            <div class="pp-section">
              <div class="pp-section-title">Taille sur la grille</div>
              <div class="cfg-field">
                <label>Largeur — <strong>{{ widgetConfig.width }}</strong> col.</label>
                <input type="range" v-model.number="widgetConfig.width" min="2" max="12" step="1" class="cfg-range" />
                <div class="cfg-range-marks"><span>2</span><span>6</span><span>12</span></div>
              </div>
              <div class="cfg-field" style="margin-top:10px">
                <label>Hauteur — <strong>{{ widgetConfig.height }}</strong> lignes</label>
                <input type="range" v-model.number="widgetConfig.height" min="2" max="10" step="1" class="cfg-range" />
                <div class="cfg-range-marks"><span>2</span><span>5</span><span>10</span></div>
              </div>
            </div>
          </template>

          <!-- ═══ TAB METTRE EN FORME ═════════════════════════ -->
          <template v-else-if="showConfigPanel && selectedWidget && ppTab === 'format'">

            <!-- Couleur principale (collapsible) -->
            <div class="pp-section pp-collapsible" :class="{ open: fmtOpen.color }">
              <div class="pp-section-toggle" @click="fmtOpen.color = !fmtOpen.color">
                <span class="pp-section-title">🎨 Couleur principale</span>
                <i :class="fmtOpen.color ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
              </div>
              <div class="pp-section-body" v-show="fmtOpen.color">
                <div class="cfg-palette">
                  <button v-for="c in COLOR_PALETTE" :key="c" class="cfg-pal-dot"
                    :class="{ active: widgetConfig.color === c }" :style="{ background: c }" :title="c"
                    @click="widgetConfig.color = c"><i v-if="widgetConfig.color === c" class="pi pi-check"></i>
                  </button>
                  <label class="cfg-pal-dot cfg-pal-custom" title="Personnalisée">
                    <input type="color" v-model="widgetConfig.color" /><i class="pi pi-palette"></i>
                  </label>
                </div>
                <div class="cfg-color-preview">
                  <span class="swatch" :style="{ background: widgetConfig.color }"></span>
                  <span class="hex">{{ widgetConfig.color }}</span>
                </div>
              </div>
            </div>

            <!-- Arrière-plan (collapsible) -->
            <div class="pp-section pp-collapsible" :class="{ open: fmtOpen.bg }">
              <div class="pp-section-toggle" @click="fmtOpen.bg = !fmtOpen.bg">
                <span class="pp-section-title">🖼 Arrière-plan</span>
                <i :class="fmtOpen.bg ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
              </div>
              <div class="pp-section-body" v-show="fmtOpen.bg">
                <div class="cfg-style-grid">
                  <button v-for="s in CARD_STYLES" :key="s.id" class="cfg-style-btn"
                    :class="{ active: widgetConfig.backgroundType === s.id }"
                    @click="widgetConfig.backgroundType = s.id">
                    <div class="cfg-style-preview" :style="getStylePreview(s.id, widgetConfig.color)"></div>
                    <span>{{ s.label }}</span>
                  </button>
                </div>
                <div class="cfg-palette" style="margin-top:10px">
                  <button v-for="c in BG_PALETTE" :key="c" class="cfg-pal-dot cfg-pal-bg"
                    :class="{ active: widgetConfig.backgroundColor === c }"
                    :style="{ background: c, border: '1px solid rgba(17,23,20,0.12)' }"
                    @click="widgetConfig.backgroundColor = c">
                    <i v-if="widgetConfig.backgroundColor === c" class="pi pi-check"></i>
                  </button>
                  <label class="cfg-pal-dot cfg-pal-custom" title="Personnalisée">
                    <input type="color" :value="widgetConfig.backgroundColor || '#FFFFFF'"
                      @input="(e: Event) => widgetConfig.backgroundColor = (e.target as HTMLInputElement).value" />
                    <i class="pi pi-palette"></i>
                  </label>
                </div>
              </div>
            </div>

            <!-- Options type-spécifique (collapsible) -->
            <!-- KPI -->
            <div class="pp-section pp-collapsible" v-if="selectedWidget.type === 'kpi'" :class="{ open: fmtOpen.typeOptions }">
              <div class="pp-section-toggle" @click="fmtOpen.typeOptions = !fmtOpen.typeOptions">
                <span class="pp-section-title">⚡ Options KPI</span>
                <i :class="fmtOpen.typeOptions ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
              </div>
              <div class="pp-section-body" v-show="fmtOpen.typeOptions">
                <div class="cfg-field">
                  <label>Icône</label>
                  <div class="cfg-icon-grid">
                    <button v-for="ic in KPI_ICONS" :key="ic.id" class="cfg-icon-btn"
                      :class="{ active: widgetConfig.kpiIcon === ic.id }" :title="ic.label"
                      @click="widgetConfig.kpiIcon = ic.id">
                      <i :class="ic.pi"></i><span>{{ ic.label }}</span>
                    </button>
                  </div>
                </div>
                <div class="cfg-toggles">
                  <label class="cfg-toggle"><span>Sparkline</span><input type="checkbox" v-model="widgetConfig.kpiShowSparkline" /><span class="toggle-track"><span class="toggle-thumb"></span></span></label>
                  <label class="cfg-toggle"><span>Tendance</span><input type="checkbox" v-model="widgetConfig.kpiShowTrend" /><span class="toggle-track"><span class="toggle-thumb"></span></span></label>
                </div>
              </div>
            </div>
            <!-- Bar -->
            <div class="pp-section pp-collapsible" v-if="selectedWidget.type === 'bar'" :class="{ open: fmtOpen.typeOptions }">
              <div class="pp-section-toggle" @click="fmtOpen.typeOptions = !fmtOpen.typeOptions">
                <span class="pp-section-title">📊 Options Barres</span>
                <i :class="fmtOpen.typeOptions ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
              </div>
              <div class="pp-section-body" v-show="fmtOpen.typeOptions">
                <div class="cfg-field">
                  <label>Orientation</label>
                  <div class="cfg-btn-group">
                    <button :class="{ active: widgetConfig.barOrientation === 'vertical' }" @click="widgetConfig.barOrientation = 'vertical'"><i class="pi pi-sort-amount-up-alt"></i> Vertical</button>
                    <button :class="{ active: widgetConfig.barOrientation === 'horizontal' }" @click="widgetConfig.barOrientation = 'horizontal'"><i class="pi pi-sort-amount-down"></i> Horizontal</button>
                  </div>
                </div>
                <div class="cfg-field">
                  <label>Arrondi — <strong>{{ widgetConfig.barBorderRadius }}px</strong></label>
                  <input type="range" v-model.number="widgetConfig.barBorderRadius" min="0" max="16" step="1" class="cfg-range" />
                </div>
                <div class="cfg-toggles">
                  <label class="cfg-toggle"><span>Afficher valeurs</span><input type="checkbox" v-model="widgetConfig.barShowValues" /><span class="toggle-track"><span class="toggle-thumb"></span></span></label>
                </div>
              </div>
            </div>
            <!-- Line / Area -->
            <div class="pp-section pp-collapsible" v-if="['line','area'].includes(selectedWidget.type)" :class="{ open: fmtOpen.typeOptions }">
              <div class="pp-section-toggle" @click="fmtOpen.typeOptions = !fmtOpen.typeOptions">
                <span class="pp-section-title">📈 Options Courbe</span>
                <i :class="fmtOpen.typeOptions ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
              </div>
              <div class="pp-section-body" v-show="fmtOpen.typeOptions">
                <div class="cfg-toggles">
                  <label class="cfg-toggle"><span>Courbe lissée</span><input type="checkbox" v-model="widgetConfig.lineSmooth" /><span class="toggle-track"><span class="toggle-thumb"></span></span></label>
                  <label class="cfg-toggle"><span>Remplissage</span><input type="checkbox" v-model="widgetConfig.lineFill" /><span class="toggle-track"><span class="toggle-thumb"></span></span></label>
                  <label class="cfg-toggle"><span>Points visibles</span><input type="checkbox" v-model="widgetConfig.lineShowDots" /><span class="toggle-track"><span class="toggle-thumb"></span></span></label>
                  <label class="cfg-toggle"><span>Libellés valeurs</span><input type="checkbox" v-model="widgetConfig.lineShowLabels" /><span class="toggle-track"><span class="toggle-thumb"></span></span></label>
                </div>
              </div>
            </div>
            <!-- Pie / Doughnut -->
            <div class="pp-section pp-collapsible" v-if="['pie','doughnut'].includes(selectedWidget.type)" :class="{ open: fmtOpen.typeOptions }">
              <div class="pp-section-toggle" @click="fmtOpen.typeOptions = !fmtOpen.typeOptions">
                <span class="pp-section-title">🥧 Options Camembert</span>
                <i :class="fmtOpen.typeOptions ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
              </div>
              <div class="pp-section-body" v-show="fmtOpen.typeOptions">

                <!-- Info banner -->
                <div class="pie-info-banner">
                  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>
                  <span>Idéal pour comparer la <strong>répartition en %</strong> de plusieurs catégories. Fonctionne mieux avec <strong>3 à 8 parts</strong>.</span>
                </div>

                <!-- Max slices + légende -->
                <div class="cfg-row-2">
                  <div class="cfg-field">
                    <label>Max de parts</label>
                    <input type="number" v-model.number="widgetConfig.pieMaxSlices" min="2" max="20" class="cfg-input" />
                    <span class="cfg-hint">Les parts supplémentaires sont regroupées en « Autres »</span>
                  </div>
                  <div class="cfg-field">
                    <label>Position légende</label>
                    <select v-model="widgetConfig.pieLegendPosition" class="cfg-select">
                      <option value="right">Droite</option>
                      <option value="bottom">Bas</option>
                      <option value="none">Masquée</option>
                    </select>
                    <span class="cfg-hint">Tableau de couleurs associé à chaque catégorie</span>
                  </div>
                </div>

                <!-- Toggles avec descriptions -->
                <div class="cfg-toggles">
                  <label class="cfg-toggle">
                    <span>
                      Labels sur les parts
                      <em class="cfg-toggle-sub">Affiche nom · valeur · % dans chaque tranche</em>
                    </span>
                    <input type="checkbox" v-model="widgetConfig.pieShowLabels" />
                    <span class="toggle-track"><span class="toggle-thumb"></span></span>
                  </label>
                </div>

                <!-- Numeric binning (visible only when xAxis is a number column) -->
                <div
                  class="cfg-field"
                  v-if="availableColumns.find(c => c.name === widgetConfig.xAxis)?.type === 'number'"
                >
                  <label>Intervalles (binning)</label>
                  <input
                    type="number"
                    v-model.number="widgetConfig.numericBins"
                    min="2"
                    max="20"
                    class="cfg-input"
                  />
                  <span class="cfg-hint">
                    Regroupe les valeurs numériques en tranches égales
                    (ex. <em>{{ widgetConfig.xAxis || 'âge' }} ≥ 10 et {{ widgetConfig.xAxis || 'âge' }} &lt; 20</em>)
                  </span>
                </div>

                <!-- Type hint -->
                <div class="pie-type-hint" v-if="selectedWidget.type === 'doughnut'">
                  <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><circle cx="12" cy="12" r="4"/></svg>
                  Donut : la zone centrale peut afficher un KPI ou une valeur totale au survol.
                </div>
                <div class="pie-type-hint" v-else>
                  <svg width="11" height="11" viewBox="0 0 24 24" fill="currentColor"><path d="M12 2a10 10 0 0 1 10 10H12V2z" opacity="0.7"/><path d="M12 2v10l-7.07 7.07A10 10 0 0 1 12 2z" opacity="0.4"/><circle cx="12" cy="12" r="10" fill="none" stroke="currentColor" stroke-width="1.5"/></svg>
                  Camembert plein : chaque secteur occupe un angle proportionnel à sa valeur.
                </div>
              </div>
            </div>

            <!-- Gauge options -->
            <div class="pp-section pp-collapsible" v-if="selectedWidget.type === 'gauge'" :class="{ open: fmtOpen.typeOptions }">
              <div class="pp-section-toggle" @click="fmtOpen.typeOptions = !fmtOpen.typeOptions">
                <span class="pp-section-title">🔵 Options Jauge</span>
                <i :class="fmtOpen.typeOptions ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
              </div>
              <div class="pp-section-body" v-show="fmtOpen.typeOptions">
                <div class="cfg-row-2">
                  <div class="cfg-field">
                    <label>Valeur min</label>
                    <input type="number" v-model.number="widgetConfig.gaugeMin" class="cfg-input" />
                  </div>
                  <div class="cfg-field">
                    <label>Valeur max</label>
                    <input type="number" v-model.number="widgetConfig.gaugeMax" class="cfg-input" />
                  </div>
                </div>
                <div class="cfg-gauge-preview">
                  <span>Plage : <strong>{{ widgetConfig.gaugeMin }}</strong> → <strong>{{ widgetConfig.gaugeMax }}</strong></span>
                </div>
              </div>
            </div>

            <!-- Text widget options -->
            <div class="pp-section pp-collapsible" v-if="selectedWidget.type === 'text'" :class="{ open: fmtOpen.typeOptions }">
              <div class="pp-section-toggle" @click="fmtOpen.typeOptions = !fmtOpen.typeOptions">
                <span class="pp-section-title">✏️ Style du texte</span>
                <i :class="fmtOpen.typeOptions ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
              </div>
              <div class="pp-section-body" v-show="fmtOpen.typeOptions">
                <div class="cfg-field">
                  <label>Taille — <strong>{{ widgetConfig.textFontSize }}px</strong></label>
                  <input type="range" v-model.number="widgetConfig.textFontSize" min="10" max="36" step="1" class="cfg-range"/>
                  <div class="cfg-range-marks"><span>10</span><span>18</span><span>36</span></div>
                </div>
                <div class="cfg-field" style="margin-top:10px">
                  <label>Alignement</label>
                  <div class="cfg-btn-group">
                    <button :class="{ active: widgetConfig.textAlign === 'left' }"   @click="widgetConfig.textAlign = 'left'">   <i class="pi pi-align-left"></i>   Gauche</button>
                    <button :class="{ active: widgetConfig.textAlign === 'center' }" @click="widgetConfig.textAlign = 'center'"> <i class="pi pi-align-center"></i> Centre</button>
                    <button :class="{ active: widgetConfig.textAlign === 'right' }"  @click="widgetConfig.textAlign = 'right'">  <i class="pi pi-align-right"></i>  Droite</button>
                  </div>
                </div>
                <div class="cfg-field" style="margin-top:10px">
                  <label>Police</label>
                  <select v-model="widgetConfig.textFontFamily" class="cfg-select">
                    <option value="inherit">Défaut (hérité)</option>
                    <option value="'Inter', system-ui, sans-serif">Inter / Système</option>
                    <option value="Arial, sans-serif">Arial</option>
                    <option value="'Helvetica Neue', Helvetica, sans-serif">Helvetica</option>
                    <option value="Georgia, serif">Georgia</option>
                    <option value="'Times New Roman', Times, serif">Times New Roman</option>
                    <option value="'Courier New', Courier, monospace">Courier New</option>
                    <option value="Verdana, sans-serif">Verdana</option>
                    <option value="Trebuchet MS, sans-serif">Trebuchet MS</option>
                  </select>
                </div>
              </div>
            </div>

            <!-- Table widget options -->
            <div class="pp-section pp-collapsible" v-if="selectedWidget.type === 'table'" :class="{ open: fmtOpen.typeOptions }">
              <div class="pp-section-toggle" @click="fmtOpen.typeOptions = !fmtOpen.typeOptions">
                <span class="pp-section-title">📋 Options Tableau</span>
                <i :class="fmtOpen.typeOptions ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
              </div>
              <div class="pp-section-body" v-show="fmtOpen.typeOptions">

                <!-- Colonnes -->
                <div class="cfg-field">
                  <div class="cfg-label-row">
                    <label>Colonnes affichées</label>
                    <div class="cfg-col-actions">
                      <button
                        class="cfg-micro-btn"
                        @click="widgetConfig.tableColumns = availableColumns.map(c => c.name)"
                        :disabled="availableColumns.length === 0"
                      >Tout</button>
                      <button
                        class="cfg-micro-btn"
                        @click="widgetConfig.tableColumns = []"
                        :disabled="widgetConfig.tableColumns.length === 0"
                      >Aucune</button>
                    </div>
                  </div>
                  <span class="cfg-hint">
                    <template v-if="widgetConfig.tableColumns.length === 0">Toutes les colonnes affichées</template>
                    <template v-else>{{ widgetConfig.tableColumns.length }} / {{ availableColumns.length }} sélectionnée(s)</template>
                  </span>

                  <div v-if="availableColumns.length === 0" class="cfg-no-data">
                    <i class="pi pi-database" style="opacity:.4"/>
                    Importez un dataset pour choisir les colonnes
                  </div>
                  <div v-else class="cfg-checkbox-list">
                    <label
                      v-for="col in availableColumns"
                      :key="col.name"
                      class="cfg-checkbox-item"
                    >
                      <input
                        type="checkbox"
                        :checked="widgetConfig.tableColumns.length === 0 || widgetConfig.tableColumns.includes(col.name)"
                        @change="(e) => {
                          const v = col.name
                          const allCols = availableColumns.map(c => c.name)
                          // If currently 'all' mode (empty array), expand to explicit list first
                          let current = widgetConfig.tableColumns.length === 0 ? [...allCols] : [...widgetConfig.tableColumns]
                          if ((e.target as HTMLInputElement).checked) {
                            if (!current.includes(v)) current.push(v)
                          } else {
                            current = current.filter(c => c !== v)
                          }
                          // If all selected, revert to 'all' mode (empty array = show all)
                          widgetConfig.tableColumns = current.length === allCols.length ? [] : current
                        }"
                      />
                      <span class="cfg-col-name">{{ col.name }}</span>
                      <span
                        class="cfg-col-type-badge"
                        :class="`col-type-${col.type}`"
                      >{{ col.type === 'number' ? '123' : col.type === 'date' ? 'date' : 'abc' }}</span>
                    </label>
                  </div>
                </div>

                <!-- Lignes par page -->
                <div class="cfg-field" style="margin-top:12px">
                  <label>Lignes par page — <strong>{{ widgetConfig.tablePageSize }}</strong></label>
                  <input
                    type="range"
                    v-model.number="widgetConfig.tablePageSize"
                    min="5" max="50" step="5"
                    class="cfg-range"
                  />
                  <div class="cfg-range-marks"><span>5</span><span>25</span><span>50</span></div>
                </div>

                <!-- Limite totale de données -->
                <div class="cfg-field" style="margin-top:10px">
                  <label>Données chargées (max lignes) — <strong>{{ widgetConfig.tableRowLimit }}</strong></label>
                  <input
                    type="range"
                    v-model.number="widgetConfig.tableRowLimit"
                    min="50" max="5000" step="50"
                    class="cfg-range"
                  />
                  <div class="cfg-range-marks"><span>50</span><span>1 000</span><span>5 000</span></div>
                  <span class="cfg-hint">Nombre total de lignes extraites du dataset</span>
                </div>

                <!-- Barre de recherche -->
                <div class="cfg-field" style="margin-top:10px">
                  <label>Barre de recherche</label>
                  <div class="cfg-btn-group">
                    <button :class="{ active: widgetConfig.tableShowSearch }"  @click="widgetConfig.tableShowSearch = true">
                      <i class="pi pi-search"/> Affichée
                    </button>
                    <button :class="{ active: !widgetConfig.tableShowSearch }" @click="widgetConfig.tableShowSearch = false">
                      <i class="pi pi-eye-slash"/> Masquée
                    </button>
                  </div>
                </div>

              </div>
            </div>

            <!-- Heatmap widget options -->
            <div class="pp-section pp-collapsible" v-if="selectedWidget.type === 'heatmap'" :class="{ open: fmtOpen.typeOptions }">
              <div class="pp-section-toggle" @click="fmtOpen.typeOptions = !fmtOpen.typeOptions">
                <span class="pp-section-title">🌡️ Options Carte thermique</span>
                <i :class="fmtOpen.typeOptions ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
              </div>
              <div class="pp-section-body" v-show="fmtOpen.typeOptions">
                <div class="pie-info-banner">
                  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>
                  <span>Visualise la <strong>corrélation de Pearson</strong> entre les colonnes numériques. Bleu = corrélation négative, Rouge = positive.</span>
                </div>
                <div class="cfg-field">
                  <label>Colonnes numériques à corréler</label>
                  <span class="cfg-hint">Sélectionnez au moins 2 colonnes numériques (toutes si aucune sélection)</span>
                  <div class="cfg-checkbox-list">
                    <label
                      v-for="col in availableColumns.filter(c => c.type === 'number')"
                      :key="col.name"
                      class="cfg-checkbox-item"
                    >
                      <input
                        type="checkbox"
                        :value="col.name"
                        :checked="widgetConfig.heatmapColumns.includes(col.name)"
                        @change="(e) => {
                          const v = col.name
                          if ((e.target as HTMLInputElement).checked) {
                            if (!widgetConfig.heatmapColumns.includes(v)) widgetConfig.heatmapColumns = [...widgetConfig.heatmapColumns, v]
                          } else {
                            widgetConfig.heatmapColumns = widgetConfig.heatmapColumns.filter(c => c !== v)
                          }
                        }"
                      />
                      <span>{{ col.name }}</span>
                    </label>
                    <div v-if="availableColumns.filter(c => c.type === 'number').length === 0" class="cfg-no-data">
                      Aucune colonne numérique détectée dans le dataset
                    </div>
                  </div>
                </div>
                <div class="cfg-field" style="margin-top:6px" v-if="widgetConfig.heatmapColumns.length > 0">
                  <span class="cfg-hint">
                    {{ widgetConfig.heatmapColumns.length }} colonne(s) :
                    <em>{{ widgetConfig.heatmapColumns.join(', ') }}</em>
                  </span>
                </div>
              </div>
            </div>

            <!-- Image widget options -->
            <div class="pp-section pp-collapsible" v-if="selectedWidget.type === 'image'" :class="{ open: fmtOpen.typeOptions }">
              <div class="pp-section-toggle" @click="fmtOpen.typeOptions = !fmtOpen.typeOptions">
                <span class="pp-section-title">🖼️ Options Image</span>
                <i :class="fmtOpen.typeOptions ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
              </div>
              <div class="pp-section-body" v-show="fmtOpen.typeOptions">

                <!-- Upload file -->
                <div class="cfg-field">
                  <label>Importer un fichier</label>
                  <div class="img-upload-zone" @click="triggerImageUpload" @dragover.prevent @drop.prevent="handleImageDrop">
                    <i class="pi pi-upload"></i>
                    <span>Cliquez ou déposez une image ici</span>
                    <span class="img-upload-hint">PNG, JPG, GIF, SVG, WebP — max 5 Mo</span>
                    <input
                      ref="imageFileInput"
                      type="file"
                      accept="image/*"
                      style="display:none"
                      @change="handleImageFile"
                    />
                  </div>
                  <!-- Preview thumbnail -->
                  <div v-if="widgetConfig.imageBase64" class="img-thumb-preview">
                    <img :src="widgetConfig.imageBase64" alt="preview" />
                    <button class="img-thumb-clear" @click="widgetConfig.imageBase64 = ''; widgetConfig.imageUrl = ''" title="Supprimer">
                      <i class="pi pi-times"></i>
                    </button>
                  </div>
                </div>

                <!-- OR separator -->
                <div class="cfg-or-sep"><span>ou</span></div>

                <!-- URL input -->
                <div class="cfg-field">
                  <label>URL de l'image</label>
                  <input
                    type="url"
                    v-model="widgetConfig.imageUrl"
                    placeholder="https://exemple.com/image.png"
                    class="cfg-input"
                    @input="widgetConfig.imageBase64 = ''"
                  />
                  <span class="cfg-hint">Lien direct vers une image en ligne</span>
                </div>

                <!-- Fit mode -->
                <div class="cfg-field" style="margin-top:10px">
                  <label>Mode d'affichage</label>
                  <div class="cfg-btn-group">
                    <button :class="{ active: widgetConfig.imageFit === 'contain' }"  @click="widgetConfig.imageFit = 'contain'">Contenir</button>
                    <button :class="{ active: widgetConfig.imageFit === 'cover' }"    @click="widgetConfig.imageFit = 'cover'">Couvrir</button>
                    <button :class="{ active: widgetConfig.imageFit === 'fill' }"     @click="widgetConfig.imageFit = 'fill'">Étirer</button>
                  </div>
                  <span class="cfg-hint">
                    <em>Contenir</em> conserve les proportions · <em>Couvrir</em> remplit le cadre · <em>Étirer</em> déforme si nécessaire
                  </span>
                </div>

                <!-- Caption/alt text -->
                <div class="cfg-field" style="margin-top:10px">
                  <label>Légende (optionnel)</label>
                  <input
                    type="text"
                    v-model="widgetConfig.imageAlt"
                    placeholder="ex : Logo entreprise, Carte de localisation…"
                    class="cfg-input"
                    maxlength="80"
                  />
                  <span class="cfg-hint">Texte affiché sous l'image et utilisé pour l'accessibilité</span>
                </div>
              </div>
            </div>

            <!-- ═══ MAP CONFIG ═══ -->
            <div class="pp-section pp-collapsible" v-if="selectedWidget.type === 'map'" :class="{ open: fmtOpen.typeOptions }">
              <div class="pp-section-toggle" @click="fmtOpen.typeOptions = !fmtOpen.typeOptions">
                <span class="pp-section-title">🗺️ Localisations</span>
                <i :class="fmtOpen.typeOptions ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
              </div>
              <div class="pp-section-body" v-show="fmtOpen.typeOptions">

                <!-- Location search -->
                <div class="cfg-field">
                  <label>Rechercher une ville / adresse</label>
                  <div style="display:flex;gap:6px">
                    <input
                      type="text"
                      v-model="mapSearchQuery"
                      placeholder="ex : Paris, Lyon, Marseille…"
                      class="cfg-input"
                      style="flex:1"
                      @keydown.enter.prevent="searchMapLocation"
                    />
                    <button class="cfg-btn-primary" @click="searchMapLocation" :disabled="mapSearching">
                      <i :class="mapSearching ? 'pi pi-spin pi-spinner' : 'pi pi-search'"></i>
                    </button>
                  </div>
                  <span v-if="mapSearchError" class="cfg-hint" style="color:#f87171">{{ mapSearchError }}</span>
                </div>

                <!-- Search results -->
                <div v-if="mapSearchResults.length" class="map-search-results">
                  <div
                    v-for="r in mapSearchResults"
                    :key="r.place_id"
                    class="map-search-item"
                    @click="addMapMarker(r)"
                  >
                    <i class="pi pi-map-marker" style="color:#3b82f6;flex-shrink:0"></i>
                    <span>{{ r.display_name }}</span>
                  </div>
                </div>

                <!-- Markers list -->
                <div v-if="widgetConfig.mapMarkers.length" style="margin-top:10px">
                  <label style="font-size:11px;color:var(--color-text-muted);text-transform:uppercase;letter-spacing:.05em">
                    Marqueurs ({{ widgetConfig.mapMarkers.length }})
                  </label>
                  <div class="map-markers-list">
                    <div
                      v-for="(m, i) in widgetConfig.mapMarkers"
                      :key="i"
                      class="map-marker-row"
                    >
                      <input
                        type="color"
                        v-model="m.color"
                        class="map-marker-color"
                        title="Couleur du marqueur"
                      />
                      <input
                        type="text"
                        v-model="m.label"
                        class="cfg-input"
                        style="flex:1;font-size:11px"
                        placeholder="Étiquette"
                      />
                      <button class="map-marker-del" @click="widgetConfig.mapMarkers.splice(i,1)" title="Supprimer">
                        <i class="pi pi-times"></i>
                      </button>
                    </div>
                  </div>
                </div>

                <!-- Couleur par défaut -->
                <div class="cfg-field" style="margin-top:10px">
                  <label>Couleur des marqueurs</label>
                  <div style="display:flex;align-items:center;gap:8px">
                    <input type="color" v-model="widgetConfig.markerColor" style="width:36px;height:28px;border-radius:6px;border:none;cursor:pointer;background:none" />
                    <span class="cfg-hint" style="margin:0">{{ widgetConfig.markerColor }}</span>
                  </div>
                </div>

                <!-- Zoom -->
                <div class="cfg-field" style="margin-top:10px">
                  <label>Zoom initial — <b>{{ widgetConfig.mapZoom }}</b></label>
                  <input type="range" min="2" max="18" v-model.number="widgetConfig.mapZoom" class="cfg-slider" />
                </div>

              </div>
            </div>

            <!-- Description & unités (collapsible) -->
            <div class="pp-section pp-collapsible" :class="{ open: fmtOpen.units }">
              <div class="pp-section-toggle" @click="fmtOpen.units = !fmtOpen.units">
                <span class="pp-section-title">
                  📝 Description &amp; unités
                  <span v-if="widgetConfig.valueSuffix || widgetConfig.valuePrefix" class="pp-section-badge">
                    {{ widgetConfig.valuePrefix }}1 234{{ widgetConfig.valueSuffix }}
                  </span>
                </span>
                <i :class="fmtOpen.units ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
              </div>
              <div class="pp-section-body" v-show="fmtOpen.units">
                <!-- Description subtitle -->
                <div class="cfg-field">
                  <label>Sous-titre / description</label>
                  <input type="text" v-model="widgetConfig.description"
                    placeholder="ex : Chiffre d'affaires mensuel"
                    class="cfg-input" maxlength="80" />
                </div>

                <!-- Prefix + suffix row -->
                <div class="cfg-row-2" style="margin-top:8px">
                  <div class="cfg-field">
                    <label>Préfixe</label>
                    <input type="text" v-model="widgetConfig.valuePrefix"
                      placeholder="€, $…" class="cfg-input" maxlength="8" />
                  </div>
                  <div class="cfg-field">
                    <label>Suffixe</label>
                    <input type="text" v-model="widgetConfig.valueSuffix"
                      placeholder="%, kg…" class="cfg-input" maxlength="8" />
                  </div>
                </div>

                <!-- Live preview -->
                <div class="cfg-unit-preview" v-if="widgetConfig.valueSuffix || widgetConfig.valuePrefix">
                  <span class="cup-label">Aperçu :</span>
                  <span class="cup-value">{{ widgetConfig.valuePrefix }}1 234{{ widgetConfig.valueSuffix }}</span>
                  <span class="cup-value cup-value--lg">{{ widgetConfig.valuePrefix }}42k{{ widgetConfig.valueSuffix }}</span>
                </div>

                <!-- Quick presets -->
                <div class="cfg-presets">
                  <span class="cfg-presets-label">Raccourcis :</span>
                  <!-- Reset button -->
                  <button class="cfg-preset-btn cfg-preset-reset"
                    :class="{ active: !widgetConfig.valueSuffix && !widgetConfig.valuePrefix }"
                    @click="widgetConfig.valueSuffix = ''; widgetConfig.valuePrefix = ''"
                    title="Aucune unité">
                    <i class="pi pi-times"></i>
                  </button>
                  <button v-for="p in VALUE_PRESETS" :key="p.value" class="cfg-preset-btn"
                    :class="{ active: widgetConfig.valueSuffix === p.value && widgetConfig.valuePrefix === (p.prefix || '') }"
                    @click="widgetConfig.valueSuffix = p.value; widgetConfig.valuePrefix = p.prefix || ''">
                    {{ p.label }}
                  </button>
                </div>

                <!-- Y-axis label (charts only) -->
                <div class="cfg-field" v-if="selectedWidget && ['bar','line','area'].includes(selectedWidget.type)" style="margin-top:10px">
                  <label>Label axe Y</label>
                  <input type="text" v-model="widgetConfig.yAxisLabel"
                    placeholder="ex : Ventes (€)" class="cfg-input" maxlength="40" />
                </div>
              </div>
            </div>

            <!-- Taille (collapsible) -->
            <div class="pp-section pp-collapsible" :class="{ open: fmtOpen.size }">
              <div class="pp-section-toggle" @click="fmtOpen.size = !fmtOpen.size">
                <span class="pp-section-title">📐 Taille du widget</span>
                <i :class="fmtOpen.size ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"></i>
              </div>
              <div class="pp-section-body" v-show="fmtOpen.size">
                <div class="cfg-field">
                  <label>Largeur — <strong>{{ widgetConfig.width }}</strong> col.</label>
                  <input type="range" v-model.number="widgetConfig.width" min="2" max="12" step="1" class="cfg-range" />
                  <div class="cfg-range-marks"><span>2</span><span>6</span><span>12</span></div>
                </div>
                <div class="cfg-field" style="margin-top:10px">
                  <label>Hauteur — <strong>{{ widgetConfig.height }}</strong> lignes</label>
                  <input type="range" v-model.number="widgetConfig.height" min="2" max="10" step="1" class="cfg-range" />
                  <div class="cfg-range-marks"><span>2</span><span>5</span><span>10</span></div>
                </div>
              </div>
            </div>

          </template>

          <!-- ═══ TAB CHAMPS (+ état idle) ═══════════════════ -->
          <template v-else>

            <div v-if="!datasetStore.currentDataset" class="fields-empty-state">
              <div class="fields-empty-icon"><i class="pi pi-table"></i></div>
              <p class="fields-empty-title">Aucun dataset</p>
              <p class="fields-empty-sub">Importez un fichier CSV pour accéder à vos champs</p>
              <button class="fields-empty-cta" @click="triggerCsvImport"><i class="pi pi-cloud-upload"></i> Importer CSV</button>
            </div>

            <template v-else>
              <div class="fields-stats-row">
                <div class="fields-stat"><span class="fstat-val">{{ datasetStore.currentDataset.data.length.toLocaleString('fr-FR') }}</span><span class="fstat-lbl">lignes</span></div>
                <div class="fstat-sep"></div>
                <div class="fields-stat"><span class="fstat-val">{{ availableColumns.filter(c => c.type === 'number').length }}</span><span class="fstat-lbl">mesures</span></div>
                <div class="fstat-sep"></div>
                <div class="fields-stat"><span class="fstat-val">{{ availableColumns.filter(c => c.type !== 'number').length }}</span><span class="fstat-lbl">dim.</span></div>
              </div>
              <div class="fields-hint-bar" v-if="!showConfigPanel">
                <i class="pi pi-info-circle"></i> Ouvrez un widget pour assigner des champs
              </div>
              <!-- Mesures -->
              <div class="fields-group" v-if="availableColumns.some(c => c.type === 'number')">
                <div class="fields-group-header">
                  <span class="fgroup-icon fgroup-icon--measure">Σ</span>
                  <span class="fgroup-label">Mesures</span>
                  <span class="fgroup-count">{{ availableColumns.filter(c => c.type === 'number').length }}</span>
                </div>
                <div class="fields-list">
                  <div v-for="col in availableColumns.filter(c => c.type === 'number')" :key="col.name"
                    class="field-item field-item--measure"
                    :class="{ 'field-item--active': showConfigPanel && widgetConfig.yAxis === col.name, 'field-item--dragging': draggingField?.name === col.name }"
                    draggable="true" @dragstart="fieldDragStart($event, col)" @dragend="draggingField = null"
                    @click="assignFieldAuto(col)">
                    <span class="fi-type fi-type--measure">Σ</span>
                    <span class="fi-name">{{ col.name }}</span>
                    <span class="fi-axis-hint" v-if="showConfigPanel && widgetConfig.yAxis !== col.name">→ Y</span>
                    <span class="fi-axis-badge fi-axis-badge--y" v-if="showConfigPanel && widgetConfig.yAxis === col.name">Y</span>
                  </div>
                </div>
              </div>
              <!-- Dimensions -->
              <div class="fields-group" v-if="availableColumns.some(c => c.type !== 'number')">
                <div class="fields-group-header">
                  <span class="fgroup-icon fgroup-icon--dim">◇</span>
                  <span class="fgroup-label">Dimensions</span>
                  <span class="fgroup-count">{{ availableColumns.filter(c => c.type !== 'number').length }}</span>
                </div>
                <div class="fields-list">
                  <div v-for="col in availableColumns.filter(c => c.type !== 'number')" :key="col.name"
                    class="field-item field-item--dim"
                    :class="{ 'field-item--date': col.type === 'date', 'field-item--active': showConfigPanel && widgetConfig.xAxis === col.name, 'field-item--dragging': draggingField?.name === col.name }"
                    draggable="true" @dragstart="fieldDragStart($event, col)" @dragend="draggingField = null"
                    @click="assignFieldAuto(col)">
                    <span class="fi-type fi-type--dim">{{ col.type === 'date' ? '📅' : col.type === 'boolean' ? '◉' : '🔤' }}</span>
                    <span class="fi-name">{{ col.name }}</span>
                    <span class="fi-axis-hint" v-if="showConfigPanel && widgetConfig.xAxis !== col.name">→ X</span>
                    <span class="fi-axis-badge fi-axis-badge--x" v-if="showConfigPanel && widgetConfig.xAxis === col.name">X</span>
                  </div>
                </div>
              </div>

              <!-- ── Mesures calculées ─────────────────────── -->
              <div class="fields-group fields-group--calc">
                <div class="fields-group-header">
                  <span class="fgroup-icon fgroup-icon--calc">ƒ</span>
                  <span class="fgroup-label">Mesures calc.</span>
                  <span class="fgroup-count" v-if="(datasetStore.currentDataset?.customMeasures ?? []).length > 0">
                    {{ (datasetStore.currentDataset?.customMeasures ?? []).length }}
                  </span>
                  <button class="fgroup-add-btn" @click="showNewMeasureForm = !showNewMeasureForm"
                    :class="{ active: showNewMeasureForm }" title="Créer une mesure">
                    <i :class="showNewMeasureForm ? 'pi pi-minus' : 'pi pi-plus'"></i>
                  </button>
                </div>

                <!-- ── Formulaire nouvelle mesure ── -->
                <div v-if="showNewMeasureForm" class="new-measure-form">
                  <input v-model="newMeasure.name" placeholder="Nom de la mesure…"
                    class="cfg-input nm-name" maxlength="40" />

                  <div class="nm-row">
                    <label>Type de formule</label>
                    <select v-model="newMeasure.formula" class="cfg-select">
                      <option value="simple">Σ Simple (agrégation)</option>
                      <option value="multiply">A × constante</option>
                      <option value="ratio">A ÷ B (ratio)</option>
                      <option value="difference">A − B (différence)</option>
                    </select>
                  </div>

                  <div class="nm-row">
                    <label>{{ ['ratio','difference'].includes(newMeasure.formula) ? 'Colonne A' : 'Colonne' }}</label>
                    <select v-model="newMeasure.col1" class="cfg-select">
                      <option value="">— Choisir —</option>
                      <option v-for="col in availableColumns.filter(c => c.type === 'number')" :key="col.name" :value="col.name">
                        {{ col.name }}
                      </option>
                    </select>
                  </div>

                  <div class="nm-row" v-if="newMeasure.formula === 'multiply'">
                    <label>Constante (k)</label>
                    <input type="number" v-model.number="newMeasure.constant" step="0.01" class="cfg-input" />
                  </div>

                  <div class="nm-row" v-if="['ratio','difference'].includes(newMeasure.formula)">
                    <label>Colonne B</label>
                    <select v-model="newMeasure.col2" class="cfg-select">
                      <option value="">— Choisir —</option>
                      <option v-for="col in availableColumns.filter(c => c.type === 'number')" :key="col.name" :value="col.name">
                        {{ col.name }}
                      </option>
                    </select>
                  </div>

                  <div class="nm-row nm-pct-row" v-if="newMeasure.formula === 'ratio'">
                    <label class="cfg-toggle nm-toggle">
                      <span>× 100 (pourcentage)</span>
                      <input type="checkbox" v-model="newMeasure.pct" />
                      <span class="toggle-track"><span class="toggle-thumb"></span></span>
                    </label>
                  </div>

                  <div class="nm-row">
                    <label>Agrégation</label>
                    <select v-model="newMeasure.aggregation" class="cfg-select">
                      <option value="none">— Aucune agrégation</option>
                      <option value="sum">Σ Somme</option>
                      <option value="avg">⌀ Moyenne</option>
                      <option value="count"># Nombre</option>
                      <option value="max">↑ Maximum</option>
                      <option value="min">↓ Minimum</option>
                    </select>
                  </div>

                  <div class="nm-actions">
                    <button class="nm-cancel" @click="showNewMeasureForm = false">Annuler</button>
                    <button class="nm-create" @click="createCustomMeasure"
                      :disabled="!newMeasure.name.trim() || !newMeasure.col1">
                      <i class="pi pi-check"></i> Créer
                    </button>
                  </div>
                </div>

                <!-- Liste des mesures créées -->
                <div class="fields-list" v-if="(datasetStore.currentDataset?.customMeasures ?? []).length > 0">
                  <div
                    v-for="m in (datasetStore.currentDataset?.customMeasures ?? [])"
                    :key="m.id"
                    class="field-item field-item--measure field-item--calc"
                    :class="{ 'field-item--active': showConfigPanel && widgetConfig.yAxis === m.name }"
                    :title="customMeasureTip(m)"
                    draggable="true"
                    @dragstart="fieldDragStartCustom($event, m)"
                    @dragend="draggingField = null"
                    @click="assignCustomMeasure(m)"
                  >
                    <span class="fi-type fi-type--calc">{{ customMeasureFormula(m.formula) }}</span>
                    <span class="fi-name">{{ m.name }}</span>
                    <button class="fi-delete-btn" @click.stop="datasetStore.removeCustomMeasure(m.id)"
                      title="Supprimer cette mesure"><i class="pi pi-times"></i></button>
                    <span class="fi-axis-badge fi-axis-badge--y" v-if="showConfigPanel && widgetConfig.yAxis === m.name">Y</span>
                  </div>
                </div>
                <div v-else-if="!showNewMeasureForm" class="nm-empty-hint">
                  <i class="pi pi-plus-circle"></i> Cliquez <strong>+</strong> pour créer une mesure
                </div>
              </div>

            </template>
          </template>

        </div><!-- /pp-body -->

        <!-- ── Footer Apply/Cancel ── -->
        <div v-if="showConfigPanel && selectedWidget" class="pp-footer">
          <button class="cfg-btn-cancel" @click="showConfigPanel = false">Annuler</button>
          <button class="cfg-btn-apply" @click="applyWidgetConfig">
            <i class="pi pi-check"></i> Appliquer
          </button>
        </div>

      </template>
    </aside>

    <!-- Modal Nouveau Dashboard -->
    <div v-if="showNewDashModal" class="modal-overlay">
      <div class="modal-content">
        <h3>Nouveau Tableau de Bord</h3>
        <div class="form-group">
          <label>Nom du dashboard</label>
          <input v-model="newDashName" placeholder="Ex: Rapport Mensuel Avril" />
        </div>
        <div class="modal-footer">
          <button @click="showNewDashModal = false" class="cancel-btn">Annuler</button>
          <button @click="createNewDashboard" class="confirm-btn" :disabled="!newDashName">
            Créer
          </button>
        </div>
      </div>
    </div>

    <!-- ══════════════════════════════════════════════════
         Tiroir Paramètres Dashboard
         ══════════════════════════════════════════════════ -->
    <transition name="drawer-in">
      <div v-if="showDashSettings" class="cfg-drawer dash-settings-drawer">

        <div class="cfg-header">
          <div class="cfg-header-left">
            <div class="cfg-type-badge"><i class="pi pi-sliders-h"></i></div>
            <span style="font-weight:600;font-size:14px;color:#111714">Paramètres</span>
          </div>
          <button class="cfg-close" @click="showDashSettings = false">
            <i class="pi pi-times"></i>
          </button>
        </div>

        <div class="cfg-body">

          <!-- Infos -->
          <div class="cfg-section">
            <div class="cfg-section-title">Informations</div>
            <div class="cfg-field">
              <label>Nom du dashboard</label>
              <input v-model="dashSettings.name" class="cfg-input" placeholder="Mon dashboard" />
            </div>
          </div>

          <!-- Thème -->
          <div class="cfg-section">
            <div class="cfg-section-title">Thème visuel</div>
            <div class="dash-theme-grid">
              <button
                v-for="t in themes" :key="t.name"
                class="dash-theme-btn"
                :class="{ active: currentTheme === t.name }"
                @click="setTheme(t.name)"
              >
                <div class="dash-theme-preview" :style="{ background: t.bg }">
                  <div class="dash-theme-dot" :style="{ background: t.primary }"></div>
                  <div class="dash-theme-bar" :style="{ background: t.sidebar }"></div>
                </div>
                <span>{{ t.label }}</span>
              </button>
            </div>
          </div>

          <!-- Grille -->
          <div class="cfg-section">
            <div class="cfg-section-title">Grille</div>
            <div class="cfg-field">
              <label>Espacement — <strong>{{ dashSettings.gridGap }}px</strong></label>
              <input type="range" v-model.number="dashSettings.gridGap" min="8" max="40" step="4" class="cfg-range" />
              <div class="cfg-range-marks"><span>8</span><span>24</span><span>40</span></div>
            </div>
            <div class="cfg-toggles">
              <label class="cfg-toggle">
                <span>Grille de points visible</span>
                <input type="checkbox" v-model="dashSettings.showGridDots" />
                <span class="toggle-track"><span class="toggle-thumb"></span></span>
              </label>
            </div>
          </div>

          <!-- Partage -->
          <div class="cfg-section" v-if="dashboardStore.currentDashboard">
            <div class="cfg-section-title">Partage</div>
            <button
              class="cfg-share-open-btn"
              @click="showShareModal = true; showDashSettings = false"
            >
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2">
                <circle cx="18" cy="5" r="3"/><circle cx="6" cy="12" r="3"/><circle cx="18" cy="19" r="3"/>
                <line x1="8.59" y1="13.51" x2="15.42" y2="17.49"/>
                <line x1="15.41" y1="6.51" x2="8.59" y2="10.49"/>
              </svg>
              <span>Gérer les liens de partage</span>
              <i class="pi pi-arrow-right" style="margin-left:auto; font-size:10px; opacity:.5"/>
            </button>
          </div>

        </div>

        <div class="cfg-footer">
          <button class="cfg-btn-cancel" @click="showDashSettings = false">Fermer</button>
          <button class="cfg-btn-apply" @click="applyDashSettings">
            <i class="pi pi-check"></i> Sauvegarder
          </button>
        </div>
      </div>
    </transition>

    <!-- Modal Aperçu CSV -->
    <div v-if="showCsvPreview" class="modal-overlay csv-preview-overlay">
      <div class="modal-content csv-preview-content">
        <div class="modal-header">
          <h3>Aperçu de l'importation CSV</h3>
          <p class="file-info">{{ csvFileName }} ({{ csvParsedData.length }} lignes détectées)</p>
        </div>

        <div class="preview-body">
          <div class="columns-info">
            <h4>Colonnes détectées</h4>
            <div class="column-tags">
              <div v-for="col in csvColumns" :key="col.name" class="col-tag">
                <span class="col-name">{{ col.name }}</span>
                <span class="col-type" :class="col.type">{{ col.type }}</span>
              </div>
            </div>
          </div>

          <div class="table-preview">
            <table>
              <thead>
                <tr>
                  <th v-for="col in csvColumns" :key="col.name">{{ col.name }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(row, i) in csvParsedData.slice(0, 5)" :key="i">
                  <td v-for="col in csvColumns" :key="col.name">{{ row[col.name] }}</td>
                </tr>
              </tbody>
            </table>
            <div v-if="csvParsedData.length > 5" class="table-footer">
              Affichage des 5 premières lignes sur {{ csvParsedData.length }}
            </div>
          </div>
        </div>

        <div class="modal-footer">
          <button @click="closeCsvPreview" class="cancel-btn">Annuler</button>
          <button @click="confirmCsvImport" class="confirm-btn">Valider l'importation</button>
        </div>
      </div>
    </div>

    <!-- Modal Connexion SQL -->
    <SqlConnectModal
      v-if="showSqlModal"
      @close="showSqlModal = false"
      @imported="handleSqlImported"
    />

    <!-- Modal Connexion API REST -->
    <RestConnectModal
      v-if="showRestModal"
      @close="showRestModal = false"
      @imported="handleRestImported"
    />

    <!-- Toast de sauvegarde -->
    <transition name="toast-slide">
      <div v-if="saveToast.show" class="save-toast" :class="saveToast.type">
        <i :class="saveToast.type === 'success' ? 'pi pi-check-circle' : 'pi pi-exclamation-circle'"></i>
        {{ saveToast.message }}
      </div>
    </transition>

    <!-- Modal Ajouter Filtre -->
    <div v-if="showAddFilterModal" class="modal-overlay">
      <div class="modal-content">
        <h3>Ajouter un Filtre</h3>
        <div class="filter-grid">
          <div class="filter-field">
            <label>Colonne à filtrer</label>
            <select v-model="newFilter.column" @change="onFilterColumnChange" class="cfg-select">
              <option value="">Choisir une colonne</option>
              <option v-for="col in availableColumns" :key="col.name" :value="col.name">
                {{ col.name }} ({{ col.type }})
              </option>
            </select>
          </div>

          <div class="filter-field" v-if="newFilter.column">
            <label>Valeur à filtrer</label>
            <div v-if="currentFilterCol?.type === 'number'" class="range-filter">
              <input type="range" v-model.number="newFilter.value" :min="currentFilterCol.min" :max="currentFilterCol.max" step="1" class="cfg-range" />
              <span style="font-size:11px;color:rgba(240,253,249,0.5)">Min: {{ newFilter.value }}</span>
            </div>
            <input v-else-if="currentFilterCol?.type === 'date'" type="date" v-model="newFilter.value" class="cfg-input" />
            <select v-else-if="currentFilterCol?.type === 'category' || currentFilterCol?.type === 'boolean'" v-model="newFilter.value" class="cfg-select">
              <option value="">Toutes les valeurs</option>
              <option v-for="val in currentFilterCol.uniqueValues" :key="val" :value="val">{{ val }}</option>
            </select>
            <input v-else type="text" v-model="newFilter.value" placeholder="Rechercher…" class="cfg-input" />
          </div>

          <div class="filter-field" v-if="newFilter.column">
            <label>Libellé (optionnel)</label>
            <input v-model="newFilter.label" :placeholder="newFilter.column" class="cfg-input" />
          </div>
        </div>
        <div class="modal-footer">
          <button @click="showAddFilterModal = false" class="cancel-btn">Annuler</button>
          <button
            @click="addFilter"
            class="confirm-btn"
            :disabled="!newFilter.column || !newFilter.value"
          >
            Ajouter le filtre
          </button>
        </div>
      </div>
    </div>

    <!-- Assistant IA (panel contrôlé depuis la topbar) -->
    <AiAssistant v-model:open="aiOpen" @create-widget="handleAiCreateWidget" />

    <!-- Share Modal -->
    <ShareModal
      v-model="showShareModal"
      :dashboard-id="dashboardStore.currentDashboard?.id ?? null"
      :dashboard-name="dashboardStore.currentDashboard?.name ?? ''"
      :is-public="dashboardStore.currentDashboard?.isPublic ?? false"
      :share-token="dashboardStore.currentDashboard?.shareToken ?? null"
      @update:is-public="onSharePublicUpdate"
      @update:share-token="onShareTokenUpdate"
    />

    <!-- Version History Panel -->
    <VersionHistoryModal
      ref="versionModalRef"
      v-model="showVersionModal"
      :dashboard-id="dashboardStore.currentDashboard?.id ?? null"
      :dashboard-name="dashboardStore.currentDashboard?.name ?? ''"
      @restored="onVersionRestored"
    />

    <!-- Auto-Generate Dashboard Modal -->
    <AutoGenerateModal
      v-model="showAutoGenerate"
      :current-step="autoGenStep"
      :is-done="autoGenDone"
      :is-error="autoGenError"
      :error-message="autoGenErrMsg"
      :widget-count="autoGenWidgetCount"
      :has-existing-widgets="dashboardStore.currentWidgets.length > 0"
      :existing-count="dashboardStore.currentWidgets.length"
      @cancel="cancelAutoGenerate"
    />

    <!-- Templates Modal -->
    <div v-if="showTemplatesModal" class="modal-overlay" @click.self="showTemplatesModal = false">
      <div class="modal-content tpl-modal">
        <div class="tpl-header">
          <h3>Templates pré-configurés</h3>
          <p class="tpl-sub">Choisissez un template pour pré-remplir votre dashboard avec des widgets. Configurez ensuite les axes selon votre dataset.</p>
          <button class="tpl-close" @click="showTemplatesModal = false">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
          </button>
        </div>
        <div class="tpl-grid">
          <div v-for="tpl in DASHBOARD_TEMPLATES" :key="tpl.name" class="tpl-card" @click="applyTemplate(tpl)">
            <div class="tpl-name">{{ tpl.name }}</div>
            <div class="tpl-desc">{{ tpl.desc }}</div>
            <div class="tpl-chips">
              <span v-for="w in tpl.widgets" :key="w.title" class="tpl-chip">{{ w.type }}</span>
            </div>
            <button class="tpl-apply-btn">Appliquer ce template</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Mobile preview banner -->
    <div v-if="mobilePreview" class="mobile-preview-banner">
      <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2"><rect x="7" y="2" width="10" height="20" rx="2"/><line x1="12" y1="18" x2="12.01" y2="18"/></svg>
      Prévisualisation mobile (390px) — <button @click="mobilePreview = false" style="background:none;border:none;color:#1B6B3A;cursor:pointer;font-weight:600;padding:0">Quitter</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, reactive, computed, watch, nextTick } from 'vue'
import domtoimage from 'dom-to-image-more'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useDashboardStore, type DashboardDetailDto as Dashboard, type Widget } from '@/stores/dashboard'
import { useDatasetStore, type ColumnProfile, type CustomMeasure } from '@/stores/dataset'
import { widgetService } from '@/services/widgetService'
import { dashboardService } from '@/services/dashboardService'
import api from '@/services/api'

// Import widget components
import KpiCardWidget   from '@/components/widgets/KpiCardWidget.vue'
import BarChartWidget  from '@/components/widgets/BarChartWidget.vue'
import LineChartWidget from '@/components/widgets/LineChartWidget.vue'
import PieChartWidget  from '@/components/widgets/PieChartWidget.vue'
import ScatterWidget   from '@/components/widgets/ScatterWidget.vue'
import GaugeWidget     from '@/components/widgets/GaugeWidget.vue'
import FunnelWidget    from '@/components/widgets/FunnelWidget.vue'
import TextWidget      from '@/components/widgets/TextWidget.vue'
import BoxPlotWidget   from '@/components/widgets/BoxPlotWidget.vue'
import TableWidget     from '@/components/widgets/TableWidget.vue'
import HeatmapWidget   from '@/components/widgets/HeatmapWidget.vue'
import ImageWidget     from '@/components/widgets/ImageWidget.vue'
import TreemapWidget   from '@/components/widgets/TreemapWidget.vue'
import MapWidget      from '@/components/widgets/MapWidget.vue'
import type { BoxPlotPoint } from '@/types/index'
import BaseChart       from '@/components/widgets/BaseChart.vue'
import SqlConnectModal  from '@/components/SqlConnectModal.vue'
import RestConnectModal from '@/components/RestConnectModal.vue'
import AiAssistant           from '@/components/AiAssistant.vue'
import AutoGenerateModal     from '@/components/AutoGenerateModal.vue'
import ShareModal            from '@/components/ShareModal.vue'
import VersionHistoryModal   from '@/components/VersionHistoryModal.vue'
import aiService             from '@/services/aiService'
import type { AiWidgetConfig } from '@/services/aiService'
import versionService        from '@/services/versionService'

const router = useRouter()
const route  = useRoute()
const authStore = useAuthStore()
const dashboardStore = useDashboardStore()
const datasetStore = useDatasetStore()

const isSaving = ref(false)
const isExporting = ref(false)
const lastSaved = ref('')   // human-readable "il y a X min" string
// Tracks the set of widget IDs that exist in the backend for the current dashboard
// (used to detect deletions on save)
const originalWidgetIds = ref<Set<number>>(new Set())
// Save toast notification
const saveToast = reactive({ show: false, type: 'success' as 'success' | 'error', message: '' })
function showToast(type: 'success' | 'error', message: string) {
  saveToast.type = type
  saveToast.message = message
  saveToast.show = true
  setTimeout(() => { saveToast.show = false }, 3500)
}
const showNewDashModal = ref(false)
const showAddFilterModal = ref(false)
const showSqlModal  = ref(false)
const showRestModal = ref(false)
const newDashName = ref('')

// ── Undo history ──────────────────────────────────────────────
const undoStack = ref<Widget[][]>([])
function pushUndo() {
  undoStack.value.push(JSON.parse(JSON.stringify(dashboardStore.currentWidgets)))
  if (undoStack.value.length > 30) undoStack.value.shift()
}
function undo() {
  const prev = undoStack.value.pop()
  if (prev) { dashboardStore.currentWidgets = prev; showToast('success', 'Action annulée') }
}

// ── Mobile preview ────────────────────────────────────────────
const mobilePreview = ref(false)

// ── Auto-refresh ──────────────────────────────────────────────
const autoRefreshInterval = ref(0)
const showAutoRefreshMenu = ref(false)
const refreshKey = ref(0)
let autoRefreshTimer: ReturnType<typeof setInterval> | null = null
watch(autoRefreshInterval, (val) => {
  if (autoRefreshTimer) { clearInterval(autoRefreshTimer); autoRefreshTimer = null }
  if (val > 0) autoRefreshTimer = setInterval(() => { refreshKey.value++ }, val * 1000)
})

// ── Templates ─────────────────────────────────────────────────
const showTemplatesModal = ref(false)
const DASHBOARD_TEMPLATES = [
  {
    name: '📊 Rapport Ventes',
    desc: 'KPIs + graphique barres + courbe tendance + camembert répartition',
    widgets: [
      { type: 'kpi', title: 'Chiffre d\'affaires', width: 3, height: 2 },
      { type: 'kpi', title: 'Nombre de ventes', width: 3, height: 2 },
      { type: 'kpi', title: 'Panier moyen', width: 3, height: 2 },
      { type: 'kpi', title: 'Taux conversion', width: 3, height: 2 },
      { type: 'bar', title: 'Ventes par mois', width: 8, height: 4 },
      { type: 'pie', title: 'Répartition produits', width: 4, height: 4 },
      { type: 'line', title: 'Tendance CA', width: 12, height: 4 },
    ],
  },
  {
    name: '👥 RH & Équipes',
    desc: 'Effectifs, turnover, performance par département',
    widgets: [
      { type: 'kpi', title: 'Effectif total', width: 3, height: 2 },
      { type: 'kpi', title: 'Taux présence', width: 3, height: 2 },
      { type: 'kpi', title: 'Nouvelles recrues', width: 3, height: 2 },
      { type: 'kpi', title: 'Turnover %', width: 3, height: 2 },
      { type: 'bar', title: 'Effectifs par département', width: 6, height: 4 },
      { type: 'doughnut', title: 'Répartition par genre', width: 6, height: 4 },
      { type: 'line', title: 'Évolution effectifs', width: 12, height: 4 },
    ],
  },
  {
    name: '🛒 E-Commerce',
    desc: 'Commandes, panier, acquisition, satisfaction client',
    widgets: [
      { type: 'kpi', title: 'Commandes', width: 3, height: 2 },
      { type: 'kpi', title: 'Revenu total', width: 3, height: 2 },
      { type: 'kpi', title: 'NPS client', width: 3, height: 2 },
      { type: 'kpi', title: 'Abandons panier', width: 3, height: 2 },
      { type: 'funnel', title: 'Tunnel conversion', width: 5, height: 5 },
      { type: 'bar', title: 'Commandes par catégorie', width: 7, height: 5 },
      { type: 'line', title: 'Revenus quotidiens', width: 12, height: 4 },
    ],
  },
  {
    name: '📈 Marketing',
    desc: 'Trafic, campagnes, ROI, canaux d\'acquisition',
    widgets: [
      { type: 'kpi', title: 'Visiteurs', width: 3, height: 2 },
      { type: 'kpi', title: 'Leads générés', width: 3, height: 2 },
      { type: 'kpi', title: 'Coût / lead', width: 3, height: 2 },
      { type: 'kpi', title: 'ROI campagnes', width: 3, height: 2 },
      { type: 'area', title: 'Trafic web', width: 8, height: 4 },
      { type: 'pie', title: 'Canaux acquisition', width: 4, height: 4 },
      { type: 'bar', title: 'Performance campagnes', width: 12, height: 4 },
    ],
  },
  {
    name: '⚙️ Opérations',
    desc: 'SLA, incidents, capacité, qualité de service',
    widgets: [
      { type: 'gauge', title: 'SLA global', width: 3, height: 3 },
      { type: 'kpi', title: 'Incidents ouverts', width: 3, height: 2 },
      { type: 'kpi', title: 'Temps résolution', width: 3, height: 2 },
      { type: 'kpi', title: 'Satisfaction %', width: 3, height: 2 },
      { type: 'line', title: 'Incidents / semaine', width: 9, height: 4 },
      { type: 'bar', title: 'Tickets par catégorie', width: 6, height: 4 },
      { type: 'table', title: 'Incidents récents', width: 6, height: 4 },
    ],
  },
]
async function applyTemplate(tpl: typeof DASHBOARD_TEMPLATES[0]) {
  if (!dashboardStore.currentDashboard) return
  pushUndo()
  dashboardStore.currentWidgets = tpl.widgets.map((w, i) => ({
    id: Date.now() + i * 1000 + Math.floor(Math.random() * 999),
    type: w.type,
    title: w.title,
    dashboardId: dashboardStore.currentDashboard!.id,
    width: w.width,
    height: w.height,
    x: 0, y: i * (w.height ?? 4),
    data_config: JSON.stringify({ color: '#1B6B3A', isDefaultColor: true }),
  }))
  showTemplatesModal.value = false
  showToast('success', `Template « ${tpl.name} » appliqué — configurez les axes de chaque widget`)
}

// ── Keyboard shortcuts ────────────────────────────────────────
function handleKeyboard(e: KeyboardEvent) {
  const tag = (e.target as HTMLElement).tagName
  if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return
  if ((e.ctrlKey || e.metaKey) && e.key === 'z') { e.preventDefault(); undo() }
  if ((e.ctrlKey || e.metaKey) && e.key === 'd' && selectedWidget.value) { e.preventDefault(); pushUndo(); duplicateWidget(selectedWidget.value) }
  if (e.key === 'Delete' && selectedWidget.value) { pushUndo(); removeWidget(selectedWidget.value.id); showConfigPanel.value = false; selectedWidget.value = null }
  if (e.key === 'Escape') { showConfigPanel.value = false; selectedWidget.value = null; showAutoRefreshMenu.value = false; showTemplatesModal.value = false }
}

// Themes State
const currentTheme = ref('dark')

// ── Back to admin ──────────────────────────────────────────────
function goBackToAdmin() {
  router.push('/admin')
}

// ── Sidebar resize ─────────────────────────────────────────────
const sidebarWidth = ref(280)
function startSidebarResize(e: MouseEvent) {
  e.preventDefault()
  const startX = e.clientX
  const startW = sidebarWidth.value
  const onMove = (ev: MouseEvent) => {
    const delta = ev.clientX - startX
    sidebarWidth.value = Math.min(480, Math.max(180, startW + delta))
  }
  const onUp = () => {
    window.removeEventListener('mousemove', onMove)
    window.removeEventListener('mouseup', onUp)
  }
  window.addEventListener('mousemove', onMove)
  window.addEventListener('mouseup', onUp)
}
const currentThemeObj = ref({ name: 'dark', label: 'Émeraude', primary: '#1B6B3A', bg: '#F5F6F5', text: '#111714', sidebar: '#FFFFFF', card: '#FFFFFF' })
const themes = [
  { name: 'dark',    label: 'Émeraude', primary: '#1B6B3A', bg: '#F5F6F5', text: '#111714', sidebar: '#FFFFFF', card: '#FFFFFF'  },
  { name: 'white',   label: 'Clair',    primary: '#134E2A', bg: '#f8fafc', text: '#0f172a', sidebar: '#ffffff', card: '#ffffff'   },
  { name: 'blue',    label: 'Saphir',   primary: '#3b82f6', bg: '#0b1322', text: '#f1f5f9', sidebar: '#111d30', card: '#1e293b'  },
  { name: 'purple',  label: 'Violet',   primary: '#8b5cf6', bg: '#0d0a1a', text: '#f5f3ff', sidebar: '#14102b', card: '#1c1535'  },
  { name: 'rose',    label: 'Rose',     primary: '#ec4899', bg: '#130a10', text: '#fdf2f8', sidebar: '#1a0d15', card: '#1f0d18'  },
  { name: 'amber',   label: 'Ambre',    primary: '#f59e0b', bg: '#0d0a04', text: '#fffbeb', sidebar: '#1a1408', card: '#1c1507'  },
  { name: 'ocean',   label: 'Océan',    primary: '#06b6d4', bg: '#050d12', text: '#ecfeff', sidebar: '#071521', card: '#0a1a2a'  },
]

function setTheme(themeName: string) {
  currentTheme.value = themeName
  const theme = themes.find((t) => t.name === themeName)
  if (theme) {
    currentThemeObj.value = theme
    const root = document.documentElement
    root.setAttribute('data-theme', themeName)   // ← used by CSS overrides

    root.style.setProperty('--primary-color', theme.primary)
    root.style.setProperty('--bg-color', theme.bg)
    root.style.setProperty('--text-color', theme.text)
    root.style.setProperty('--sidebar-bg', theme.sidebar)
    root.style.setProperty('--card-bg', theme.card)

    // Convert hex to rgb for rgba usage
    const r = parseInt(theme.primary.slice(1, 3), 16)
    const g = parseInt(theme.primary.slice(3, 5), 16)
    const b = parseInt(theme.primary.slice(5, 7), 16)
    root.style.setProperty('--primary-color-rgb', `${r}, ${g}, ${b}`)

    const tr = parseInt(theme.text.slice(1, 3), 16)
    const tg = parseInt(theme.text.slice(3, 5), 16)
    const tb = parseInt(theme.text.slice(5, 7), 16)
    root.style.setProperty('--text-color-rgb', `${tr}, ${tg}, ${tb}`)

    // Add background RGB for overlays
    const bgr = parseInt(theme.bg.slice(1, 3), 16)
    const bgg = parseInt(theme.bg.slice(3, 5), 16)
    const bgb = parseInt(theme.bg.slice(5, 7), 16)
    root.style.setProperty('--bg-color-rgb', `${bgr}, ${bgg}, ${bgb}`)

    // Semantic surface tokens — used by CSS to adapt hardcoded dark colors
    const isLight = themeName === 'white'
    root.style.setProperty('--on-surface',        isLight ? 'rgba(15,23,42,0.92)'  : 'rgba(255,255,255,0.92)')
    root.style.setProperty('--on-surface-sec',    isLight ? 'rgba(15,23,42,0.65)'  : 'rgba(255,255,255,0.65)')
    root.style.setProperty('--on-surface-muted',  isLight ? 'rgba(15,23,42,0.42)'  : 'rgba(255,255,255,0.42)')
    root.style.setProperty('--on-surface-dim',    isLight ? 'rgba(15,23,42,0.25)'  : 'rgba(255,255,255,0.25)')
    root.style.setProperty('--surface-border',    isLight ? 'rgba(15,23,42,0.12)'  : 'rgba(17,23,20,0.08)')
    root.style.setProperty('--surface-border-md', isLight ? 'rgba(15,23,42,0.18)'  : 'rgba(17,23,20,0.10)')
    root.style.setProperty('--surface-hover',     isLight ? 'rgba(15,23,42,0.05)'  : 'rgba(27,107,58,0.06)')
    root.style.setProperty('--input-bg',          isLight ? 'rgba(15,23,42,0.05)'  : 'rgba(27,107,58,0.06)')
    root.style.setProperty('--input-bg-focus',    isLight ? 'rgba(15,23,42,0.08)'  : 'rgba(17,23,20,0.08)')
    root.style.setProperty('--overlay-bg',        isLight ? '#f8fafc'              : '#FFFFFF')
    root.style.setProperty('--overlay-border',    isLight ? 'rgba(15,23,42,0.12)'  : 'rgba(17,23,20,0.08)')

    // Add muted text color based on theme
    const mutedColor = isLight ? 'rgba(10, 31, 26, 0.5)' : 'rgba(17, 23, 20, 0.4)'
    root.style.setProperty('--text-muted', mutedColor)

    // Update widget colors if they are using the default primary
    dashboardStore.currentWidgets.forEach((w) => {
      const config = JSON.parse(w.data_config || '{}')
      if (config.isDefaultColor !== false) {
        config.color = '#1B6B3A'
        config.isDefaultColor = true
        w.data_config = JSON.stringify(config)
      }
    })
  }
}

// Filters State
const activeFilters = ref<{ type: string; label: string; value: string; column?: string }[]>([])

const newFilter = reactive({
  type: 'category',
  label: '',
  value: '' as any,
  column: '',
})

const availableColumns = computed(() => {
  return datasetStore.currentDataset?.columns || []
})

// ── Properties Panel (Power BI style) ────────────────────────────────────
const ppCollapsed = ref(false)
const ppTab       = ref<'build' | 'format' | 'fields'>('fields')

// ── Properties panel resize ──────────────────────────────────────────────────
const PP_MIN = 200
const PP_MAX = 560
const ppWidth   = ref(280)   // current width in px (mirrors CSS default)
const ppResizing = ref(false) // true while dragging → disables width transition

function startPpResize(e: MouseEvent) {
  ppResizing.value = true
  const startX     = e.clientX
  const startWidth = ppWidth.value

  function onMove(ev: MouseEvent) {
    // Panel is on the right → dragging LEFT widens it
    const delta = startX - ev.clientX
    ppWidth.value = Math.min(PP_MAX, Math.max(PP_MIN, startWidth + delta))
  }

  function onUp() {
    ppResizing.value = false
    document.removeEventListener('mousemove', onMove)
    document.removeEventListener('mouseup',   onUp)
    document.body.style.cursor    = ''
    document.body.style.userSelect = ''
  }

  document.addEventListener('mousemove', onMove)
  document.addEventListener('mouseup',   onUp)
  document.body.style.cursor     = 'col-resize'
  document.body.style.userSelect = 'none'
}
const draggingField = ref<ColumnProfile | null>(null)
const wellHover     = ref<'x' | 'y' | null>(null)

// Format tab: collapsible sections open/closed state
const fmtOpen = reactive({ color: true, bg: false, typeOptions: true, size: false, units: true })

// ── Nouvelle mesure calculée ──────────────────────────────────────────────
const showNewMeasureForm = ref(false)
const newMeasure = reactive({
  name:        '',
  formula:     'simple' as CustomMeasure['formula'],
  col1:        '',
  col2:        '',
  constant:    1,
  aggregation: 'sum' as CustomMeasure['aggregation'],
  pct:         false,
})

/** Returns an emoji icon for a column type */
function getColumnTypeIcon(colName: string): string {
  const col = availableColumns.value.find(c => c.name === colName)
  if (!col) return '◇'
  if (col.type === 'date')    return '📅'
  if (col.type === 'boolean') return '◉'
  if (col.type === 'number')  return 'Σ'
  return '🔤'
}

/** Called when the user starts dragging a field chip from the fields panel */
function fieldDragStart(e: DragEvent, col: ColumnProfile) {
  draggingField.value = col
  e.dataTransfer?.setData('fieldName', col.name)
  e.dataTransfer?.setData('fieldType', col.type)
}

/** Drop a field onto an axis well */
function wellDrop(e: DragEvent, axis: 'x' | 'y') {
  wellHover.value = null
  const name = e.dataTransfer?.getData('fieldName')
  const type = e.dataTransfer?.getData('fieldType')
  if (!name) return
  // Scatter: both axes are numeric — no redirect, assign exactly where dropped.
  const isScatter = selectedWidget.value?.type === 'scatter'
  if (isScatter) {
    if (axis === 'x') widgetConfig.xAxis = name
    else              widgetConfig.yAxis = name
  } else if (axis === 'y' && type !== 'number') {
    // Non-numeric (text/date) dropped on Y well → redirect to X (dimension)
    widgetConfig.xAxis = name
  } else if (axis === 'x' && type === 'number') {
    // Numeric dropped on X well (dimension) → redirect to Y (measure)
    widgetConfig.yAxis = name
  } else {
    if (axis === 'x') widgetConfig.xAxis = name
    else              widgetConfig.yAxis = name
  }
  // Stay on (or switch to) Build tab so the assignment is visible
  ppTab.value = 'build'
}

/** Click on a field chip: auto-assign to the correct axis depending on type */
function assignFieldAuto(col: ColumnProfile) {
  if (!showConfigPanel.value) return
  if (col.type === 'number') {
    widgetConfig.yAxis = col.name
  } else {
    widgetConfig.xAxis = col.name
  }
  // Switch to Build tab so the user can see the assignment
  ppTab.value = 'build'
}

/** Explicit assign to a specific axis */
function assignField(col: ColumnProfile, axis: 'x' | 'y') {
  if (!showConfigPanel.value) return
  if (axis === 'x') widgetConfig.xAxis = col.name
  else              widgetConfig.yAxis = col.name
}

// ── Mesures calculées — logique ───────────────────────────────────────────

/** Drag d'une mesure calculée vers un well (toujours Y) */
function fieldDragStartCustom(e: DragEvent, m: CustomMeasure) {
  e.dataTransfer?.setData('fieldName', m.name)
  e.dataTransfer?.setData('fieldType', 'number')
}

/** Clic sur une mesure calculée → assigne au Y axis */
function assignCustomMeasure(m: CustomMeasure) {
  if (!showConfigPanel.value) return
  widgetConfig.yAxis = m.name
  // Switch to Build tab so the user can see the assignment
  ppTab.value = 'build'
}

/** Crée et enregistre une nouvelle mesure calculée */
function createCustomMeasure() {
  if (!newMeasure.name.trim() || !newMeasure.col1) return
  const measure: CustomMeasure = {
    id:          'cm_' + Date.now(),
    name:        newMeasure.name.trim(),
    formula:     newMeasure.formula,
    col1:        newMeasure.col1,
    col2:        (['ratio','difference'].includes(newMeasure.formula) && newMeasure.col2)
                   ? newMeasure.col2 : undefined,
    constant:    newMeasure.formula === 'multiply' ? newMeasure.constant : undefined,
    aggregation: newMeasure.aggregation,
    pct:         newMeasure.formula === 'ratio' ? newMeasure.pct : undefined,
  }
  datasetStore.addCustomMeasure(measure)
  showToast('success', `Mesure « ${measure.name} » créée`)
  // Reset form
  showNewMeasureForm.value = false
  newMeasure.name = ''
  newMeasure.col1 = ''
  newMeasure.col2 = ''
  newMeasure.constant = 1
  newMeasure.formula = 'simple'
  newMeasure.pct = false
}

/** Icône du type de mesure calculée */
function customMeasureFormula(f: CustomMeasure['formula']): string {
  if (f === 'ratio')      return 'A÷B'
  if (f === 'difference') return 'A−B'
  if (f === 'multiply')   return 'A×k'
  return 'Σ'
}

/** Retourne un tooltip descriptif pour la mesure */
function customMeasureTip(m: CustomMeasure): string {
  const agg = m.aggregation.toUpperCase()
  if (m.formula === 'simple')     return `${agg}(${m.col1})`
  if (m.formula === 'multiply')   return `${agg}(${m.col1}) × ${m.constant ?? 1}`
  if (m.formula === 'ratio')      return `${agg}(${m.col1}) / ${agg}(${m.col2})${m.pct ? ' × 100' : ''}`
  if (m.formula === 'difference') return `${agg}(${m.col1}) - ${agg}(${m.col2 ?? '?'})`
  return m.name
}

const currentFilterCol = computed(() => {
  return availableColumns.value.find((c) => c.name === newFilter.column)
})

function onFilterColumnChange() {
  if (currentFilterCol.value?.type === 'number') {
    newFilter.value = currentFilterCol.value.min || 0
  } else {
    newFilter.value = ''
  }
}

function addFilter() {
  const col = availableColumns.value.find((c) => c.name === newFilter.column)
  activeFilters.value.push({
    type: col?.type || 'category',
    label: newFilter.label || newFilter.column,
    value: String(newFilter.value),
    column: newFilter.column,
  })
  showAddFilterModal.value = false
  newFilter.label = ''
  newFilter.value = ''
  newFilter.column = ''
}

function removeFilter(index: number) {
  activeFilters.value.splice(index, 1)
}

function getFilterIcon(type: string) {
  switch (type) {
    case 'date':
      return 'pi pi-calendar'
    case 'region':
      return 'pi pi-map-marker'
    case 'category':
      return 'pi pi-tag'
    default:
      return 'pi pi-filter'
  }
}

// CSV Import State
const csvInput = ref<HTMLInputElement | null>(null)
const showCsvPreview = ref(false)
const csvFileName = ref('')
const csvParsedData = ref<any[]>([])
const csvColumns = ref<{ name: string; type: string }[]>([])

// ── Palettes & constantes ─────────────────────────────────────────────────
const VALUE_PRESETS: { label: string; value: string; prefix?: string }[] = [
  { label: '€',      value: ' €' },
  { label: '$',      value: ' $' },
  { label: '%',      value: '%'  },
  { label: 'k€',     value: ' k€' },
  { label: 'M€',     value: ' M€' },
  { label: 'pts',    value: ' pts' },
  { label: 'j',      value: ' j' },
  { label: 'clients',value: ' clients' },
]

const COLOR_PALETTE = [
  '#1B6B3A','#3b82f6','#f59e0b','#ef4444','#8b5cf6',
  '#06b6d4','#ec4899','#84cc16','#f97316','#a78bfa',
]
const BG_PALETTE = [
  '#EEF7F1','#111827','#0f172a','#1e1b4b','#1a1a2e',
  '#1f2937','#1c1917','#052e16','#0c0a09','#18181b',
]
const CARD_STYLES = [
  { id: 'none',           label: 'Standard'  },
  { id: 'glass',          label: 'Verre'     },
  { id: 'gradient-mesh',  label: 'Dégradé'   },
  { id: 'soft-glow',      label: 'Lueur'     },
  { id: 'modern-lines',   label: 'Lignes'    },
]
const KPI_ICONS = [
  { id: 'auto',     label: 'Auto',    pi: 'pi pi-star' },
  { id: 'trending', label: 'Tendance',pi: 'pi pi-arrow-up-right' },
  { id: 'users',    label: 'Usagers', pi: 'pi pi-users' },
  { id: 'dollar',   label: 'Finance', pi: 'pi pi-dollar' },
  { id: 'bar',      label: 'Stats',   pi: 'pi pi-chart-bar' },
  { id: 'line',     label: 'Courbe',  pi: 'pi pi-chart-line' },
]

function getStylePreview(style: string, color: string) {
  const r = parseInt(color.slice(1,3)||'10',16)
  const g = parseInt(color.slice(3,5)||'b9',16)
  const b = parseInt(color.slice(5,7)||'81',16)
  const rgb = `${r},${g},${b}`
  if (style === 'glass')         return { background: 'rgba(27,107,58,0.06)', border: `1px solid rgba(${rgb},0.4)` }
  if (style === 'gradient-mesh') return { background: `radial-gradient(circle at 0% 0%, rgba(${rgb},0.3), transparent 60%), radial-gradient(circle at 100% 100%, rgba(${rgb},0.2), transparent 60%)` }
  if (style === 'soft-glow')     return { background: `linear-gradient(180deg,rgba(${rgb},0.15) 0%,transparent 100%)`, boxShadow: `0 0 12px rgba(${rgb},0.3)` }
  if (style === 'modern-lines')  return { backgroundImage: `repeating-linear-gradient(45deg,rgba(27,107,58,0.06) 0px,rgba(27,107,58,0.06) 1px,transparent 1px,transparent 8px)` }
  return { background: 'rgba(17,23,20,0.02)' }
}

// ── Widget Configuration State ────────────────────────────────────────────
const selectedWidget = ref<Widget | null>(null)
const showConfigPanel = ref(false)
const widgetConfig = reactive({
  title: '',
  activeTab: 'data',
  // Data
  xAxis: '',
  yAxis: '',
  aggregation: 'sum',
  dateGranularity: 'month',
  maxItems: 15,
  sortBy: 'value',
  sortDir: 'desc',
  // Appearance
  color: '#1B6B3A',
  backgroundColor: '',
  backgroundType: 'none',
  width: 6,
  height: 4,
  // KPI
  kpiIcon: 'auto',
  kpiSuffix: '',
  kpiShowTrend: false,
  kpiTrendValue: 0,
  kpiShowSparkline: false,
  // Bar
  barOrientation: 'vertical',
  barShowValues: false,
  barBorderRadius: 3,
  // Line
  lineSmooth: false,
  lineFill: true,
  lineShowDots: true,
  lineShowLabels: false,
  // Pie
  pieShowLegend: true,
  pieLegendPosition: 'right',
  pieMaxSlices: 8,
  pieShowLabels: false,
  numericBins: 5,
  // Description & unités
  description: '',
  valuePrefix: '',
  valueSuffix: '',
  yAxisLabel: '',
  // Scatter
  scatterDotSize: 4,
  labelAxis: '',
  // Gauge
  gaugeMin: 0,
  gaugeMax: 100,
  // Text widget
  textContent:    '',
  textFontSize:   14,
  textAlign:      'left' as 'left' | 'center' | 'right',
  textFontFamily: 'inherit',
  // Table widget
  tableColumns:   [] as string[],
  tableRowLimit:  100,
  tablePageSize:  10,
  tableShowSearch: true,
  // Heatmap widget
  heatmapColumns: [] as string[],
  // Image widget
  imageUrl:   '',
  imageBase64: '',
  imageFit:   'contain' as 'contain' | 'cover' | 'fill',
  imageAlt:   '',
  // Map widget
  mapMarkers:    [] as { lat: number; lon: number; label: string; color: string }[],
  mapZoom:       5,
  mapCenterLat:  46.5,
  mapCenterLon:  2.3,
  markerColor:   '#3b82f6',
})

// ── Live config preview ────────────────────────────────────────────────────
// Any change in widgetConfig is instantly reflected on the canvas widget
// without waiting for "Appliquer" — matches Power BI live-format behaviour.

/** Guard: prevents the deep watcher from firing during openWidgetConfig() */
let _suppressLiveApply = false

/** Build the full cfg object from current widgetConfig state */
function buildCfgPayload(): Record<string, unknown> {
  return {
    xAxis:             widgetConfig.xAxis,
    yAxis:             widgetConfig.yAxis,
    aggregation:       widgetConfig.aggregation,
    dateGranularity:   widgetConfig.dateGranularity,
    maxItems:          widgetConfig.maxItems,
    sortBy:            widgetConfig.sortBy,
    sortDir:           widgetConfig.sortDir,
    color:             widgetConfig.color,
    isDefaultColor:    false,
    backgroundType:    widgetConfig.backgroundType,
    backgroundColor:   widgetConfig.backgroundColor,
    // KPI
    kpiIcon:           widgetConfig.kpiIcon,
    kpiSuffix:         widgetConfig.kpiSuffix,
    kpiShowTrend:      widgetConfig.kpiShowTrend,
    kpiTrendValue:     widgetConfig.kpiTrendValue,
    kpiShowSparkline:  widgetConfig.kpiShowSparkline,
    // Bar
    barOrientation:    widgetConfig.barOrientation,
    barShowValues:     widgetConfig.barShowValues,
    barBorderRadius:   widgetConfig.barBorderRadius,
    // Line / Area
    lineSmooth:        widgetConfig.lineSmooth,
    lineFill:          widgetConfig.lineFill,
    lineShowDots:      widgetConfig.lineShowDots,
    lineShowLabels:    widgetConfig.lineShowLabels,
    // Pie / Doughnut
    pieMaxSlices:      widgetConfig.pieMaxSlices,
    pieLegendPosition: widgetConfig.pieLegendPosition,
    pieShowLabels:     widgetConfig.pieShowLabels,
    numericBins:       widgetConfig.numericBins,
    // Scatter
    scatterDotSize:    widgetConfig.scatterDotSize,
    labelAxis:         widgetConfig.labelAxis,
    // Description & unités
    description:       widgetConfig.description,
    valuePrefix:       widgetConfig.valuePrefix,
    valueSuffix:       widgetConfig.valueSuffix,
    yAxisLabel:        widgetConfig.yAxisLabel,
    // Gauge
    gaugeMin:          widgetConfig.gaugeMin,
    gaugeMax:          widgetConfig.gaugeMax,
    // Text widget
    textContent:       widgetConfig.textContent,
    textFontSize:      widgetConfig.textFontSize,
    textAlign:         widgetConfig.textAlign,
    textFontFamily:    widgetConfig.textFontFamily,
    // Table widget
    tableColumns:    widgetConfig.tableColumns,
    tableRowLimit:   widgetConfig.tableRowLimit,
    tablePageSize:   widgetConfig.tablePageSize,
    tableShowSearch: widgetConfig.tableShowSearch,
    // Heatmap widget
    heatmapColumns:    widgetConfig.heatmapColumns,
    // Image widget
    imageUrl:          widgetConfig.imageUrl,
    imageBase64:       widgetConfig.imageBase64,
    imageFit:          widgetConfig.imageFit,
    imageAlt:          widgetConfig.imageAlt,
    // Map widget
    mapMarkers:        widgetConfig.mapMarkers,
    mapZoom:           widgetConfig.mapZoom,
    mapCenterLat:      widgetConfig.mapCenterLat,
    mapCenterLon:      widgetConfig.mapCenterLon,
    markerColor:       widgetConfig.markerColor,
  }
}

/**
 * Apply the current widgetConfig state to the canvas widget immediately.
 * Does NOT close the panel, does NOT persist to backend.
 */
function liveApplyConfig() {
  if (!selectedWidget.value) return
  const idx = dashboardStore.currentWidgets.findIndex(x => x.id === selectedWidget.value!.id)
  if (idx === -1) return
  const w = dashboardStore.currentWidgets[idx]!
  w.data_config = JSON.stringify(buildCfgPayload())
  w.title  = widgetConfig.title
  w.width  = widgetConfig.width
  w.height = widgetConfig.height
  // For table, heatmap, image: also refresh widget.data so the canvas re-renders
  if (w.type === 'table' || w.type === 'heatmap' || w.type === 'image') {
    try { w.data = JSON.stringify(getWidgetData(w)) } catch { /* ignore */ }
  }
  if (w.type === 'map') {
    try { w.data = JSON.stringify(widgetConfig.mapMarkers) } catch { /* ignore */ }
  }
  // Keep selectedWidget reference in sync
  selectedWidget.value = w
}

watch(widgetConfig, () => {
  if (_suppressLiveApply || !showConfigPanel.value || !selectedWidget.value) return
  liveApplyConfig()
}, { deep: true })

/** Scan the raw string values of a column and extract the most common prefix/suffix unit */
function detectUnitFromColumn(colName: string): { prefix: string; suffix: string } {
  const dataset = datasetStore.currentDataset
  if (!dataset) return { prefix: '', suffix: '' }

  const values = dataset.data
    .map((row: any) => String(row[colName] ?? '').trim())
    .filter((v: string) => v !== '' && /\d/.test(v))
    .slice(0, 30)

  if (!values.length) return { prefix: '', suffix: '' }

  const prefixes: string[] = []
  const suffixes: string[] = []

  for (const v of values) {
    const firstDigit = v.search(/\d/)
    const lastDigitMatch = v.match(/\d(?=[^\d]*$)/)
    const lastDigitIdx = lastDigitMatch ? v.lastIndexOf(lastDigitMatch[0], v.length) : -1

    const pre = firstDigit > 0 ? v.slice(0, firstDigit).trim() : ''
    const suf = lastDigitIdx >= 0 && lastDigitIdx < v.length - 1 ? v.slice(lastDigitIdx + 1).trim() : ''

    prefixes.push(pre)
    suffixes.push(suf)
  }

  const mostCommon = (arr: string[]) => {
    const counts: Record<string, number> = {}
    for (const s of arr) counts[s] = (counts[s] ?? 0) + 1
    return Object.entries(counts)
      .filter(([k]) => k !== '')
      .sort((a, b) => b[1] - a[1])[0]?.[0] ?? ''
  }

  return { prefix: mostCommon(prefixes), suffix: mostCommon(suffixes) }
}

/** Auto-fill prefix/suffix when the Y axis column changes */
watch(() => widgetConfig.yAxis, (newCol) => {
  if (_suppressLiveApply || !newCol) return

  // 1. Scan raw values
  let { prefix, suffix } = detectUnitFromColumn(newCol)

  // 2. Fallback: column name patterns
  if (!prefix && !suffix) {
    const col = newCol.toLowerCase()
    const PATTERNS: [RegExp, string, string][] = [
      [/€|eur|euro|prix|montant|cout|coût|revenue|revenu|salaire|ca\b|chiffre|vente/,  '', ' €'],
      [/\$|usd|dollar/,                                                                '$', ''],
      [/£|gbp|livre/,                                                                  '£', ''],
      [/%|pct|taux|rate|ratio|percent|pourcent/,                                       '', '%'],
      [/\bkg\b|kilo(?!metre)/,                                                         '', ' kg'],
      [/\bkm\b|kilometre|kilomètre/,                                                   '', ' km'],
      [/\bg\b|gramme/,                                                                  '', ' g'],
      [/\bl\b|litre/,                                                                   '', ' L'],
      [/\bm\b|metre|mètre/,                                                            '', ' m'],
      [/\bh\b|heure/,                                                                   '', ' h'],
    ]
    for (const [re, pre, suf] of PATTERNS) {
      if (re.test(col)) { prefix = pre; suffix = suf; break }
    }
  }

  // Always apply the detected unit directly
  widgetConfig.valuePrefix = prefix
  widgetConfig.valueSuffix = suffix
})


// ── AI Assistant ──────────────────────────────────────────────────────────
const aiOpen = ref(false)

// ── Share modal ───────────────────────────────────────────────────────────
const showShareModal = ref(false)

// ── Version history ───────────────────────────────────────────────────────────
const showVersionModal  = ref(false)
const versionModalRef   = ref<{ loadVersions: () => void } | null>(null)

async function onVersionRestored() {
  // Après restauration, recharger le dashboard complet depuis le backend :
  // 1. Forcer le rechargement de la liste (metadata + nom)
  // 2. Resélectionner le dashboard courant (recharge les widgets)
  const currentId = dashboardStore.currentDashboard?.id
  if (!currentId) return
  await dashboardStore.fetchDashboards(true)
  const refreshed = dashboardStore.dashboards.find(d => d.id === currentId)
  if (refreshed) await selectDashboard(refreshed)
  showToast('success', 'Dashboard restauré avec succès')
}

function onSharePublicUpdate(val: boolean) {
  if (dashboardStore.currentDashboard) {
    dashboardStore.currentDashboard.isPublic = val
  }
}
function onShareTokenUpdate(val: string | null) {
  if (dashboardStore.currentDashboard) {
    dashboardStore.currentDashboard.shareToken = val ?? ''
  }
}

// ── Auto-Generate Dashboard ───────────────────────────────────────────────
const showAutoGenerate   = ref(false)
const isAutoGenerating   = ref(false)
const autoGenStep        = ref(0)       // 0-3
const autoGenDone        = ref(false)
const autoGenError       = ref(false)
const autoGenErrMsg      = ref('')
const autoGenWidgetCount = ref(0)
let   autoGenAborted     = false

// ── Dashboard Settings State ──────────────────────────────────────────────
const showDashSettings = ref(false)
const dashSettings = reactive({
  name: '',
  gridGap: 20,
  showGridDots: true,
  isPublic: false,
  shareToken: '' as string | null,
})

interface WidgetType {
  id: string
  label: string
  icon: string
}

const widgetTypes: WidgetType[] = [
  { id: 'kpi',      label: 'Carte KPI',          icon: 'pi pi-info-circle' },
  { id: 'gauge',    label: 'Jauge',              icon: 'pi pi-circle'      },
  { id: 'bar',      label: 'Barres',             icon: 'pi pi-chart-bar'   },
  { id: 'line',     label: 'Courbe',             icon: 'pi pi-chart-line'  },
  { id: 'area',     label: 'Aire',               icon: 'pi pi-chart-line'  },
  { id: 'pie',      label: 'Camembert',          icon: 'pi pi-chart-pie'   },
  { id: 'doughnut', label: 'Donut',              icon: 'pi pi-circle'      },
  { id: 'funnel',   label: 'Entonnoir',          icon: 'pi pi-filter'      },
  { id: 'scatter',  label: 'Nuage de points',    icon: 'pi pi-th-large'    },
  { id: 'boxplot',  label: 'Boîte à moustaches', icon: 'pi pi-chart-bar'   },
  { id: 'radar',    label: 'Radar',              icon: 'pi pi-compass'     },
  { id: 'table',    label: 'Tableau',            icon: 'pi pi-table'       },
  { id: 'text',     label: 'Texte / Note',       icon: 'pi pi-file-edit'   },
  { id: 'heatmap',  label: 'Carte thermique',    icon: 'pi pi-th-large'    },
  { id: 'image',    label: 'Image',              icon: 'pi pi-image'       },
  { id: 'treemap',  label: 'Treemap',            icon: 'pi pi-th-large'    },
  { id: 'map',      label: 'Carte géographique', icon: 'pi pi-map-marker'  },
]

onUnmounted(() => {
  window.removeEventListener('keydown', handleKeyboard)
  if (autoRefreshTimer) clearInterval(autoRefreshTimer)
})

onMounted(async () => {
  window.addEventListener('keydown', handleKeyboard)
  await dashboardStore.fetchDashboards()
  setTheme('dark')

  // Auto-select dashboard when navigating from /builder/:id
  const routeId = Number(route.params.id)
  if (routeId) {
    const target = dashboardStore.dashboards.find(d => d.id === routeId)
    if (target) {
      await selectDashboard(target)
    } else if (authStore.user?.role === 'Admin') {
      // Admin peut éditer n'importe quel dashboard (pas dans son store)
      try {
        const full = await dashboardService.getById(routeId)
        await selectDashboard(full as unknown as Dashboard)
      } catch { /* dashboard introuvable */ }
    }
  }
})

async function selectDashboard(dash: Dashboard) {
  dashboardStore.currentDashboard = dash
  await dashboardStore.fetchWidgets(dash.id)
  // Capture backend IDs so handleSave can detect deletions
  originalWidgetIds.value = new Set(
    dashboardStore.currentWidgets.map((w) => w.id).filter((id) => id < 1e12),
  )
  // Sync dashboard settings panel
  dashSettings.name        = dash.name
  dashSettings.isPublic    = dash.isPublic
  dashSettings.shareToken  = dash.shareToken

  // Auto-load linked datasource so the AI assistant is unlocked.
  // The list endpoint doesn't include datasetId — fetch full details to get it.
  if (!datasetStore.currentDataset) {
    try {
      const full = await dashboardService.getById(dash.id)
      const dsId = (full as any).datasetId
      if (dsId) {
        const res = await api.post(`/datasource/${dsId}/preview`)
        const raw = res.data as any
        const cols = (raw.columns ?? []).map((c: any) => ({
          name: c.name ?? c.Name ?? '',
          type: c.type ?? c.Type ?? 'string',
        }))
        const rows = (raw.preview ?? raw.rows ?? []) as Record<string, string>[]
        datasetStore.setDataset(raw.name ?? `Dataset #${dsId}`, cols, rows, raw.dataSourceId ?? dsId)
      }
    } catch { /* ignore — AI assistant stays disabled */ }
  }
}

async function createNewDashboard() {
  if (!newDashName.value) return
  const dash = await dashboardStore.createDashboard(newDashName.value)
  showNewDashModal.value = false
  newDashName.value = ''
  selectDashboard(dash)
}

function confirmDeleteDashboard(dash: Dashboard) {
  if (confirm(`Êtes-vous sûr de vouloir supprimer le dashboard "${dash.name}" ?`)) {
    dashboardStore.deleteDashboard(dash.id)
  }
}

function handleLogout() {
  authStore.logout()
  router.push('/login')
}

// ── CSV helpers ────────────────────────────────────────────────────────────

/**
 * Smart numeric parser — handles English and French/European number formats.
 * "1,234.56" → 1234.56  |  "1.234,56" → 1234.56  |  "1 234,56" → 1234.56
 * Returns NaN for non-numeric strings.
 */
function parseNumStr(s: string): number {
  const t = s.trim()
  if (!t) return NaN
  // Keep only digits, dot, comma, minus
  const raw = t.replace(/[^\d.,-]/g, '')
  if (!raw || raw === '-') return NaN

  const lastDot   = raw.lastIndexOf('.')
  const lastComma = raw.lastIndexOf(',')

  if (lastDot === -1 && lastComma === -1) return parseFloat(raw)

  if (lastDot > lastComma) {
    // "1,234.56" — dot is decimal separator, remove commas
    return parseFloat(raw.replace(/,/g, ''))
  }

  // comma is the last separator — could be decimal ("1234,56") or thousand ("1,234")
  const afterComma = raw.slice(lastComma + 1)
  if (afterComma.length === 3 && !/\./.test(afterComma)) {
    // "1,234" or "1,234,567" → thousand separator
    return parseFloat(raw.replace(/,/g, ''))
  }
  // "1234,56" or "1,5" → decimal comma, also strip dot thousand separators
  return parseFloat(raw.replace(/\./g, '').replace(',', '.'))
}

/** RFC 4180-compliant CSV line splitter: handles quoted fields with embedded delimiters/newlines. */
function splitCsvLine(line: string, delim: string): string[] {
  const fields: string[] = []
  let cur = ''
  let inQ = false
  for (let i = 0; i < line.length; i++) {
    const c = line[i]!
    if (c === '"') {
      if (inQ && line[i + 1] === '"') { cur += '"'; i++ }   // escaped ""
      else inQ = !inQ
    } else if (c === delim && !inQ) {
      fields.push(cur.trim().replace(/^"|"$/g, ''))
      cur = ''
    } else {
      cur += c
    }
  }
  fields.push(cur.trim().replace(/^"|"$/g, ''))
  return fields
}

// CSV Methods
function triggerCsvImport() {
  csvInput.value?.click()
}

function handleCsvFile(event: Event) {
  const file = (event.target as HTMLInputElement).files?.[0]
  if (!file) return

  csvFileName.value = file.name
  const reader = new FileReader()
  reader.onload = (e) => {
    const text = e.target?.result as string
    parseCsv(text)
  }
  reader.readAsText(file)
}

function parseCsv(text: string) {
  // Strip UTF-8 BOM (Excel exports) and normalise line endings
  const clean = text.startsWith('﻿') ? text.slice(1) : text
  const lines = clean.split(/\r?\n/).filter((l) => l.trim() !== '')
  if (lines.length < 2) {
    alert('Le fichier CSV est vide ou invalide.')
    return
  }

  // Auto-detect delimiter (semicolon for French/European CSVs, comma otherwise)
  const firstLine = lines[0] || ''
  const delimiter = firstLine.includes(';') ? ';' : ','

  // Parse header row
  const headers = splitCsvLine(firstLine, delimiter)

  // Parse data rows — skip rows whose field count doesn't match (avoids silent corruption)
  const data: Record<string, string>[] = []
  for (let i = 1; i < lines.length; i++) {
    const fields = splitCsvLine(lines[i] || '', delimiter)
    if (fields.length === headers.length) {
      const obj: Record<string, string> = {}
      for (let j = 0; j < headers.length; j++) obj[headers[j]!] = fields[j]!
      data.push(obj)
    }
  }

  // Detect column types
  const DATE_RE = /^\d{1,4}[-\/\.]\d{1,2}[-\/\.]\d{2,4}$|^\d{4}-\d{2}-\d{2}(T[\d:Z.+-]+)?$/

  const columns = headers.map((name) => {
    const allValues  = data.map((d) => d[name] ?? '')
    const nonEmpty   = allValues.filter((v) => v !== '')
    const uniqueValues = [...new Set(allValues)]

    let type: 'number' | 'date' | 'category' | 'text' | 'boolean' = 'text'

    // Boolean: only true/false/oui/non/1/0/yes/no and at most 3 distinct values
    const isBoolean =
      nonEmpty.length > 0 &&
      nonEmpty.every((v) =>
        ['true', 'false', 'oui', 'non', '1', '0', 'yes', 'no'].includes(v.toLowerCase()),
      ) && uniqueValues.filter(v => v !== '').length <= 3

    // Date: matches a recognisable date pattern AND parses cleanly
    const isDate =
      !isBoolean &&
      nonEmpty.length > 0 &&
      nonEmpty.every((v) => DATE_RE.test(v.trim()) && !isNaN(Date.parse(v)))

    // Number: not a date, and every non-empty cell parses to a finite number
    const isNumber =
      !isBoolean &&
      !isDate &&
      nonEmpty.length > 0 &&
      nonEmpty.every((v) => isFinite(parseNumStr(v)))

    if (isBoolean)                      type = 'boolean'
    else if (isNumber)                  type = 'number'
    else if (isDate)                    type = 'date'
    else if (uniqueValues.length < 20)  type = 'category'
    else                                type = 'text'

    const metadata: any = { name, type }

    if (type === 'number') {
      const nums = nonEmpty.map(parseNumStr).filter(isFinite)
      metadata.min = nums.length ? Math.min(...nums) : 0
      metadata.max = nums.length ? Math.max(...nums) : 0
    } else if (type === 'category' || type === 'boolean') {
      metadata.uniqueValues = uniqueValues.filter((v) => v !== '')
    }

    return metadata
  })

  csvParsedData.value = data
  csvColumns.value    = columns
  showCsvPreview.value = true
  if (csvInput.value) csvInput.value.value = ''
}

function confirmCsvImport() {
  datasetStore.setDataset(csvFileName.value, csvColumns.value, csvParsedData.value)
  alert(`Importation réussie : ${csvParsedData.value.length} lignes chargées.`)
  closeCsvPreview()
}

/** Réceptionne les données SQL importées depuis SqlConnectModal */
function handleSqlImported(payload: {
  name: string
  columns: { name: string; type: string; min?: number; max?: number; uniqueValues?: string[] }[]
  rows: Record<string, string>[]
  sourceId: number
}) {
  datasetStore.setDataset(payload.name, payload.columns, payload.rows, payload.sourceId)
  showSqlModal.value = false
  showToast('success', `Source SQL « ${payload.name} » — ${payload.rows.length} lignes chargées`)
}

/** Réceptionne les données REST importées depuis RestConnectModal */
function handleRestImported(payload: {
  name:     string
  columns:  { name: string; type: string; min?: number; max?: number; uniqueValues?: string[] }[]
  rows:     Record<string, string>[]
  sourceId: number
}) {
  datasetStore.setDataset(payload.name, payload.columns, payload.rows, payload.sourceId)
  showRestModal.value = false
  showToast('success', `API REST « ${payload.name} » — ${payload.rows.length} lignes chargées`)
}

function closeCsvPreview() {
  showCsvPreview.value = false
  csvParsedData.value = []
  csvColumns.value = []
}

// ── Image Upload helpers ───────────────────────────────────────────────────
const imageFileInput = ref<HTMLInputElement | null>(null)

// ── Map widget — Nominatim geocoding ──────────────────────────────────────
const mapSearchQuery   = ref('')
const mapSearchResults = ref<any[]>([])
const mapSearching     = ref(false)
const mapSearchError   = ref('')

async function searchMapLocation() {
  const q = mapSearchQuery.value.trim()
  if (!q) return
  mapSearching.value = true
  mapSearchError.value = ''
  mapSearchResults.value = []
  try {
    const res = await fetch(
      `https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(q)}&format=json&limit=5&addressdetails=0`,
      { headers: { 'Accept-Language': 'fr' } }
    )
    const data = await res.json()
    if (!data.length) { mapSearchError.value = 'Aucun résultat trouvé'; return }
    mapSearchResults.value = data
  } catch {
    mapSearchError.value = 'Erreur de connexion'
  } finally {
    mapSearching.value = false
  }
}

function addMapMarker(result: any) {
  widgetConfig.mapMarkers.push({
    lat:   parseFloat(result.lat),
    lon:   parseFloat(result.lon),
    label: result.display_name.split(',')[0],
    color: widgetConfig.markerColor,
  })
  mapSearchResults.value = []
  mapSearchQuery.value = ''
  // Center map on first marker
  if (widgetConfig.mapMarkers.length === 1) {
    widgetConfig.mapCenterLat = parseFloat(result.lat)
    widgetConfig.mapCenterLon = parseFloat(result.lon)
    widgetConfig.mapZoom = 10
  }
}

function triggerImageUpload() {
  imageFileInput.value?.click()
}

function handleImageFile(event: Event) {
  const file = (event.target as HTMLInputElement).files?.[0]
  if (!file) return
  if (file.size > 5 * 1024 * 1024) {
    showToast('error', 'Image trop grande (max 5 Mo)')
    return
  }
  const reader = new FileReader()
  reader.onload = (e) => {
    widgetConfig.imageBase64 = e.target?.result as string
    widgetConfig.imageUrl    = ''
    // Immediately refresh the canvas preview
    if (selectedWidget.value) liveApplyConfig()
  }
  reader.readAsDataURL(file)
  // Reset so same file can be re-selected
  if (imageFileInput.value) imageFileInput.value.value = ''
}

function handleImageDrop(event: DragEvent) {
  const file = event.dataTransfer?.files?.[0]
  if (!file || !file.type.startsWith('image/')) return
  if (file.size > 5 * 1024 * 1024) {
    showToast('error', 'Image trop grande (max 5 Mo)')
    return
  }
  const reader = new FileReader()
  reader.onload = (e) => {
    widgetConfig.imageBase64 = e.target?.result as string
    widgetConfig.imageUrl    = ''
    if (selectedWidget.value) liveApplyConfig()
  }
  reader.readAsDataURL(file)
}

// ── Table / Heatmap parsers (used in canvas preview) ─────────────────────
function parseTableData(raw: string | null | undefined): { columns: string[]; rows: (string|number)[][] } {
  if (!raw) return { columns: [], rows: [] }
  try {
    const p = JSON.parse(raw)
    if (p && Array.isArray(p.columns) && Array.isArray(p.rows)) return p
    if (Array.isArray(p) && p.length && 'label' in p[0])
      return { columns: ['Label', 'Valeur'], rows: p.map((r: any) => [r.label, r.value]) }
  } catch { /* ignore */ }
  return { columns: [], rows: [] }
}

function parseHeatmapData(raw: string | null | undefined): { labels: string[]; matrix: number[][] } {
  if (!raw) return { labels: [], matrix: [] }
  try {
    const p = JSON.parse(raw)
    if (p && Array.isArray(p.labels) && Array.isArray(p.matrix)) return p
  } catch { /* ignore */ }
  return { labels: [], matrix: [] }
}

function parseImageData(raw: string | null | undefined): { src: string; fit: 'contain'|'cover'|'fill'; altText: string } {
  const empty = { src: '', fit: 'contain' as const, altText: '' }
  if (!raw) return empty
  try {
    const p = JSON.parse(raw)
    if (p && typeof p === 'object') {
      return {
        src:     p.imageBase64 || p.imageUrl || '',
        fit:     p.imageFit    || 'contain',
        altText: p.imageAlt    || '',
      }
    }
  } catch { /* ignore */ }
  return empty
}

// Data Processing for Widgets
function getMapData(widget: Widget) {
  // Use stored markers from data field
  if (widget.data) {
    try {
      const stored = typeof widget.data === 'string' ? JSON.parse(widget.data) : widget.data
      if (Array.isArray(stored)) return stored
    } catch { /* ignore */ }
  }
  return []
}

function getWidgetData(widget: Widget) {
  let config: Record<string, any> = {}
  try { config = JSON.parse(widget.data_config || '{}') } catch { config = {} }
  const dataset = datasetStore.currentDataset

  // Without a dataset, fall back to the snapshot stored in the backend
  if (!dataset) {
    if (widget.data) {
      try {
        const stored = typeof widget.data === 'string' ? JSON.parse(widget.data) : widget.data
        if (Array.isArray(stored) && stored.length > 0) return stored
      } catch { /* ignore bad JSON */ }
    }
    return []
  }

  // ── Image widget: store the config blob as data ──────────────────────────
  if (widget.type === 'image') {
    return config  // config already contains imageUrl/imageBase64/imageFit/imageAlt
  }

  // No axes configured at all → try snapshot, then return empty
  if (!config.yAxis && !config.xAxis && widget.type !== 'table' && widget.type !== 'heatmap') {
    if (widget.data) {
      try {
        const stored = typeof widget.data === 'string' ? JSON.parse(widget.data) : widget.data
        if (Array.isArray(stored) && stored.length > 0) return stored
      } catch { /* ignore bad JSON */ }
    }
    return []
  }

  // X axis set but Y axis empty → auto-count mode (count rows per group)
  const useCountMode = !config.yAxis

  // Real data filtering logic
  let filteredData = [...dataset.data]
  if (activeFilters.value.length > 0) {
    filteredData = dataset.data.filter((row) => {
      return activeFilters.value.every((filter) => {
        if (!filter.column) return true
        const colMeta = dataset.columns.find((c) => c.name === filter.column)
        const cellValue = row[filter.column]

        if (colMeta?.type === 'number') {
          const numValue = parseNumStr(String(cellValue ?? ''))
          const filterMin = parseNumStr(filter.value)
          return isFinite(numValue) && isFinite(filterMin) ? numValue >= filterMin : true
        } else if (colMeta?.type === 'date') {
          const cellDate = new Date(cellValue).getTime()
          const filterDate = new Date(filter.value).getTime()
          if (isNaN(cellDate) || isNaN(filterDate)) return true
          return cellDate >= filterDate
        } else {
          const strCellValue = String(cellValue || '').toLowerCase()
          const filterValue = String(filter.value).toLowerCase()
          return strCellValue.includes(filterValue)
        }
      })
    })
  }

  if (filteredData.length === 0 && widget.type !== 'table' && widget.type !== 'heatmap') {
    return []
  }

  // ── Table widget: return { columns, rows } from dataset ─────────────────
  if (widget.type === 'table') {
    const selectedCols: string[] = Array.isArray(config.tableColumns) && config.tableColumns.length > 0
      ? config.tableColumns
      : dataset.columns.map((c: { name: string }) => c.name)
    const rowLimit: number = config.tableRowLimit ?? 100
    const rows = filteredData.slice(0, rowLimit).map((row: any) =>
      selectedCols.map((col: string) => {
        const raw = row[col]
        if (raw === null || raw === undefined) return ''
        const n = parseNumStr(String(raw))
        return isFinite(n) ? n : String(raw)
      })
    )
    return { columns: selectedCols, rows }
  }

  // ── Heatmap: compute Pearson correlation matrix ───────────────────────
  if (widget.type === 'heatmap') {
    const numCols: string[] = Array.isArray(config.heatmapColumns) && config.heatmapColumns.length >= 2
      ? config.heatmapColumns
      : dataset.columns.filter((c: { name: string; type: string }) => c.type === 'number').map((c: { name: string }) => c.name)

    if (numCols.length < 2) return { labels: numCols, matrix: [] }

    // Extract numeric vectors (pairs matched by row index)
    const allVectors: number[][] = numCols.map((col: string) =>
      filteredData.map((row: any) => {
        const v = parseNumStr(String(row[col] ?? ''))
        return isFinite(v) ? v : 0
      })
    )

    // Pearson correlation between two equal-length arrays
    function pearson(a: number[], b: number[]): number {
      const n = a.length
      if (n < 2) return 0
      const meanA = a.reduce((s, v) => s + v, 0) / n
      const meanB = b.reduce((s, v) => s + v, 0) / n
      let num = 0, dA = 0, dB = 0
      for (let i = 0; i < n; i++) {
        const da = a[i]! - meanA, db = b[i]! - meanB
        num += da * db; dA += da * da; dB += db * db
      }
      const denom = Math.sqrt(dA * dB)
      return denom === 0 ? 0 : Math.round((num / denom) * 1000) / 1000
    }

    const matrix: number[][] = numCols.map((_: string, i: number) =>
      numCols.map((_: string, j: number) => pearson(allVectors[i]!, allVectors[j]!))
    )
    return { labels: numCols, matrix }
  }

  // ── Scatter : retourne les paires (x, y) brutes sans agrégation ───────────
  if (widget.type === 'scatter' && config.xAxis && config.yAxis) {
    const maxPts = config.maxItems || 500
    const labelAxis = config.labelAxis || ''
    return filteredData.slice(0, maxPts).map((row: any) => {
      const nx = parseNumStr(String(row[config.xAxis] ?? ''))
      const ny = parseNumStr(String(row[config.yAxis] ?? ''))
      return {
        x: isFinite(nx) ? nx : 0,
        y: isFinite(ny) ? ny : 0,
        label: labelAxis ? String(row[labelAxis] ?? '') : '',
      }
    })
  }

  // ── Résolution de la mesure Y (colonne brute ou mesure calculée) ──────────
  const customMeasures = dataset.customMeasures ?? []
  const customMeasure  = config.yAxis ? customMeasures.find(m => m.name === config.yAxis) : null

  // In count mode (X set, Y empty) the effective aggregation is always 'count'
  // Normalize aggregation: treat "none" or any unknown value as "sum"
  const VALID_AGG = ['none', 'sum', 'avg', 'median', 'count', 'count_distinct', 'min', 'max', 'std']
  const normalizedAgg = VALID_AGG.includes(config.aggregation) ? config.aggregation : 'sum'
  const effectiveAggregation = useCountMode ? 'count' : normalizedAgg
  // Aggregations that require storing all row values (for post-processing)
  const needsAllValues = ['median', 'std', 'none'].includes(effectiveAggregation)

  /**
   * Retourne la valeur numérique brute d'une ligne pour la mesure Y.
   * Retourne NaN si la cellule est vide ou non-numérique — les appelants
   * filtrent via isFinite() pour exclure ces lignes des agrégations.
   */
  function getRowValue(row: Record<string, string>): number {
    if (useCountMode) return 1  // pour 'count' : on compte toujours
    if (!customMeasure) {
      return parseNumStr(String(row[config.yAxis] ?? ''))  // NaN si vide/non-num
    }
    const v1 = parseNumStr(String(row[customMeasure.col1] ?? ''))
    if (!isFinite(v1)) return NaN  // col1 invalide → résultat invalide
    if (customMeasure.formula === 'simple')   return v1
    if (customMeasure.formula === 'multiply') return v1 * (customMeasure.constant ?? 1)
    const v2 = parseNumStr(String(row[customMeasure.col2 ?? ''] ?? ''))
    if (!isFinite(v2)) return NaN  // col2 invalide → résultat invalide
    if (customMeasure.formula === 'ratio')      return v2 !== 0 ? (customMeasure.pct ? v1 / v2 * 100 : v1 / v2) : NaN
    if (customMeasure.formula === 'difference') return v1 - v2
    return v1
  }

  // Case: No X Axis (Total aggregation)
  if (!config.xAxis) {
    // Only Y is set, no grouping — aggregate across all rows.
    // getRowValue returns NaN for empty/non-numeric cells → filter them out.
    const numericValues = filteredData
      .map(row => getRowValue(row))
      .filter(v => isFinite(v))

    let finalValue = 0

    if (effectiveAggregation === 'none') {
      finalValue = numericValues[0] ?? 0
    } else if (effectiveAggregation === 'sum') {
      finalValue = numericValues.reduce((a, b) => a + b, 0)
    } else if (effectiveAggregation === 'avg') {
      finalValue = numericValues.length ? numericValues.reduce((a, b) => a + b, 0) / numericValues.length : 0
    } else if (effectiveAggregation === 'median') {
      const sorted = [...numericValues].sort((a, b) => a - b)
      const mid = Math.floor(sorted.length / 2)
      finalValue = sorted.length % 2 !== 0 ? sorted[mid]! : (((sorted[mid - 1] ?? 0) + (sorted[mid] ?? 0)) / 2)
    } else if (effectiveAggregation === 'count') {
      finalValue = filteredData.length  // count ALL rows, not just numeric
    } else if (effectiveAggregation === 'count_distinct') {
      finalValue = new Set(filteredData.map(r => String(r[config.yAxis] ?? ''))).size
    } else if (effectiveAggregation === 'min') {
      finalValue = numericValues.length ? Math.min(...numericValues) : 0
    } else if (effectiveAggregation === 'max') {
      finalValue = numericValues.length ? Math.max(...numericValues) : 0
    } else if (effectiveAggregation === 'std') {
      const n    = numericValues.length
      const mean = n ? numericValues.reduce((a, b) => a + b, 0) / n : 0
      const variance = numericValues.reduce((s, v) => s + Math.pow(v - mean, 2), 0) / (n || 1)
      finalValue = Math.sqrt(variance)
    }

    return [{ label: config.yAxis || 'Nombre', value: Math.round(finalValue * 1e6) / 1e6 }]
  }

  // Real data aggregation logic (with X Axis)
  interface GroupBucket {
    sum:      number
    count:    number      // total rows (for 'count' aggregation)
    numCount: number      // rows with a finite numeric Y value (for avg)
    max:      number
    min:      number
    vals:     number[]    // only populated when needsAllValues
    distinct: Set<string> // only populated for count_distinct
  }
  const groups: Record<string, GroupBucket> = {}

  const xAxisCol = dataset.columns.find((c) => c.name === config.xAxis)

  // ── Numeric binning: pre-compute boundaries & pre-insert ordered buckets ────
  // Only active for pie/doughnut when xAxis is numeric and numericBins > 1.
  let binBoundaries: number[] | null = null
  const numericBinsRequested =
    (widget.type === 'pie' || widget.type === 'doughnut') &&
    xAxisCol?.type === 'number' &&
    (config.numericBins as number | undefined ?? 5) > 1

  if (numericBinsRequested) {
    const xVals = filteredData
      .map((row: Record<string, string>) => parseNumStr(String(row[config.xAxis] ?? '')))
      .filter((v: number) => isFinite(v))
    if (xVals.length > 0) {
      const xMin  = Math.min(...xVals)
      const xMax  = Math.max(...xVals)
      const nBins = config.numericBins as number ?? 5
      const step  = (xMax - xMin) / nBins || 1
      const fmtB  = (n: number) => Number.isInteger(n) ? String(n) : +n.toFixed(2) + ''
      binBoundaries = Array.from({ length: nBins + 1 }, (_, i) => xMin + i * step)
      // Pre-insert buckets in order so Object.entries() preserves ascending order
      for (let b = 0; b < nBins; b++) {
        const lo     = binBoundaries[b]!
        const hi     = binBoundaries[b + 1]!
        const isLast = b === nBins - 1
        const lbl    = isLast
          ? `${config.xAxis} ≥ ${fmtB(lo)} et ${config.xAxis} ≤ ${fmtB(hi)}`
          : `${config.xAxis} ≥ ${fmtB(lo)} et ${config.xAxis} < ${fmtB(hi)}`
        groups[lbl] = { sum: 0, count: 0, numCount: 0, max: -Infinity, min: Infinity, vals: [], distinct: new Set() }
      }
    }
  }

  filteredData.forEach((row) => {
    let key: string

    // ── Numeric binning key ───────────────────────────────────────────────────
    if (binBoundaries) {
      const rawX = parseNumStr(String(row[config.xAxis] ?? ''))
      if (isFinite(rawX)) {
        const nBins = binBoundaries.length - 1
        const fmtB  = (n: number) => Number.isInteger(n) ? String(n) : +n.toFixed(2) + ''
        let binIdx  = -1
        for (let b = 0; b < nBins; b++) {
          const lo = binBoundaries[b]!
          const hi = binBoundaries[b + 1]!
          const isLast = b === nBins - 1
          if (isLast ? rawX >= lo && rawX <= hi : rawX >= lo && rawX < hi) {
            binIdx = b
            break
          }
        }
        if (binIdx !== -1) {
          const lo     = binBoundaries[binIdx]!
          const hi     = binBoundaries[binIdx + 1]!
          const isLast = binIdx === nBins - 1
          key = isLast
            ? `${config.xAxis} ≥ ${fmtB(lo)} et ${config.xAxis} ≤ ${fmtB(hi)}`
            : `${config.xAxis} ≥ ${fmtB(lo)} et ${config.xAxis} < ${fmtB(hi)}`
        } else {
          key = 'Sans valeur'
        }
      } else {
        key = 'Sans valeur'
      }
    } else {
      key = String(row[config.xAxis] ?? 'Sans valeur')
    }

    // Handle Date Granularity
    if (!binBoundaries && xAxisCol?.type === 'date') {
      const date = new Date(key)
      if (!isNaN(date.getTime())) {
        if (config.dateGranularity === 'month') {
          key = date.toLocaleString('default', { month: 'short', year: '2-digit' })
        } else if (config.dateGranularity === 'year') {
          key = date.getFullYear().toString()
        } else {
          key = date.toLocaleDateString()
        }
      }
    }

    const val = getRowValue(row)

    if (!groups[key]) {
      groups[key] = { sum: 0, count: 0, numCount: 0, max: -Infinity, min: Infinity, vals: [], distinct: new Set() }
    }
    const g = groups[key]
    g.count += 1   // always count rows (for 'count' aggregation)
    if (isFinite(val)) {
      g.sum      += val
      g.numCount += 1
      if (val > g.max) g.max = val
      if (val < g.min) g.min = val
      if (needsAllValues) g.vals.push(val)
    }
    if (effectiveAggregation === 'count_distinct') g.distinct.add(String(row[config.yAxis] ?? ''))
  })

  const result = Object.entries(groups).map(([label, g]) => {
    let value = 0
    if (effectiveAggregation === 'none')                value = g.vals[0] ?? 0
    else if (effectiveAggregation === 'sum')            value = g.sum
    else if (effectiveAggregation === 'avg')            value = g.numCount ? g.sum / g.numCount : 0
    else if (effectiveAggregation === 'count')          value = g.count
    else if (effectiveAggregation === 'count_distinct') value = g.distinct.size
    else if (effectiveAggregation === 'min')            value = g.min === Infinity  ? 0 : g.min
    else if (effectiveAggregation === 'max')            value = g.max === -Infinity ? 0 : g.max
    else if (effectiveAggregation === 'median') {
      const sorted = [...g.vals].sort((a, b) => a - b)
      const mid = Math.floor(sorted.length / 2)
      value = sorted.length % 2 !== 0 ? sorted[mid] : ((sorted[mid - 1] ?? 0) + (sorted[mid] ?? 0)) / 2
    } else if (effectiveAggregation === 'std') {
      const n = g.vals.length
      const mean = n ? g.sum / n : 0
      const variance = g.vals.reduce((s, v) => s + Math.pow(v - mean, 2), 0) / (n || 1)
      value = Math.sqrt(variance)
    }
    return { label, value: Math.round(value * 1e6) / 1e6 }
  })

  // Sort results
  // In numeric-binning mode the pre-inserted keys already define the correct
  // ascending order (Object.entries preserves insertion order).  Skip any
  // user sort so the bins always display low → high.
  const sortedResult = [...result]
  if (!binBoundaries) {
    if (config.sortBy === 'value') {
      sortedResult.sort((a, b) => config.sortDir === 'asc' ? a.value - b.value : b.value - a.value)
    } else {
      sortedResult.sort((a, b) => config.sortDir === 'asc'
        ? a.label.localeCompare(b.label)
        : b.label.localeCompare(a.label))
    }
  }

  // Limit results
  const maxItems = config.maxItems || 15
  const finalResult = sortedResult.slice(0, maxItems)

  return finalResult
}

/**
 * Compute box-plot statistics from raw dataset rows.
 * Groups by xAxis (categorical), computes five-number summary for yAxis.
 * Returns an array of BoxPlotPoint for BoxPlotWidget.
 */
function getBoxPlotData(widget: Widget): BoxPlotPoint[] {
  const config = getWidgetCfg(widget)
  const dataset = datasetStore.currentDataset

  // No dataset → try stored snapshot
  if (!dataset) {
    if (widget.data) {
      try {
        const stored = typeof widget.data === 'string' ? JSON.parse(widget.data) : widget.data
        if (Array.isArray(stored) && stored.length > 0 && 'median' in (stored[0] ?? {})) return stored
      } catch { /* ignore */ }
    }
    return []
  }

  if (!config.xAxis || !config.yAxis) return []

  // Group rows by xAxis value
  const groups: Record<string, number[]> = {}
  for (const row of dataset.data) {
    const key = String(row[config.xAxis] ?? 'Sans valeur')
    const raw = parseNumStr(String(row[config.yAxis] ?? ''))
    if (!isFinite(raw)) continue
    if (!groups[key]) groups[key] = []
    groups[key]!.push(raw)
  }

  // Five-number summary helper
  function fiveNum(vals: number[]): { min: number; q1: number; median: number; q3: number; max: number; outliers: number[] } {
    const sorted = [...vals].sort((a, b) => a - b)
    const n = sorted.length
    const med = (arr: number[]) => {
      const m = Math.floor(arr.length / 2)
      return arr.length % 2 !== 0 ? arr[m]! : ((arr[m - 1]! + arr[m]!) / 2)
    }
    const median = med(sorted)
    const lower  = sorted.slice(0, Math.floor(n / 2))
    const upper  = sorted.slice(Math.ceil(n / 2))
    const q1     = lower.length ? med(lower) : sorted[0]!
    const q3     = upper.length ? med(upper) : sorted[n - 1]!
    const iqr    = q3 - q1
    const lo     = q1 - 1.5 * iqr
    const hi     = q3 + 1.5 * iqr
    const inliers  = sorted.filter(v => v >= lo && v <= hi)
    const outliers = sorted.filter(v => v < lo || v > hi)
    return {
      min:      inliers[0]       ?? sorted[0]!,
      max:      inliers[inliers.length - 1] ?? sorted[n - 1]!,
      q1, q3, median, outliers,
    }
  }

  const maxGroups = config.maxItems || 15
  return Object.entries(groups)
    .slice(0, maxGroups)
    .map(([label, vals]) => ({ label, ...fiveNum(vals) }))
}

function getKpiValue(widget: Widget): number {
  const data = getWidgetData(widget)
  if (Array.isArray(data)) {
    return data.reduce((acc, curr) => acc + curr.value, 0)
  }
  return (data as any)?.value ?? 0
}

/**
 * Returns an ordered array of Y values suitable for the KPI sparkline.
 * Sorts by label (ascending) to get chronological order for time axes.
 */
function getKpiSparkline(widget: Widget): number[] {
  const cfg = getWidgetCfg(widget)
  if (!cfg.kpiShowSparkline || !cfg.xAxis) return []
  const raw = getWidgetData(widget)
  if (!Array.isArray(raw) || raw.length < 2) return []
  return [...raw]
    .sort((a, b) => a.label.localeCompare(b.label, undefined, { numeric: true }))
    .map(d => d.value)
}

// ── Helpers ───────────────────────────────────────────────────────────────
/** Parse le data_config d'un widget en objet (jamais d'erreur) */
function getWidgetCfg(widget: Widget): Record<string, any> {
  try { return JSON.parse(widget.data_config || '{}') } catch { return {} }
}

// ── Widget Configuration Methods ──────────────────────────────────────────
function openWidgetConfig(widget: Widget) {
  // Suppress the deep watcher while we populate widgetConfig from the widget's
  // stored config — we don't want to write back half-initialised values.
  _suppressLiveApply = true

  selectedWidget.value = widget
  showConfigPanel.value = true
  showDashSettings.value = false  // ferme l'autre tiroir

  const cfg = getWidgetCfg(widget)
  // Data
  widgetConfig.title            = widget.title
  widgetConfig.xAxis            = cfg.xAxis        || ''
  widgetConfig.yAxis            = cfg.yAxis        || ''
  widgetConfig.aggregation      = cfg.aggregation  || 'sum'
  widgetConfig.dateGranularity  = cfg.dateGranularity || 'month'
  widgetConfig.maxItems         = cfg.maxItems     ?? 15
  widgetConfig.sortBy           = cfg.sortBy       || 'value'
  widgetConfig.sortDir          = cfg.sortDir      || 'desc'
  // Appearance
  widgetConfig.color            = cfg.color || '#1B6B3A'
  widgetConfig.backgroundType   = cfg.backgroundType || 'none'
  const rawBg = cfg.backgroundColor || ''
  widgetConfig.backgroundColor  = rawBg.startsWith('#') ? rawBg : ''
  widgetConfig.width            = widget.width  ?? 6
  widgetConfig.height           = widget.height ?? 4
  // KPI
  widgetConfig.kpiIcon          = cfg.kpiIcon       || 'auto'
  widgetConfig.kpiSuffix        = cfg.kpiSuffix     || ''
  widgetConfig.kpiShowTrend     = !!cfg.kpiShowTrend
  widgetConfig.kpiTrendValue    = cfg.kpiTrendValue ?? 0
  widgetConfig.kpiShowSparkline = !!cfg.kpiShowSparkline
  // Bar
  widgetConfig.barOrientation   = cfg.barOrientation   || 'vertical'
  widgetConfig.barShowValues    = !!cfg.barShowValues
  widgetConfig.barBorderRadius  = cfg.barBorderRadius  ?? 3
  // Line
  widgetConfig.lineSmooth       = !!cfg.lineSmooth
  widgetConfig.lineFill         = cfg.lineFill !== undefined ? !!cfg.lineFill : true
  widgetConfig.lineShowDots     = cfg.lineShowDots !== undefined ? !!cfg.lineShowDots : true
  widgetConfig.lineShowLabels   = !!cfg.lineShowLabels
  // Pie
  widgetConfig.pieMaxSlices     = cfg.pieMaxSlices      ?? 8
  widgetConfig.pieLegendPosition= cfg.pieLegendPosition || 'right'
  widgetConfig.pieShowLabels    = !!cfg.pieShowLabels
  widgetConfig.numericBins      = cfg.numericBins       ?? 5
  // Scatter
  widgetConfig.scatterDotSize   = cfg.scatterDotSize ?? 4
  widgetConfig.labelAxis        = cfg.labelAxis   || ''
  // Description & unités
  widgetConfig.description      = cfg.description  || ''
  widgetConfig.valuePrefix      = cfg.valuePrefix  || ''
  widgetConfig.valueSuffix      = cfg.valueSuffix  || ''
  widgetConfig.yAxisLabel       = cfg.yAxisLabel   || ''
  // Gauge
  widgetConfig.gaugeMin         = cfg.gaugeMin     ?? 0
  widgetConfig.gaugeMax         = cfg.gaugeMax     ?? 100
  // Text
  widgetConfig.textContent      = cfg.textContent    || ''
  widgetConfig.textFontSize     = cfg.textFontSize   ?? 14
  widgetConfig.textAlign        = cfg.textAlign      || 'left'
  widgetConfig.textFontFamily   = cfg.textFontFamily || 'inherit'
  // Table
  widgetConfig.tableColumns    = Array.isArray(cfg.tableColumns) ? cfg.tableColumns : []
  widgetConfig.tableRowLimit   = cfg.tableRowLimit  ?? 100
  widgetConfig.tablePageSize   = cfg.tablePageSize  ?? 10
  widgetConfig.tableShowSearch = cfg.tableShowSearch !== false
  // Heatmap
  widgetConfig.heatmapColumns   = Array.isArray(cfg.heatmapColumns) ? cfg.heatmapColumns : []
  // Image
  widgetConfig.imageUrl         = cfg.imageUrl    || ''
  widgetConfig.imageBase64      = cfg.imageBase64 || ''
  widgetConfig.imageFit         = cfg.imageFit    || 'contain'
  widgetConfig.imageAlt         = cfg.imageAlt    || ''
  // Map
  widgetConfig.mapMarkers       = Array.isArray(cfg.mapMarkers) ? cfg.mapMarkers : []
  widgetConfig.mapZoom          = cfg.mapZoom      ?? 5
  widgetConfig.mapCenterLat     = cfg.mapCenterLat ?? 46.5
  widgetConfig.mapCenterLon     = cfg.mapCenterLon ?? 2.3
  widgetConfig.markerColor      = cfg.markerColor  || '#3b82f6'

  widgetConfig.activeTab = 'data'
  ppTab.value = 'build'
  fmtOpen.typeOptions = true

  // Re-enable live preview after Vue has flushed all the reactive assignments above
  nextTick(() => {
    _suppressLiveApply = false

    // Auto-detect unit if nothing was saved yet for this widget
    if (!widgetConfig.valuePrefix && !widgetConfig.valueSuffix && widgetConfig.yAxis) {
      let { prefix, suffix } = detectUnitFromColumn(widgetConfig.yAxis)

      if (!prefix && !suffix) {
        const col = widgetConfig.yAxis.toLowerCase()
        const PATTERNS: [RegExp, string, string][] = [
          [/€|eur|euro|prix|montant|cout|coût|revenue|revenu|salaire|ca\b|chiffre|vente/, '', ' €'],
          [/\$|usd|dollar/,                                                               '$', ''],
          [/£|gbp|livre/,                                                                 '£', ''],
          [/%|pct|taux|rate|ratio|percent|pourcent/,                                      '', '%'],
          [/\bkg\b|kilo(?!metre)/,                                                        '', ' kg'],
          [/\bkm\b|kilometre|kilomètre/,                                                  '', ' km'],
          [/\bg\b|gramme/,                                                                 '', ' g'],
          [/\bl\b|litre/,                                                                  '', ' L'],
          [/\bm\b|metre|mètre/,                                                           '', ' m'],
          [/\bh\b|heure/,                                                                  '', ' h'],
        ]
        for (const [re, pre, suf] of PATTERNS) {
          if (re.test(col)) { prefix = pre; suffix = suf; break }
        }
      }

      widgetConfig.valuePrefix = prefix
      widgetConfig.valueSuffix = suffix
    }
  })
}

async function applyWidgetConfig() {
  if (!selectedWidget.value) return

  // Flush all current widgetConfig values to the canvas widget
  liveApplyConfig()

  const cfg = buildCfgPayload()
  showConfigPanel.value = false

  // Auto-persist to backend for saved widgets (real IDs are small integers; temp IDs > 1e12)
  const isTemp = (id: number) => id > 1e12
  const w = selectedWidget.value!
  if (!isTemp(w.id) && dashboardStore.currentDashboard) {
    try {
      const widgetData = w.type === 'boxplot' ? getBoxPlotData(w) : getWidgetData(w)
      const configPayload = JSON.stringify({
        ...cfg,
        width:  w.width  ?? 6,
        height: w.height ?? 4,
        x:      w.x      ?? 0,
        y:      w.y      ?? 0,
      })
      await widgetService.update(w.id, {
        type:   w.type,
        title:  w.title,
        data:   JSON.stringify(widgetData),
        config: configPayload,
      })
      // Keep local snapshot in sync (tables always empty — live data only)
      w.data = JSON.stringify(widgetData)
      showToast('success', `Widget « ${widgetConfig.title} » sauvegardé`)
    } catch {
      // Save failed silently — config is still applied locally
      showToast('success', `Widget « ${widgetConfig.title} » mis à jour (cliquez Enregistrer pour persister)`)
    }
  } else {
    showToast('success', `Widget « ${widgetConfig.title} » mis à jour`)
  }
}

function handleDragStart(event: DragEvent, type: WidgetType) {
  if (event.dataTransfer) {
    event.dataTransfer.setData('widgetType', type.id)
    event.dataTransfer.setData('widgetLabel', type.label)
    event.dataTransfer.effectAllowed = 'copy'
  }
}

// Widget Reordering logic
const draggedWidgetIndex = ref<number | null>(null)
const resizingWidget = ref<Widget | null>(null)
const resizeCorner = ref<string>('bottom-right')
let initialMouseX = 0
let initialMouseY = 0
let initialWidth = 0
let initialHeight = 0

function startResizing(event: MouseEvent, widget: Widget, corner: string) {
  resizingWidget.value = widget
  resizeCorner.value = corner
  initialMouseX = event.clientX
  initialMouseY = event.clientY
  initialWidth = widget.width
  initialHeight = widget.height

  window.addEventListener('mousemove', doResize)
  window.addEventListener('mouseup', stopResize)

  if (corner === 'top-left' || corner === 'bottom-right') {
    document.body.style.cursor = 'nwse-resize'
  } else {
    document.body.style.cursor = 'nesw-resize'
  }
}

function doResize(event: MouseEvent) {
  if (!resizingWidget.value) return

  const deltaX = event.clientX - initialMouseX
  const deltaY = event.clientY - initialMouseY

  const gridContainer = document.querySelector('.canvas-grid')
  if (!gridContainer) return

  const cellWidth = gridContainer.clientWidth / 12
  const cellHeight = 100

  let newWidth = initialWidth
  let newHeight = initialHeight

  if (resizeCorner.value.includes('right')) {
    newWidth = Math.max(1, Math.min(12, initialWidth + Math.round(deltaX / cellWidth)))
  } else if (resizeCorner.value.includes('left')) {
    // Left resizing is tricky with CSS grid without moving X,
    // for now we'll support size change from right/bottom mainly or simplify
    newWidth = Math.max(1, Math.min(12, initialWidth - Math.round(deltaX / cellWidth)))
  }

  if (resizeCorner.value.includes('bottom')) {
    newHeight = Math.max(1, Math.min(20, initialHeight + Math.round(deltaY / cellHeight)))
  } else if (resizeCorner.value.includes('top')) {
    newHeight = Math.max(1, Math.min(20, initialHeight - Math.round(deltaY / cellHeight)))
  }

  resizingWidget.value.width = newWidth
  resizingWidget.value.height = newHeight
}

function stopResize() {
  resizingWidget.value = null
  window.removeEventListener('mousemove', doResize)
  window.removeEventListener('mouseup', stopResize)
  document.body.style.cursor = ''
}

function handleWidgetDragStart(event: DragEvent, index: number) {
  draggedWidgetIndex.value = index
  if (event.dataTransfer) {
    event.dataTransfer.effectAllowed = 'move'
    // To distinguish from palette drag
    event.dataTransfer.setData('isReorder', 'true')
  }
}

function handleWidgetDrop(event: DragEvent, targetIndex: number) {
  const isReorder = event.dataTransfer?.getData('isReorder') === 'true'

  if (isReorder && draggedWidgetIndex.value !== null) {
    const widgets = [...dashboardStore.currentWidgets]
    const [draggedWidget] = widgets.splice(draggedWidgetIndex.value, 1)
    if (draggedWidget) {
      widgets.splice(targetIndex, 0, draggedWidget)
      dashboardStore.currentWidgets = widgets
    }
    draggedWidgetIndex.value = null
  }
}

function handleDrop(event: DragEvent) {
  const isReorder = event.dataTransfer?.getData('isReorder') === 'true'
  if (isReorder) return // Handled by handleWidgetDrop

  const type = event.dataTransfer?.getData('widgetType')
  const label = event.dataTransfer?.getData('widgetLabel')
  if (!type || !dashboardStore.currentDashboard) return

  const newWidget: Widget = {
    id: Date.now() + Math.floor(Math.random() * 1_000_000),
    dashboard_id: dashboardStore.currentDashboard.id,
    type,
    title: `Nouveau ${label}`,
    x: 0,
    y: 0,
    width: type === 'kpi' ? 3 : 6,
    height: type === 'kpi' ? 2 : 4,
    data_config: JSON.stringify(defaultConfig(type)),
  }

  dashboardStore.currentWidgets.push(newWidget)
}

function removeWidget(id: number) {
  pushUndo()
  dashboardStore.currentWidgets = dashboardStore.currentWidgets.filter((w) => w.id !== id)
}

/** Bascule les libellés de valeurs directement sur le widget (sans ouvrir le panneau) */
function toggleWidgetLabels(widget: Widget) {
  const idx = dashboardStore.currentWidgets.findIndex(x => x.id === widget.id)
  if (idx === -1) return
  const w = dashboardStore.currentWidgets[idx]!
  const cfg = getWidgetCfg(w)
  if (widget.type === 'bar' || widget.type === 'funnel') {
    cfg.barShowValues = !cfg.barShowValues
  } else if (widget.type === 'line' || widget.type === 'area') {
    cfg.lineShowLabels = !cfg.lineShowLabels
  } else if (widget.type === 'pie' || widget.type === 'doughnut') {
    cfg.pieShowLabels = !cfg.pieShowLabels
  }
  w.data_config = JSON.stringify(cfg)
}

/** Indique si les libellés sont actuellement actifs sur ce widget */
function widgetLabelsOn(widget: Widget): boolean {
  const cfg = getWidgetCfg(widget)
  if (widget.type === 'bar' || widget.type === 'funnel') return !!cfg.barShowValues
  if (widget.type === 'line' || widget.type === 'area')  return !!cfg.lineShowLabels
  if (widget.type === 'pie' || widget.type === 'doughnut') return !!cfg.pieShowLabels
  return false
}

function duplicateWidget(w: Widget) {
  pushUndo()
  const copy: Widget = {
    ...w,
    id:    Date.now() + Math.floor(Math.random() * 1_000_000),
    title: w.title + ' (copie)',
    x:     (w.x ?? 0),
    y:     (w.y ?? 0) + (w.height ?? 4),
  }
  dashboardStore.currentWidgets.push(copy)
  showToast('success', `Widget « ${copy.title} » dupliqué`)
}

/** Click sur un item de la palette → ajoute directement sans drag */
/** Config initiale selon le type de widget — libellés activés par défaut */
function defaultConfig(typeId: string): Record<string, unknown> {
  const base: Record<string, unknown> = { color: '#1B6B3A', isDefaultColor: true }
  if (typeId === 'bar') {
    return { ...base, barShowValues: true, barBorderRadius: 3, barOrientation: 'vertical' }
  }
  if (typeId === 'line' || typeId === 'area') {
    return { ...base, lineShowLabels: true, lineShowDots: true, lineFill: true }
  }
  if (typeId === 'pie' || typeId === 'doughnut') {
    return { ...base, pieShowLabels: true, pieMaxSlices: 8, pieLegendPosition: 'right' }
  }
  if (typeId === 'scatter') {
    return { ...base, scatterDotSize: 4, maxItems: 500, labelAxis: '' }
  }
  return base
}

/** Trouve la colonne réelle la plus proche d'un nom suggéré par l'IA */
function resolveColumn(aiName: string | null | undefined): string {
  if (!aiName) return ''
  const cols = datasetStore.currentDataset?.columns ?? []
  // 1. Correspondance exacte
  const exact = cols.find(c => c.name === aiName)
  if (exact) return exact.name
  // 2. Insensible à la casse
  const lower = aiName.toLowerCase()
  const ci = cols.find(c => c.name.toLowerCase() === lower)
  if (ci) return ci.name
  // 3. Inclusion partielle (ex : l'IA dit "montant" et la col s'appelle "Montant_Total")
  const partial = cols.find(c =>
    c.name.toLowerCase().includes(lower) || lower.includes(c.name.toLowerCase())
  )
  if (partial) return partial.name
  // 4. Aucun match → retourne le nom tel quel (sera affiché comme vide côté widget)
  return aiName
}

/** Reçoit une config IA et crée un widget sur le canvas */
// ── Export PNG (builder) ─────────────────────────────────────────
const builderWidgetRefs: Record<number, HTMLElement | null> = reactive({})

async function exportBuilderWidgetPng(widgetId: number, title: string) {
  const el = builderWidgetRefs[widgetId]
  if (!el) return
  await exportToPng(el, title, '#1e1e2e')
}

/** Capture un élément DOM en PNG et déclenche le téléchargement */
async function exportToPng(el: HTMLElement, title: string, bg = '#1e1e2e') {
  try {
    const scale = 2
    const w = el.offsetWidth
    const h = el.offsetHeight
    const dataUrl = await domtoimage.toPng(el, {
      bgcolor: bg,
      width:  w * scale,
      height: h * scale,
      style: {
        transform:       `scale(${scale})`,
        transformOrigin: 'top left',
        width:  `${w}px`,
        height: `${h}px`,
      },
    })
    // Redessiner dans un canvas aux bonnes dimensions finales
    const img = new Image()
    img.src = dataUrl
    await new Promise(r => { img.onload = r })
    const canvas = document.createElement('canvas')
    canvas.width  = w * scale
    canvas.height = h * scale
    const ctx = canvas.getContext('2d')!
    ctx.drawImage(img, 0, 0, w * scale, h * scale)
    const link = document.createElement('a')
    link.download = `${(title || 'widget').replace(/\s+/g, '_')}.png`
    link.href = canvas.toDataURL('image/png')
    link.click()
  } catch (e) {
    console.error('Export PNG failed', e)
  }
}

function handleAiCreateWidget(cfg: AiWidgetConfig) {
  if (!dashboardStore.currentDashboard) return

  // ── 1. Normalise le type ─────────────────────────────────────────────────
  const TYPE_MAP: Record<string, string> = {
    barchart: 'bar', 'bar chart': 'bar', 'bar_chart': 'bar',
    linechart: 'line', 'line chart': 'line', 'line_chart': 'line', area: 'line',
    piechart: 'pie', 'pie chart': 'pie', 'pie_chart': 'pie', donut: 'doughnut', doughnut: 'doughnut',
    scatter: 'scatter', scatterplot: 'scatter', 'scatter plot': 'scatter', 'scatter_plot': 'scatter',
    'nuage de points': 'scatter', 'nuage': 'scatter',
    'regression plot': 'scatter', 'pair plot': 'scatter',
    table: 'table', tableau: 'table',
    kpi: 'kpi', card: 'kpi', 'carte kpi': 'kpi',
  }
  const rawType = (cfg.type ?? 'bar').toLowerCase().trim()
  const type = TYPE_MAP[rawType] ?? rawType

  // ── 2. Résout les colonnes ───────────────────────────────────────────────
  const xAxis = resolveColumn(cfg.xAxis)
  const yAxis = resolveColumn(cfg.yAxis)

  // ── 3. Normalise l'agrégation ─────────────────────────────────────────────
  const VALID_AGG = ['none', 'sum', 'avg', 'median', 'count', 'count_distinct', 'min', 'max', 'std']
  const rawAgg = (cfg.aggregation ?? 'sum').toLowerCase()
  const aggregation = VALID_AGG.includes(rawAgg) ? rawAgg : 'sum'

  // ── 4. Auto-détecte l'unité depuis les valeurs + le nom de colonne ───────
  let { prefix, suffix } = detectUnitFromColumn(yAxis)
  if (!prefix && !suffix && yAxis) {
    const col = yAxis.toLowerCase()
    const UNIT_PATTERNS: [RegExp, string, string][] = [
      [/€|eur|euro|prix|montant|cout|coût|revenue|revenu|salaire|\bca\b|chiffre|vente/, '', ' €'],
      [/\$|usd|dollar/, '$', ''],
      [/£|gbp|livre/, '£', ''],
      [/%|pct|taux|rate|ratio|percent|pourcent/, '', '%'],
      [/\bkg\b|kilo(?!metre)/, '', ' kg'],
      [/\bkm\b|kilometre|kilomètre/, '', ' km'],
      [/\bl\b|litre/, '', ' L'],
      [/\bh\b|heure/, '', ' h'],
    ]
    for (const [re, pre, suf] of UNIT_PATTERNS) {
      if (re.test(col)) { prefix = pre; suffix = suf; break }
    }
  }

  // ── 5. Granularité date + tri ────────────────────────────────────────────
  const xColMeta = datasetStore.currentDataset?.columns.find(c => c.name === xAxis)
  const isDateX  = xColMeta?.type === 'date'
  const dateGranularity = isDateX ? 'month' : undefined

  // ── 6. Construit la config finale ────────────────────────────────────────
  const aiCfg: Record<string, unknown> = {
    ...defaultConfig(type),
    xAxis,
    yAxis,
    aggregation,
    description: cfg.explanation ?? '',
    valuePrefix: prefix,
    valueSuffix: suffix,
    sortBy:  isDateX ? 'label' : 'value',
    sortDir: isDateX ? 'asc'   : 'desc',
    // scatter needs many points — don't cap at 20
    ...(type !== 'scatter' ? { maxItems: 20 } : {}),
    ...(dateGranularity ? { dateGranularity } : {}),
  }

  // ── 7. Titre lisible ─────────────────────────────────────────────────────
  let title = cfg.title || ''
  if (!title || ['bar','line','pie','kpi','scatter','table','doughnut'].includes(title.toLowerCase())) {
    title = yAxis
      ? (xAxis ? `${yAxis} par ${xAxis}` : yAxis)
      : type
  }

  const newWidget: Widget = {
    id: Date.now() + Math.floor(Math.random() * 1_000_000),
    dashboard_id: dashboardStore.currentDashboard.id,
    type,
    title,
    x: 0, y: 0,
    width:  type === 'kpi' ? 3 : 6,
    height: type === 'kpi' ? 2 : 4,
    data_config: JSON.stringify(aiCfg),
  }

  // Calcule les données immédiatement pour que le graphique s'affiche sans attendre la sauvegarde
  try {
    const widgetData = type === 'boxplot' ? getBoxPlotData(newWidget) : getWidgetData(newWidget)
    newWidget.data = JSON.stringify(widgetData)
  } catch { /* ignore — sera calculé à la sauvegarde */ }

  dashboardStore.currentWidgets.push(newWidget)
  showToast('success', `✨ Widget IA « ${title} » ajouté`)
}

// ── Auto-Generate: full dashboard from dataset ────────────────────────────

/**
 * Simple grid packer: places widgets row by row on a 12-column grid.
 * Respects the AI's suggested width/height; recomputes x/y to avoid overlaps.
 */
function packWidgets(raw: Array<{ width: number; height: number; [k: string]: any }>) {
  const COLS = 12
  // occupancy[row][col] = true when occupied
  const occ: boolean[][] = []

  function canPlace(x: number, y: number, w: number, h: number): boolean {
    for (let r = y; r < y + h; r++) {
      for (let c = x; c < x + w; c++) {
        if (occ[r]?.[c]) return false
      }
    }
    return true
  }

  function mark(x: number, y: number, w: number, h: number) {
    for (let r = y; r < y + h; r++) {
      if (!occ[r]) occ[r] = []
      for (let c = x; c < x + w; c++) occ[r]![c] = true
    }
  }

  function findSlot(w: number, h: number): { x: number; y: number } {
    for (let y = 0; ; y++) {
      for (let x = 0; x <= COLS - w; x++) {
        if (canPlace(x, y, w, h)) return { x, y }
      }
    }
  }

  return raw.map(item => {
    const w = Math.min(Math.max(item.width  || 6, 1), COLS)
    const h = Math.max(item.height || 4, 1)
    const { x, y } = findSlot(w, h)
    mark(x, y, w, h)
    return { ...item, x, y, width: w, height: h }
  })
}

function cancelAutoGenerate() {
  autoGenAborted = true
  showAutoGenerate.value  = false
  isAutoGenerating.value  = false
}

async function autoGenerate() {
  const dataset = datasetStore.currentDataset
  if (!dataset || !dashboardStore.currentDashboard) {
    showToast('error', 'Importez d\'abord un dataset pour générer le dashboard.')
    return
  }

  // Reset state
  autoGenAborted       = false
  autoGenStep.value    = 0
  autoGenDone.value    = false
  autoGenError.value   = false
  autoGenErrMsg.value  = ''
  autoGenWidgetCount.value = 0
  showAutoGenerate.value   = true
  isAutoGenerating.value   = true

  try {
    // ── Step 0: Analyse du schéma ────────────────────────────────────────
    autoGenStep.value = 0
    await new Promise(r => setTimeout(r, 400))
    if (autoGenAborted) return

    const columns = dataset.columns.map(c => ({
      name: c.name,
      type: c.type === 'number' ? 'number' : c.type === 'date' ? 'date' : 'string',
    }))
    const preview = dataset.data.slice(0, 10) as Record<string, unknown>[]

    // ── Step 1: Appel IA ─────────────────────────────────────────────────
    autoGenStep.value = 1
    await new Promise(r => setTimeout(r, 300))
    if (autoGenAborted) return

    const plan = await aiService.generateDashboard(columns, preview)
    if (autoGenAborted) return

    const rawWidgets = plan?.widgets
    if (!Array.isArray(rawWidgets) || rawWidgets.length === 0)
      throw new Error('L\'IA n\'a retourné aucun widget. Réessayez.')

    // ── Step 2: Positionnement ───────────────────────────────────────────
    autoGenStep.value = 2
    await new Promise(r => setTimeout(r, 250))
    if (autoGenAborted) return

    // Normalise types & pack
    const TYPE_MAP: Record<string, string> = {
      barchart: 'bar', bar_chart: 'bar',
      linechart: 'line', line_chart: 'line',
      piechart: 'pie', pie_chart: 'pie',
      donut: 'doughnut', donut_chart: 'doughnut',
      scatterplot: 'scatter', scatter_plot: 'scatter',
      card: 'kpi', tableau: 'table',
    }

    const normalised = rawWidgets.map(w => ({
      ...w,
      type:        TYPE_MAP[w.type?.toLowerCase()] ?? w.type ?? 'bar',
      xAxis:       resolveColumn(w.xAxis),
      yAxis:       resolveColumn(w.yAxis),
      aggregation: (['sum','avg','count','min','max','none'].includes((w.aggregation ?? '').toLowerCase())
                    ? w.aggregation.toLowerCase() : 'sum'),
      width:  w.type === 'kpi' || w.type === 'card' ? 3
              : w.type === 'table' || w.type === 'tableau' ? 12
              : (w.width && w.width >= 2 && w.width <= 12 ? w.width : 6),
      height: w.type === 'kpi' || w.type === 'card' ? 2
              : w.type === 'table' || w.type === 'tableau' ? 5
              : (w.height && w.height >= 2 && w.height <= 8 ? w.height : 4),
    }))

    // Sort: KPIs first, then charts, table last
    normalised.sort((a, b) => {
      const rank = (t: string) => t === 'kpi' ? 0 : t === 'table' ? 2 : 1
      return rank(a.type) - rank(b.type)
    })

    const packed = packWidgets(normalised)

    // ── Step 3: Construction du dashboard ───────────────────────────────
    autoGenStep.value = 3
    await new Promise(r => setTimeout(r, 200))
    if (autoGenAborted) return

    // Clear existing widgets
    dashboardStore.currentWidgets = []

    const dashId = dashboardStore.currentDashboard.id
    const now    = Date.now()

    for (const [i, w] of packed.entries()) {
      const cfg = {
        ...defaultConfig(w.type),
        xAxis:       w.xAxis       || undefined,
        yAxis:       w.yAxis       || undefined,
        aggregation: w.aggregation || 'sum',
        color:       w.color       || '#1B6B3A',
        isDefaultColor: !w.color,
        ...(w.type === 'kpi' ? {
          kpiColumn:      w.yAxis || w.xAxis || '',
          kpiAggregation: w.aggregation || 'sum',
        } : {}),
      }

      dashboardStore.currentWidgets.push({
        id:           now + i + 1,
        dashboard_id: dashId,
        type:         w.type,
        title:        w.title || w.type,
        x:            w.x,
        y:            w.y,
        width:        w.width,
        height:       w.height,
        data_config:  JSON.stringify(cfg),
      })
    }

    autoGenWidgetCount.value = packed.length
    autoGenDone.value        = true
    isAutoGenerating.value   = false

  } catch (err: any) {
    if (autoGenAborted) return
    autoGenError.value  = true
    autoGenErrMsg.value = err?.message ?? 'Une erreur est survenue.'
    isAutoGenerating.value = false
  }
}

function quickAddWidget(type: WidgetType) {
  if (!dashboardStore.currentDashboard) return
  const newWidget: Widget = {
    id: Date.now() + Math.floor(Math.random() * 1_000_000),
    dashboard_id: dashboardStore.currentDashboard.id,
    type: type.id,
    title: type.label,
    x: 0, y: 0,
    width:  type.id === 'kpi' ? 3 : 6,
    height: type.id === 'kpi' ? 2 : 4,
    data_config: JSON.stringify(defaultConfig(type.id)),
  }
  dashboardStore.currentWidgets.push(newWidget)
  showToast('success', `Widget « ${type.label} » ajouté`)
}

// ── Dashboard Settings functions ──────────────────────────────────────────
const shareUrl = computed(() =>
  dashSettings.shareToken
    ? `${window.location.origin}/share/${dashSettings.shareToken}`
    : '',
)

async function applyDashSettings() {
  if (!dashboardStore.currentDashboard) return
  const newName = dashSettings.name?.trim() || dashboardStore.currentDashboard.name

  // Persist name to backend — send the full payload to avoid wiping columns/insights
  try {
    const { default: api } = await import('@/services/api')
    const d = dashboardStore.currentDashboard
    await api.put(`/Dashboard/${d.id}`, {
      name:            newName,
      datasetId:       d.datasetId,
      // isPublic / shareToken are managed exclusively by onToggleShare → PUT /Dashboard/share/{id}
      columns:         d.columns         ?? [],
      insights:        d.insights        ?? [],
      recommendations: d.recommendations ?? [],
    })
  } catch { /* ignore — still update locally */ }

  // Update local state
  dashboardStore.currentDashboard.name = newName
  const idx = dashboardStore.dashboards.findIndex(d => d.id === dashboardStore.currentDashboard!.id)
  if (idx !== -1) dashboardStore.dashboards[idx]!.name = newName

  showToast('success', 'Paramètres du dashboard sauvegardés')
  showDashSettings.value = false
}

async function onToggleShare() {
  if (!dashboardStore.currentDashboard) return
  try {
    const res = await dashboardStore.toggleShare(dashboardStore.currentDashboard.id, dashSettings.isPublic)
    dashSettings.shareToken = res.shareToken
    dashboardStore.currentDashboard.isPublic    = res.isPublic
    dashboardStore.currentDashboard.shareToken  = res.shareToken
    showToast('success', dashSettings.isPublic ? 'Dashboard rendu public' : 'Partage désactivé')
  } catch {
    showToast('error', 'Erreur lors du changement de visibilité')
    dashSettings.isPublic = !dashSettings.isPublic  // rollback toggle
  }
}

function copyShareUrl() {
  if (!shareUrl.value) return
  navigator.clipboard.writeText(shareUrl.value)
    .then(() => showToast('success', 'Lien copié dans le presse-papiers'))
    .catch(() => showToast('error', 'Impossible de copier'))
}

// ── Data save ─────────────────────────────────────────────────────────────
async function handleSave() {
  if (!dashboardStore.currentDashboard) return
  isSaving.value = true

  try {
    const dashId = dashboardStore.currentDashboard.id
    // Temp IDs are generated with Date.now() → they are > 1e12
    const isTemp = (id: number) => id > 1e12
    const currentIds = new Set(
      dashboardStore.currentWidgets.map((w) => w.id).filter((id) => !isTemp(id)),
    )

    // ── 1. Upsert each widget ─────────────────────────────────────────────
    for (const widget of dashboardStore.currentWidgets) {
      const cfg = JSON.parse(widget.data_config || '{}')
      const config = JSON.stringify({
        ...cfg,
        width: widget.width ?? 6,
        height: widget.height ?? 4,
        x: widget.x ?? 0,
        y: widget.y ?? 0,
      })
      // Build the data payload from the live computed data.
      // Tables never persist their rows — they are always re-computed live from the
      // current dataset so the saved dashboard stays lightweight and schema-agnostic.
      // Box plots use their own five-number-summary format; all other types use {label,value}[].
      const widgetData = widget.type === 'boxplot' ? getBoxPlotData(widget) : getWidgetData(widget)
      const data = JSON.stringify(widgetData)

      if (isTemp(widget.id)) {
        // New widget — POST then overwrite temp ID with real backend ID
        const created = await widgetService.create({
          dashboardId: dashId,
          type: widget.type,
          title: widget.title,
          data,
          config,
        })
        widget.id = created.id
        widget.dashboardId = dashId
        widget.dashboard_id = dashId
        currentIds.add(created.id)
      } else {
        // Existing widget — PUT
        await widgetService.update(widget.id, {
          type: widget.type,
          title: widget.title,
          data,
          config,
        })
      }
    }

    // ── 2. Delete widgets that were removed since last load/save ──────────
    for (const origId of originalWidgetIds.value) {
      if (!currentIds.has(origId)) {
        try { await widgetService.remove(origId) } catch { /* ignore */ }
      }
    }

    // ── 3. Sync the tracked original IDs ─────────────────────────────────
    originalWidgetIds.value = new Set(
      dashboardStore.currentWidgets.map((w) => w.id).filter((id) => !isTemp(id)),
    )

    const now = new Date()
    lastSaved.value = now.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' })
    showToast('success', `Dashboard sauvegardé — ${dashboardStore.currentWidgets.length} widget(s)`)

    // ── 4. Snapshot automatique de version ──────────────────────────────
    try {
      await versionService.saveVersion(dashboardId)
      // Rafraîchir la liste si le panel historique est ouvert
      if (showVersionModal.value) {
        versionModalRef.value?.loadVersions()
      }
    } catch { /* ne pas bloquer la sauvegarde principale si le versionnage échoue */ }
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : 'Erreur lors de la sauvegarde'
    showToast('error', msg)
  } finally {
    isSaving.value = false
  }
}

// ── Reference to the canvas grid element for PDF capture ─────────────────
const canvasGridRef = ref<HTMLElement | null>(null)

async function handleExport(type: 'pdf' | 'image') {
  if (!dashboardStore.currentDashboard) return
  isExporting.value = true

  try {
    if (type === 'pdf') {
      await exportToPdf()
    } else {
      showToast('error', "Export image non disponible — utilisez l'export PDF")
    }
  } catch (err) {
    console.error('Export PDF error:', err)
    showToast('error', "Erreur lors de l'export PDF")
  } finally {
    isExporting.value = false
  }
}

async function exportToPdf() {
  const dash = dashboardStore.currentDashboard
  if (!dash) return

  // Lazy-load html2canvas and jsPDF to keep initial bundle size small
  const [html2canvasModule, jsPDFModule] = await Promise.all([
    import('html2canvas'),
    import('jspdf'),
  ])
  const html2canvas = html2canvasModule.default
  const { jsPDF }   = jsPDFModule

  // Target the canvas grid
  const canvasEl = canvasGridRef.value
  if (!canvasEl) {
    showToast('error', 'Aucun contenu à exporter')
    return
  }

  showToast('success', 'Génération du PDF en cours…')

  // ── Préparer le canvas pour la capture complète ──────────────────────────
  //    • Fixer une largeur d'export normalisée (1 440 px) quel que soit l'écran
  //      → les widgets auront toujours les mêmes proportions dans le PDF.
  //    • Débloquer overflow pour capturer TOUTE la hauteur (pas seulement le viewport).
  const EXPORT_W = 1440   // px logiques — donne des colonnes de 120 px (12 colonnes)

  const parentEl = canvasEl.parentElement as HTMLElement | null

  const savedGridOverflow   = canvasEl.style.overflow
  const savedGridHeight     = canvasEl.style.height
  const savedGridWidth      = canvasEl.style.width
  const savedGridMinWidth   = canvasEl.style.minWidth
  const savedParentOverflow = parentEl ? parentEl.style.overflow : ''
  const savedParentHeight   = parentEl ? parentEl.style.height   : ''
  const savedParentWidth    = parentEl ? parentEl.style.width    : ''

  // Largeur fixe pour un rendu cohérent
  canvasEl.style.width    = `${EXPORT_W}px`
  canvasEl.style.minWidth = `${EXPORT_W}px`

  canvasEl.style.overflow = 'visible'
  canvasEl.style.height   = 'auto'
  if (parentEl) {
    parentEl.style.overflow = 'visible'
    parentEl.style.height   = 'auto'
    parentEl.style.width    = `${EXPORT_W}px`
  }

  // Attendre deux frames pour que le browser re-flow la grille à la nouvelle largeur
  await new Promise<void>(r => requestAnimationFrame(() => requestAnimationFrame(() => r())))

  // ── 1. Capture the dashboard canvas ─────────────────────────────────────
  /**
   * html2canvas CSS color Level-4 compatibility shim.
   *
   * html2canvas v1.x chokes on color() / oklch() / oklab() / color-mix()
   * at two distinct points:
   *   1. Its CSS text parser → "unsupported color function" error
   *   2. Its gradient renderer → addColorStop() with non-finite offset
   *      (happens when a gradient stop colour can't be parsed, producing NaN)
   *
   * Four passes in the onclone callback cover every source:
   *   A. Replace raw <style> text (what html2canvas actually re-reads)
   *   B. Inject CSS-var overrides from live :root so var() resolves to rgb()
   *   C. Inline [style] attribute strings
   *   D. Computed paint/gradient properties applied as inline overrides
   *      → this is the critical pass for the addColorStop crash
   */

  const UNSUPPORTED_COLOR_RE =
    /\b(color\s*\(|oklch\s*\(|oklab\s*\(|color-mix\s*\(|lch\s*\(|lab\s*\()/i

  // ── colour converters ─────────────────────────────────────────────────────
  function sanitizeColorVal(val: string): string {
    // color(space r g b [/ a])  →  rgb() / rgba()
    let v = val.replace(
      /color\(\s*[\w-]+\s+([\d.e+-]+%?)\s+([\d.e+-]+%?)\s+([\d.e+-]+%?)(?:\s*\/\s*([\d.e+-]+%?))?\s*\)/gi,
      (_m, rs: string, gs: string, bs: string, as?: string) => {
        const p = (x: string) => x.endsWith('%') ? parseFloat(x) / 100 : parseFloat(x)
        const r = Math.min(255, Math.max(0, Math.round(p(rs) * 255)))
        const g = Math.min(255, Math.max(0, Math.round(p(gs) * 255)))
        const b = Math.min(255, Math.max(0, Math.round(p(bs) * 255)))
        const a = as !== undefined ? Math.min(1, Math.max(0, p(as))) : 1
        if (!isFinite(r) || !isFinite(g) || !isFinite(b)) return 'rgb(0,0,0)'
        return a < 1 ? `rgba(${r},${g},${b},${a.toFixed(3)})` : `rgb(${r},${g},${b})`
      }
    )
    // oklch(l c h [/ a]) → grey approximated from lightness
    v = v.replace(
      /oklch\(\s*([\d.e+-]+%?)\s+[\d.e+-]+%?\s+[\d.e+-]+(?:\s*\/\s*([\d.e+-]+%?))?\s*\)/gi,
      (_m, ls: string, as?: string) => {
        const p = (x: string) => x.endsWith('%') ? parseFloat(x) / 100 : parseFloat(x)
        const lv = p(ls)
        const n  = Math.min(255, Math.max(0, Math.round((isFinite(lv) ? lv : 0.5) * 255)))
        const a  = as !== undefined ? Math.min(1, Math.max(0, p(as))) : 1
        return a < 1 ? `rgba(${n},${n},${n},${a.toFixed(3)})` : `rgb(${n},${n},${n})`
      }
    )
    // oklab / lab / lch → neutral grey (full match to avoid leaving stray parens)
    v = v.replace(/ok?lab\s*\([^)]*\)/gi,  'rgb(128,128,128)')
    v = v.replace(/\blch\s*\([^)]*\)/gi,   'rgb(128,128,128)')
    // color-mix() → first colour argument
    v = v.replace(/color-mix\([^,]+,\s*([^,)]+)[^)]*\)/gi, (_m, c: string) => c.trim())
    return v
  }

  // ── properties that carry colour values (solid + gradient) ───────────────
  const PAINT_PROPS = [
    'color', 'background-color', 'background-image',
    'border-color', 'border-top-color', 'border-right-color',
    'border-bottom-color', 'border-left-color',
    'outline-color', 'text-decoration-color', 'box-shadow',
    'fill', 'stroke',
  ] as const

  function patchStyles(clonedDoc: Document) {
    // ── A. Replace raw text of every <style> tag ──────────────────────────────
    clonedDoc.querySelectorAll('style').forEach(tag => {
      const txt = tag.textContent
      if (txt && UNSUPPORTED_COLOR_RE.test(txt)) {
        tag.textContent = sanitizeColorVal(txt)
      }
    })

    // ── B. Override CSS custom properties from the live :root ─────────────────
    //    Reads already-resolved values (rgb / hex) from the real page and
    //    re-injects them in the clone so var() calls resolve to safe values.
    const liveCS = window.getComputedStyle(document.documentElement)
    const lines: string[] = []
    for (let i = 0; i < liveCS.length; i++) {
      const prop = liveCS[i]!
      if (!prop.startsWith('--')) continue
      const val = liveCS.getPropertyValue(prop).trim()
      if (UNSUPPORTED_COLOR_RE.test(val)) {
        lines.push(`${prop}:${sanitizeColorVal(val)}`)
      }
    }
    if (lines.length) {
      const overrideTag = clonedDoc.createElement('style')
      overrideTag.textContent = `:root{${lines.join(';')}}`
      clonedDoc.head.appendChild(overrideTag) // last = highest cascade priority
    }

    // ── C. Inline [style] attribute strings ───────────────────────────────────
    clonedDoc.querySelectorAll<HTMLElement>('[style]').forEach(el => {
      const raw = el.getAttribute('style') ?? ''
      if (UNSUPPORTED_COLOR_RE.test(raw)) {
        el.setAttribute('style', sanitizeColorVal(raw))
      }
    })

    // ── D. Computed paint / gradient properties ────────────────────────────────
    //    This is the pass that fixes the addColorStop(non-finite) crash.
    //    Gradients like linear-gradient(…, oklch(…), …) survive passes A-C
    //    because their computed value is assembled by the browser at render time.
    //    We walk every element, read the computed value, and force-apply a
    //    sanitised inline override so html2canvas never sees a Level-4 colour.
    const view = clonedDoc.defaultView
    if (!view) return

    clonedDoc.querySelectorAll<HTMLElement>('*').forEach(el => {
      try {
        const cs = view.getComputedStyle(el)
        const overrides: [string, string][] = []
        for (const prop of PAINT_PROPS) {
          const val = cs.getPropertyValue(prop)
          if (val && UNSUPPORTED_COLOR_RE.test(val)) {
            overrides.push([prop, sanitizeColorVal(val)])
          }
        }
        if (overrides.length) {
          // Build the new inline style by merging with any existing value
          for (const [p, v] of overrides) {
            el.style.setProperty(p, v, 'important')
          }
        }
      } catch { /* detached / SVG foreign element — skip */ }
    })
  }

  // ── Monkey-patch CanvasGradient.addColorStop ──────────────────────────────
  // html2canvas internally calls addColorStop with offsets derived from parsed
  // CSS colour strings.  When a Level-4 colour (oklch, color(), …) slips
  // through unresolved, the offset becomes NaN / Infinity and the native API
  // throws a TypeError.  We intercept those bad calls and silently discard them
  // rather than letting the whole export crash.
  const _origAddColorStop = CanvasGradient.prototype.addColorStop
  CanvasGradient.prototype.addColorStop = function (offset: number, color: string) {
    if (!isFinite(offset) || offset < 0 || offset > 1) return
    try {
      _origAddColorStop.call(this, Math.max(0, Math.min(1, offset)), color)
    } catch { /* swallow any remaining native errors */ }
  }

  // Mesurer après reflow — fullW doit correspondre à EXPORT_W
  const fullW = EXPORT_W
  const fullH = canvasEl.scrollHeight

  // The `!` definite-assignment assertion tells TS that snapshot will be set
  // before first use (html2canvas throws → the lines below are never reached).
  let snapshot!: HTMLCanvasElement
  try {
    snapshot = await html2canvas(canvasEl, {
      scale:           2,              // 2× qualité retina
      useCORS:         true,
      allowTaint:      true,
      backgroundColor: currentThemeObj.value.bg || '#F5F6F5',
      logging:         false,
      // Dimensions fixes = EXPORT_W × hauteur réelle après reflow
      width:        fullW,
      height:       fullH,
      windowWidth:  fullW + 400,   // simuler une fenêtre plus large (sidebar incluse)
      windowHeight: fullH,
      // Strip unsupported CSS color functions from the clone before rendering
      onclone: (_doc: Document, _el: HTMLElement) => { patchStyles(_doc) },
      // Ignore tooltip / modal overlays
      ignoreElements: (el: Element) =>
        el.classList.contains('hm-tooltip') ||
        el.classList.contains('cfg-panel')  ||
        el.classList.contains('pp-panel'),
    })
  } finally {
    // Always restore the original method, even if html2canvas throws
    CanvasGradient.prototype.addColorStop = _origAddColorStop

    // Restaurer tous les styles modifiés
    canvasEl.style.overflow  = savedGridOverflow
    canvasEl.style.height    = savedGridHeight
    canvasEl.style.width     = savedGridWidth
    canvasEl.style.minWidth  = savedGridMinWidth
    if (parentEl) {
      parentEl.style.overflow = savedParentOverflow
      parentEl.style.height   = savedParentHeight
      parentEl.style.width    = savedParentWidth
    }
  }

  const imgData   = snapshot.toDataURL('image/png')
  const imgWidth  = snapshot.width  / 2   // back to CSS pixels
  const imgHeight = snapshot.height / 2

  // ── 2. Page setup ─────────────────────────────────────────────────────────
  const A4_W = 210, A4_H = 297                    // mm  (portrait)
  // Always use portrait — wide dashboards get landscape content pages
  const landscape  = imgWidth > imgHeight * 1.4   // only truly wide layouts
  const pageW      = landscape ? A4_H : A4_W
  const pageH      = landscape ? A4_W : A4_H
  const pdf        = new jsPDF({ orientation: landscape ? 'l' : 'p', unit: 'mm', format: 'a4' })

  const now      = new Date()
  const dateStr  = now.toLocaleDateString('fr-FR', { day: '2-digit', month: 'long', year: 'numeric' })
  const timeStr  = now.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' })
  const widgets  = dashboardStore.currentWidgets
  const px2mm    = 25.4 / 96   // 1 CSS px @ 96 dpi → mm

  // ── Couleurs de la charte ──────────────────────────────────────────────────
  const C = {
    bg:        [11,  17,  28 ] as [number,number,number],
    surface:   [18,  26,  40 ] as [number,number,number],
    surface2:  [24,  34,  52 ] as [number,number,number],
    primary:   [16, 185, 129 ] as [number,number,number],
    primary2:  [5,  150, 105 ] as [number,number,number],
    accent:    [99, 102, 241 ] as [number,number,number],
    text:      [226,232,240 ] as [number,number,number],
    textMuted: [100,116,139 ] as [number,number,number],
    textDim:   [51,  65,  85 ] as [number,number,number],
    border:    [30,  41,  59 ] as [number,number,number],
  }

  // ── Helpers ───────────────────────────────────────────────────────────────
  const fill  = (c: [number,number,number]) => pdf.setFillColor(...c)
  const color = (c: [number,number,number]) => pdf.setTextColor(...c)
  const stroke= (c: [number,number,number]) => pdf.setDrawColor(...c)

  function pageBackground() {
    fill(C.bg); pdf.rect(0, 0, pageW, pageH, 'F')
  }

  /** Thin top accent bar + bottom footer line */
  function pageChrome(title: string, pageLabel: string) {
    // Top accent gradient simulation (two rects)
    fill(C.primary);  pdf.rect(0, 0, pageW * 0.6, 1.5, 'F')
    fill(C.accent);   pdf.rect(pageW * 0.6, 0, pageW * 0.4, 1.5, 'F')
    // Bottom rule
    stroke(C.border); pdf.setLineWidth(0.2)
    pdf.line(12, pageH - 9, pageW - 12, pageH - 9)
    // Footer left: title
    pdf.setFont('helvetica', 'normal'); pdf.setFontSize(7); color(C.textDim)
    pdf.text(title, 12, pageH - 5)
    // Footer right: page
    pdf.setFont('helvetica', 'bold'); color(C.textMuted)
    pdf.text(pageLabel, pageW - 12, pageH - 5, { align: 'right' })
    // Footer center: date
    pdf.setFont('helvetica', 'normal'); color(C.textDim)
    pdf.text(dateStr, pageW / 2, pageH - 5, { align: 'center' })
  }

  // ══════════════════════════════════════════════════════════════════════════
  // ── 3. PAGE DE COUVERTURE ─────────────────────────────────────────────────
  // ══════════════════════════════════════════════════════════════════════════
  pageBackground()

  // ── Bande décorative gauche ──────────────────────────────────────────────
  fill(C.primary2); pdf.rect(0, 0, 6, pageH, 'F')
  fill(C.primary);  pdf.rect(0, 0, 3, pageH, 'F')

  // ── Bloc header (haut) ───────────────────────────────────────────────────
  fill(C.surface); pdf.rect(0, 0, pageW, 38, 'F')
  // Logo "DashGen"
  pdf.setFont('helvetica', 'bold'); pdf.setFontSize(18); color(C.primary)
  pdf.text('DashGen', 18, 16)
  pdf.setFont('helvetica', 'normal'); pdf.setFontSize(8); color(C.textMuted)
  pdf.text('Générateur de tableaux de bord interactifs', 18, 24)
  // Ligne séparatrice verte
  fill(C.primary); pdf.rect(18, 30, 40, 0.8, 'F')
  // Date/heure en haut à droite
  pdf.setFont('helvetica', 'normal'); pdf.setFontSize(8); color(C.textMuted)
  pdf.text(`${dateStr}  ·  ${timeStr}`, pageW - 14, 16, { align: 'right' })

  // ── Zone titre (centre) ───────────────────────────────────────────────────
  const titleY = 90
  // Étiquette "TABLEAU DE BORD"
  fill(C.surface2); pdf.roundedRect(18, titleY - 12, 52, 7, 1, 1, 'F')
  pdf.setFont('helvetica', 'bold'); pdf.setFontSize(7); color(C.primary)
  pdf.text('TABLEAU DE BORD', 21, titleY - 6.5)

  // Titre principal
  pdf.setFont('helvetica', 'bold'); pdf.setFontSize(30); color(C.text)
  const titleLines = pdf.splitTextToSize(dash.name || 'Sans titre', pageW - 36)
  pdf.text(titleLines, 18, titleY + 8)

  // Ligne déco sous le titre
  const titleBlockH = titleLines.length * 12
  fill(C.primary); pdf.rect(18, titleY + titleBlockH + 4, 28, 1, 'F')
  fill(C.accent);  pdf.rect(48, titleY + titleBlockH + 4, 12, 1, 'F')

  // ── Grille de méta-données ────────────────────────────────────────────────
  const metaY0 = titleY + titleBlockH + 18
  const metaItems: { label: string; value: string; color: [number,number,number] }[] = [
    { label: 'Widgets',    value: String(widgets.length),  color: C.primary },
    { label: 'Visibilité', value: dash.isPublic ? 'Public' : 'Privé', color: dash.isPublic ? C.primary : C.accent },
    { label: 'Exporté le', value: dateStr,                 color: C.textMuted },
    { label: 'Heure',      value: timeStr,                 color: C.textMuted },
  ]
  const cardW = (pageW - 36 - 9) / 2   // 2 cartes par ligne, gap 9
  metaItems.forEach((item, i) => {
    const col = i % 2
    const row = Math.floor(i / 2)
    const cx  = 18 + col * (cardW + 9)
    const cy  = metaY0 + row * 22
    fill(C.surface2); pdf.roundedRect(cx, cy, cardW, 16, 1.5, 1.5, 'F')
    stroke(C.border); pdf.setLineWidth(0.2)
    pdf.roundedRect(cx, cy, cardW, 16, 1.5, 1.5, 'S')
    // Valeur
    pdf.setFont('helvetica', 'bold'); pdf.setFontSize(11); pdf.setTextColor(...item.color)
    pdf.text(item.value, cx + cardW / 2, cy + 7.5, { align: 'center' })
    // Label
    pdf.setFont('helvetica', 'normal'); pdf.setFontSize(7); color(C.textMuted)
    pdf.text(item.label.toUpperCase(), cx + cardW / 2, cy + 13, { align: 'center' })
  })

  // Dataset info
  if (datasetStore.currentDataset) {
    const ds = datasetStore.currentDataset
    const dsY = metaY0 + Math.ceil(metaItems.length / 2) * 22 + 6
    fill(C.surface2); pdf.roundedRect(18, dsY, pageW - 36, 14, 1.5, 1.5, 'F')
    pdf.setFont('helvetica', 'bold'); pdf.setFontSize(8); color(C.primary)
    pdf.text('Source de données', 24, dsY + 5.5)
    pdf.setFont('helvetica', 'normal'); color(C.text)
    pdf.text(`${ds.fileName}`, 24, dsY + 11)
    pdf.setFont('helvetica', 'normal'); color(C.textMuted)
    pdf.text(`${ds.data.length} lignes  ·  ${ds.columns.length} colonnes`, pageW - 22, dsY + 11, { align: 'right' })
  }

  // ── Pied de couverture ────────────────────────────────────────────────────
  fill(C.surface); pdf.rect(0, pageH - 20, pageW, 20, 'F')
  fill(C.primary); pdf.rect(0, pageH - 20, pageW, 0.6, 'F')
  pdf.setFont('helvetica', 'bold'); pdf.setFontSize(8); color(C.primary)
  pdf.text('DashGen', 18, pageH - 11)
  pdf.setFont('helvetica', 'normal'); color(C.textMuted)
  pdf.text('Rapport généré automatiquement', 18, pageH - 6)
  color(C.textDim)
  pdf.text('dashgen.app', pageW - 14, pageH - 8, { align: 'right' })

  // ══════════════════════════════════════════════════════════════════════════
  // ── 4. PAGE(S) DASHBOARD  (découpage multi-pages si trop haut) ────────────
  // ══════════════════════════════════════════════════════════════════════════
  const marginTop  = 16   // mm sous l'accent bar
  const marginSide = 10   // mm gauche/droite
  const marginBot  = 14   // mm au-dessus du footer
  const availW     = pageW - marginSide * 2
  const availH     = pageH - marginTop - marginBot

  // Toujours étirer l'image pour remplir toute la largeur disponible.
  // • Dashboard plus large que la page  → scaleX < 1  (réduction)
  // • Dashboard plus étroit que la page → scaleX > 1  (agrandissement)
  // Dans les deux cas les widgets occupent toute la largeur et restent
  // proportionnels — pas de bande vide sur les côtés, pas de réduction inutile.
  const natW   = imgWidth * px2mm      // largeur naturelle en mm (@ 96 dpi)
  const scaleX = availW / natW         // facteur uniforme largeur → pleine page
  const drawW  = availW                // largeur de dessin = toute la zone dispo

  // Nombre de pixels CSS source qui tiennent dans availH mm une fois mis à l'échelle
  const sliceH_px   = availH / (px2mm * scaleX)          // CSS px par tranche
  const totalSlices = Math.ceil(imgHeight / sliceH_px)

  // On crée un canvas intermédiaire pour découper des tranches
  const srcCanvas = snapshot   // le canvas html2canvas
  const sliceCanvas = document.createElement('canvas')
  sliceCanvas.width  = srcCanvas.width   // pleine largeur @2×
  const sliceCanvas2x = Math.round(sliceH_px * 2)  // @2× (html2canvas scale=2)

  for (let s = 0; s < totalSlices; s++) {
    pdf.addPage()
    pageBackground()
    pageChrome(
      `${dash.name}  ·  Vue dashboard`,
      `${s + 2} / ${totalSlices + 1}`   // page 1 = couverture
    )

    // Mini-titre de section (page 2 uniquement)
    if (s === 0) {
      pdf.setFont('helvetica', 'bold'); pdf.setFontSize(9); color(C.primary)
      pdf.text('Vue du tableau de bord', marginSide, marginTop - 3)
    } else {
      pdf.setFont('helvetica', 'normal'); pdf.setFontSize(7); color(C.textMuted)
      pdf.text(`Suite  (partie ${s + 1} / ${totalSlices})`, marginSide, marginTop - 3)
    }

    // Découpe la tranche verticale
    const srcY = Math.round(s * sliceH_px * 2)  // @2×
    const srcH = Math.min(sliceCanvas2x, srcCanvas.height - srcY)
    if (srcH <= 0) break
    sliceCanvas.height = srcH
    const ctx = sliceCanvas.getContext('2d')!
    ctx.drawImage(srcCanvas, 0, srcY, srcCanvas.width, srcH, 0, 0, srcCanvas.width, srcH)

    const sliceData = sliceCanvas.toDataURL('image/png')
    const drawH     = (srcH / 2) * px2mm * scaleX   // hauteur réelle en mm

    // drawW === availW → pas de centrage nécessaire, on colle au margin
    pdf.addImage(sliceData, 'PNG', marginSide, marginTop, drawW, drawH, undefined, 'FAST')
  }

  // ══════════════════════════════════════════════════════════════════════════
  // ── 5. PAGE RÉCAPITULATIF DES WIDGETS ────────────────────────────────────
  // ══════════════════════════════════════════════════════════════════════════
  if (widgets.length > 0) {
    pdf.addPage()
    pageBackground()
    const totalFinal = pdf.getNumberOfPages()
    pageChrome(`${dash.name}  ·  Récapitulatif`, `${totalFinal} / ${totalFinal}`)

    // Titre de section
    fill(C.surface); pdf.rect(0, 0, pageW, 22, 'F')
    fill(C.primary); pdf.rect(0, 0, pageW, 1.5, 'F')
    pdf.setFont('helvetica', 'bold'); pdf.setFontSize(14); color(C.text)
    pdf.text('Récapitulatif des widgets', 14, 14)

    // En-tête tableau
    const tY0 = 28
    const cols = { n: 10, type: 22, title: 70, data: 130 }
    fill(C.surface2); pdf.rect(10, tY0, pageW - 20, 8, 'F')
    pdf.setFont('helvetica', 'bold'); pdf.setFontSize(8); color(C.primary)
    pdf.text('#',       cols.n,    tY0 + 5.5)
    pdf.text('Type',    cols.type, tY0 + 5.5)
    pdf.text('Titre',   cols.title, tY0 + 5.5)
    if (pageW > 220) pdf.text('Données', cols.data, tY0 + 5.5)

    // Lignes
    const rowH = 7.5
    widgets.forEach((w, idx) => {
      const ry = tY0 + 8 + idx * rowH
      if (ry + rowH > pageH - 16) return   // ne pas déborder sur le footer
      // Alternance de fond
      if (idx % 2 === 0) { fill(C.surface); pdf.rect(10, ry, pageW - 20, rowH, 'F') }
      // Séparateur
      stroke(C.border); pdf.setLineWidth(0.15)
      pdf.line(10, ry + rowH, pageW - 10, ry + rowH)

      pdf.setFont('helvetica', 'normal'); pdf.setFontSize(8)
      // N°
      color(C.textMuted); pdf.text(String(idx + 1), cols.n, ry + 5)
      // Type (badge coloré)
      const typeColors: Record<string, [number,number,number]> = {
        bar: C.primary, line: C.accent, area: [6,182,212], pie: [245,158,11],
        doughnut: [245,158,11], scatter: [236,72,153], kpi: [16,185,129],
        table: [100,116,139], funnel: [139,92,246], gauge: [34,197,94],
      }
      const tc = typeColors[w.type] ?? C.textMuted
      pdf.setTextColor(...tc)
      pdf.text(w.type.toUpperCase(), cols.type, ry + 5)
      // Titre
      color(C.text)
      const tTitle = pdf.splitTextToSize(w.title || '—', 55)
      pdf.text(tTitle[0], cols.title, ry + 5)
      // Données (colonne optionnelle)
      if (pageW > 220) {
        color(C.textMuted)
        let cfg: Record<string, unknown> = {}
        try { cfg = JSON.parse(w.data_config || '{}') } catch { /* */ }
        const axes = [cfg.xAxis, cfg.yAxis].filter(Boolean).join(' / ') || '—'
        pdf.text(axes, cols.data, ry + 5)
      }
    })
  }

  // ── 6. Numérotation finale ────────────────────────────────────────────────
  const totalPages = pdf.getNumberOfPages()
  // Repasse sur toutes les pages pour mettre le bon total dans le footer
  // (le chrome est déjà dessiné avec le bon numéro, on corrige juste la couverture)
  pdf.setPage(1)
  pdf.setFont('helvetica', 'normal'); pdf.setFontSize(7); color(C.textDim)
  pdf.text(`1 / ${totalPages}`, pageW - 14, pageH - 8, { align: 'right' })

  // ── 7. Sauvegarde ─────────────────────────────────────────────────────────
  const safeName = (dash.name || 'dashboard').replace(/[^a-zA-Z0-9_\- ]/g, '').trim()
  pdf.save(`${safeName}_${now.toISOString().slice(0, 10)}.pdf`)
  showToast('success', 'PDF téléchargé')
}

function handleExportJson() {
  if (!dashboardStore.currentDashboard) return
  isExporting.value = true

  try {
    const dash = dashboardStore.currentDashboard

    // Build the export payload
    const payload = {
      version: '1.0',
      exportDate: new Date().toISOString(),
      dashboard: {
        id:    dash.id,
        name:  dash.name,
        theme: currentTheme.value,
      },
      widgets: dashboardStore.currentWidgets.map(w => {
        let cfg: Record<string, unknown> = {}
        try { cfg = JSON.parse(w.data_config || '{}') } catch { /* ignore */ }

        return {
          id:    w.id,
          type:  w.type,
          title: w.title,
          layout: {
            x:      w.x      ?? 0,
            y:      w.y      ?? 0,
            width:  w.width  ?? 6,
            height: w.height ?? 4,
          },
          config: cfg,
          data: (() => {
            try {
              const stored = typeof w.data === 'string' ? JSON.parse(w.data ?? '[]') : (w.data ?? [])
              return Array.isArray(stored) ? stored : getWidgetData(w)
            } catch {
              return getWidgetData(w)
            }
          })(),
        }
      }),
      dataset: datasetStore.currentDataset
        ? {
            fileName:  datasetStore.currentDataset.fileName,
            rowCount:  datasetStore.currentDataset.data.length,
            columns:   datasetStore.currentDataset.columns,
          }
        : null,
    }

    const json    = JSON.stringify(payload, null, 2)
    const blob    = new Blob([json], { type: 'application/json' })
    const url     = URL.createObjectURL(blob)
    const a       = document.createElement('a')
    const safeName = (dash.name ?? 'dashboard').replace(/[^a-z0-9_\-]/gi, '_')
    a.href        = url
    a.download    = `${safeName}_export.json`
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    URL.revokeObjectURL(url)

    showToast('success', `Export JSON téléchargé — ${dashboardStore.currentWidgets.length} widget(s)`)
  } catch {
    showToast('error', "Erreur lors de l'export JSON")
  } finally {
    isExporting.value = false
  }
}

function getWidgetStyle(widget: Widget) {
  const config = JSON.parse(widget.data_config || '{}')
  const style: any = {
    gridColumn: `span ${widget.width || 6}`,
    gridRow: `span ${widget.height || 4}`,
    position: 'relative' as const,
    display: 'flex',
    flexDirection: 'column' as const,
    background: config.backgroundColor || 'var(--card-bg, #FFFFFF)',
  }

  // Handle patterns/designs
  if (config.backgroundType && config.backgroundType !== 'none') {
    const p = config.backgroundType
    const theme = currentThemeObj.value
    // Compute rgb components from the hex primary color (avoid undefined root variable)
    const _r = parseInt(theme.primary.slice(1, 3), 16)
    const _g = parseInt(theme.primary.slice(3, 5), 16)
    const _b = parseInt(theme.primary.slice(5, 7), 16)
    const primaryRgb = `${_r}, ${_g}, ${_b}`

    if (p === 'glass') {
      style.backdropFilter = 'blur(10px)'
      style.background = `rgba(${theme.name === 'white' ? '255,255,255' : '10,31,26'}, 0.7)`
      style.border = `1px solid rgba(${primaryRgb}, 0.3)`
    } else if (p === 'gradient-mesh') {
      style.background = `radial-gradient(at 0% 0%, rgba(${primaryRgb}, 0.15) 0px, transparent 50%),
                          radial-gradient(at 100% 100%, rgba(${primaryRgb}, 0.1) 0px, transparent 50%)`
    } else if (p === 'soft-glow') {
      style.boxShadow = `0 0 30px rgba(${primaryRgb}, 0.15)`
      style.background = `linear-gradient(180deg, rgba(${primaryRgb}, 0.05) 0%, transparent 100%)`
    } else if (p === 'modern-lines') {
      const lineCol = theme.name === 'white' ? 'rgba(0,0,0,0.05)' : 'rgba(17,23,20,0.02)'
      style.backgroundImage = `repeating-linear-gradient(45deg, ${lineCol} 0px, ${lineCol} 1px, transparent 1px, transparent 10px)`
    }
  }

  return style
}

function getWidgetIcon(type: string) {
  return widgetTypes.find((t) => t.id === type)?.icon || 'pi pi-box'
}

/**
 * Returns a human-readable caption for a widget.
 * Priority: manual description → auto-generated from axes.
 */
/** Single-character symbol for an aggregation mode */
function aggSymbol(agg: string): string {
  const sym: Record<string, string> = {
    none: '·', sum: 'Σ', avg: '⌀', median: '~',
    count: '#', count_distinct: '⊞', min: '↓', max: '↑', std: 'σ',
  }
  return sym[agg] ?? 'Σ'
}

/** Human-readable hint for each aggregation mode */
function aggHint(agg: string): string {
  const hints: Record<string, string> = {
    none:           'Utilise la première valeur brute du groupe sans calcul',
    sum:            'Additionne toutes les valeurs du groupe',
    avg:            'Calcule la moyenne arithmétique du groupe',
    median:         'Valeur du milieu de la distribution (50ᵉ percentile)',
    count:          'Compte le nombre total de lignes du groupe',
    count_distinct: 'Compte les valeurs uniques de la colonne Y',
    min:            'Retourne la valeur la plus basse du groupe',
    max:            'Retourne la valeur la plus haute du groupe',
    std:            'Mesure la dispersion des valeurs (écart-type populationnel)',
  }
  return hints[agg] ?? ''
}

function getWidgetCaption(widget: Widget): string {
  const cfg = getWidgetCfg(widget)

  // 1 — manual description always wins
  if (cfg.description?.trim()) return cfg.description.trim()

  // text widgets need no auto caption (they ARE the text)
  if (widget.type === 'text') return ''

  const y   = (cfg.yAxis || '').trim()
  const x   = (cfg.xAxis || '').trim()
  const agg = cfg.aggregation || 'sum'
  const suf = (cfg.valueSuffix || cfg.kpiSuffix || '').trim()

  if (!y && !x) return ''

  // ── Box plot: "Distribution de <Y> par <X>" ──────────────────────────────
  if (widget.type === 'boxplot') {
    if (y && x) return `Distribution de ${y} · par ${x}`
    if (y)      return `Distribution de ${y}`
    return ''
  }

  // ── Scatter: "Corrélation <X> vs <Y>" ────────────────────────────────────
  if (widget.type === 'scatter') {
    if (x && y) return `Corrélation ${x} vs ${y}`
    if (y)      return y
    return ''
  }

  // ── Pie/Doughnut: "Répartition par <X>" ──────────────────────────────────
  if (widget.type === 'pie' || widget.type === 'doughnut') {
    const measure = y ? ` (${aggSymbol(agg)} ${y})` : ''
    if (x) return `Répartition par ${x}${measure}`
    return ''
  }

  // ── Funnel: "Entonnoir par <X>" ───────────────────────────────────────────
  if (widget.type === 'funnel') {
    if (x && y) return `Entonnoir — ${aggSymbol(agg)} ${y} · par ${x}`
    if (y)      return `Entonnoir — ${aggSymbol(agg)} ${y}`
    return ''
  }

  // Aggregation symbol + label
  // Value label: "Σ Ventes (€)" or "# Lignes" (count mode)
  const useCount = !y && x
  const valLabel = useCount
    ? `# Éléments`
    : y ? `${aggSymbol(agg)} ${y}${suf ? ` (${suf})` : ''}`
        : ''

  // Single-value types: just the value label
  if (widget.type === 'kpi' || widget.type === 'gauge') return valLabel

  // Grouped types: "Σ Ventes (€) · par Mois"
  if (valLabel && x) return `${valLabel} · par ${x}`
  if (valLabel)      return valLabel
  if (x)             return `Par ${x}`
  return ''
}

</script>

<style scoped>
.builder-layout {
  display: flex;
  height: 100vh;
  background: var(--bg-color, #F5F6F5);
  color: var(--text-color, #111714);
  font-family: 'DM Sans', sans-serif;
  transition: background 0.4s, color 0.3s;
  --radius-card: 16px;
  --shadow-card: 0 1px 3px rgba(0,0,0,0.04), 0 6px 20px rgba(0,0,0,0.06);
  --shadow-card-hover: 0 4px 12px rgba(0,0,0,0.06), 0 16px 40px rgba(0,0,0,0.1);
}

/* ── Sidebar (left) ─────────────────────────────────────────── */
.sidebar {
  flex-shrink: 0;
  background: #FFFFFF;
  border-right: 1px solid #E4E8E4;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  transition: background 0.4s;
  position: relative;
  min-width: 180px;
  max-width: 480px;
  box-shadow: 2px 0 12px rgba(17,23,20,0.05);
}

.sidebar-resize-handle {
  position: absolute;
  top: 0;
  right: -3px;
  width: 6px;
  height: 100%;
  cursor: col-resize;
  z-index: 20;
  background: transparent;
  transition: background 0.15s;
}
.sidebar-resize-handle:hover,
.sidebar-resize-handle:active {
  background: rgba(27, 107, 58, 0.25);
  border-radius: 3px;
}

.back-admin-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border-radius: 7px;
  border: 1px solid #E4E8E4;
  background: #F5F6F5;
  color: #4B5E52;
  cursor: pointer;
  flex-shrink: 0;
  transition: all 0.15s;
}
.back-admin-btn:hover {
  background: #EEF7F1;
  border-color: rgba(27,107,58,0.3);
  color: #1B6B3A;
}

.sidebar-header {
  padding: 18px 20px 16px;
  border-bottom: 1px solid #E4E8E4;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  flex-shrink: 0;
  background: linear-gradient(180deg, rgba(27,107,58,0.03) 0%, transparent 100%);
}

.logo {
  display: flex;
  align-items: center;
  gap: 10px;
}

.logo-text {
  font-family: 'Syne', sans-serif;
  font-size: 17px;
  font-weight: 800;
  letter-spacing: -0.6px;
  color: #111714;
}

/* Theme dots in sidebar header */
.header-theme-dots {
  display: flex;
  gap: 5px;
  flex-wrap: wrap;
  max-width: 80px;
}
.theme-dot {
  width: 12px;
  height: 12px;
  border-radius: 50%;
  cursor: pointer;
  border: 2px solid transparent;
  transition: all 0.15s;
  flex-shrink: 0;
}
.theme-dot:hover { transform: scale(1.2); }
.theme-dot.active { border-color: #111714; box-shadow: 0 0 0 1px rgba(0,0,0,0.3); }

/* Dataset status bar */
.sidebar-dataset-status {
  margin: 12px 12px;
  padding: 10px 12px;
  background: #F5F6F5;
  border: 1px solid #E4E8E4;
  border-radius: 12px;
  display: flex;
  align-items: center;
  gap: 10px;
  transition: all 0.2s;
  flex-shrink: 0;
}
.sidebar-dataset-status.loaded {
  border-color: rgba(27,107,58,0.25);
  background: #EEF7F1;
}
.ds-icon {
  font-size: 14px;
  color: #94A99A;
  flex-shrink: 0;
}
.loaded .ds-icon { color: #1B6B3A; }
.ds-info { flex: 1; min-width: 0; }
.ds-name {
  font-size: 11px;
  font-weight: 600;
  color: #4B5E52;
  display: block;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.loaded .ds-name { color: #1B6B3A; }
.ds-meta {
  font-size: 9px;
  color: #94A99A;
  display: block;
  margin-top: 1px;
}
.ds-import-btn {
  width: 22px; height: 22px;
  background: rgba(27,107,58,0.08);
  border: 1px solid rgba(27,107,58,0.2);
  border-radius: 6px;
  color: #1B6B3A;
  font-size: 10px;
  cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  flex-shrink: 0;
  transition: all 0.15s;
}
.ds-import-btn:hover { background: rgba(27,107,58,0.15); }
.ds-import-btn--sql {
  background: rgba(29,78,216,0.08);
  border-color: rgba(29,78,216,0.2);
  color: #1D4ED8;
}
.ds-import-btn--sql:hover { background: rgba(29,78,216,0.15); }
.ds-import-btn--rest {
  background: rgba(109,40,217,0.08);
  border-color: rgba(109,40,217,0.2);
  color: #6D28D9;
}
.ds-import-btn--rest:hover { background: rgba(109,40,217,0.15); }
.fields-import-btn--rest {
  background: rgba(124,58,237,0.1);
  border-color: rgba(124,58,237,0.3);
  color: #7c3aed;
}

.sidebar-nav {
  flex: 1;
  padding: 20px 0;
  overflow-y: auto;
}

.nav-section {
  margin-bottom: 32px;
}

.section-title {
  padding: 0 20px;
  font-size: 10px;
  text-transform: uppercase;
  letter-spacing: 1.2px;
  color: #94A99A;
  font-weight: 700;
  margin-bottom: 6px;
}

.dashboard-list {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.dashboard-item {
  padding: 8px 12px;
  margin: 0 8px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  gap: 10px;
  cursor: pointer;
  transition: all 0.18s;
  font-size: 13px;
  color: #4B5E52;
}

.dash-name {
  flex: 1;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.delete-dash-btn {
  background: none;
  border: none;
  color: rgba(185,28,28,0.35);
  cursor: pointer;
  padding: 4px;
  border-radius: 4px;
  transition: all 0.2s;
  opacity: 0;
}

.dashboard-item:hover .delete-dash-btn {
  opacity: 1;
}

.delete-dash-btn:hover {
  color: #f87171;
  background: rgba(248, 113, 113, 0.15);
}

.dashboard-item:hover {
  background: #EEF7F1;
  color: #1B6B3A;
}

.dashboard-item.active {
  background: #EEF7F1;
  color: #1B6B3A;
  font-weight: 600;
  box-shadow: inset 3px 0 0 #1B6B3A;
}

.add-dash-btn {
  margin: 6px 16px;
  padding: 9px;
  background: rgba(27,107,58,0.04);
  border: 1.5px dashed rgba(27,107,58,0.25);
  border-radius: 10px;
  color: #1B6B3A;
  font-size: 12px;
  font-weight: 500;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 7px;
  transition: all 0.18s;
}

.add-dash-btn:hover {
  background: #EEF7F1;
  border-color: rgba(27,107,58,0.5);
  transform: translateY(-1px);
}

/* dataset status bar is in sidebar header now — see .sidebar-dataset-status */
.data-actions { display: none; }
.import-csv-btn { display: none; }

.widget-palette {
  padding: 0 14px;
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 8px;
}

.palette-item {
  padding: 10px 8px 8px;
  background: #FAFAFA;
  border: 1px solid #E4E8E4;
  border-radius: 12px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  cursor: pointer;
  transition: all 0.18s;
  position: relative;
  overflow: hidden;
}

.palette-item:hover {
  background: #EEF7F1;
  border-color: rgba(27,107,58,0.35);
  transform: translateY(-2px);
  box-shadow: 0 4px 16px rgba(27,107,58,0.12);
}
.palette-item:active { transform: scale(0.97); }

.palette-preview {
  width: 48px;
  height: 28px;
  display: flex;
  align-items: center;
  justify-content: center;
}
.mini-chart { overflow: visible; }

.palette-label {
  font-size: 10px;
  font-weight: 600;
  color: #4B5E52;
  text-align: center;
  line-height: 1.2;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 100%;
}

.palette-add-hint {
  font-size: 8px;
  color: rgba(var(--primary-color-rgb, 27, 107, 58), 0.5);
  opacity: 0;
  transition: opacity 0.15s;
}
.palette-item:hover .palette-add-hint { opacity: 1; }

.sidebar-footer {
  padding: 14px 16px;
  border-top: 1px solid #E4E8E4;
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: #FAFAFA;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 12px;
}

.avatar {
  width: 34px;
  height: 34px;
  background: linear-gradient(135deg, #1B6B3A 0%, #15803D 100%);
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
  font-size: 13px;
  color: #fff;
  box-shadow: 0 2px 8px rgba(27,107,58,0.35);
  letter-spacing: 0.5px;
}

.details {
  display: flex;
  flex-direction: column;
}

.name {
  font-size: 13px;
  font-weight: 600;
  color: #111714;
}
.email {
  font-size: 11px;
  color: #94A99A;
}

.logout-btn {
  background: none;
  border: none;
  color: #94A99A;
  cursor: pointer;
  padding: 8px;
  transition: color 0.2s;
}

.logout-btn:hover {
  color: #f87171;
}

/* Main Content */
.canvas-container {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  background: var(--bg-color, #F2F4F2);
  overflow: hidden;
  transition: background 0.4s;
}

.canvas-header {
  padding: 10px 20px;
  background: rgba(255,255,255,0.92);
  backdrop-filter: blur(16px);
  -webkit-backdrop-filter: blur(16px);
  border-bottom: 1px solid rgba(0,0,0,0.07);
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  flex-shrink: 0;
  overflow: hidden;
  box-shadow: 0 1px 0 rgba(0,0,0,0.04), 0 4px 20px rgba(0,0,0,0.05);
  position: relative;
  z-index: 10;
}

.header-info { flex-shrink: 0; }

.header-info { display: flex; flex-direction: column; gap: 3px; flex-shrink: 0; }
.header-title-row { display: flex; align-items: center; gap: 8px; }
.header-info h1 {
  font-family: 'Syne', sans-serif;
  font-size: 15px;
  font-weight: 800;
  margin: 0;
  color: #0f172a;
  letter-spacing: -0.4px;
}
.status-badge {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: #15803d;
  background: rgba(21,128,61,0.08);
  border: 1px solid rgba(21,128,61,0.2);
  padding: 4px 11px;
  border-radius: 100px;
  box-shadow: inset 0 1px 0 rgba(255,255,255,0.5);
}
.status-dot {
  width: 5px; height: 5px; border-radius: 50%;
  background: #1B6B3A;
  animation: pulse-dot 2s infinite;
}
@keyframes pulse-dot {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.4; }
}
.header-meta {
  display: flex;
  align-items: center;
  gap: 14px;
}
.header-meta-item {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 11px;
  color: rgba(var(--text-color-rgb,240,253,249), 0.35);
}
.header-meta-item.meta-ok { color: #1B6B3A; opacity: 0.8; }

/* Filters Toolbar */
.filters-toolbar {
  margin: 0 24px 20px 24px;
  padding: 12px 20px;
  background: rgba(255,255,255,0.8);
  border: 1px solid rgba(0,0,0,0.06);
  border-radius: 14px;
  display: flex;
  align-items: center;
  gap: 16px;
  box-shadow: 0 1px 4px rgba(0,0,0,0.04);
  backdrop-filter: blur(8px);
}

.filters-label {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 13px;
  color: #4B5E52;
}

.filter-tags {
  display: flex;
  align-items: center;
  gap: 12px;
}

.filter-tag {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 12px;
  background: rgba(var(--primary-color-rgb, 27, 107, 58), 0.05);
  border: 1px solid rgba(var(--primary-color-rgb, 27, 107, 58), 0.3);
  border-radius: 30px;
  color: #1B6B3A;
  font-size: 12px;
  font-weight: 500;
  transition: all 0.2s;
}

.filter-tag:hover {
  background: rgba(var(--primary-color-rgb, 27, 107, 58), 0.1);
  border-color: #1B6B3A;
}

.remove-filter {
  background: none;
  border: none;
  color: inherit;
  cursor: pointer;
  padding: 2px;
  display: flex;
  align-items: center;
  opacity: 0.6;
}

.remove-filter:hover {
  opacity: 1;
}

.clear-filters {
  margin-left: auto;
  background: none;
  border: none;
  color: rgba(17, 23, 20, 0.3);
  font-size: 12px;
  cursor: pointer;
  text-decoration: underline;
}

.clear-filters:hover {
  color: #f87171;
}

.add-filter-btn {
  background: transparent;
  border: 1px dashed rgba(17, 23, 20, 0.2);
  color: rgba(17, 23, 20, 0.4);
  padding: 8px 16px;
  border-radius: 30px;
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  font-size: 13px;
}

.add-filter-btn:hover {
  border-color: #1B6B3A;
  color: #1B6B3A;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 5px;
  flex-shrink: 1;
  min-width: 0;
  overflow-x: auto;
  scrollbar-width: none;
  -ms-overflow-style: none;
  background: rgba(0,0,0,0.025);
  border: 1px solid rgba(0,0,0,0.06);
  border-radius: 12px;
  padding: 4px 6px;
}
.header-actions::-webkit-scrollbar { display: none; }

/* ── Auto-generate button ─────────────────────────────────── */
.autogen-btn {
  display: flex; align-items: center; gap: 5px;
  padding: 7px 13px; border: none; border-radius: 9px;
  background: linear-gradient(135deg, #f59e0b 0%, #f97316 100%);
  color: #fff; font-size: 12px; font-weight: 700; cursor: pointer;
  box-shadow: 0 2px 8px rgba(245, 158, 11, 0.35);
  transition: transform 0.15s, box-shadow 0.15s, opacity 0.15s;
  font-family: var(--font-sans, sans-serif);
  white-space: nowrap;
  letter-spacing: 0.1px;
}
.autogen-btn:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 6px 20px rgba(245,158,11,0.55);
}
.autogen-btn:disabled {
  opacity: 0.45;
  cursor: not-allowed;
  box-shadow: none;
  transform: none;
}

/* ── Share topbar button ──────────────────────────────────── */
.share-topbar-btn {
  display: flex; align-items: center; gap: 5px;
  padding: 7px 13px; border: 1.5px solid rgba(74,108,247,0.35); border-radius: 9px;
  background: rgba(74,108,247,0.08);
  color: #7a9cf9; font-size: 12px; font-weight: 700; cursor: pointer;
  transition: transform 0.15s, background 0.15s, border-color 0.15s, color 0.15s;
  font-family: var(--font-sans, sans-serif);
  white-space: nowrap;
  letter-spacing: 0.1px;
}
.share-topbar-btn:hover:not(:disabled) {
  background: rgba(74,108,247,0.18);
  border-color: rgba(74,108,247,0.65);
  color: #fff;
  transform: translateY(-1px);
}
.share-topbar-btn:disabled {
  opacity: 0.35; cursor: not-allowed;
}

/* ── Version history button ─────────────────────────────────────────────── */
.version-topbar-btn {
  display: flex; align-items: center; gap: 5px;
  padding: 7px 13px; border: 1.5px solid rgba(99,102,241,0.3); border-radius: 9px;
  background: rgba(99,102,241,0.07);
  color: #a5b4fc; font-size: 12px; font-weight: 700; cursor: pointer;
  transition: transform 0.15s, background 0.15s, border-color 0.15s, color 0.15s;
  font-family: var(--font-sans, sans-serif);
  white-space: nowrap;
  letter-spacing: 0.1px;
}
.version-topbar-btn:hover:not(:disabled) {
  background: rgba(99,102,241,0.16);
  border-color: rgba(99,102,241,0.55);
  color: #fff;
  transform: translateY(-1px);
}
.version-topbar-btn:disabled { opacity: 0.35; cursor: not-allowed; }

/* Share open button in settings panel */
.cfg-share-open-btn {
  display: flex; align-items: center; gap: 8px;
  width: 100%; padding: 10px 12px; border-radius: 8px;
  background: rgba(74,108,247,0.08);
  border: 1px solid rgba(74,108,247,0.22);
  color: #7a9cf9; font-size: 12px; font-weight: 600; cursor: pointer;
  transition: background .15s, border-color .15s, color .15s;
  text-align: left;
}
.cfg-share-open-btn:hover {
  background: rgba(74,108,247,0.15);
  border-color: rgba(74,108,247,0.4);
  color: #fff;
}

/* ── AI topbar button ─────────────────────────────────────── */
.ai-topbar-btn {
  position: relative;
  display: flex; align-items: center; gap: 5px;
  padding: 7px 13px; border: none; border-radius: 9px;
  background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%);
  color: #fff; font-size: 12px; font-weight: 700; cursor: pointer;
  box-shadow: 0 2px 8px rgba(99,102,241,0.35);
  transition: transform 0.15s, box-shadow 0.15s, background 0.15s;
  font-family: var(--font-sans, sans-serif);
  overflow: visible;
  white-space: nowrap;
  letter-spacing: 0.1px;
}
.ai-topbar-btn:hover {
  transform: translateY(-1px);
  box-shadow: 0 6px 22px rgba(99,102,241,0.6);
}
.ai-topbar-btn--open {
  background: linear-gradient(135deg, #4f46e5, #7c3aed);
  box-shadow: 0 2px 10px rgba(99,102,241,0.3);
}
.ai-topbar-pulse {
  position: absolute; inset: -5px; border-radius: 12px;
  background: rgba(99,102,241,0.3);
  animation: ai-topbar-pulse 2.4s ease-out infinite;
  pointer-events: none;
}
@keyframes ai-topbar-pulse {
  0%   { transform: scale(0.9); opacity: 0.7; }
  70%  { transform: scale(1.15); opacity: 0;  }
  100% { transform: scale(1.15); opacity: 0;  }
}
.header-sep {
  width: 1px; height: 22px;
  background: rgba(0,0,0,0.1);
  flex-shrink: 0;
  margin: 0 2px;
}

.export-btn {
  padding: 7px 12px;
  background: rgba(255,255,255,0.7);
  border: 1px solid rgba(0,0,0,0.1);
  border-radius: 9px;
  color: #111714;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 5px;
  transition: all 0.18s;
  white-space: nowrap;
  box-shadow: 0 1px 3px rgba(0,0,0,0.06);
}

.export-btn:hover:not(:disabled) {
  background: rgba(255,255,255,0.95);
  border-color: rgba(0,0,0,0.18);
  transform: translateY(-1px);
  box-shadow: 0 4px 10px rgba(0,0,0,0.08);
}

.export-btn.pdf:hover:not(:disabled) {
  color: #f87171;
  border-color: rgba(248, 113, 113, 0.3);
}

.export-btn.json:hover:not(:disabled) {
  color: #fbbf24;
  border-color: rgba(251, 191, 36, 0.3);
}

.save-btn {
  padding: 7px 16px;
  background: linear-gradient(135deg, #1B6B3A 0%, #15803D 100%);
  border: none;
  border-radius: 9px;
  color: #fff;
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 6px;
  transition: all 0.18s;
  white-space: nowrap;
  box-shadow: 0 2px 8px rgba(27,107,58,0.3);
  letter-spacing: 0.1px;
}

.save-btn:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 6px 20px rgba(27, 107, 58, 0.45);
  background: linear-gradient(135deg, #1e7a43 0%, #16a34a 100%);
}

.canvas-grid {
  flex: 1;
  padding: 28px;
  overflow-y: auto;
  position: relative;
  display: grid;
  grid-template-columns: repeat(12, 1fr);
  grid-auto-rows: 80px;
  align-content: flex-start;
  /* gap et background-image sont injectés inline via dashSettings */
  background-color: #F5F6F5;
}

.widget-card {
  background: #ffffff;
  border: 1px solid rgba(0,0,0,0.07);
  border-radius: 18px;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  box-shadow: 0 1px 2px rgba(0,0,0,0.04), 0 4px 12px rgba(0,0,0,0.06), 0 12px 32px rgba(0,0,0,0.04);
  transition: box-shadow 0.25s ease, border-color 0.25s ease, transform 0.2s ease;
  cursor: default;
}

.widget-card:hover {
  border-color: rgba(27,107,58,0.2);
  box-shadow: 0 2px 4px rgba(0,0,0,0.04), 0 8px 24px rgba(0,0,0,0.1), 0 24px 48px rgba(0,0,0,0.06);
  transform: translateY(-2px);
}

.widget-card[draggable='true'] {
  cursor: grab;
}

.widget-card.dragging {
  opacity: 0.5;
  border: 2px dashed #1B6B3A;
}

.widget-card.resizing {
  border-color: #1B6B3A;
  box-shadow: 0 0 15px rgba(var(--primary-color-rgb), 0.3);
  z-index: 100;
}

.resize-handle {
  position: absolute;
  width: 16px;
  height: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  opacity: 0;
  transition: all 0.2s;
  z-index: 50;
}

.widget-card:hover .resize-handle {
  opacity: 1;
}

.resize-icon {
  width: 8px;
  height: 8px;
  border: 2px solid #1B6B3A;
  border-radius: 2px;
}

.resize-handle.top-left {
  top: 4px;
  left: 4px;
  cursor: nw-resize;
}
.resize-handle.top-right {
  top: 4px;
  right: 4px;
  cursor: ne-resize;
}
.resize-handle.bottom-left {
  bottom: 4px;
  left: 4px;
  cursor: sw-resize;
}
.resize-handle.bottom-right {
  bottom: 4px;
  right: 4px;
  cursor: se-resize;
}

.resize-handle:hover .resize-icon {
  background: #1B6B3A;
  transform: scale(1.2);
}

.widget-header {
  padding: 12px 16px 11px;
  border-bottom: 1px solid rgba(0,0,0,0.05);
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 6px;
  background: #ffffff;
  position: relative;
}
.widget-header::after {
  content: '';
  position: absolute;
  bottom: -1px;
  left: 0; right: 0;
  height: 1px;
  background: linear-gradient(90deg, rgba(27,107,58,0.15) 0%, transparent 60%);
}

.widget-title-group {
  display: flex;
  flex-direction: column;
  gap: 1px;
  min-width: 0;
  flex: 1;
}

.widget-title {
  font-size: 12px;
  font-weight: 700;
  color: #111714;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  letter-spacing: -0.1px;
}

.widget-description {
  font-size: 10px;
  color: var(--color-text-muted, #94A99A);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* ── Caption strip ────────────────────────────────────── */
.widget-caption {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 5px;
  padding: 4px 12px 5px;
  border-top: 1px solid #F0F2F0;
  background: #FAFBFA;
  flex-shrink: 0;
}

.widget-caption-icon {
  font-size: 9px;
  color: #C4CFC7;
  flex-shrink: 0;
}

.widget-caption-text {
  font-size: 10px;
  color: #94A99A;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 100%;
  font-style: italic;
  letter-spacing: 0.02em;
}

/* Manual description: slightly brighter + not italic */
.widget-caption--manual .widget-caption-text {
  color: #4B5E52;
  font-style: normal;
  font-weight: 500;
}
.widget-caption--manual .widget-caption-icon {
  color: #94A99A;
}

.widget-actions button {
  background: none;
  border: none;
  color: var(--text-muted);
  cursor: pointer;
  padding: 4px;
  transition: color 0.2s;
}

.widget-actions button:hover {
  color: #f87171;
}

.widget-body {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background: #ffffff;
  padding: 14px 16px 12px;
  min-height: 0;
  width: 100%;
}

.placeholder-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
  color: var(--text-muted);
}

.placeholder-content i {
  font-size: 32px;
}
.placeholder-content span {
  font-size: 12px;
}

/* New Widget Placeholders */
.heatmap-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 15px;
  gap: 10px;
  color: rgba(var(--text-color-rgb, 240, 253, 249), 0.3);
  font-size: 10px;
}

.heatmap-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 4px;
  width: 80px;
  height: 80px;
}

.heatmap-cell {
  border-radius: 2px;
}

.empty-state {
  grid-column: 1 / -1;
  min-height: 300px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 12px;
  color: var(--text-muted);
  text-align: center;
  padding: 40px;
}
.empty-state-visual { margin-bottom: 8px; opacity: 0.6; }
.empty-state h3 {
  font-family: 'Syne', sans-serif;
  font-size: 18px;
  font-weight: 700;
  color: #111714;
  opacity: 0.6;
  margin: 0;
}
.empty-state p {
  font-size: 13px;
  color: var(--text-muted);
  margin: 0;
  max-width: 320px;
}
.empty-quick-add {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
  justify-content: center;
  margin-top: 8px;
}
.quick-add-btn {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 7px 14px;
  background: rgba(var(--primary-color-rgb, 27, 107, 58), 0.1);
  border: 1px solid rgba(var(--primary-color-rgb, 27, 107, 58), 0.25);
  border-radius: 20px;
  color: #1B6B3A;
  font-size: 12px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.15s;
}
.quick-add-btn:hover {
  background: rgba(var(--primary-color-rgb, 27, 107, 58), 0.2);
  transform: translateY(-1px);
}

.no-dashboard-state {
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 40px;
}

.empty-hero {
  max-width: 420px;
  text-align: center;
}
.empty-hero-icon {
  width: 80px; height: 80px;
  border-radius: 24px;
  background: rgba(var(--primary-color-rgb, 27, 107, 58), 0.08);
  border: 1px solid rgba(var(--primary-color-rgb, 27, 107, 58), 0.15);
  display: flex; align-items: center; justify-content: center;
  margin: 0 auto 24px;
  font-size: 36px;
  color: rgba(var(--primary-color-rgb, 27, 107, 58), 0.4);
}
.empty-hero h2 {
  font-family: 'Syne', sans-serif;
  font-size: 22px;
  font-weight: 700;
  margin-bottom: 12px;
  color: #111714;
}
.empty-hero p {
  color: var(--text-muted);
  line-height: 1.6;
  font-size: 14px;
  margin-bottom: 24px;
}
.hero-create-btn {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  background: #1B6B3A;
  color: #fff;
  border: none;
  border-radius: 12px;
  padding: 12px 24px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}
.hero-create-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 24px rgba(var(--primary-color-rgb, 27, 107, 58), 0.3);
}

/* Modal */
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(var(--bg-color-rgb, 0, 0, 0), 0.85);
  backdrop-filter: blur(8px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-content {
  background: var(--card-bg, #FFFFFF);
  border: 1px solid rgba(var(--primary-color-rgb, 27, 107, 58), 0.2);
  padding: 32px;
  border-radius: 24px;
  width: 100%;
  max-width: 400px;
  color: #111714;
}

.modal-content h3 {
  font-family: 'Syne', sans-serif;
  margin-bottom: 24px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-bottom: 24px;
}
.form-group label {
  font-size: 13px;
  color: rgba(17, 23, 20, 0.4);
}
.form-group input {
  background: rgba(var(--text-color-rgb, 17, 23, 20), 0.04);
  border: 1px solid rgba(var(--text-color-rgb, 17, 23, 20), 0.1);
  padding: 12px;
  border-radius: 10px;
  color: #111714;
  outline: none;
}

.form-group input::placeholder {
  color: var(--text-muted);
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}

.cancel-btn {
  background: none;
  border: none;
  color: rgba(17, 23, 20, 0.4);
  cursor: pointer;
}
.confirm-btn {
  background: #1B6B3A;
  border: none;
  padding: 10px 24px;
  border-radius: 10px;
  color: #fff;
  font-weight: 600;
  cursor: pointer;
}

.confirm-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

/* CSV Preview Specific */
.csv-preview-content {
  max-width: 800px;
  width: 90%;
}

.modal-header {
  margin-bottom: 20px;
}

.modal-header h3 {
  margin-bottom: 4px;
}

.file-info {
  font-size: 12px;
  color: rgba(17, 23, 20, 0.4);
}

.preview-body {
  display: flex;
  flex-direction: column;
  gap: 24px;
  margin-bottom: 24px;
}

.columns-info h4 {
  font-size: 14px;
  margin-bottom: 12px;
  color: rgba(17, 23, 20, 0.6);
}

.column-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.col-tag {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 12px;
  background: rgba(17,23,20,0.03);
  border: 1px solid rgba(17,23,20,0.08);
  border-radius: 8px;
}

.col-name {
  font-size: 13px;
  font-weight: 600;
}

.col-type {
  font-size: 10px;
  text-transform: uppercase;
  padding: 2px 6px;
  border-radius: 4px;
  font-weight: 700;
}

.col-type.number {
  background: rgba(52, 211, 153, 0.2);
  color: #1B6B3A;
}
.col-type.date {
  background: rgba(96, 165, 250, 0.2);
  color: #60a5fa;
}
.col-type.string {
  background: rgba(161, 161, 170, 0.2);
  color: #4B5E52;
}

.table-preview {
  overflow-x: auto;
  border: 1px solid rgba(17,23,20,0.08);
  border-radius: 12px;
  background: rgba(0, 0, 0, 0.2);
}

.table-preview table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
}

.table-preview th {
  text-align: left;
  padding: 12px 16px;
  background: rgba(17,23,20,0.02);
  border-bottom: 1px solid rgba(17,23,20,0.08);
  color: rgba(17, 23, 20, 0.5);
  font-weight: 600;
}

.table-preview td {
  padding: 10px 16px;
  border-bottom: 1px solid rgba(17,23,20,0.03);
  color: rgba(17, 23, 20, 0.8);
}

.table-footer {
  padding: 10px 16px;
  font-size: 11px;
  color: rgba(17, 23, 20, 0.3);
  text-align: center;
  background: rgba(17,23,20,0.02);
}

/* Widget type badge */
.widget-type-badge {
  width: 24px; height: 24px;
  border-radius: 7px;
  display: flex; align-items: center; justify-content: center;
  font-size: 11px;
  flex-shrink: 0;
  background: rgba(var(--primary-color-rgb, 27, 107, 58), 0.1);
  color: #1B6B3A;
  border: 1px solid rgba(var(--primary-color-rgb, 27, 107, 58), 0.18);
  box-shadow: 0 1px 2px rgba(0,0,0,0.06);
}
.wtype-bar    { background: rgba(59,130,246,0.12);  color: #3b82f6;  border-color: rgba(59,130,246,0.25); }
.wtype-line   { background: rgba(168,85,247,0.12);  color: #a855f7;  border-color: rgba(168,85,247,0.25); }
.wtype-area   { background: rgba(168,85,247,0.1);   color: #a855f7;  border-color: rgba(168,85,247,0.2); }
.wtype-pie    { background: rgba(245,158,11,0.12);  color: #f59e0b;  border-color: rgba(245,158,11,0.25); }
.wtype-doughnut { background: rgba(245,158,11,0.1); color: #f59e0b;  border-color: rgba(245,158,11,0.2); }
.wtype-kpi    { background: rgba(27,107,58,0.12);  color: #1B6B3A;  border-color: rgba(27,107,58,0.25); }
.wtype-scatter{ background: rgba(236,72,153,0.12);  color: #ec4899;  border-color: rgba(236,72,153,0.25); }
.wtype-radar  { background: rgba(6,182,212,0.12);   color: #06b6d4;  border-color: rgba(6,182,212,0.25); }
.wtype-table  { background: rgba(161,161,170,0.12); color: #4B5E52;  border-color: rgba(161,161,170,0.2); }
.wtype-gauge  { background: rgba(251,146,60,0.12);  color: #fb923c;  border-color: rgba(251,146,60,0.25); }
.wtype-funnel  { background: rgba(167,139,250,0.12); color: #a78bfa;  border-color: rgba(167,139,250,0.25); }
.wtype-boxplot { background: rgba(99,102,241,0.12);  color: #818cf8;  border-color: rgba(99,102,241,0.25); }
.wtype-text    { background: rgba(148,163,184,0.12); color: #94a3b8;  border-color: rgba(148,163,184,0.25); }

.wbtn-config, .wbtn-delete, .wbtn-labels, .wbtn-export {
  width: 26px; height: 26px;
  background: rgba(255,255,255,0.8); border: 1px solid rgba(0,0,0,0.07);
  border-radius: 7px;
  display: flex; align-items: center; justify-content: center;
  font-size: 11px;
  cursor: pointer;
  color: #6b7280;
  transition: all 0.15s;
  opacity: 0;
  box-shadow: 0 1px 3px rgba(0,0,0,0.06);
  backdrop-filter: blur(4px);
}
.widget-card:hover .wbtn-config,
.widget-card:hover .wbtn-delete,
.widget-card:hover .wbtn-labels,
.widget-card:hover .wbtn-export { opacity: 1; }
.wbtn-export:hover { background: rgba(59,130,246,0.15); color: #3b82f6; }
/* Le bouton libellés reste visible quand actif même sans survol */
.wbtn-labels.active { opacity: 1 !important; color: #1B6B3A; }
.wbtn-config:hover  { background: rgba(var(--primary-color-rgb, 27, 107, 58), 0.15); color: #1B6B3A; }
.wbtn-delete:hover  { background: rgba(239,68,68,0.15); color: #f87171; }
.wbtn-labels:hover  { background: rgba(var(--primary-color-rgb, 27, 107, 58), 0.12); color: #1B6B3A; }
.wbtn-labels.active { background: rgba(var(--primary-color-rgb, 27, 107, 58), 0.15); }

/* ══════════════════════════════════════════════════════════════
   CONFIG DRAWER (widget config + dashboard settings)
   ══════════════════════════════════════════════════════════════ */
.cfg-drawer {
  position: fixed;
  top: 0;
  left: 280px; /* right next to left sidebar */
  width: 360px;
  height: 100vh;
  background: var(--card-bg, #FFFFFF);
  border-right: 1px solid rgba(27,107,58,0.18);
  border-left: 1px solid rgba(27,107,58,0.08);
  display: flex;
  flex-direction: column;
  z-index: 200;
  box-shadow: 8px 0 40px rgba(0,0,0,0.4);
  color: #111714;
  font-family: 'DM Sans', sans-serif;
}

/* Header */
.cfg-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 18px;
  border-bottom: 1px solid rgba(27,107,58,0.06);
  background: rgba(27,107,58,0.05);
  flex-shrink: 0;
}
.cfg-header-left {
  display: flex;
  align-items: center;
  gap: 10px;
  flex: 1;
  min-width: 0;
}
.cfg-type-badge {
  width: 32px;
  height: 32px;
  border-radius: 8px;
  background: rgba(27,107,58,0.15);
  border: 1px solid rgba(27,107,58,0.3);
  display: flex;
  align-items: center;
  justify-content: center;
  color: #1B6B3A;
  flex-shrink: 0;
  font-size: 13px;
}
.cfg-title-input {
  flex: 1;
  background: rgba(27,107,58,0.06);
  border: 1px solid rgba(255,255,255,0.1);
  border-radius: 8px;
  padding: 6px 10px;
  color: #111714;
  font-size: 13px;
  font-weight: 600;
  font-family: inherit;
  outline: none;
  min-width: 0;
}
.cfg-title-input:focus { border-color: #1B6B3A; }
.cfg-close {
  width: 28px; height: 28px;
  background: none; border: none;
  color: rgba(255,255,255,0.3);
  cursor: pointer; border-radius: 6px;
  display: flex; align-items: center; justify-content: center;
  transition: all 0.15s; flex-shrink: 0;
}
.cfg-close:hover { background: rgba(239,68,68,0.15); color: #f87171; }

/* Tabs */
.cfg-tabs {
  display: flex;
  border-bottom: 1px solid rgba(27,107,58,0.06);
  background: rgba(0,0,0,0.15);
  flex-shrink: 0;
}
.cfg-tab {
  flex: 1;
  padding: 10px 4px;
  background: none;
  border: none;
  border-bottom: 2px solid transparent;
  color: #94A99A;
  font-size: 11px;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 5px;
  transition: all 0.15s;
  letter-spacing: 0.02em;
}
.cfg-tab:hover { color: rgba(255,255,255,0.7); }
.cfg-tab.active {
  color: #1B6B3A;
  border-bottom-color: #1B6B3A;
  background: rgba(var(--primary-color-rgb, 27, 107, 58), 0.05);
}

/* Body */
.cfg-body {
  flex: 1;
  overflow-y: auto;
  padding: 12px 14px;
  display: flex;
  flex-direction: column;
  gap: 0;
}
.cfg-body::-webkit-scrollbar { width: 4px; }
.cfg-body::-webkit-scrollbar-track { background: transparent; }
.cfg-body::-webkit-scrollbar-thumb { background: rgba(255,255,255,0.1); border-radius: 2px; }

/* Sections */
.cfg-section {
  padding: 14px 0;
  border-bottom: 1px solid rgba(27,107,58,0.06);
}
.cfg-section:last-child { border-bottom: none; }
.cfg-section-title {
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: rgba(255,255,255,0.3);
  margin-bottom: 12px;
}

/* Fields */
/* ── Pie fields explainer banner (in "Champs du visuel") ────────────────── */
.pie-fields-banner {
  display: flex; align-items: flex-start; gap: 7px;
  background: rgba(99, 102, 241, 0.08);
  border: 1px solid rgba(99, 102, 241, 0.22);
  border-radius: 8px;
  padding: 8px 10px;
  font-size: 11px;
  color: rgba(255,255,255,0.5);
  line-height: 1.5;
  margin-bottom: 10px;
}
.pie-fields-banner svg { flex-shrink: 0; margin-top: 1px; color: #818cf8; opacity: 0.9; }
.pie-fields-banner strong { color: #111714; font-weight: 600; }

/* ── Pie section descriptions ────────────────────────────────────────────── */
.pie-info-banner {
  display: flex; align-items: flex-start; gap: 7px;
  background: rgba(245,158,11,0.07);
  border: 1px solid rgba(245,158,11,0.18);
  border-radius: 8px;
  padding: 8px 10px;
  font-size: 11px;
  color: rgba(255,255,255,0.5);
  line-height: 1.5;
  margin-bottom: 12px;
}
.pie-info-banner svg { flex-shrink: 0; margin-top: 1px; color: #f59e0b; opacity: 0.8; }
.pie-info-banner strong { color: rgba(255,255,255,0.75); font-weight: 600; }

.cfg-hint {
  font-size: 10px;
  color: rgba(255,255,255,0.3);
  line-height: 1.4;
  margin-top: -2px;
}

.cfg-toggle-sub {
  display: block;
  font-size: 10px;
  font-style: normal;
  color: rgba(255,255,255,0.3);
  font-weight: 400;
  margin-top: 1px;
}

.pie-type-hint {
  display: flex; align-items: center; gap: 6px;
  font-size: 10px;
  color: rgba(255,255,255,0.28);
  margin-top: 10px;
  padding-top: 10px;
  border-top: 1px solid rgba(27,107,58,0.06);
  line-height: 1.4;
}
.pie-type-hint svg { flex-shrink: 0; opacity: 0.5; }

.cfg-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin-bottom: 10px;
}
.cfg-field:last-child { margin-bottom: 0; }
.cfg-field label {
  font-size: 11px;
  color: rgba(255,255,255,0.5);
  font-weight: 500;
}
.cfg-field label strong { color: #111714; }

.cfg-row-2 {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 10px;
  margin-bottom: 10px;
}

.cfg-input {
  background: rgba(27,107,58,0.06);
  border: 1px solid rgba(255,255,255,0.1);
  border-radius: 7px;
  padding: 7px 10px;
  color: #111714;
  font-size: 12px;
  font-family: inherit;
  outline: none;
  transition: border-color 0.15s;
  width: 100%;
}
.cfg-input:focus { border-color: #1B6B3A; }

.cfg-textarea {
  background: rgba(27,107,58,0.06);
  border: 1px solid rgba(255,255,255,0.1);
  border-radius: 7px;
  padding: 8px 10px;
  color: #111714;
  font-size: 12px;
  font-family: inherit;
  outline: none;
  transition: border-color 0.15s;
  width: 100%;
  resize: vertical;
  line-height: 1.55;
  min-height: 90px;
}
.cfg-textarea:focus { border-color: #1B6B3A; }
.cfg-textarea::placeholder { color: #C4CFC7; }

.cfg-gauge-preview {
  margin-top: 8px;
  padding: 6px 10px;
  background: rgba(251,146,60,0.08);
  border: 1px solid rgba(251,146,60,0.2);
  border-radius: 6px;
  font-size: 11px;
  color: rgba(255,255,255,0.6);
}
.cfg-gauge-preview strong { color: #fb923c; }

.cfg-presets {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 4px;
  margin-top: 8px;
}
.cfg-presets-label {
  font-size: 10px;
  color: rgba(255,255,255,0.4);
  flex-basis: 100%;
}
.cfg-preset-btn {
  background: rgba(27,107,58,0.06);
  border: 1px solid rgba(255,255,255,0.12);
  border-radius: 5px;
  padding: 3px 8px;
  font-size: 11px;
  color: rgba(255,255,255,0.7);
  cursor: pointer;
  transition: background 0.15s, border-color 0.15s, color 0.15s;
  font-family: inherit;
}
.cfg-preset-btn:hover {
  background: rgba(27,107,58,0.15);
  border-color: rgba(27,107,58,0.4);
  color: #1B6B3A;
}
.cfg-preset-btn.active {
  background: rgba(27,107,58,0.2);
  border-color: #1B6B3A;
  color: #1B6B3A;
  font-weight: 600;
}
.cfg-detect-btn {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 4px 10px;
  font-size: 10px;
  background: rgba(99,102,241,0.12);
  border: 1px solid rgba(99,102,241,0.3);
  border-radius: 6px;
  color: #818cf8;
  cursor: pointer;
  transition: background 0.15s, border-color 0.15s;
  font-family: var(--font-sans, sans-serif);
}
.cfg-detect-btn:hover {
  background: rgba(99,102,241,0.25);
  border-color: rgba(99,102,241,0.55);
}
.cfg-detect-btn--full {
  width: 100%;
  justify-content: center;
}
.cfg-detect-msg {
  margin-top: 5px;
  padding: 4px 8px;
  border-radius: 5px;
  font-size: 10px;
  text-align: center;
  background: rgba(239,68,68,0.12);
  border: 1px solid rgba(239,68,68,0.25);
  color: #f87171;
}
.cfg-detect-msg--ok {
  background: rgba(27,107,58,0.12);
  border-color: rgba(27,107,58,0.3);
  color: #1B6B3A;
}
.cfg-preset-reset {
  padding: 3px 6px;
  color: rgba(255,255,255,0.4);
}
.cfg-preset-reset:hover {
  background: rgba(239,68,68,0.12);
  border-color: rgba(239,68,68,0.35);
  color: #ef4444;
}
.cfg-preset-reset.active {
  background: rgba(17,23,20,0.08);
  border-color: rgba(17,23,20,0.15);
  color: rgba(255,255,255,0.6);
  font-weight: 400;
}

/* Live unit preview */
.cfg-unit-preview {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 8px;
  padding: 7px 10px;
  background: rgba(27,107,58,0.07);
  border: 1px solid rgba(27,107,58,0.2);
  border-radius: 8px;
}
.cup-label {
  font-size: 10px;
  color: rgba(255,255,255,0.4);
  flex-shrink: 0;
}
.cup-value {
  font-size: 12px;
  font-weight: 700;
  color: #1B6B3A;
  font-family: var(--font-mono, monospace);
}
.cup-value--lg {
  font-size: 14px;
  opacity: 0.7;
}

/* Section badge showing active unit */
.pp-section-badge {
  display: inline-flex;
  align-items: center;
  margin-left: 6px;
  padding: 1px 6px;
  background: rgba(27,107,58,0.15);
  border: 1px solid rgba(27,107,58,0.3);
  border-radius: 4px;
  font-size: 9px;
  font-weight: 700;
  color: #1B6B3A;
  font-family: var(--font-mono, monospace);
  letter-spacing: 0;
}

.cfg-select {
  background: rgba(27,107,58,0.06);
  border: 1px solid rgba(255,255,255,0.1);
  border-radius: 7px;
  padding: 7px 10px;
  color: #111714;
  font-size: 12px;
  font-family: inherit;
  outline: none;
  cursor: pointer;
  width: 100%;
  transition: border-color 0.15s;
  appearance: auto;
}
.cfg-select:focus { border-color: #1B6B3A; }
.cfg-select option { background: #FFFFFF; color: #111714; }

/* Range slider */
.cfg-range {
  width: 100%;
  accent-color: #1B6B3A;
  cursor: pointer;
}
.cfg-range-marks {
  display: flex;
  justify-content: space-between;
  font-size: 9px;
  color: #C4CFC7;
  margin-top: 2px;
}

/* Button group */
.cfg-btn-group {
  display: flex;
  gap: 4px;
}
.cfg-btn-group button {
  flex: 1;
  padding: 6px 4px;
  background: rgba(27,107,58,0.06);
  border: 1px solid rgba(255,255,255,0.1);
  border-radius: 6px;
  color: rgba(255,255,255,0.5);
  font-size: 11px;
  cursor: pointer;
  transition: all 0.15s;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 4px;
}
.cfg-btn-group button.active {
  background: rgba(27,107,58,0.2);
  border-color: #1B6B3A;
  color: #1B6B3A;
  font-weight: 600;
}
.cfg-btn-group button:hover:not(.active) {
  background: rgba(17,23,20,0.08);
  color: #111714;
}

/* Checkbox column list (Table / Heatmap config) */
.cfg-checkbox-list {
  display: flex;
  flex-direction: column;
  gap: 2px;
  max-height: 200px;
  overflow-y: auto;
  margin-top: 6px;
  padding: 4px 6px;
  background: rgba(17,23,20,0.02);
  border: 1px solid rgba(17,23,20,0.08);
  border-radius: 6px;
  scrollbar-width: thin;
}
.cfg-checkbox-item {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 11px;
  color: rgba(255,255,255,0.75);
  cursor: pointer;
  padding: 4px 6px;
  border-radius: 4px;
  transition: background 0.12s;
}
.cfg-checkbox-item:hover { background: rgba(27,107,58,0.06); }
.cfg-checkbox-item input[type="checkbox"] {
  width: 13px;
  height: 13px;
  accent-color: #1B6B3A;
  cursor: pointer;
  flex-shrink: 0;
}
.cfg-col-name {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
/* New badge replacing the old flat text */
.cfg-col-type-badge {
  margin-left: auto;
  flex-shrink: 0;
  font-size: 9px;
  font-weight: 700;
  padding: 1px 5px;
  border-radius: 4px;
  letter-spacing: 0.02em;
}
.col-type-number { background: rgba(74,108,247,0.18); color: #7a9cf9; border: 1px solid rgba(74,108,247,0.3); }
.col-type-date   { background: rgba(27,107,58,0.15); color: #1B6B3A; border: 1px solid rgba(27,107,58,0.25); }
.col-type-category,
.col-type-string,
.col-type-boolean { background: rgba(245,158,11,0.12); color: #fbbf24; border: 1px solid rgba(245,158,11,0.22); }

/* Label row with inline action buttons */
.cfg-label-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 3px;
}
.cfg-label-row label { margin-bottom: 0; }
.cfg-col-actions { display: flex; gap: 4px; }
.cfg-micro-btn {
  font-size: 9px;
  font-weight: 600;
  padding: 2px 7px;
  border-radius: 4px;
  border: 1px solid rgba(255,255,255,0.12);
  background: rgba(27,107,58,0.06);
  color: rgba(255,255,255,0.6);
  cursor: pointer;
  transition: background 0.15s, color 0.15s;
  letter-spacing: 0.03em;
}
.cfg-micro-btn:hover:not(:disabled) { background: rgba(255,255,255,0.12); color: #111714; }
.cfg-micro-btn:disabled { opacity: 0.3; cursor: not-allowed; }

.cfg-col-type {
  margin-left: auto;
  font-size: 9px;
  color: rgba(255,255,255,0.3);
  font-style: italic;
}
.cfg-no-data {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  font-size: 10px;
  color: rgba(255,255,255,0.3);
  text-align: center;
  padding: 10px 0;
}

/* Image upload zone */
.img-upload-zone {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 5px;
  padding: 16px 12px;
  border: 1.5px dashed rgba(255,255,255,0.18);
  border-radius: 8px;
  cursor: pointer;
  font-size: 11px;
  color: rgba(255,255,255,0.5);
  text-align: center;
  transition: border-color 0.2s, background 0.2s;
  margin-top: 4px;
}
.img-upload-zone:hover {
  border-color: #1B6B3A;
  background: rgba(27,107,58,0.06);
  color: rgba(255,255,255,0.75);
}
.img-upload-zone .pi { font-size: 18px; margin-bottom: 4px; color: rgba(255,255,255,0.4); }
.img-upload-hint { font-size: 9px; opacity: 0.5; }

.img-thumb-preview {
  position: relative;
  margin-top: 8px;
  border-radius: 6px;
  overflow: hidden;
  max-height: 100px;
  border: 1px solid rgba(255,255,255,0.12);
}
.img-thumb-preview img {
  width: 100%;
  height: 100px;
  object-fit: contain;
  display: block;
  background: rgba(0,0,0,0.3);
}
.img-thumb-clear {
  position: absolute;
  top: 4px;
  right: 4px;
  width: 20px;
  height: 20px;
  background: rgba(0,0,0,0.7);
  border: none;
  border-radius: 50%;
  color: white;
  font-size: 9px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.15s;
}
.img-thumb-clear:hover { background: rgba(239,68,68,0.8); }

/* ── Map widget styles ── */
.map-search-results {
  margin-top: 6px;
  border: 1px solid rgba(255,255,255,0.1);
  border-radius: 8px;
  overflow: hidden;
  max-height: 180px;
  overflow-y: auto;
}
.map-search-item {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  padding: 8px 10px;
  font-size: 11px;
  color: rgba(255,255,255,0.75);
  cursor: pointer;
  border-bottom: 1px solid rgba(27,107,58,0.06);
  transition: background 0.15s;
  line-height: 1.4;
}
.map-search-item:last-child { border-bottom: none; }
.map-search-item:hover { background: rgba(59,130,246,0.15); color: white; }
.map-markers-list { display: flex; flex-direction: column; gap: 5px; margin-top: 6px; }
.map-marker-row {
  display: flex;
  align-items: center;
  gap: 6px;
}
.map-marker-color {
  width: 28px;
  height: 28px;
  border-radius: 6px;
  border: 1px solid rgba(17,23,20,0.12);
  cursor: pointer;
  background: none;
  padding: 0;
  flex-shrink: 0;
}
.map-marker-del {
  width: 26px;
  height: 26px;
  border: none;
  background: rgba(239,68,68,0.15);
  color: #f87171;
  border-radius: 6px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 10px;
  flex-shrink: 0;
  transition: background 0.15s;
}
.map-marker-del:hover { background: rgba(239,68,68,0.35); }
.cfg-btn-primary {
  padding: 0 12px;
  height: 34px;
  background: #3b82f6;
  color: white;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-size: 13px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  transition: background 0.15s;
}
.cfg-btn-primary:hover { background: #2563eb; }
.cfg-btn-primary:disabled { opacity: 0.5; cursor: not-allowed; }

.cfg-or-sep {
  display: flex;
  align-items: center;
  gap: 8px;
  margin: 8px 0;
  color: rgba(17,23,20,0.15);
  font-size: 10px;
}
.cfg-or-sep::before, .cfg-or-sep::after {
  content: '';
  flex: 1;
  height: 1px;
  background: rgba(255,255,255,0.1);
}

/* Color palette */
.cfg-palette {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-bottom: 10px;
}
.cfg-pal-dot {
  width: 28px; height: 28px;
  border-radius: 7px;
  cursor: pointer;
  border: 2px solid transparent;
  display: flex; align-items: center; justify-content: center;
  transition: all 0.15s;
  position: relative;
  flex-shrink: 0;
}
.cfg-pal-dot:hover { transform: scale(1.12); }
.cfg-pal-dot.active { border-color: #111714; box-shadow: 0 0 0 1px rgba(0,0,0,0.5); }
.cfg-pal-dot .pi-check { font-size: 10px; color: #fff; text-shadow: 0 1px 2px rgba(0,0,0,0.8); }
.cfg-pal-custom {
  background: rgba(17,23,20,0.08);
  border: 1px dashed rgba(17,23,20,0.15);
  color: rgba(255,255,255,0.5);
  font-size: 12px;
  overflow: hidden;
}
.cfg-pal-custom input[type=color] {
  position: absolute; inset: 0; opacity: 0; cursor: pointer; width: 100%; height: 100%;
}
.cfg-color-preview {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 11px;
  color: rgba(255,255,255,0.5);
  font-family: monospace;
}
.cfg-color-preview .swatch {
  width: 16px; height: 16px; border-radius: 4px; flex-shrink: 0;
}

/* Style cards */
.cfg-style-grid {
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: 6px;
}
.cfg-style-btn {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 5px;
  padding: 8px 4px;
  background: rgba(17,23,20,0.02);
  border: 1px solid rgba(17,23,20,0.08);
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.15s;
  font-size: 9px;
  color: rgba(255,255,255,0.4);
}
.cfg-style-btn:hover { border-color: rgba(17,23,20,0.15); color: rgba(255,255,255,0.7); }
.cfg-style-btn.active { border-color: #1B6B3A; color: #1B6B3A; background: rgba(27,107,58,0.1); }
.cfg-style-preview {
  width: 36px; height: 20px; border-radius: 4px;
  border: 1px solid rgba(255,255,255,0.1);
}

/* Toggle switch */
.cfg-toggles {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.cfg-toggle {
  display: flex;
  align-items: center;
  justify-content: space-between;
  cursor: pointer;
  user-select: none;
  gap: 8px;
  font-size: 12px;
  color: rgba(255,255,255,0.65);
}
.cfg-toggle input[type=checkbox] { display: none; }
.toggle-track {
  width: 34px; height: 18px;
  background: rgba(255,255,255,0.12);
  border-radius: 9px;
  position: relative;
  flex-shrink: 0;
  transition: background 0.2s;
}
.cfg-toggle input:checked ~ .toggle-track { background: #1B6B3A; }
.toggle-thumb {
  position: absolute;
  top: 2px; left: 2px;
  width: 14px; height: 14px;
  background: #fff;
  border-radius: 50%;
  transition: left 0.2s;
  box-shadow: 0 1px 4px rgba(0,0,0,0.4);
}
.cfg-toggle input:checked ~ .toggle-track .toggle-thumb { left: 18px; }

/* KPI icon grid */
.cfg-icon-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 6px;
}
.cfg-icon-btn {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  padding: 8px 4px;
  background: rgba(17,23,20,0.02);
  border: 1px solid rgba(17,23,20,0.08);
  border-radius: 8px;
  cursor: pointer;
  font-size: 10px;
  color: rgba(255,255,255,0.4);
  transition: all 0.15s;
}
.cfg-icon-btn.active { border-color: #1B6B3A; color: #1B6B3A; background: rgba(27,107,58,0.1); }
.cfg-icon-btn i { font-size: 16px; }

/* Footer */
.cfg-footer {
  padding: 12px 14px;
  border-top: 1px solid rgba(27,107,58,0.06);
  display: flex;
  gap: 8px;
  background: rgba(0,0,0,0.2);
  flex-shrink: 0;
}
.cfg-btn-cancel {
  flex: 1;
  padding: 9px;
  background: rgba(27,107,58,0.06);
  border: 1px solid rgba(255,255,255,0.1);
  border-radius: 8px;
  color: rgba(255,255,255,0.5);
  font-size: 13px;
  cursor: pointer;
  transition: all 0.15s;
}
.cfg-btn-cancel:hover { background: rgba(17,23,20,0.08); color: #111714; }
.cfg-btn-apply {
  flex: 2;
  padding: 9px;
  background: linear-gradient(135deg, #134E2A, #1B6B3A);
  border: none;
  border-radius: 8px;
  color: #fff;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  transition: opacity 0.15s;
}
.cfg-btn-apply:hover { opacity: 0.9; }

/* Dashboard settings specific */
.dash-settings-drawer { left: 280px; }
.dash-theme-grid {
  display: flex;
  gap: 10px;
}
.dash-theme-btn {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  background: none;
  border: 1px solid rgba(17,23,20,0.08);
  border-radius: 10px;
  padding: 8px 4px;
  cursor: pointer;
  font-size: 10px;
  color: rgba(255,255,255,0.4);
  transition: all 0.15s;
}
.dash-theme-btn.active { border-color: #1B6B3A; color: #1B6B3A; }
.dash-theme-preview {
  width: 44px; height: 28px;
  border-radius: 6px;
  border: 1px solid rgba(255,255,255,0.1);
  position: relative;
  overflow: hidden;
}
.dash-theme-dot {
  position: absolute;
  top: 50%; left: 50%;
  transform: translate(-50%, -50%);
  width: 10px; height: 10px;
  border-radius: 50%;
}
.dash-theme-bar {
  position: absolute;
  top: 0; left: 0;
  width: 12px; height: 100%;
  opacity: 0.8;
}
.share-url-row {
  display: flex;
  gap: 6px;
  margin-top: 8px;
}
.share-url-input { font-size: 10px; font-family: monospace; }
.share-copy-btn {
  padding: 7px 10px;
  background: rgba(27,107,58,0.15);
  border: 1px solid rgba(27,107,58,0.3);
  border-radius: 7px;
  color: #1B6B3A;
  cursor: pointer;
  flex-shrink: 0;
  transition: all 0.15s;
}
.share-copy-btn:hover { background: rgba(27,107,58,0.25); }

/* Drawer animation — slides in from left */
.drawer-in-enter-active, .drawer-in-leave-active {
  transition: transform 0.3s cubic-bezier(0.16, 1, 0.3, 1), opacity 0.25s;
}
.drawer-in-enter-from, .drawer-in-leave-to {
  transform: translateX(-20px);
  opacity: 0;
}

/* Filter modal grid */
.filter-grid {
  display: flex;
  flex-direction: column;
  gap: 14px;
  margin-bottom: 20px;
}
.filter-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.filter-field label {
  font-size: 12px;
  color: #94A99A;
  font-weight: 500;
}
.range-filter {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

/* Icon button in header */
.icon-btn {
  width: 36px; height: 36px;
  background: rgba(27,107,58,0.06);
  border: 1px solid rgba(255,255,255,0.1);
  border-radius: 9px;
  color: rgba(255,255,255,0.6);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.15s;
  font-size: 14px;
}
.icon-btn:hover, .icon-btn.active {
  background: rgba(27,107,58,0.15);
  border-color: rgba(27,107,58,0.4);
  color: #1B6B3A;
}

/* (ancienne animation slide-up — remplacée par drawer-in) */

.palette-item.disabled {
  opacity: 0.3;
  cursor: not-allowed;
  filter: grayscale(0.8);
  pointer-events: none;
}

/* View Transitions */
::view-transition-group(*),
::view-transition-old(*),
::view-transition-new(*) {
  animation-duration: 0.25s;
  animation-timing-function: cubic-bezier(0.19, 1, 0.22, 1);
}

/* Save toast */
.save-toast {
  position: fixed;
  bottom: 32px;
  left: 50%;
  transform: translateX(-50%);
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 24px;
  border-radius: 12px;
  font-size: 14px;
  font-weight: 600;
  z-index: 9999;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4);
  backdrop-filter: blur(8px);
  pointer-events: none;
}
.save-toast.success {
  background: rgba(27, 107, 58, 0.15);
  border: 1px solid rgba(27, 107, 58, 0.4);
  color: #1B6B3A;
}
.save-toast.error {
  background: rgba(239, 68, 68, 0.15);
  border: 1px solid rgba(239, 68, 68, 0.4);
  color: #fca5a5;
}
.toast-slide-enter-active, .toast-slide-leave-active {
  transition: all 0.35s cubic-bezier(0.16, 1, 0.3, 1);
}
.toast-slide-enter-from, .toast-slide-leave-to {
  opacity: 0;
  transform: translateX(-50%) translateY(20px) scale(0.95);
}

/* ════════════════════════════════════════════════════════════════
   FIELDS PANEL  (Power BI–style right pane)
   ════════════════════════════════════════════════════════════════ */
.fields-panel {
  width: 240px;
  flex-shrink: 0;
  background: #FFFFFF;
  border-left: 1px solid #E4E8E4;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  transition: width 0.25s cubic-bezier(0.16,1,0.3,1), background 0.4s;
  position: relative;
  font-family: 'DM Sans', sans-serif;
  box-shadow: -2px 0 12px rgba(17,23,20,0.04);
}
.fields-panel--collapsed {
  width: 36px;
}

/* Toggle button */
.fields-toggle {
  position: absolute;
  top: 14px;
  right: 8px;
  width: 24px; height: 24px;
  border-radius: 6px;
  border: 1px solid #E4E8E4;
  background: #F5F6F5;
  color: #94A99A;
  display: flex; align-items: center; justify-content: center;
  cursor: pointer;
  transition: background 0.15s, color 0.15s;
  flex-shrink: 0;
  z-index: 2;
}
.fields-toggle:hover { background: rgba(27,107,58,0.15); color: #1B6B3A; }
.fields-panel--collapsed .fields-toggle { right: 6px; top: 10px; }

/* Collapsed vertical label */
.fields-collapsed-label {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  padding-top: 52px;
  color: #94A99A;
}
.fields-collapsed-label i { font-size: 14px; }
.fields-collapsed-label span {
  writing-mode: vertical-rl;
  text-orientation: mixed;
  font-size: 10px;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: #94A99A;
}

/* Header */
.fields-header {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 14px 12px 12px;
  border-bottom: 1px solid #E4E8E4;
  flex-shrink: 0;
  padding-right: 40px;
  background: linear-gradient(180deg, rgba(27,107,58,0.03) 0%, transparent 100%);
}
.fields-header-left {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.fields-panel-title {
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: #111714;
}
.fields-panel-subtitle {
  font-size: 10px;
  color: #94A99A;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.fields-import-btn {
  width: 26px; height: 26px;
  border-radius: 6px;
  border: 1px solid rgba(17,23,20,0.08);
  background: transparent;
  color: #94A99A;
  cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  font-size: 11px;
  flex-shrink: 0;
  transition: background 0.15s, color 0.15s;
}
.fields-import-btn:hover { background: rgba(27,107,58,0.15); color: #1B6B3A; }

/* Empty state */
.fields-empty-state {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 20px 16px;
  text-align: center;
}
.fields-empty-icon { font-size: 28px; color: rgba(17,23,20,0.12); }
.fields-empty-title { font-size: 12px; font-weight: 600; color: #4B5E52; margin: 0; }
.fields-empty-sub { font-size: 10px; color: #C4CFC7; line-height: 1.5; margin: 0; }
.fields-empty-cta {
  margin-top: 6px;
  padding: 7px 14px;
  border-radius: 8px;
  border: 1px solid rgba(27,107,58,0.35);
  background: rgba(27,107,58,0.08);
  color: #1B6B3A;
  font-size: 11px;
  font-weight: 600;
  cursor: pointer;
  display: flex; align-items: center; gap: 6px;
  transition: background 0.15s;
}
.fields-empty-cta:hover { background: rgba(27,107,58,0.18); }

/* Stats row */
.fields-stats-row {
  display: flex;
  align-items: center;
  gap: 0;
  padding: 10px 12px;
  border-bottom: 1px solid #E4E8E4;
  flex-shrink: 0;
  background: #FAFAFA;
}
.fields-stat {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
}
.fstat-val { font-size: 14px; font-weight: 800; color: #1B6B3A; }
.fstat-lbl { font-size: 9px; color: #94A99A; text-transform: uppercase; letter-spacing: 0.06em; font-weight: 600; }
.fstat-sep { width: 1px; height: 24px; background: #E4E8E4; margin: 0 4px; flex-shrink: 0; }

/* Hint bar */
.fields-hint-bar {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 7px 12px;
  font-size: 10px;
  color: #94A99A;
  background: #F5F6F5;
  border-bottom: 1px solid #E4E8E4;
  flex-shrink: 0;
}
.fields-hint-bar i { font-size: 11px; color: #1B6B3A; }

/* Field group */
.fields-group {
  flex-shrink: 0;
  padding-bottom: 4px;
}
.fields-group-header {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 10px 12px 4px;
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: #94A99A;
}
.fgroup-icon {
  width: 16px; height: 16px;
  border-radius: 4px;
  display: flex; align-items: center; justify-content: center;
  font-size: 9px; font-weight: 800;
  flex-shrink: 0;
}
.fgroup-icon--measure { background: rgba(27,107,58,0.2); color: #1B6B3A; }
.fgroup-icon--dim     { background: rgba(99,102,241,0.2); color: #a5b4fc; }
.fgroup-label { flex: 1; }
.fgroup-count {
  font-size: 9px;
  background: rgba(27,107,58,0.06);
  border-radius: 10px;
  padding: 1px 6px;
  color: #94A99A;
}

/* Field list */
.fields-list {
  display: flex;
  flex-direction: column;
  gap: 1px;
  padding: 0 6px;
  overflow-y: auto;
  max-height: 180px;
}
.fields-list::-webkit-scrollbar { width: 3px; }
.fields-list::-webkit-scrollbar-track { background: transparent; }
.fields-list::-webkit-scrollbar-thumb { background: rgba(17,23,20,0.08); border-radius: 2px; }

/* Field item */
.field-item {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 5px 8px;
  border-radius: 6px;
  cursor: grab;
  transition: background 0.12s, transform 0.12s;
  border: 1px solid transparent;
  position: relative;
}
.field-item:hover {
  background: rgba(27,107,58,0.06);
  border-color: rgba(27,107,58,0.06);
}
.field-item:active { cursor: grabbing; transform: scale(0.98); }

.field-item--measure:hover  { background: rgba(27,107,58,0.08); border-color: rgba(27,107,58,0.15); }
.field-item--dim:hover      { background: rgba(99,102,241,0.08); border-color: rgba(99,102,241,0.15); }
.field-item--active {
  background: rgba(27,107,58,0.12) !important;
  border-color: rgba(27,107,58,0.3) !important;
}
.field-item--dragging { opacity: 0.4; }

.fi-type {
  width: 18px; height: 18px;
  border-radius: 4px;
  display: flex; align-items: center; justify-content: center;
  font-size: 9px; font-weight: 700;
  flex-shrink: 0;
}
.fi-type--measure { background: rgba(27,107,58,0.2); color: #1B6B3A; }
.fi-type--dim     { background: rgba(99,102,241,0.18); color: #6366f1; font-size: 12px; }

.fi-name {
  flex: 1;
  font-size: 11px;
  color: #4B5E52;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.fi-axis-hint {
  font-size: 9px;
  color: #94A99A;
  opacity: 0;
  transition: opacity 0.15s;
}
.field-item:hover .fi-axis-hint { opacity: 1; }
.fi-axis-badge {
  font-size: 9px;
  font-weight: 800;
  padding: 1px 5px;
  border-radius: 4px;
}
.fi-axis-badge--x { background: rgba(99,102,241,0.15); color: #6366f1; }
.fi-axis-badge--y { background: rgba(27,107,58,0.25); color: #1B6B3A; }

/* ════════════════════════════════════════════════════════════════
   AXIS WELLS  (field drop zones in config panel)
   ════════════════════════════════════════════════════════════════ */
.axis-wells {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-bottom: 12px;
}

.axis-well {
  border: 1px solid rgba(17,23,20,0.08);
  border-radius: 10px;
  background: rgba(17,23,20,0.02);
  overflow: hidden;
  transition: border-color 0.15s, background 0.15s;
}
.axis-well--over {
  border-color: rgba(27,107,58,0.5) !important;
  background: rgba(27,107,58,0.05) !important;
  box-shadow: 0 0 0 3px rgba(27,107,58,0.1);
}

.axis-well-header {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 10px;
  background: rgba(17,23,20,0.02);
  border-bottom: 1px solid rgba(27,107,58,0.06);
}
.axis-badge {
  width: 18px; height: 18px;
  border-radius: 5px;
  display: flex; align-items: center; justify-content: center;
  font-size: 9px; font-weight: 800;
  flex-shrink: 0;
}
.axis-badge--x { background: rgba(99,102,241,0.25); color: #a5b4fc; }
.axis-badge--y { background: rgba(27,107,58,0.25); color: #1B6B3A; }

.axis-well-label {
  flex: 1;
  font-size: 10px;
  color: #94A99A;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  font-weight: 600;
}
.axis-clear {
  width: 18px; height: 18px;
  border-radius: 4px;
  border: none;
  background: rgba(27,107,58,0.06);
  color: #94A99A;
  cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  font-size: 9px;
  transition: background 0.15s, color 0.15s;
}
.axis-clear:hover { background: rgba(239,68,68,0.2); color: #f87171; }

.axis-well-body {
  padding: 6px 8px;
  min-height: 38px;
  display: flex;
  align-items: center;
}

.axis-chip {
  display: flex;
  align-items: center;
  gap: 5px;
  padding: 4px 10px;
  border-radius: 20px;
  font-size: 11px;
  font-weight: 600;
  max-width: 100%;
}
.axis-chip--dim     { background: rgba(99,102,241,0.18); border: 1px solid rgba(99,102,241,0.25); color: #c7d2fe; }
.axis-chip--measure { background: rgba(27,107,58,0.15); border: 1px solid rgba(27,107,58,0.25); color: #1B6B3A; }

.aChip-icon { font-size: 11px; flex-shrink: 0; }
.aChip-name {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 160px;
}

.axis-well-placeholder {
  display: flex;
  align-items: center;
  gap: 6px;
  color: rgba(17,23,20,0.15);
  font-size: 10px;
  font-style: italic;
  padding: 2px 4px;
}
.axis-well-placeholder i { font-size: 11px; }

/* Fallback select (compact, below the chip) */
.axis-fallback-select {
  display: block;
  width: 100%;
  padding: 4px 8px;
  font-size: 10px;
  color: rgba(255,255,255,0.45);
  background: transparent;
  border: none;
  border-top: 1px solid rgba(27,107,58,0.06);
  cursor: pointer;
  outline: none;
  appearance: none;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='10' height='6' fill='none'%3E%3Cpath d='M1 1l4 4 4-4' stroke='rgba(255,255,255,0.25)' stroke-width='1.5' stroke-linecap='round' stroke-linejoin='round'/%3E%3C/svg%3E");
  background-repeat: no-repeat;
  background-position: right 8px center;
  padding-right: 22px;
}
.axis-fallback-select:focus { color: rgba(255,255,255,0.7); }
.axis-fallback-select option { background: #FFFFFF; color: #111714; }
.axis-fallback-select optgroup { color: rgba(255,255,255,0.4); font-size: 9px; }

.mb-10 { margin-bottom: 10px; }

/* ════════════════════════════════════════════════════════════════
   FORMAT TOOLBAR  (barre contextuelle Power BI au-dessus du canvas)
   ════════════════════════════════════════════════════════════════ */
.format-toolbar {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 6px 14px;
  background: var(--card-bg, #FFFFFF);
  border-bottom: 1px solid rgba(27,107,58,0.12);
  flex-shrink: 0;
  min-height: 42px;
  overflow-x: auto;
}
.format-toolbar::-webkit-scrollbar { height: 3px; }
.format-toolbar::-webkit-scrollbar-track { background: transparent; }
.format-toolbar::-webkit-scrollbar-thumb { background: rgba(17,23,20,0.08); border-radius: 2px; }

.ftb-info {
  display: flex;
  align-items: center;
  gap: 7px;
  flex-shrink: 0;
}
.ftb-type-badge {
  width: 22px; height: 22px;
  border-radius: 6px;
  display: flex; align-items: center; justify-content: center;
  font-size: 11px;
}
.ftb-title {
  font-size: 12px;
  font-weight: 600;
  color: #111714;
  max-width: 120px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.ftb-sep {
  width: 1px;
  height: 20px;
  background: rgba(255,255,255,0.1);
  margin: 0 4px;
  flex-shrink: 0;
}
.ftb-colors {
  display: flex;
  gap: 4px;
  align-items: center;
}
.ftb-color {
  width: 18px; height: 18px;
  border-radius: 5px;
  border: 2px solid transparent;
  cursor: pointer;
  transition: transform 0.12s, border-color 0.12s;
  flex-shrink: 0;
}
.ftb-color:hover { transform: scale(1.15); }
.ftb-color.active { border-color: #111714; }

.ftb-btn {
  width: 28px; height: 28px;
  border-radius: 7px;
  border: 1px solid transparent;
  background: transparent;
  color: rgba(255,255,255,0.45);
  cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  font-size: 12px;
  transition: background 0.12s, color 0.12s, border-color 0.12s;
  flex-shrink: 0;
}
.ftb-btn:hover { background: rgba(17,23,20,0.08); color: #111714; }
.ftb-btn.active { background: rgba(27,107,58,0.18); color: #1B6B3A; border-color: rgba(27,107,58,0.3); }
.ftb-btn--danger:hover { background: rgba(239,68,68,0.15); color: #f87171; }

.ftb-apply-btn {
  display: flex; align-items: center; gap: 5px;
  padding: 5px 12px;
  border-radius: 8px;
  border: 1px solid rgba(27,107,58,0.4);
  background: rgba(27,107,58,0.12);
  color: #1B6B3A;
  font-size: 11px;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.15s;
  flex-shrink: 0;
}
.ftb-apply-btn:hover { background: rgba(27,107,58,0.22); }

.ftb-close-btn {
  width: 24px; height: 24px;
  border-radius: 6px;
  border: none;
  background: transparent;
  color: #C4CFC7;
  cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  font-size: 11px;
  transition: background 0.12s, color 0.12s;
  margin-left: 2px;
}
.ftb-close-btn:hover { background: rgba(239,68,68,0.1); color: #f87171; }

/* Format toolbar slide animation */
.ftb-slide-enter-active, .ftb-slide-leave-active {
  transition: all 0.2s cubic-bezier(0.16,1,0.3,1);
  overflow: hidden;
}
.ftb-slide-enter-from, .ftb-slide-leave-to {
  max-height: 0;
  opacity: 0;
  padding-top: 0;
  padding-bottom: 0;
}
.ftb-slide-enter-to, .ftb-slide-leave-from {
  max-height: 60px;
  opacity: 1;
}

/* ════════════════════════════════════════════════════════════════
   PROPERTIES PANEL  (panneau droit intégré — remplace fields-panel + cfg-drawer)
   ════════════════════════════════════════════════════════════════ */
.pp {
  /* width is driven by inline style (ppWidth ref); 280px is the fallback */
  width: 280px;
  min-width: 200px;
  max-width: 560px;
  flex-shrink: 0;
  background: var(--sidebar-bg, #FFFFFF);
  border-left: 1px solid rgba(27,107,58,0.12);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  transition: width 0.25s cubic-bezier(0.16,1,0.3,1), background 0.4s;
  position: relative;
  font-family: 'DM Sans', sans-serif;
}
/* While the user is actively dragging, kill the transition so it tracks 1:1 */
.pp--resizing { transition: background 0.4s !important; }
.pp--collapsed { width: 36px !important; }

/* ── Drag-to-resize handle ── */
.pp-resize-handle {
  position: absolute;
  top: 0; left: 0;
  width: 5px;
  height: 100%;
  cursor: col-resize;
  z-index: 10;
  /* Invisible by default; subtle highlight on hover */
  background: transparent;
  transition: background 0.15s;
}
.pp-resize-handle:hover,
.pp--resizing .pp-resize-handle {
  background: rgba(27,107,58,0.25);
}

/* Toggle */
.pp-toggle {
  position: absolute;
  top: 12px; right: 8px;
  width: 22px; height: 22px;
  border-radius: 6px;
  border: 1px solid rgba(17,23,20,0.08);
  background: rgba(17,23,20,0.03);
  color: rgba(255,255,255,0.4);
  cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  font-size: 10px;
  z-index: 2;
  transition: background 0.15s, color 0.15s;
}
.pp-toggle:hover { background: rgba(27,107,58,0.15); color: #1B6B3A; }

/* Collapsed hint */
.pp-collapsed-hint {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  padding-top: 50px;
  color: #C4CFC7;
}
.pp-collapsed-hint i { font-size: 14px; }
.pp-collapsed-hint span {
  writing-mode: vertical-rl;
  font-size: 9px;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

/* Header variants */
.pp-header {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 12px;
  border-bottom: 1px solid rgba(27,107,58,0.06);
  flex-shrink: 0;
  padding-right: 40px;
}
.pp-header--widget {
  background: rgba(27,107,58,0.04);
}
.pp-widget-badge {
  width: 26px; height: 26px;
  border-radius: 7px;
  display: flex; align-items: center; justify-content: center;
  font-size: 12px;
  flex-shrink: 0;
}
.pp-title-input {
  flex: 1;
  background: transparent;
  border: none;
  outline: none;
  color: #111714;
  font-size: 13px;
  font-weight: 600;
  font-family: inherit;
  min-width: 0;
}
.pp-title-input::placeholder { color: rgba(255,255,255,0.3); }
.pp-close-btn {
  width: 22px; height: 22px;
  border-radius: 5px;
  border: none;
  background: rgba(27,107,58,0.06);
  color: rgba(255,255,255,0.3);
  cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  font-size: 10px;
  transition: background 0.15s, color 0.15s;
  flex-shrink: 0;
}
.pp-close-btn:hover { background: rgba(239,68,68,0.15); color: #f87171; }
.pp-header--idle {
  gap: 8px;
}
.pp-idle-title {
  flex: 1;
  font-size: 12px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: rgba(255,255,255,0.55);
}

/* Tabs */
.pp-tabs {
  display: flex;
  border-bottom: 1px solid rgba(27,107,58,0.06);
  flex-shrink: 0;
}
.pp-tab {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
  padding: 8px 4px;
  border: none;
  background: transparent;
  color: #94A99A;
  font-size: 9px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  cursor: pointer;
  border-bottom: 2px solid transparent;
  transition: color 0.15s, border-color 0.15s, background 0.15s;
  font-family: inherit;
}
.pp-tab i { font-size: 13px; }
.pp-tab:hover { color: rgba(255,255,255,0.65); background: rgba(17,23,20,0.02); }
.pp-tab.active { color: #1B6B3A; border-bottom-color: #1B6B3A; background: rgba(27,107,58,0.05); }

/* Body */
.pp-body {
  flex: 1;
  overflow-y: auto;
  padding: 0;
}
.pp-body::-webkit-scrollbar { width: 3px; }
.pp-body::-webkit-scrollbar-track { background: transparent; }
.pp-body::-webkit-scrollbar-thumb { background: rgba(17,23,20,0.08); border-radius: 2px; }

/* Section (non-collapsible) */
.pp-section {
  border-bottom: 1px solid rgba(17,23,20,0.03);
  padding: 12px;
}
.pp-section:last-child { border-bottom: none; }

.pp-section-title {
  font-size: 10px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: rgba(255,255,255,0.45);
  margin-bottom: 10px;
  display: block;
}

/* Collapsible sections */
.pp-collapsible {
  padding: 0;
}
.pp-section-toggle {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 12px;
  cursor: pointer;
  user-select: none;
  transition: background 0.12s;
}
.pp-section-toggle:hover { background: rgba(17,23,20,0.02); }
.pp-section-toggle .pp-section-title { margin-bottom: 0; }
.pp-section-toggle i { font-size: 10px; color: rgba(255,255,255,0.3); flex-shrink: 0; }

.pp-section-body {
  padding: 4px 12px 12px;
  border-top: 1px solid rgba(17,23,20,0.03);
}

/* Footer */
.pp-footer {
  display: flex;
  gap: 8px;
  padding: 10px 12px;
  border-top: 1px solid rgba(17,23,20,0.08);
  flex-shrink: 0;
  background: rgba(0,0,0,0.1);
}
.pp-footer .cfg-btn-cancel { flex: 1; }
.pp-footer .cfg-btn-apply  { flex: 2; }

/* ── Champs — mesures calculées ─────────────────────────────────────── */
.fields-group--calc {
  border-top: 1px solid rgba(27,107,58,0.06);
  margin-top: 4px;
}

.fgroup-icon--calc {
  background: rgba(245,158,11,0.12);
  color: #f59e0b;
  border-color: rgba(245,158,11,0.2);
}

.fgroup-add-btn {
  margin-left: auto;
  width: 20px; height: 20px;
  border-radius: 5px;
  border: 1px solid rgba(255,255,255,0.1);
  background: rgba(17,23,20,0.03);
  color: rgba(255,255,255,0.4);
  cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  font-size: 10px;
  transition: background 0.15s, color 0.15s, border-color 0.15s;
}
.fgroup-add-btn:hover, .fgroup-add-btn.active {
  background: rgba(245,158,11,0.18);
  color: #f59e0b;
  border-color: rgba(245,158,11,0.35);
}

/* Formulaire */
.new-measure-form {
  margin: 0 10px 10px;
  background: rgba(245,158,11,0.04);
  border: 1px solid rgba(245,158,11,0.15);
  border-radius: 8px;
  padding: 10px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.nm-name {
  font-size: 12px !important;
  font-weight: 600;
  border-color: rgba(245,158,11,0.25) !important;
}
.nm-name:focus { border-color: rgba(245,158,11,0.5) !important; }

.nm-row {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.nm-row > label:first-child {
  font-size: 9px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: rgba(255,255,255,0.4);
}
.nm-pct-row { flex-direction: row; align-items: center; }

.nm-toggle {
  font-size: 10px !important;
  gap: 6px !important;
}
.nm-toggle span:first-child { font-size: 10px !important; color: rgba(255,255,255,0.6) !important; }

.nm-actions {
  display: flex;
  gap: 6px;
  margin-top: 2px;
}
.nm-cancel {
  flex: 1;
  padding: 5px 8px;
  border-radius: 6px;
  border: 1px solid rgba(255,255,255,0.1);
  background: rgba(17,23,20,0.03);
  color: rgba(255,255,255,0.5);
  font-size: 11px;
  cursor: pointer;
  font-family: inherit;
  transition: background 0.15s;
}
.nm-cancel:hover { background: rgba(17,23,20,0.08); }
.nm-create {
  flex: 2;
  padding: 5px 10px;
  border-radius: 6px;
  border: none;
  background: rgba(245,158,11,0.2);
  color: #fbbf24;
  font-size: 11px;
  font-weight: 600;
  cursor: pointer;
  font-family: inherit;
  display: flex; align-items: center; justify-content: center; gap: 4px;
  transition: background 0.15s;
}
.nm-create:hover:not(:disabled) { background: rgba(245,158,11,0.32); }
.nm-create:disabled { opacity: 0.35; cursor: not-allowed; }

/* Chip mesure calculée */
.field-item--calc { border-left: 2px solid rgba(245,158,11,0.4) !important; }
.fi-type--calc {
  background: rgba(245,158,11,0.12);
  color: #f59e0b;
  font-family: monospace;
  font-size: 8px !important;
  font-weight: 700;
  letter-spacing: -0.03em;
}
.fi-delete-btn {
  margin-left: auto;
  width: 16px; height: 16px;
  border-radius: 4px;
  border: none;
  background: transparent;
  color: rgba(17,23,20,0.15);
  cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  font-size: 8px;
  transition: background 0.12s, color 0.12s;
  flex-shrink: 0;
}
.fi-delete-btn:hover { background: rgba(239,68,68,0.2); color: #f87171; }

.nm-empty-hint {
  padding: 8px 12px;
  font-size: 10px;
  color: #C4CFC7;
  text-align: center;
}
.nm-empty-hint strong { color: rgba(245,158,11,0.6); }

/* ── Light-theme overrides → see src/assets/main.css ── */

/* ── Mobile ─────────────────────────────────────────────── */
@media (max-width: 768px) {
  .builder-layout {
    flex-direction: column;
    height: auto;
    min-height: 100vh;
  }

  .sidebar {
    width: 100% !important;
    max-width: none;
    min-width: 0;
    height: auto;
    max-height: 45vh;
    overflow-y: auto;
    border-right: none;
    border-bottom: 1px solid #E4E8E4;
  }

  .sidebar-resize-handle { display: none; }

  .canvas-container {
    min-height: 55vh;
    overflow-y: auto;
  }

  .canvas-header { padding: 10px 14px; flex-wrap: wrap; gap: 8px; }

  .widget-grid { grid-template-columns: repeat(auto-fill, minmax(140px, 1fr)); gap: 8px; }
}

/* ── Mobile preview ─────────────────────────────────────── */
.canvas-grid--mobile {
  max-width: 390px;
  margin: 0 auto;
  grid-template-columns: repeat(4, 1fr) !important;
  border: 2px solid #1B6B3A;
  border-radius: 24px;
  padding: 16px;
  box-shadow: 0 0 0 6px rgba(27,107,58,0.08);
}

.mobile-preview-banner {
  position: fixed;
  bottom: 16px;
  left: 50%;
  transform: translateX(-50%);
  background: #111714;
  color: #F0F9F4;
  font-size: 12px;
  font-weight: 500;
  padding: 8px 16px;
  border-radius: 20px;
  display: flex;
  align-items: center;
  gap: 8px;
  z-index: 999;
  box-shadow: 0 4px 20px rgba(0,0,0,0.3);
}

/* ── Auto-refresh menu ───────────────────────────────────── */
.auto-refresh-menu {
  position: absolute;
  top: calc(100% + 6px);
  right: 0;
  background: var(--color-surface, #fff);
  border: 1px solid var(--color-border, #E4E8E4);
  border-radius: 10px;
  padding: 6px;
  z-index: 200;
  min-width: 130px;
  box-shadow: 0 4px 16px rgba(0,0,0,0.12);
}
.arm-title {
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 0.8px;
  color: var(--color-text-muted, #94A99A);
  padding: 4px 8px 6px;
}
.arm-opt {
  display: block;
  width: 100%;
  text-align: left;
  background: none;
  border: none;
  padding: 6px 10px;
  border-radius: 6px;
  font-size: 12px;
  cursor: pointer;
  color: var(--color-text, #111714);
}
.arm-opt:hover { background: rgba(27,107,58,0.08); color: #1B6B3A; }
.arm-opt.active { background: rgba(27,107,58,0.12); color: #1B6B3A; font-weight: 600; }

/* ── Templates modal ─────────────────────────────────────── */
.tpl-modal {
  max-width: 760px;
  width: 90vw;
  max-height: 85vh;
  overflow-y: auto;
  padding: 0;
}
.tpl-header {
  padding: 24px 24px 16px;
  border-bottom: 1px solid var(--color-border, #E4E8E4);
  position: relative;
}
.tpl-header h3 { font-size: 17px; font-weight: 700; margin: 0 0 6px; }
.tpl-sub { font-size: 12px; color: var(--color-text-muted, #94A99A); margin: 0; line-height: 1.5; }
.tpl-close {
  position: absolute; top: 20px; right: 20px;
  background: none; border: none; cursor: pointer;
  color: var(--color-text-muted, #94A99A);
  padding: 4px; border-radius: 6px;
}
.tpl-close:hover { background: rgba(0,0,0,0.06); }
.tpl-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
  padding: 20px;
}
.tpl-card {
  border: 1px solid var(--color-border, #E4E8E4);
  border-radius: 12px;
  padding: 16px;
  cursor: pointer;
  transition: all 0.15s;
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.tpl-card:hover { border-color: #1B6B3A; background: rgba(27,107,58,0.04); }
.tpl-name { font-size: 14px; font-weight: 700; color: var(--color-text, #111714); }
.tpl-desc { font-size: 11px; color: var(--color-text-muted, #94A99A); line-height: 1.4; }
.tpl-chips { display: flex; flex-wrap: wrap; gap: 4px; }
.tpl-chip {
  font-size: 10px; font-weight: 600;
  background: rgba(27,107,58,0.1); color: #1B6B3A;
  padding: 2px 7px; border-radius: 20px;
}
.tpl-apply-btn {
  margin-top: 4px;
  background: #1B6B3A; color: #fff;
  border: none; border-radius: 8px;
  padding: 8px 14px; font-size: 12px; font-weight: 600;
  cursor: pointer;
  transition: background 0.15s;
}
.tpl-apply-btn:hover { background: #134E2A; }
</style>







