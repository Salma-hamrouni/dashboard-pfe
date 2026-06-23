<template>
  <!--
    isSharedView → standalone full-page layout sans sidebar
    !isSharedView → AppLayout habituel avec sidebar
  -->
  <component
    :is="isSharedView ? 'div' : AppLayout"
    :class="isSharedView ? 'shared-fullpage' : undefined"
  >

    <!-- ── Barre supérieure pour la vue partagée ──────────────────── -->
    <header v-if="isSharedView" class="shared-topbar">
      <div class="shared-logo">
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="var(--color-primary)" stroke-width="2">
          <line x1="18" y1="20" x2="18" y2="10"/><line x1="12" y1="20" x2="12" y2="4"/>
          <line x1="6"  y1="20" x2="6"  y2="14"/><line x1="2" y1="20" x2="22" y2="20"/>
        </svg>
        <span class="shared-logo-name">DashGen</span>
        <span class="shared-logo-sep">·</span>
        <span class="shared-dash-name">{{ dashboard?.name || 'Dashboard partagé' }}</span>
      </div>
      <div class="shared-topbar-right">
        <span class="shared-readonly-tag">
          <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
            <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
            <circle cx="12" cy="12" r="3"/>
          </svg>
          Lecture seule
        </span>
      </div>
    </header>

    <div class="viewer-page" :class="{ 'viewer-page--shared': isSharedView }">

      <!-- Loading -->
      <div v-if="isLoading" class="state-center">
        <div class="ds-spinner"></div>
        <p class="text-muted">Chargement…</p>
      </div>

      <!-- Not found -->
      <div v-else-if="!dashboard" class="state-center">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="rgba(27,107,58,.35)" stroke-width="1.5">
          <circle cx="11" cy="11" r="8"/><path d="m21 21-4.35-4.35"/>
        </svg>
        <p class="state-title">Dashboard introuvable</p>
        <button class="ds-btn ds-btn-secondary" @click="router.push('/dashboard')">← Retour à la liste</button>
      </div>

      <template v-else>
        <!-- Header owner (masqué pour les vues partagées dont le titre est dans la topbar) -->
        <div v-if="!isSharedView" class="viewer-header">
          <button class="back-btn" @click="router.push('/dashboard')">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
              <polyline points="15 18 9 12 15 6"/>
            </svg>
            Mes Dashboards
          </button>
          <div class="header-main">
            <div class="header-title-block">
              <h1 class="viewer-title">{{ dashboard.name || 'Sans titre' }}</h1>
              <div class="viewer-meta">
                <span>
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/>
                    <rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/>
                  </svg>
                  {{ dashboard.widgets?.length ?? 0 }} widget(s)
                </span>
                <span>
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <rect x="3" y="4" width="18" height="18" rx="2"/><line x1="16" y1="2" x2="16" y2="6"/>
                    <line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/>
                  </svg>
                  {{ formatDate(dashboard.createdAt) }}
                </span>
                <span v-if="dashboard.isPublic" class="ds-badge ds-badge-green">
                  <svg width="8" height="8" viewBox="0 0 24 24" fill="currentColor"><circle cx="12" cy="12" r="10"/></svg>
                  Publié
                </span>
              </div>
            </div>
            <div class="header-actions">
              <button v-if="dashboard.shareToken && !isViewer" class="ds-btn ds-btn-secondary ds-btn-sm" @click="copyShareLink">
                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <circle cx="18" cy="5" r="3"/><circle cx="6" cy="12" r="3"/><circle cx="18" cy="19" r="3"/>
                  <line x1="8.59" y1="13.51" x2="15.42" y2="17.49"/><line x1="15.41" y1="6.51" x2="8.59" y2="10.49"/>
                </svg>
                {{ copied ? 'Copié !' : 'Partager' }}
              </button>
              <button v-if="!isViewer" class="ds-btn ds-btn-primary ds-btn-sm" @click="router.push(`/builder/${dashboard.id}`)">
                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                  <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                </svg>
                Éditer
              </button>
            </div>
          </div>
        </div>

        <!-- AI Insights strip -->
        <div v-if="dashboard.insights?.length" class="insights-strip">
          <div class="insights-label">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/>
            </svg>
            Insights IA
          </div>
          <div class="insights-chips">
            <span v-for="(ins, i) in dashboard.insights" :key="i" class="insight-chip">{{ ins }}</span>
          </div>
        </div>

        <!-- ── Filtres ── -->
        <div v-if="dashboard.widgets?.length" class="vf-bar">

          <!-- Ligne principale -->
          <div class="vf-main-row">

            <!-- Recherche -->
            <div class="vf-search">
              <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="11" cy="11" r="8"/><path d="m21 21-4.35-4.35"/>
              </svg>
              <input v-model="wfSearch" type="text" placeholder="Rechercher un widget…" class="vf-input" />
              <button v-if="wfSearch" class="vf-clear" @click="wfSearch = ''">✕</button>
            </div>

            <!-- Types (pills) -->
            <div class="vf-pills-row">
              <button
                v-for="t in availableTypes" :key="t"
                class="vf-pill" :class="{ active: wfTypes.includes(t) }"
                @click="toggleType(t)"
              >
                <component :is="typeIcon(t)" style="width:11px;height:11px" />
                {{ t }}
              </button>
            </div>

            <!-- Tri -->
            <div class="vf-sort">
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="8" y1="6" x2="21" y2="6"/><line x1="8" y1="12" x2="21" y2="12"/>
                <line x1="8" y1="18" x2="21" y2="18"/>
                <line x1="3" y1="6"  x2="3.01" y2="6"/>
                <line x1="3" y1="12" x2="3.01" y2="12"/>
                <line x1="3" y1="18" x2="3.01" y2="18"/>
              </svg>
              <select v-model="wfSort" class="vf-select">
                <option value="default">Ordre par défaut</option>
                <option value="title-asc">Titre A → Z</option>
                <option value="title-desc">Titre Z → A</option>
                <option value="type">Par type</option>
              </select>
            </div>

            <!-- Bouton filtres avancés -->
            <button class="vf-adv-btn" :class="{ active: showAdvFilters }" @click="showAdvFilters = !showAdvFilters">
              <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polygon points="22 3 2 3 10 12.46 10 19 14 21 14 12.46 22 3"/>
              </svg>
              Filtres avancés
              <span v-if="advFilterCount > 0" class="vf-adv-count">{{ advFilterCount }}</span>
            </button>

            <!-- Reset global -->
            <button v-if="activeWfCount > 0" class="vf-reset" @click="resetWf">
              <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="1 4 1 10 7 10"/><path d="M3.51 15a9 9 0 1 0 .49-4.5"/>
              </svg>
              Réinitialiser ({{ activeWfCount }})
            </button>
          </div>

          <!-- Panneau filtres avancés -->
          <Transition name="vf-adv">
            <div v-if="showAdvFilters" class="vf-advanced">

              <!-- Plage KPI -->
              <div class="vf-adv-group" v-if="hasKpi">
                <span class="vf-adv-label">
                  <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/>
                  </svg>
                  Valeur KPI
                </span>
                <div class="vf-range">
                  <input v-model.number="wfMin" type="number" class="vf-num" placeholder="Min" />
                  <span class="vf-sep">–</span>
                  <input v-model.number="wfMax" type="number" class="vf-num" placeholder="Max" />
                </div>
              </div>

              <!-- Filtre colonne (si colonnes disponibles) -->
              <div class="vf-adv-group" v-if="availableColumns.length">
                <span class="vf-adv-label">
                  <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <line x1="8" y1="6" x2="21" y2="6"/><line x1="8" y1="12" x2="21" y2="12"/>
                    <line x1="8" y1="18" x2="21" y2="18"/>
                  </svg>
                  Colonne
                </span>
                <select v-model="wfColumn" class="vf-select">
                  <option value="">Toutes</option>
                  <option v-for="col in availableColumns" :key="col" :value="col">{{ col }}</option>
                </select>
              </div>

              <!-- Filtre mot-clé dans les données -->
              <div class="vf-adv-group">
                <span class="vf-adv-label">
                  <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <circle cx="11" cy="11" r="8"/><path d="m21 21-4.35-4.35"/>
                  </svg>
                  Données contiennent
                </span>
                <input v-model="wfDataKeyword" type="text" class="vf-adv-input" placeholder="Ex: Paris, 2024…" />
              </div>

              <!-- Taille de la grille -->
              <div class="vf-adv-group">
                <span class="vf-adv-label">
                  <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/>
                    <rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/>
                  </svg>
                  Affichage
                </span>
                <div class="vf-grid-size">
                  <button
                    v-for="s in gridSizes" :key="s.val"
                    class="vf-gs-btn" :class="{ active: wfGridSize === s.val }"
                    :title="s.label"
                    @click="wfGridSize = s.val"
                  >
                    <component :is="s.icon" style="width:12px;height:12px" />
                  </button>
                </div>
              </div>

            </div>
          </Transition>

          <!-- Tags filtres actifs -->
          <div v-if="activeWfCount > 0" class="vf-active-tags">
            <span class="vf-at-label">Filtres actifs :</span>
            <span v-if="wfSearch" class="vf-tag" @click="wfSearch = ''">
              🔍 "{{ wfSearch }}" ✕
            </span>
            <span v-for="t in wfTypes" :key="t" class="vf-tag" @click="toggleType(t)">
              {{ t }} ✕
            </span>
            <span v-if="wfMin !== null || wfMax !== null" class="vf-tag" @click="wfMin = null; wfMax = null">
              KPI {{ wfMin ?? '…' }} – {{ wfMax ?? '…' }} ✕
            </span>
            <span v-if="wfColumn" class="vf-tag" @click="wfColumn = ''">
              col: {{ wfColumn }} ✕
            </span>
            <span v-if="wfDataKeyword" class="vf-tag" @click="wfDataKeyword = ''">
              data: "{{ wfDataKeyword }}" ✕
            </span>
          </div>
        </div>

        <!-- Widgets grid -->
        <div v-if="filteredWidgets.length" class="widgets-grid" :class="`widgets-grid--${wfGridSize}`">
          <div
            v-for="widget in filteredWidgets"
            :key="widget.id"
            class="widget-card ds-card"
            :ref="el => widgetRefs[widget.id] = el as HTMLElement"
          >
            <div class="wc-header">
              <div class="wc-type-icon" :style="{ color: typeColor(widget.type) }">
                <component :is="typeIcon(widget.type)" />
              </div>
              <div class="wc-title-block">
                <h3 class="wc-title">{{ widget.title }}</h3>
                <p v-if="widgetDescription(widget)" class="wc-description">
                  {{ widgetDescription(widget) }}
                </p>
              </div>
              <span class="wc-badge">{{ widget.type }}</span>
              <button class="wc-export-btn" @click="exportWidgetPng(widget.id, widget.title)" title="Exporter en PNG">
                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
                  <polyline points="7 10 12 15 17 10"/>
                  <line x1="12" y1="15" x2="12" y2="3"/>
                </svg>
              </button>
            </div>
            <div class="wc-body">
              <!-- KPI -->
              <KpiCardWidget
                v-if="widget.type === 'kpi'"
                v-bind="parseKpi(widget.data)"
              />
              <!-- Bar -->
              <BarChartWidget
                v-else-if="widget.type === 'bar'"
                :data="parseChartData(widget.data)"
              />
              <!-- Line -->
              <LineChartWidget
                v-else-if="widget.type === 'line'"
                :data="parseChartData(widget.data)"
              />
              <!-- Pie / Doughnut -->
              <PieChartWidget
                v-else-if="widget.type === 'pie' || widget.type === 'doughnut'"
                :data="parseChartData(widget.data)"
                :donut="widget.type === 'doughnut'"
              />
              <!-- Scatter -->
              <ScatterWidget
                v-else-if="widget.type === 'scatter'"
                :data="parseScatterData(widget.data)"
              />
              <!-- Funnel -->
              <FunnelWidget
                v-else-if="widget.type === 'funnel'"
                :data="parseChartData(widget.data)"
              />
              <!-- Box Plot -->
              <BoxPlotWidget
                v-else-if="widget.type === 'boxplot'"
                :data="parseBoxPlotData(widget.data)"
              />
              <!-- Table -->
              <TableWidget
                v-else-if="widget.type === 'table'"
                v-bind="parseTableData(widget.data)"
                :page-size="parseWidgetConfig(widget.config).tablePageSize ?? 10"
                :show-search="parseWidgetConfig(widget.config).tableShowSearch !== false"
              />
              <!-- Heatmap -->
              <HeatmapWidget
                v-else-if="widget.type === 'heatmap'"
                v-bind="parseHeatmapData(widget.data)"
              />
              <!-- Image -->
              <ImageWidget
                v-else-if="widget.type === 'image'"
                :src="parseImageData(widget.data).src"
                :fit="parseImageData(widget.data).fit"
                :alt-text="parseImageData(widget.data).altText"
              />
              <!-- Text / Note -->
              <TextWidget
                v-else-if="widget.type === 'text'"
                :content="parseTextData(widget.data).textContent"
                :font-size="parseTextData(widget.data).textFontSize"
                :align="parseTextData(widget.data).textAlign"
                :color="parseTextData(widget.data).color"
                :font-family="parseTextData(widget.data).textFontFamily"
              />
              <!-- Treemap -->
              <TreemapWidget
                v-else-if="widget.type === 'treemap'"
                :data="parseChartData(widget.data)"
              />
              <!-- Map -->
              <MapWidget
                v-else-if="widget.type === 'map'"
                :data="parseMapData(widget.data)"
                :marker-color="parseWidgetConfig(widget.data_config).markerColor || '#3b82f6'"
                :zoom="parseWidgetConfig(widget.data_config).mapZoom || 5"
                :center-lat="parseWidgetConfig(widget.data_config).mapCenterLat || 46.5"
                :center-lon="parseWidgetConfig(widget.data_config).mapCenterLon || 2.3"
              />
              <!-- Unknown -->
              <div v-else class="wc-raw">
                <pre>{{ formatWidgetData(widget.data) }}</pre>
              </div>
            </div>
          </div>
        </div>

        <!-- No filter results -->
        <div v-else-if="dashboard.widgets?.length && filteredWidgets.length === 0" class="empty-widgets ds-empty">
          <div class="ds-empty-icon">🔍</div>
          <h3>Aucun widget trouvé</h3>
          <p>Aucun widget ne correspond aux filtres sélectionnés.</p>
          <button class="ds-btn ds-btn-secondary" @click="resetWf">Réinitialiser les filtres</button>
        </div>

        <!-- Empty widgets -->
        <div v-else-if="!dashboard.widgets?.length" class="empty-widgets ds-empty">
          <div class="ds-empty-icon">📊</div>
          <h3>Aucun widget</h3>
          <p v-if="!isSharedView">Ce dashboard n'a pas encore de widgets. Cliquez sur Éditer pour en ajouter.</p>
          <p v-else>Ce dashboard ne contient pas encore de widgets.</p>
          <button v-if="!isSharedView && !isViewer" class="ds-btn ds-btn-primary" @click="router.push(`/builder/${dashboard.id}`)">
            Ajouter des widgets →
          </button>
        </div>

        <!-- ── Analyse Narrative ────────────────────────────────── -->
        <div class="narrative-section">
          <div class="narrative-header">
            <div class="narrative-header-left">
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="var(--color-primary)" stroke-width="2">
                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                <polyline points="14 2 14 8 20 8"/>
                <line x1="16" y1="13" x2="8" y2="13"/>
                <line x1="16" y1="17" x2="8" y2="17"/>
                <polyline points="10 9 9 9 8 9"/>
              </svg>
              <h2 class="ds-section-title" style="margin:0">Analyse Narrative</h2>
              <span class="narrative-badge">✨ Gemini IA</span>
            </div>
            <div class="narrative-header-right">
              <button
                v-if="narrativeText"
                class="narrative-copy-btn"
                @click="copyNarrativeViewer"
                :title="narrativeCopiedViewer ? 'Copié !' : 'Copier le rapport'"
              >
                <svg v-if="!narrativeCopiedViewer" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="9" y="9" width="13" height="13" rx="2"/>
                  <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/>
                </svg>
                <svg v-else width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="var(--color-primary)" stroke-width="2.5">
                  <polyline points="20 6 9 17 4 12"/>
                </svg>
                {{ narrativeCopiedViewer ? 'Copié !' : 'Copier' }}
              </button>
              <button
                class="narrative-gen-btn"
                :disabled="narrativeLoading"
                @click="generateNarrative"
              >
                <svg v-if="!narrativeLoading" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/>
                </svg>
                <svg v-else width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="spin">
                  <path d="M21 12a9 9 0 1 1-6.219-8.56"/>
                </svg>
                {{ narrativeLoading ? 'Génération…' : (narrativeText ? 'Regénérer' : 'Générer l\'analyse') }}
              </button>
            </div>
          </div>

          <!-- État vide -->
          <div v-if="!narrativeText && !narrativeLoading && !narrativeError" class="narrative-empty">
            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="rgba(27,107,58,.3)" stroke-width="1.5">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
              <polyline points="14 2 14 8 20 8"/>
              <line x1="16" y1="13" x2="8" y2="13"/>
              <line x1="16" y1="17" x2="8" y2="17"/>
            </svg>
            <p>Cliquez sur <strong>Générer l'analyse</strong> pour obtenir un rapport narratif complet de ce dashboard.</p>
          </div>

          <!-- Chargement -->
          <div v-if="narrativeLoading" class="narrative-loading">
            <div class="narrative-dots"><span/><span/><span/></div>
            <p>Gemini analyse votre dashboard…</p>
          </div>

          <!-- Erreur -->
          <div v-if="narrativeError && !narrativeLoading" class="narrative-error">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
            </svg>
            {{ narrativeError }}
          </div>

          <!-- Contenu narratif -->
          <div v-if="narrativeText && !narrativeLoading" class="narrative-content">
            <div class="narrative-meta">
              <span>
                <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="3" y="4" width="18" height="18" rx="2"/><line x1="16" y1="2" x2="16" y2="6"/>
                  <line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/>
                </svg>
                {{ new Date().toLocaleDateString('fr-FR', { day: 'numeric', month: 'long', year: 'numeric' }) }}
              </span>
              <span>
                <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/>
                  <rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/>
                </svg>
                {{ dashboard.widgets?.length ?? 0 }} widget(s)
              </span>
              <span v-if="dashboard.columns?.length">
                <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <line x1="8" y1="6" x2="21" y2="6"/><line x1="8" y1="12" x2="21" y2="12"/>
                  <line x1="8" y1="18" x2="21" y2="18"/>
                  <line x1="3" y1="6" x2="3.01" y2="6"/><line x1="3" y1="12" x2="3.01" y2="12"/><line x1="3" y1="18" x2="3.01" y2="18"/>
                </svg>
                {{ dashboard.columns.length }} colonne(s)
              </span>
            </div>
            <div class="narrative-text" v-html="renderNarrativeViewer(narrativeText)"></div>
          </div>
        </div>

        <!-- Recommendations -->
        <div v-if="dashboard.recommendations?.length" class="reco-section">
          <h2 class="ds-section-title">Recommandations</h2>
          <div class="reco-list">
            <div v-for="(reco, i) in dashboard.recommendations" :key="i" class="reco-item">
              <span class="reco-num">{{ i + 1 }}</span>
              <span>{{ reco }}</span>
            </div>
          </div>
        </div>
      </template>

    </div><!-- /viewer-page -->

    <!-- ── Assistant conversationnel ─────────────────────────────── -->
    <!-- Bouton flottant -->
    <button class="chat-fab" @click="chatOpen = !chatOpen" :class="{ active: chatOpen }" title="Assistant IA">
      <svg v-if="!chatOpen" width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/>
      </svg>
      <svg v-else width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
        <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
      </svg>
      <span v-if="unreadCount > 0 && !chatOpen" class="chat-fab-badge">{{ unreadCount }}</span>
    </button>

    <!-- Panneau de chat -->
    <Transition name="chat-panel">
      <div v-if="chatOpen" class="chat-panel">
        <!-- Header -->
        <div class="chat-header">
          <div class="chat-header-left">
            <div class="chat-avatar">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/>
              </svg>
            </div>
            <div>
              <p class="chat-title">Assistant IA</p>
              <p class="chat-subtitle">Posez vos questions sur ce dashboard</p>
            </div>
          </div>
          <div class="chat-header-actions">
            <button class="chat-clear-btn" @click="clearChat" title="Effacer la conversation">
              <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="3 6 5 6 21 6"/>
                <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
              </svg>
            </button>
            <button class="chat-close-btn" @click="chatOpen = false">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
              </svg>
            </button>
          </div>
        </div>

        <!-- Messages -->
        <div class="chat-messages" ref="chatMessagesEl">
          <!-- Message de bienvenue -->
          <div class="chat-msg chat-msg--bot">
            <div class="chat-msg-avatar">IA</div>
            <div class="chat-msg-bubble">
              Bonjour ! Je suis votre assistant pour ce dashboard
              <strong>{{ dashboard?.name }}</strong>. Je peux analyser vos données,
              expliquer vos graphiques et répondre à vos questions.
            </div>
          </div>

          <!-- Suggestions rapides -->
          <div v-if="chatMessages.length === 0" class="chat-suggestions">
            <button
              v-for="s in suggestions"
              :key="s"
              class="chat-suggestion"
              @click="sendSuggestion(s)"
            >{{ s }}</button>
          </div>

          <!-- Historique des messages -->
          <template v-for="(msg, i) in chatMessages" :key="i">
            <div :class="['chat-msg', msg.role === 'user' ? 'chat-msg--user' : 'chat-msg--bot']">
              <div v-if="msg.role === 'bot'" class="chat-msg-avatar">IA</div>
              <div class="chat-msg-bubble" v-html="formatMessage(msg.content)"></div>
              <div v-if="msg.role === 'user'" class="chat-msg-avatar chat-msg-avatar--user">
                {{ userInitials }}
              </div>
            </div>
          </template>

          <!-- Typing indicator -->
          <div v-if="isTyping" class="chat-msg chat-msg--bot">
            <div class="chat-msg-avatar">IA</div>
            <div class="chat-msg-bubble chat-msg-typing">
              <span></span><span></span><span></span>
            </div>
          </div>
        </div>

        <!-- Input -->
        <div class="chat-input-zone">
          <div class="chat-input-wrapper">
            <textarea
              v-model="chatInput"
              ref="chatInputEl"
              class="chat-input"
              placeholder="Posez une question sur vos données..."
              rows="1"
              @keydown.enter.exact.prevent="sendMessage"
              @input="autoResize"
            ></textarea>
            <button
              class="chat-send-btn"
              :disabled="!chatInput.trim() || isTyping"
              @click="sendMessage"
            >
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                <line x1="22" y1="2" x2="11" y2="13"/>
                <polygon points="22 2 15 22 11 13 2 9 22 2"/>
              </svg>
            </button>
          </div>
          <p class="chat-hint">Entrée pour envoyer · Shift+Entrée pour aller à la ligne</p>
        </div>
      </div>
    </Transition>

  </component>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, h, reactive } from 'vue'
import domtoimage from 'dom-to-image-more'
import { useRoute, useRouter } from 'vue-router'
import AppLayout from '@/components/layout/AppLayout.vue'
import BarChartWidget from '@/components/widgets/BarChartWidget.vue'
import LineChartWidget from '@/components/widgets/LineChartWidget.vue'
import PieChartWidget from '@/components/widgets/PieChartWidget.vue'
import KpiCardWidget from '@/components/widgets/KpiCardWidget.vue'
import ScatterWidget from '@/components/widgets/ScatterWidget.vue'
import FunnelWidget from '@/components/widgets/FunnelWidget.vue'
import BoxPlotWidget from '@/components/widgets/BoxPlotWidget.vue'
import TableWidget from '@/components/widgets/TableWidget.vue'
import HeatmapWidget from '@/components/widgets/HeatmapWidget.vue'
import ImageWidget from '@/components/widgets/ImageWidget.vue'
import TextWidget from '@/components/widgets/TextWidget.vue'
import TreemapWidget from '@/components/widgets/TreemapWidget.vue'
import MapWidget from '@/components/widgets/MapWidget.vue'
import type { BoxPlotPoint } from '@/types/index'
import { generateWidgetDescription } from '@/utils/widgetDescription'
import { dashboardService, type DashboardDetailDto } from '@/services/dashboardService'
import { datasetService } from '@/services/datasetService'
import { useDatasetStore } from '@/stores/dataset'
import { useDashboardStore } from '@/stores/dashboard'
import { useAuthStore } from '@/stores/auth'
import api from '@/services/api'

const route = useRoute()
const router = useRouter()
const dashboardStore = useDashboardStore()
const datasetStore   = useDatasetStore()
const authStore      = useAuthStore()

const dashboard   = ref<DashboardDetailDto | null>(null)
const isLoading   = ref(true)
const copied      = ref(false)

// ── Export PNG ───────────────────────────────────────────────────
const widgetRefs: Record<number, HTMLElement | null> = reactive({})

async function exportWidgetPng(widgetId: number, title: string) {
  const el = widgetRefs[widgetId]
  if (!el) return
  await exportToPng(el, title)
}

async function exportToPng(el: HTMLElement, title: string, bg = '#1a1a2e') {
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

/** True quand la page est ouverte via un lien de partage (/share/:token) */
const isSharedView = computed(() => route.name === 'SharedDashboard')
const isViewer     = computed(() => authStore.user?.role === 'Viewer')

// ── Filtres widgets ───────────────────────────────────────────────
const wfSearch      = ref('')
const wfTypes       = ref<string[]>([])
const wfMin         = ref<number | null>(null)
const wfMax         = ref<number | null>(null)
const wfSort        = ref<'default'|'title-asc'|'title-desc'|'type'>('default')
const wfColumn      = ref('')
const wfDataKeyword = ref('')
const wfGridSize    = ref<'compact'|'normal'|'large'>('normal')
const showAdvFilters = ref(false)

// Icônes inline pour le sélecteur de grille
const gridSizes = [
  { val: 'compact', label: 'Compact', icon: { render: () => h('svg', { viewBox:'0 0 24 24', fill:'none', stroke:'currentColor', 'stroke-width':'2' }, [h('rect',{x:'3',y:'3',width:'4',height:'4',rx:'1'}),h('rect',{x:'10',y:'3',width:'4',height:'4',rx:'1'}),h('rect',{x:'17',y:'3',width:'4',height:'4',rx:'1'}),h('rect',{x:'3',y:'10',width:'4',height:'4',rx:'1'}),h('rect',{x:'10',y:'10',width:'4',height:'4',rx:'1'}),h('rect',{x:'17',y:'10',width:'4',height:'4',rx:'1'})]) } },
  { val: 'normal',  label: 'Normal',  icon: { render: () => h('svg', { viewBox:'0 0 24 24', fill:'none', stroke:'currentColor', 'stroke-width':'2' }, [h('rect',{x:'3',y:'3',width:'8',height:'8',rx:'1'}),h('rect',{x:'13',y:'3',width:'8',height:'8',rx:'1'}),h('rect',{x:'3',y:'13',width:'8',height:'8',rx:'1'}),h('rect',{x:'13',y:'13',width:'8',height:'8',rx:'1'})]) } },
  { val: 'large',   label: 'Large',   icon: { render: () => h('svg', { viewBox:'0 0 24 24', fill:'none', stroke:'currentColor', 'stroke-width':'2' }, [h('rect',{x:'3',y:'3',width:'18',height:'8',rx:'1'}),h('rect',{x:'3',y:'13',width:'18',height:'8',rx:'1'})]) } },
]

const availableTypes = computed(() =>
  [...new Set((dashboard.value?.widgets ?? []).map((w: any) => w.type).filter(Boolean))].sort()
)

const availableColumns = computed(() => {
  const cols = (dashboard.value?.columns ?? []) as any[]
  return cols.map((c: any) => typeof c === 'string' ? c : (c.name ?? c.Name ?? '')).filter(Boolean)
})

const hasKpi = computed(() =>
  (dashboard.value?.widgets ?? []).some((w: any) => w.type === 'kpi')
)

const advFilterCount = computed(() => [
  wfMin.value !== null ? 1 : 0,
  wfMax.value !== null ? 1 : 0,
  wfColumn.value ? 1 : 0,
  wfDataKeyword.value ? 1 : 0,
].reduce((a, b) => a + b, 0))

const activeWfCount = computed(() => [
  wfSearch.value                   ? 1 : 0,
  wfTypes.value.length             ? 1 : 0,
  wfMin.value !== null             ? 1 : 0,
  wfMax.value !== null             ? 1 : 0,
  wfSort.value !== 'default'       ? 1 : 0,
  wfColumn.value                   ? 1 : 0,
  wfDataKeyword.value              ? 1 : 0,
].reduce((a, b) => a + b, 0))

function toggleType(t: string) {
  const i = wfTypes.value.indexOf(t)
  if (i >= 0) wfTypes.value.splice(i, 1)
  else wfTypes.value.push(t)
}

function resetWf() {
  wfSearch.value      = ''
  wfTypes.value       = []
  wfMin.value         = null
  wfMax.value         = null
  wfSort.value        = 'default'
  wfColumn.value      = ''
  wfDataKeyword.value = ''
}

const filteredWidgets = computed(() => {
  let widgets = [...(dashboard.value?.widgets ?? [])]

  // 1. Recherche par titre
  if (wfSearch.value) {
    const q = wfSearch.value.toLowerCase()
    widgets = widgets.filter(w => (w.title ?? '').toLowerCase().includes(q))
  }

  // 2. Filtre par type
  if (wfTypes.value.length)
    widgets = widgets.filter(w => wfTypes.value.includes(w.type))

  // 3. Plage valeur KPI
  if (wfMin.value !== null || wfMax.value !== null)
    widgets = widgets.filter(w => {
      if (w.type !== 'kpi') return true
      const val = Number(parseKpi(w.data).value)
      if (isNaN(val)) return true
      if (wfMin.value !== null && val < wfMin.value) return false
      if (wfMax.value !== null && val > wfMax.value) return false
      return true
    })

  // 4. Filtre colonne (vérifie si xAxis ou yAxis du widget contient la colonne)
  if (wfColumn.value) {
    const col = wfColumn.value.toLowerCase()
    widgets = widgets.filter(w => {
      const cfg = parseWidgetConfig(w.config ?? null)
      const x   = (cfg?.xAxis ?? '').toLowerCase()
      const y   = (cfg?.yAxis ?? '').toLowerCase()
      return x.includes(col) || y.includes(col) || (w.title ?? '').toLowerCase().includes(col)
    })
  }

  // 5. Mot-clé dans les données brutes du widget
  if (wfDataKeyword.value) {
    const kw = wfDataKeyword.value.toLowerCase()
    widgets = widgets.filter(w => (w.data ?? '').toLowerCase().includes(kw))
  }

  // 6. Tri
  if (wfSort.value === 'title-asc')
    widgets = [...widgets].sort((a, b) => (a.title ?? '').localeCompare(b.title ?? ''))
  else if (wfSort.value === 'title-desc')
    widgets = [...widgets].sort((a, b) => (b.title ?? '').localeCompare(a.title ?? ''))
  else if (wfSort.value === 'type')
    widgets = [...widgets].sort((a, b) => a.type.localeCompare(b.type))

  return widgets
})

async function loadLinkedDataset(d: DashboardDetailDto | null) {
  if (!d || !d.datasetId) return
  try {
    const res = await api.post(`/datasource/${d.datasetId}/preview`)
    const raw = res.data as any
    const cols = (raw.columns ?? []).map((c: any) => ({
      name: c.name ?? c.Name ?? '',
      type: c.type ?? c.Type ?? 'string',
    }))
    const rows = (raw.preview ?? raw.rows ?? []) as Record<string, string>[]
    datasetStore.setDataset(raw.name ?? `Dataset #${d.datasetId}`, cols, rows, raw.dataSourceId ?? d.datasetId)
  } catch { /* AI assistant stays disabled if datasource unavailable */ }
}

onMounted(async () => {
  try {
    // ── Route publique /share/:token ──────────────────────────────────
    if (route.name === 'SharedDashboard' && route.params.token) {
      dashboard.value = await dashboardService.getShared(route.params.token as string)
      await loadLinkedDataset(dashboard.value)
      return
    }

    // ── Route normale /dashboard/:id ──────────────────────────────────
    const id = Number(route.params.id)
    if (!id) { dashboard.value = null; return }

    // Admin → accès direct sans passer par le store (le store ne contient que ses propres dashboards)
    if (authStore.user?.role === 'Admin') {
      dashboard.value = await dashboardService.getById(id)
      await loadLinkedDataset(dashboard.value)
      return
    }

    // 1. Check store cache first (avoids unnecessary API call + console 404)
    const cached = dashboardStore.dashboards.find(d => d.id === id)
    if (cached) {
      // Cache hit — load full detail (widgets included) via getById
      // If getById fails (deleted/forbidden), fall through to null
      try {
        dashboard.value = await dashboardService.getById(id)
        await loadLinkedDataset(dashboard.value)
        return
      } catch { /* fall through */ }
    }

    // 2. Store empty or not found in cache → fetch list then getById
    await dashboardStore.fetchDashboards()
    const inStore = dashboardStore.dashboards.find(d => d.id === id)
    if (inStore) {
      try {
        dashboard.value = await dashboardService.getById(id)
        await loadLinkedDataset(dashboard.value)
        return
      } catch { /* fall through */ }
    }

    // 3. Not in the user's list after a successful fetch → dashboard doesn't exist
    //    or doesn't belong to this user. Set null directly — avoid a pointless
    //    GET /Dashboard/{id} that would always 404 and pollute the console.
    //    (If the list fetch itself failed, dashboardStore.error will be set —
    //    fall through to the outer catch so the user sees the error state.)
    if (!dashboardStore.error) {
      dashboard.value = null
      return
    }

    // 4. List fetch failed (network error etc.) → last-resort attempt
    dashboard.value = await dashboardService.getById(id)
    await loadLinkedDataset(dashboard.value)
  } catch {
    dashboard.value = null
  } finally {
    isLoading.value = false
  }
})

function formatDate(iso: string) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' })
}

function formatWidgetData(data: string | null) {
  if (!data) return '—'
  try { return JSON.stringify(JSON.parse(data), null, 2) } catch { return data }
}


function parseMapData(raw: string | null): { lat: number; lon: number; label?: string; value?: number }[] {
  if (!raw) return []
  try {
    const parsed = JSON.parse(raw)
    if (Array.isArray(parsed)) {
      return parsed
        .map((item: Record<string, unknown>) => ({
          lat:   Number(item.lat ?? item.latitude ?? 0),
          lon:   Number(item.lon ?? item.lng ?? item.longitude ?? 0),
          label: item.label ? String(item.label) : undefined,
          value: item.value !== undefined ? Number(item.value) : undefined,
        }))
        .filter(p => !isNaN(p.lat) && !isNaN(p.lon))
    }
  } catch { /* ignore */ }
  return []
}

// Parse stringified JSON data for chart widgets
function parseChartData(raw: string | null): { label: string; value: number }[] {
  if (!raw) return []
  try {
    const parsed = JSON.parse(raw)
    if (Array.isArray(parsed)) {
      return parsed.map((item: Record<string, unknown>) => ({
        label: String(item.label ?? item.name ?? item.x ?? ''),
        value: Number(item.value ?? item.y ?? item.count ?? 0),
      })).filter(d => d.label)
    }
  } catch { /* ignore */ }
  return []
}

// Auto-description from stored config
function widgetDescription(widget: { type: string; config?: string | null }): string {
  return generateWidgetDescription(widget.type, (widget as any).config ?? null)
}

// Parse scatter widget data (needs {x, y} pairs)
function parseScatterData(raw: string | null): { x: number; y: number; label?: string }[] {
  if (!raw) return []
  try {
    const parsed = JSON.parse(raw)
    if (Array.isArray(parsed)) {
      return parsed.map((item: Record<string, unknown>) => ({
        x:     Number(item.x  ?? item.value ?? 0),
        y:     Number(item.y  ?? item.value2 ?? 0),
        label: item.label != null ? String(item.label) : undefined,
      }))
    }
  } catch { /* ignore */ }
  return []
}

// Parse box plot data from stored JSON
function parseBoxPlotData(raw: string | null): BoxPlotPoint[] {
  if (!raw) return []
  try {
    const parsed = JSON.parse(raw)
    if (Array.isArray(parsed) && parsed.length > 0 && 'median' in (parsed[0] ?? {})) {
      return parsed.map((item: Record<string, any>) => ({
        label:    String(item.label   ?? ''),
        min:      Number(item.min     ?? 0),
        q1:       Number(item.q1      ?? 0),
        median:   Number(item.median  ?? 0),
        q3:       Number(item.q3      ?? 0),
        max:      Number(item.max     ?? 0),
        outliers: Array.isArray(item.outliers) ? item.outliers.map(Number) : [],
      }))
    }
  } catch { /* ignore */ }
  return []
}

// Parse table widget data  { columns, rows }
function parseTableData(raw: string | null): { columns: string[]; rows: (string|number)[][] } {
  if (!raw) return { columns: [], rows: [] }
  try {
    const p = JSON.parse(raw)
    if (p && Array.isArray(p.columns) && Array.isArray(p.rows)) return p
    // Fallback: array of {label, value}
    if (Array.isArray(p) && p.length && 'label' in p[0]) {
      return { columns: ['Label', 'Valeur'], rows: p.map((r: any) => [r.label, r.value]) }
    }
  } catch { /* ignore */ }
  return { columns: [], rows: [] }
}

// Parse heatmap widget data  { labels, matrix }
function parseHeatmapData(raw: string | null): { labels: string[]; matrix: number[][] } {
  if (!raw) return { labels: [], matrix: [] }
  try {
    const p = JSON.parse(raw)
    if (p && Array.isArray(p.labels) && Array.isArray(p.matrix)) return p
  } catch { /* ignore */ }
  return { labels: [], matrix: [] }
}

/** Lit la config générique d'un widget (stockée dans widget.config) */
function parseWidgetConfig(raw: string | null): Record<string, any> {
  if (!raw) return {}
  try { return JSON.parse(raw) ?? {} } catch { return {} }
}

function parseTextData(raw: string | null): { textContent: string; textFontSize: number; textAlign: 'left'|'center'|'right'; color: string; textFontFamily: string } {
  const empty = { textContent: '', textFontSize: 14, textAlign: 'left' as const, color: '', textFontFamily: 'inherit' }
  if (!raw) return empty
  try {
    const p = JSON.parse(raw)
    if (p && typeof p === 'object') {
      return {
        textContent:    p.textContent    || '',
        textFontSize:   p.textFontSize   ?? 14,
        textAlign:      p.textAlign      || 'left',
        color:          p.color          || '',
        textFontFamily: p.textFontFamily || 'inherit',
      }
    }
  } catch { /* ignore */ }
  return empty
}

function parseImageData(raw: string | null): { src: string; fit: 'contain'|'cover'|'fill'; altText: string } {
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

// Parse KPI widget data
function parseKpi(raw: string | null) {
  if (!raw) return { title: 'KPI', value: '—' }
  try {
    const p = JSON.parse(raw)
    // Format tableau : [{label, value}] — format standard utilisé par le backend
    if (Array.isArray(p) && p.length > 0) {
      const item = p[0] as Record<string, unknown>
      return {
        title:  String(item.label ?? item.name ?? 'KPI'),
        value:  item.value ?? '—',
        trend:  item.trend  !== undefined ? Number(item.trend)  : undefined,
        suffix: item.suffix as string | undefined,
        icon:   item.icon   as string | undefined,
      }
    }
    // Format objet : {title, value}
    return {
      title: p.title ?? 'KPI',
      value: p.value ?? '—',
      trend: p.trend !== undefined ? Number(p.trend) : undefined,
      suffix: p.suffix,
      icon: p.icon,
    }
  } catch {
    return { title: 'KPI', value: raw }
  }
}

const CHART_COLORS: Record<string, string> = {
  bar:      'var(--chart-2)',
  line:     'var(--chart-1)',
  pie:      'var(--chart-5)',
  doughnut: 'var(--chart-5)',
  scatter:  'var(--chart-4)',
  funnel:   'var(--chart-6)',
  boxplot:  'var(--chart-3)',
  kpi:      'var(--chart-3)',
}

function typeColor(type: string) {
  return CHART_COLORS[type] ?? 'var(--color-primary)'
}

// Inline SVG icon components per type
function typeIcon(type: string) {
  const icons: Record<string, () => unknown> = {
    bar: () => h('svg', { viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor', 'stroke-width': '2', width: '15', height: '15' }, [
      h('line', { x1: '18', y1: '20', x2: '18', y2: '10' }),
      h('line', { x1: '12', y1: '20', x2: '12', y2: '4' }),
      h('line', { x1: '6', y1: '20', x2: '6', y2: '14' }),
      h('line', { x1: '2', y1: '20', x2: '22', y2: '20' }),
    ]),
    line: () => h('svg', { viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor', 'stroke-width': '2', width: '15', height: '15' }, [
      h('polyline', { points: '22 12 18 12 15 21 9 3 6 12 2 12' }),
    ]),
    pie: () => h('svg', { viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor', 'stroke-width': '2', width: '15', height: '15' }, [
      h('path', { d: 'M21.21 15.89A10 10 0 1 1 8 2.83' }),
      h('path', { d: 'M22 12A10 10 0 0 0 12 2v10z' }),
    ]),
    doughnut: () => h('svg', { viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor', 'stroke-width': '2', width: '15', height: '15' }, [
      h('circle', { cx: '12', cy: '12', r: '10' }),
      h('circle', { cx: '12', cy: '12', r: '4' }),
    ]),
    scatter: () => h('svg', { viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor', 'stroke-width': '2', width: '15', height: '15' }, [
      h('circle', { cx: '6',  cy: '17', r: '2' }),
      h('circle', { cx: '14', cy: '8',  r: '2' }),
      h('circle', { cx: '18', cy: '15', r: '2' }),
      h('circle', { cx: '10', cy: '13', r: '2' }),
    ]),
    funnel: () => h('svg', { viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor', 'stroke-width': '2', width: '15', height: '15' }, [
      h('path', { d: 'M22 3H2l8 9.46V19l4 2v-8.54L22 3z' }),
    ]),
    boxplot: () => h('svg', { viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor', 'stroke-width': '2', width: '15', height: '15' }, [
      h('rect', { x: '9', y: '6', width: '6', height: '10', rx: '1' }),
      h('line', { x1: '12', y1: '6',  x2: '12', y2: '3'  }),
      h('line', { x1: '12', y1: '16', x2: '12', y2: '19' }),
      h('line', { x1: '9',  y1: '12', x2: '15', y2: '12' }),
      h('line', { x1: '10', y1: '3',  x2: '14', y2: '3'  }),
      h('line', { x1: '10', y1: '19', x2: '14', y2: '19' }),
    ]),
    kpi: () => h('svg', { viewBox: '0 0 24 24', fill: 'none', stroke: 'currentColor', 'stroke-width': '2', width: '15', height: '15' }, [
      h('polyline', { points: '22 7 13.5 15.5 8.5 10.5 2 17' }),
    ]),
  }
  return icons[type] ?? icons.bar
}

// ── Analyse Narrative ────────────────────────────────────────────────────────
const narrativeText          = ref('')
const narrativeLoading       = ref(false)
const narrativeError         = ref('')
const narrativeCopiedViewer  = ref(false)

async function generateNarrative() {
  if (narrativeLoading.value) return
  narrativeLoading.value = true
  narrativeError.value   = ''
  narrativeText.value    = ''

  try {
    const widgetsSummary = (dashboard.value?.widgets ?? [])
      .map((w: any) => `- ${w.title || w.type} (${w.type})`)
      .join('\n') || 'Aucun widget'

    const cols = (dashboard.value?.columns ?? []) as any[]
    const colNames = cols.map((c: any) => typeof c === 'string' ? c : (c.name ?? c.Name ?? '')).filter(Boolean)

    const insights = (dashboard.value?.insights as string[] | undefined) ?? []
    const recos    = (dashboard.value?.recommendations as string[] | undefined) ?? []

    const context = [
      `Dashboard : "${dashboard.value?.name}"`,
      `Widgets (${dashboard.value?.widgets?.length ?? 0}) :\n${widgetsSummary}`,
      colNames.length  ? `Colonnes : ${colNames.join(', ')}` : '',
      insights.length  ? `Insights : ${insights.join(' | ')}` : '',
      recos.length     ? `Recommandations existantes : ${recos.join(' | ')}` : '',
    ].filter(Boolean).join('\n\n')

    const message =
      `Génère une analyse narrative complète et professionnelle de ce tableau de bord BI.\n\n` +
      `Structure en 5 parties :\n` +
      `1. **Présentation** — décrire l'objectif probable du dashboard et son domaine métier\n` +
      `2. **Observations clés** — ce que les widgets et données révèlent\n` +
      `3. **Tendances & patterns** — évolutions, corrélations, saisonnalités\n` +
      `4. **Points d'attention** — anomalies, données manquantes, risques\n` +
      `5. **Recommandations** — actions concrètes pour améliorer le dashboard\n\n` +
      `Rédige en français, en prose fluide et professionnelle. Utilise **gras** pour les termes importants.`

    const { data } = await api.post('ai/ask', {
      message,
      dashboardName: dashboard.value?.name ?? 'Dashboard',
      context,
    })

    narrativeText.value = data?.reply ?? (typeof data === 'string' ? data : 'Analyse indisponible.')
  } catch (e: any) {
    narrativeError.value = e?.message?.length > 120 ? e.message.slice(0, 120) + '…' : (e?.message ?? 'Erreur lors de la génération.')
  } finally {
    narrativeLoading.value = false
  }
}

async function copyNarrativeViewer() {
  if (!narrativeText.value) return
  await navigator.clipboard.writeText(narrativeText.value)
  narrativeCopiedViewer.value = true
  setTimeout(() => { narrativeCopiedViewer.value = false }, 2000)
}

function renderNarrativeViewer(text: string): string {
  return text
    .replace(/^(\d+)\.\s+\*\*(.+?)\*\*/gm, '<h4 class="nv-section"><span class="nv-num">$1</span>$2</h4>')
    .replace(/^\*\*(.+?)\*\*\s*$/gm, '<h4 class="nv-section">$1</h4>')
    .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
    .replace(/\*(.*?)\*/g, '<em>$1</em>')
    .replace(/^[-•]\s+(.+)$/gm, '<li>$1</li>')
    .replace(/(<li>[\s\S]*?<\/li>)/g, '<ul class="nv-ul">$1</ul>')
    .split(/\n{2,}/)
    .map(b => b.trim().startsWith('<') ? b : `<p>${b.replace(/\n/g, '<br>')}</p>`)
    .join('\n')
}

async function copyShareLink() {
  const token = dashboard.value?.shareToken
  if (!token) return
  const url = `${window.location.origin}/share/${token}`
  await navigator.clipboard.writeText(url)
  copied.value = true
  setTimeout(() => { copied.value = false }, 2000)
}

// ── Assistant conversationnel ────────────────────────────────────────────
interface ChatMessage { role: 'user' | 'bot'; content: string }

const chatOpen       = ref(false)
const chatMessages   = ref<ChatMessage[]>([])
const chatInput      = ref('')
const isTyping       = ref(false)
const unreadCount    = ref(0)
const chatMessagesEl = ref<HTMLElement | null>(null)
const chatInputEl    = ref<HTMLTextAreaElement | null>(null)

const userInitials = computed(() => {
  const name = authStore.user?.name ?? authStore.user?.email ?? 'U'
  return name.slice(0, 2).toUpperCase()
})

const suggestions = computed(() => [
  `Quels sont les widgets de ce dashboard ?`,
  `Donne-moi un résumé des données affichées`,
  `Quelles tendances observes-tu ?`,
  `Suggère-moi des améliorations`,
])

function autoResize(e: Event) {
  const ta = e.target as HTMLTextAreaElement
  ta.style.height = 'auto'
  ta.style.height = Math.min(ta.scrollHeight, 120) + 'px'
}

function clearChat() {
  chatMessages.value = []
  unreadCount.value = 0
}

function formatMessage(text: string): string {
  // Convert markdown-like formatting to HTML
  return text
    .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
    .replace(/\*(.*?)\*/g, '<em>$1</em>')
    .replace(/`(.*?)`/g, '<code>$1</code>')
    .replace(/\n/g, '<br>')
    .replace(/^• /gm, '&bull; ')
    .replace(/^- /gm, '&bull; ')
    .replace(/^\d+\. /gm, (m) => `<strong>${m}</strong>`)
}

async function sendSuggestion(text: string) {
  chatInput.value = text
  await sendMessage()
}

async function sendMessage() {
  const text = chatInput.value.trim()
  if (!text || isTyping.value) return

  chatMessages.value.push({ role: 'user', content: text })
  chatInput.value = ''

  // Reset textarea height
  if (chatInputEl.value) {
    chatInputEl.value.style.height = 'auto'
  }

  await scrollChat()
  isTyping.value = true

  try {
    // Build context string from dashboard data
    const widgetsSummary = dashboard.value?.widgets
      ?.map((w: any) => `- ${w.title || w.type} (type: ${w.type})`)
      .join('\n') ?? 'Aucun widget'

    const rawCols = (dashboard.value?.columns ?? []) as any[]
    const colNames = rawCols
      .map((c: any) => typeof c === 'string' ? c : (c.name ?? c.Name ?? ''))
      .filter(Boolean)

    const insightsList = (dashboard.value?.insights as string[] | undefined) ?? []

    const context = [
      `Widgets (${dashboard.value?.widgets?.length ?? 0}) :\n${widgetsSummary}`,
      colNames.length  ? `Colonnes de données : ${colNames.join(', ')}` : '',
      insightsList.length ? `Insights : ${insightsList.join(' | ')}` : '',
    ].filter(Boolean).join('\n\n')

    // Call the dedicated conversational endpoint — returns plain text, no columns required
    const data = await api.post('ai/ask', {
      message:       text,
      dashboardName: dashboard.value?.name ?? 'Dashboard',
      context,
    }).then(r => r.data)

    const reply: string = data?.reply ?? (typeof data === 'string' ? data : 'Réponse reçue.')

    chatMessages.value.push({ role: 'bot', content: reply })
    if (!chatOpen.value) unreadCount.value++
  } catch (err: any) {
    chatMessages.value.push({
      role: 'bot',
      content: `Désolé, je n'ai pas pu traiter votre demande. ${err?.message ?? 'Veuillez réessayer.'}`
    })
  } finally {
    isTyping.value = false
    await scrollChat()
  }
}

async function scrollChat() {
  await new Promise(r => setTimeout(r, 50))
  if (chatMessagesEl.value) {
    chatMessagesEl.value.scrollTop = chatMessagesEl.value.scrollHeight
  }
}
</script>

<style scoped>
.viewer-page {
  padding: var(--space-8) var(--space-10);
  min-height: 100%;
  display: flex;
  flex-direction: column;
  gap: var(--space-6);
}

.state-center {
  display: flex; flex-direction: column; align-items: center;
  justify-content: center; gap: var(--space-4); min-height: 40vh;
}
.state-title { font-size: var(--text-lg); color: var(--color-text-secondary); margin: 0; }

/* Header */
.viewer-header { display: flex; flex-direction: column; gap: var(--space-4); }

.back-btn {
  display: inline-flex; align-items: center; gap: var(--space-2);
  background: none; border: none; color: var(--color-text-muted);
  font-size: var(--text-sm); cursor: pointer; padding: 0;
  transition: color var(--transition-fast); width: fit-content;
}
.back-btn:hover { color: var(--color-primary); }

.header-main {
  display: flex; justify-content: space-between;
  align-items: flex-start; gap: var(--space-4); flex-wrap: wrap;
}
.header-title-block { display: flex; flex-direction: column; gap: var(--space-2); }

.viewer-title {
  font-size: var(--text-2xl); font-weight: var(--weight-bold);
  color: var(--color-text); margin: 0; letter-spacing: -0.02em;
}

.viewer-meta {
  display: flex; align-items: center; gap: var(--space-4);
  font-size: var(--text-sm); color: var(--color-text-muted); flex-wrap: wrap;
}
.viewer-meta span { display: flex; align-items: center; gap: var(--space-1); }

.header-actions { display: flex; gap: var(--space-2); flex-shrink: 0; }

/* Insights */
.insights-strip {
  background: var(--color-primary-light);
  border: 1px solid rgba(27, 107, 58, 0.15);
  border-radius: var(--radius-lg);
  padding: var(--space-4) var(--space-5);
  display: flex; gap: var(--space-4); align-items: flex-start; flex-wrap: wrap;
}
.insights-label {
  display: flex; align-items: center; gap: var(--space-2);
  font-size: var(--text-xs); font-weight: var(--weight-bold);
  color: var(--color-primary); text-transform: uppercase;
  letter-spacing: 0.08em; white-space: nowrap; padding-top: 2px;
}
.insights-chips { display: flex; flex-wrap: wrap; gap: var(--space-2); }
.insight-chip {
  font-size: var(--text-xs); color: #134E2A;
  background: rgba(27, 107, 58, 0.10);
  border: 1px solid rgba(27, 107, 58, 0.20);
  border-radius: var(--radius-full); padding: 3px 10px;
}

/* ── Filtres viewer ─────────────────────────────────── */
.vf-bar {
  display: flex;
  flex-direction: column;
  gap: 10px;
  background: var(--color-surface-2);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: 12px 16px;
}

/* Ligne principale */
.vf-main-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px 12px;
}

.vf-search {
  display: flex; align-items: center; gap: 7px;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: 6px 11px;
  min-width: 190px; flex: 1; max-width: 280px;
  transition: border-color .15s;
}
.vf-search:focus-within { border-color: var(--color-primary); }
.vf-input {
  background: none; border: none; outline: none;
  color: var(--color-text); font-size: 12.5px; flex: 1;
}
.vf-input::placeholder { color: var(--color-text-muted); }
.vf-clear {
  background: none; border: none; cursor: pointer;
  color: var(--color-text-muted); font-size: 11px; padding: 0; line-height: 1;
}

/* Pills types */
.vf-pills-row {
  display: flex; flex-wrap: wrap; gap: 5px;
}
.vf-pill {
  display: inline-flex; align-items: center; gap: 4px;
  padding: 4px 10px; border-radius: 999px;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  color: var(--color-text-muted);
  font-size: 11.5px; font-weight: 500; cursor: pointer;
  text-transform: capitalize; transition: all .15s;
}
.vf-pill:hover  { border-color: var(--color-primary); color: var(--color-primary); }
.vf-pill.active { background: var(--color-primary); border-color: var(--color-primary); color: #fff; }

/* Tri select */
.vf-sort {
  display: flex; align-items: center; gap: 6px;
  color: var(--color-text-muted);
}
.vf-select {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  color: var(--color-text);
  font-size: 12px; padding: 5px 8px; outline: none; cursor: pointer;
  transition: border-color .15s;
}
.vf-select:focus { border-color: var(--color-primary); }

/* Bouton filtres avancés */
.vf-adv-btn {
  display: inline-flex; align-items: center; gap: 5px;
  padding: 5px 12px; border-radius: var(--radius-md);
  border: 1px solid var(--color-border);
  background: var(--color-surface);
  color: var(--color-text-muted);
  font-size: 12px; cursor: pointer;
  transition: all .15s; white-space: nowrap;
}
.vf-adv-btn:hover, .vf-adv-btn.active {
  border-color: var(--color-primary);
  color: var(--color-primary);
  background: rgba(27,107,58,.06);
}
.vf-adv-count {
  background: var(--color-primary); color: #fff;
  font-size: 10px; font-weight: 700;
  border-radius: 999px; padding: 1px 6px; min-width: 16px; text-align: center;
}

/* Reset */
.vf-reset {
  display: inline-flex; align-items: center; gap: 5px;
  margin-left: auto;
  background: none; border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  color: var(--color-text-muted); font-size: 12px; padding: 5px 12px;
  cursor: pointer; transition: all .15s; white-space: nowrap;
}
.vf-reset:hover { border-color: #f87171; color: #f87171; }

/* Panneau avancé */
.vf-advanced {
  display: flex; flex-wrap: wrap; gap: 12px 20px;
  padding: 12px 14px;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
}
.vf-adv-group {
  display: flex; align-items: center; gap: 8px; flex-shrink: 0;
}
.vf-adv-label {
  display: inline-flex; align-items: center; gap: 4px;
  font-size: 11px; font-weight: 600;
  color: var(--color-text-muted); text-transform: uppercase;
  letter-spacing: .05em; white-space: nowrap;
}
.vf-range { display: flex; align-items: center; gap: 6px; }
.vf-num {
  width: 76px; padding: 5px 8px;
  background: var(--color-surface-2);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  color: var(--color-text); font-size: 12px; outline: none;
  transition: border-color .15s;
}
.vf-num:focus { border-color: var(--color-primary); }
.vf-sep { font-size: 11px; color: var(--color-text-muted); }
.vf-adv-input {
  padding: 5px 10px; border-radius: var(--radius-md);
  background: var(--color-surface-2);
  border: 1px solid var(--color-border);
  color: var(--color-text); font-size: 12px; outline: none; width: 160px;
  transition: border-color .15s;
}
.vf-adv-input:focus { border-color: var(--color-primary); }

/* Sélecteur de taille de grille */
.vf-grid-size { display: flex; gap: 3px; }
.vf-gs-btn {
  display: flex; align-items: center; justify-content: center;
  width: 28px; height: 28px;
  border-radius: 6px;
  border: 1px solid var(--color-border);
  background: var(--color-surface-2);
  color: var(--color-text-muted);
  cursor: pointer; transition: all .15s;
}
.vf-gs-btn:hover  { border-color: var(--color-primary); color: var(--color-primary); }
.vf-gs-btn.active { background: var(--color-primary); border-color: var(--color-primary); color: #fff; }

/* Tags filtres actifs */
.vf-active-tags {
  display: flex; flex-wrap: wrap; align-items: center; gap: 6px;
  padding-top: 2px;
}
.vf-at-label {
  font-size: 11px; color: var(--color-text-muted);
  font-weight: 600; text-transform: uppercase; letter-spacing: .05em;
}
.vf-tag {
  display: inline-flex; align-items: center;
  padding: 3px 10px; border-radius: 999px;
  background: rgba(27,107,58,.08);
  border: 1px solid rgba(27,107,58,.2);
  color: var(--color-primary);
  font-size: 11.5px; cursor: pointer;
  transition: background .15s;
}
.vf-tag:hover { background: rgba(27,107,58,.16); }

/* Transition panneau avancé */
.vf-adv-enter-active, .vf-adv-leave-active { transition: opacity .2s, transform .2s; }
.vf-adv-enter-from, .vf-adv-leave-to { opacity: 0; transform: translateY(-6px); }

/* Widgets grid */

.widgets-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: var(--space-5);
}
.widgets-grid--compact {
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: var(--space-3);
}
.widgets-grid--normal {
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: var(--space-5);
}
.widgets-grid--large {
  grid-template-columns: 1fr;
  gap: var(--space-6);
}

.widget-card {
  display: flex; flex-direction: column; gap: var(--space-3);
  min-height: 220px;
}

.wc-header {
  display: flex; align-items: center; gap: var(--space-2);
  padding-bottom: var(--space-3);
  border-bottom: 1px solid var(--color-border);
}
.wc-type-icon { display: flex; align-items: center; flex-shrink: 0; }

/* Title block: stacks title + description vertically */
.wc-title-block {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 1px;
}
.wc-title {
  font-size: var(--text-sm); font-weight: var(--weight-semibold);
  color: var(--color-text); margin: 0;
  overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
}
.wc-description {
  font-size: 10px;
  color: var(--color-text-muted);
  margin: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  opacity: 0.8;
  font-style: italic;
}
.wc-badge {
  font-size: 10px; font-weight: var(--weight-bold); letter-spacing: 0.08em;
  text-transform: uppercase; background: var(--color-surface-3);
  color: var(--color-text-muted); border-radius: var(--radius-sm);
  padding: 2px 6px; flex-shrink: 0;
}
.wc-export-btn {
  display: flex; align-items: center; justify-content: center;
  width: 28px; height: 28px; flex-shrink: 0;
  background: var(--color-surface-2);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  color: var(--color-text-muted);
  cursor: pointer; transition: all .2s;
  opacity: 0;
}
.widget-card:hover .wc-export-btn { opacity: 1; }
.wc-export-btn:hover { border-color: var(--color-primary); color: var(--color-primary); background: var(--color-surface-3); }

.wc-body { flex: 1; min-height: 160px; }

.wc-raw pre {
  font-size: var(--text-xs); color: var(--color-text-muted);
  white-space: pre-wrap; word-break: break-all;
  max-height: 160px; overflow: auto; margin: 0;
}

/* Recommendations */
.reco-section { display: flex; flex-direction: column; gap: var(--space-3); }
.reco-list { display: flex; flex-direction: column; gap: var(--space-2); }
.reco-item {
  display: flex; align-items: flex-start; gap: var(--space-3);
  background: var(--color-surface-2); border: 1px solid var(--color-border);
  border-radius: var(--radius-md); padding: var(--space-3) var(--space-4);
  font-size: var(--text-sm); color: var(--color-text-secondary);
}
.reco-num {
  min-width: 22px; height: 22px; background: var(--color-primary-light);
  color: var(--color-primary); border-radius: 50%;
  display: flex; align-items: center; justify-content: center;
  font-size: var(--text-xs); font-weight: var(--weight-bold); flex-shrink: 0;
}

/* Read-only badge for shared views */
.ds-badge {
  display: inline-flex; align-items: center; gap: 4px;
  font-size: var(--text-xs); font-weight: var(--weight-semibold);
  border-radius: var(--radius-full); padding: 2px 8px;
}
.ds-badge-readonly {
  background: rgba(99, 102, 241, 0.12);
  color: #818cf8;
  border: 1px solid rgba(99, 102, 241, 0.2);
}
.ds-badge-green {
  background: rgba(27, 107, 58, 0.1);
  color: var(--color-primary);
  border: 1px solid rgba(27, 107, 58, 0.2);
}

/* ── Shared standalone layout ───────────────────────────────────── */
.shared-fullpage {
  min-height: 100vh;
  background: var(--color-bg, #0f1117);
  display: flex;
  flex-direction: column;
}

.shared-topbar {
  position: sticky;
  top: 0;
  z-index: 50;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 var(--space-8, 32px);
  height: 56px;
  background: var(--color-surface-1, #151921);
  border-bottom: 1px solid var(--color-border, rgba(255,255,255,.07));
  flex-shrink: 0;
}

.shared-logo {
  display: flex;
  align-items: center;
  gap: var(--space-2, 8px);
  font-size: var(--text-sm, 13px);
  color: var(--color-text, #e2e8f0);
  letter-spacing: -0.01em;
  min-width: 0;
}

.shared-logo-name {
  font-weight: var(--weight-bold, 700);
  font-size: var(--text-base, 15px);
  white-space: nowrap;
  flex-shrink: 0;
}

.shared-logo-sep {
  color: var(--color-text-muted, #64748b);
  flex-shrink: 0;
}

.shared-dash-name {
  font-weight: var(--weight-semibold, 600);
  font-size: var(--text-base, 15px);
  color: var(--color-text-secondary, #94a3b8);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.shared-topbar-right {
  display: flex;
  align-items: center;
  gap: var(--space-3, 12px);
}

.shared-readonly-tag {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  font-size: var(--text-xs, 11px);
  font-weight: var(--weight-semibold, 600);
  color: #a5b4fc;
  background: rgba(99, 102, 241, 0.1);
  border: 1px solid rgba(99, 102, 241, 0.18);
  border-radius: var(--radius-full, 9999px);
  padding: 4px 10px;
  letter-spacing: 0.01em;
}

/* ── Analyse Narrative ──────────────────────────────────────────────────────── */
.narrative-section {
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
  background: var(--color-surface-2);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-xl, 16px);
  padding: var(--space-5) var(--space-6);
}

.narrative-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: var(--space-3);
}
.narrative-header-left {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}
.narrative-badge {
  font-size: 10px;
  font-weight: 700;
  padding: 2px 8px;
  border-radius: var(--radius-full);
  background: rgba(27,107,58,.1);
  border: 1px solid rgba(27,107,58,.2);
  color: var(--color-primary);
  letter-spacing: .04em;
}
.narrative-header-right {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}

.narrative-gen-btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 7px 14px;
  border-radius: var(--radius-md);
  border: none;
  background: var(--color-primary);
  color: #fff;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  transition: opacity .18s, transform .15s;
  font-family: var(--font-sans);
}
.narrative-gen-btn:hover:not(:disabled) { opacity: .88; transform: translateY(-1px); }
.narrative-gen-btn:disabled { opacity: .5; cursor: not-allowed; transform: none; }

.narrative-copy-btn {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 6px 11px;
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border);
  background: var(--color-surface-3);
  color: var(--color-text-muted);
  font-size: 11px;
  cursor: pointer;
  transition: border-color .15s, color .15s;
  font-family: var(--font-sans);
}
.narrative-copy-btn:hover { border-color: var(--color-primary); color: var(--color-primary); }

/* Empty / Loading / Error */
.narrative-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: var(--space-3);
  padding: var(--space-8) var(--space-4);
  text-align: center;
}
.narrative-empty p { font-size: var(--text-sm); color: var(--color-text-muted); margin: 0; }
.narrative-empty strong { color: var(--color-primary); }

.narrative-loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-6);
}
.narrative-loading p { font-size: var(--text-sm); color: var(--color-text-muted); margin: 0; }
.narrative-dots {
  display: flex;
  gap: 6px;
}
.narrative-dots span {
  width: 8px; height: 8px;
  border-radius: 50%;
  background: var(--color-primary);
  animation: ndot 1s infinite ease-in-out;
}
.narrative-dots span:nth-child(2) { animation-delay: .18s; }
.narrative-dots span:nth-child(3) { animation-delay: .36s; }
@keyframes ndot {
  0%, 80%, 100% { transform: scale(.7); opacity: .35; }
  40%           { transform: scale(1.15); opacity: 1; }
}

.narrative-error {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  padding: var(--space-3) var(--space-4);
  border-radius: var(--radius-md);
  background: rgba(239,68,68,.08);
  border: 1px solid rgba(239,68,68,.18);
  color: #f87171;
  font-size: var(--text-sm);
}

/* Content */
.narrative-content {
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
}

.narrative-meta {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-4);
  font-size: 11px;
  color: var(--color-text-muted);
  padding: var(--space-2) 0;
  border-bottom: 1px solid var(--color-border);
}
.narrative-meta span {
  display: inline-flex;
  align-items: center;
  gap: 5px;
}

.narrative-text {
  font-size: var(--text-sm);
  line-height: 1.8;
  color: var(--color-text-secondary);
}
.narrative-text :deep(p) { margin: 0 0 .9em; }
.narrative-text :deep(h4.nv-section) {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 11px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: .07em;
  color: var(--color-primary);
  margin: 1.4em 0 .6em;
  padding-bottom: .5em;
  border-bottom: 1px solid rgba(27,107,58,.15);
}
.narrative-text :deep(.nv-num) {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 20px; height: 20px;
  background: rgba(27,107,58,.12);
  border-radius: 50%;
  font-size: 10px;
  color: var(--color-primary);
  flex-shrink: 0;
}
.narrative-text :deep(strong) { color: var(--color-text); font-weight: 600; }
.narrative-text :deep(em)     { color: var(--color-text-muted); font-style: italic; }
.narrative-text :deep(ul.nv-ul) {
  margin: .4em 0 .9em;
  padding: 0 0 0 18px;
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.narrative-text :deep(ul.nv-ul li) {
  font-size: 13px;
  color: var(--color-text-secondary);
  line-height: 1.6;
}

/* Spin animation for loading icon */
.spin { animation: spin .8s linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }

/* Viewer page inside shared layout: no sidebar offset needed */
.viewer-page--shared {
  max-width: 1400px;
  margin: 0 auto;
  width: 100%;
  padding: var(--space-8, 32px) var(--space-10, 40px);
}

/* ── Assistant conversationnel ─────────────────────────────────────── */

/* Bouton flottant */
.chat-fab {
  position: fixed;
  bottom: 28px;
  right: 28px;
  z-index: 1000;
  width: 52px;
  height: 52px;
  border-radius: 50%;
  background: var(--color-primary, #1B6B3A);
  border: none;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  box-shadow: 0 4px 20px rgba(27, 107, 58, 0.45);
  transition: transform 0.2s, box-shadow 0.2s, background 0.2s;
}
.chat-fab:hover {
  transform: scale(1.07);
  box-shadow: 0 6px 28px rgba(27, 107, 58, 0.6);
}
.chat-fab.active {
  background: #0ea5e9;
  box-shadow: 0 4px 20px rgba(14, 165, 233, 0.45);
}
.chat-fab-badge {
  position: absolute;
  top: 2px;
  right: 2px;
  background: #ef4444;
  color: #fff;
  font-size: 10px;
  font-weight: 700;
  border-radius: 50%;
  width: 16px;
  height: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  line-height: 1;
}

/* Panneau de chat */
.chat-panel {
  position: fixed;
  bottom: 92px;
  right: 28px;
  z-index: 999;
  width: 380px;
  max-height: calc(100vh - 120px);
  display: flex;
  flex-direction: column;
  background: var(--color-surface-2, #1a1f2e);
  border: 1px solid var(--color-border, rgba(255,255,255,.08));
  border-radius: 16px;
  box-shadow: 0 12px 40px rgba(0, 0, 0, 0.5), 0 2px 8px rgba(0,0,0,0.3);
  overflow: hidden;
}

/* Header */
.chat-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 14px 16px;
  background: var(--color-surface-3, #1e2435);
  border-bottom: 1px solid var(--color-border, rgba(255,255,255,.06));
  flex-shrink: 0;
}
.chat-header-left {
  display: flex;
  align-items: center;
  gap: 10px;
}
.chat-avatar {
  width: 32px;
  height: 32px;
  border-radius: 10px;
  background: linear-gradient(135deg, #1B6B3A, #134E2A);
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  flex-shrink: 0;
}
.chat-title {
  margin: 0;
  font-size: 13px;
  font-weight: 700;
  color: var(--color-text, #e2e8f0);
  line-height: 1.2;
}
.chat-subtitle {
  margin: 0;
  font-size: 10px;
  color: var(--color-text-muted, #64748b);
  line-height: 1.2;
}
.chat-header-actions {
  display: flex;
  align-items: center;
  gap: 4px;
}
.chat-clear-btn,
.chat-close-btn {
  width: 28px;
  height: 28px;
  border-radius: 8px;
  border: none;
  background: none;
  color: var(--color-text-muted, #64748b);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.15s, color 0.15s;
}
.chat-clear-btn:hover { background: rgba(239,68,68,.12); color: #f87171; }
.chat-close-btn:hover { background: var(--color-surface, rgba(255,255,255,.05)); color: var(--color-text, #e2e8f0); }

/* Messages zone */
.chat-messages {
  flex: 1;
  overflow-y: auto;
  padding: 16px 14px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-height: 200px;
  max-height: 380px;
  scrollbar-width: thin;
  scrollbar-color: rgba(255,255,255,.1) transparent;
}
.chat-messages::-webkit-scrollbar { width: 4px; }
.chat-messages::-webkit-scrollbar-thumb { background: rgba(255,255,255,.1); border-radius: 2px; }

/* Individual message */
.chat-msg {
  display: flex;
  align-items: flex-end;
  gap: 8px;
}
.chat-msg--user { flex-direction: row-reverse; }

.chat-msg-avatar {
  width: 26px;
  height: 26px;
  border-radius: 8px;
  background: linear-gradient(135deg, #1B6B3A, #134E2A);
  color: #fff;
  font-size: 10px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}
.chat-msg-avatar--user {
  background: linear-gradient(135deg, #3b82f6, #2563eb);
}

.chat-msg-bubble {
  max-width: 80%;
  padding: 9px 13px;
  border-radius: 12px;
  font-size: 12.5px;
  line-height: 1.5;
  color: var(--color-text, #e2e8f0);
  word-break: break-word;
}
.chat-msg--bot .chat-msg-bubble {
  background: var(--color-surface-3, #1e2435);
  border: 1px solid var(--color-border, rgba(255,255,255,.06));
  border-bottom-left-radius: 4px;
}
.chat-msg--user .chat-msg-bubble {
  background: var(--color-primary, #1B6B3A);
  color: #fff;
  border-bottom-right-radius: 4px;
}
.chat-msg-bubble code {
  background: rgba(255,255,255,.1);
  border-radius: 3px;
  padding: 1px 4px;
  font-size: 11px;
}

/* Typing indicator */
.chat-msg-typing {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 12px 16px;
}
.chat-msg-typing span {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: var(--color-primary, #1B6B3A);
  animation: bounce 1.2s infinite;
}
.chat-msg-typing span:nth-child(2) { animation-delay: 0.2s; }
.chat-msg-typing span:nth-child(3) { animation-delay: 0.4s; }

@keyframes bounce {
  0%, 80%, 100% { transform: translateY(0); opacity: 0.4; }
  40% { transform: translateY(-6px); opacity: 1; }
}

/* Suggestions rapides */
.chat-suggestions {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 4px 0;
}
.chat-suggestion {
  background: none;
  border: 1px solid var(--color-border, rgba(255,255,255,.08));
  border-radius: 8px;
  color: var(--color-text-secondary, #94a3b8);
  font-size: 11.5px;
  padding: 7px 12px;
  cursor: pointer;
  text-align: left;
  transition: all 0.15s;
}
.chat-suggestion:hover {
  border-color: var(--color-primary, #1B6B3A);
  color: var(--color-primary, #1B6B3A);
  background: rgba(27, 107, 58, 0.06);
}

/* Input zone */
.chat-input-zone {
  padding: 10px 12px 8px;
  border-top: 1px solid var(--color-border, rgba(255,255,255,.06));
  flex-shrink: 0;
}
.chat-input-wrapper {
  display: flex;
  align-items: flex-end;
  gap: 8px;
  background: var(--color-surface, #131720);
  border: 1px solid var(--color-border, rgba(255,255,255,.1));
  border-radius: 10px;
  padding: 8px 10px;
  transition: border-color 0.2s;
}
.chat-input-wrapper:focus-within {
  border-color: var(--color-primary, #1B6B3A);
}
.chat-input {
  flex: 1;
  background: none;
  border: none;
  outline: none;
  color: var(--color-text, #e2e8f0);
  font-size: 13px;
  line-height: 1.4;
  resize: none;
  max-height: 120px;
  overflow-y: auto;
  font-family: inherit;
}
.chat-input::placeholder { color: var(--color-text-muted, #4b5563); }

.chat-send-btn {
  width: 32px;
  height: 32px;
  border-radius: 8px;
  border: none;
  background: var(--color-primary, #1B6B3A);
  color: #fff;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  transition: background 0.2s, transform 0.15s;
}
.chat-send-btn:hover:not(:disabled) {
  background: #134E2A;
  transform: scale(1.05);
}
.chat-send-btn:disabled {
  background: var(--color-surface-3, #2a2f3e);
  color: var(--color-text-muted, #4b5563);
  cursor: not-allowed;
  transform: none;
}

.chat-hint {
  margin: 5px 0 0;
  font-size: 10px;
  color: var(--color-text-muted, #4b5563);
  text-align: center;
}

/* Transition d'apparition du panneau */
.chat-panel-enter-active {
  transition: opacity 0.2s ease, transform 0.25s cubic-bezier(0.34, 1.56, 0.64, 1);
}
.chat-panel-leave-active {
  transition: opacity 0.15s ease, transform 0.15s ease;
}
.chat-panel-enter-from,
.chat-panel-leave-to {
  opacity: 0;
  transform: translateY(16px) scale(0.96);
}

/* Light theme overrides for chat */
[data-theme="white"] .chat-panel {
  background: #ffffff !important;
  border-color: rgba(15,23,42,.1) !important;
  box-shadow: 0 12px 40px rgba(0,0,0,.15), 0 2px 8px rgba(0,0,0,.08) !important;
}
[data-theme="white"] .chat-header { background: #f8fafc !important; border-color: rgba(15,23,42,.08) !important; }
[data-theme="white"] .chat-title { color: #1e293b !important; }
[data-theme="white"] .chat-subtitle { color: #64748b !important; }
[data-theme="white"] .chat-msg--bot .chat-msg-bubble { background: #f1f5f9 !important; border-color: rgba(15,23,42,.08) !important; color: #1e293b !important; }
[data-theme="white"] .chat-msg--user .chat-msg-bubble { color: #fff !important; }
[data-theme="white"] .chat-input-wrapper { background: #f8fafc !important; border-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .chat-input { color: #1e293b !important; }
[data-theme="white"] .chat-input::placeholder { color: #94a3b8 !important; }
[data-theme="white"] .chat-suggestion { color: #475569 !important; border-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .chat-hint { color: #94a3b8 !important; }

/* ── Mobile ─────────────────────────────────────────────── */
@media (max-width: 768px) {
  .viewer-page { padding: 1rem; }
  .viewer-page--shared { padding: 1rem; }
  .widgets-grid,
  .widgets-grid--compact,
  .widgets-grid--normal { grid-template-columns: 1fr; gap: var(--space-3); }
  .shared-topbar { padding: 0 var(--space-4); }
  .chat-panel { width: calc(100vw - 24px); right: 12px; bottom: 80px; }
  .chat-fab { bottom: 20px; right: 16px; }
}
</style>


