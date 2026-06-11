<template>
  <AppLayout>
  <div class="admin-view">
    <!-- Header -->
    <div class="admin-header">
      <div class="admin-header-left">
        <div class="admin-icon">
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M12 1l9 4v6c0 5.55-3.84 10.74-9 12-5.16-1.26-9-6.45-9-12V5l9-4z"/>
          </svg>
        </div>
        <div>
          <h1 class="admin-title">Panneau d'administration</h1>
          <p class="admin-subtitle">Gérez tous les utilisateurs, dashboards, widgets et datasets</p>
        </div>
      </div>
    </div>

    <!-- Stats Cards -->
    <div class="stats-grid">
      <div class="stat-card">
        <div class="stat-icon users">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
            <circle cx="9" cy="7" r="4"/>
            <path d="M23 21v-2a4 4 0 0 0-3-3.87"/>
            <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
          </svg>
        </div>
        <div class="stat-info">
          <span class="stat-value">{{ users?.length ?? 0 }}</span>
          <span class="stat-label">Utilisateurs</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon dashboards">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/>
            <rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/>
          </svg>
        </div>
        <div class="stat-info">
          <span class="stat-value">{{ dashboards?.length ?? 0 }}</span>
          <span class="stat-label">Dashboards</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon widgets">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <rect x="2" y="3" width="20" height="14" rx="2"/>
            <line x1="8" y1="21" x2="16" y2="21"/>
            <line x1="12" y1="17" x2="12" y2="21"/>
          </svg>
        </div>
        <div class="stat-info">
          <span class="stat-value">{{ widgets?.length ?? 0 }}</span>
          <span class="stat-label">Widgets</span>
        </div>
      </div>
      <div class="stat-card">
        <div class="stat-icon datasets">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <ellipse cx="12" cy="5" rx="9" ry="3"/>
            <path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3"/>
            <path d="M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5"/>
          </svg>
        </div>
        <div class="stat-info">
          <span class="stat-value">{{ datasets?.length ?? 0 }}</span>
          <span class="stat-label">Datasets</span>
        </div>
      </div>
    </div>

    <!-- Tabs -->
    <div class="tabs-bar">
      <button
        v-for="tab in tabs"
        :key="tab.key"
        class="tab-btn"
        :class="{ active: activeTab === tab.key }"
        @click="activeTab = tab.key"
      >
        <span v-html="tab.icon"></span>
        {{ tab.label }}
        <span
          v-if="tab.key === 'notifications' && notifications.length > 0"
          class="notif-badge"
        >{{ notifications.length }}</span>
      </button>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="loading-state">
      <div class="spinner"></div>
      <span>Chargement...</span>
    </div>

    <!-- Error -->
    <div v-else-if="error" class="error-state">
      <span>{{ error }}</span>
      <button @click="loadAll">Réessayer</button>
    </div>

    <template v-else>

      <!-- ── FILTRES GLOBAUX ── -->
      <div class="global-filters">
        <div class="gf-group">
          <svg class="gf-icon" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <rect x="3" y="4" width="18" height="18" rx="2"/><line x1="16" y1="2" x2="16" y2="6"/>
            <line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/>
          </svg>
          <label class="gf-label">Date</label>
          <input v-model="gf.dateFrom" type="date" class="gf-input" title="Du" />
          <span class="gf-sep">→</span>
          <input v-model="gf.dateTo"   type="date" class="gf-input" title="Au" />
        </div>

        <div class="gf-group">
          <svg class="gf-icon" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="8" y1="6" x2="21" y2="6"/><line x1="8" y1="12" x2="21" y2="12"/>
            <line x1="8" y1="18" x2="21" y2="18"/>
            <line x1="3" y1="6" x2="3.01" y2="6"/><line x1="3" y1="12" x2="3.01" y2="12"/>
            <line x1="3" y1="18" x2="3.01" y2="18"/>
          </svg>
          <label class="gf-label">Liste</label>
          <!-- Rôle (users) -->
          <select v-if="activeTab === 'users'" v-model="gf.role" class="gf-select">
            <option value="">Tous les rôles</option>
            <option value="Admin">Admin</option>
            <option value="Editor">Editor</option>
            <option value="Viewer">Viewer</option>
          </select>
          <!-- Visibilité (dashboards) -->
          <select v-else-if="activeTab === 'dashboards'" v-model="gf.visibility" class="gf-select">
            <option value="">Tous</option>
            <option value="public">Public</option>
            <option value="private">Privé</option>
          </select>
          <!-- Type widget -->
          <select v-else-if="activeTab === 'widgets'" v-model="gf.widgetType" class="gf-select">
            <option value="">Tous les types</option>
            <option v-for="t in widgetTypes" :key="t" :value="t">{{ t }}</option>
          </select>
          <span v-else class="gf-na">—</span>
        </div>

        <div class="gf-group">
          <svg class="gf-icon" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="4" y1="6" x2="20" y2="6"/><line x1="4" y1="12" x2="14" y2="12"/><line x1="4" y1="18" x2="10" y2="18"/>
          </svg>
          <label class="gf-label">Range</label>
          <!-- Dashboards par user (users) -->
          <template v-if="activeTab === 'users'">
            <input v-model.number="gf.minDashboards" type="number" min="0" class="gf-input gf-num" placeholder="Min dash." />
            <span class="gf-sep">–</span>
            <input v-model.number="gf.maxDashboards" type="number" min="0" class="gf-input gf-num" placeholder="Max dash." />
          </template>
          <!-- Widgets par dashboard (dashboards) -->
          <template v-else-if="activeTab === 'dashboards'">
            <input v-model.number="gf.minWidgets" type="number" min="0" class="gf-input gf-num" placeholder="Min widgets" />
            <span class="gf-sep">–</span>
            <input v-model.number="gf.maxWidgets" type="number" min="0" class="gf-input gf-num" placeholder="Max widgets" />
          </template>
          <!-- Lignes dataset -->
          <template v-else-if="activeTab === 'datasets'">
            <input v-model.number="gf.minRows" type="number" min="0" class="gf-input gf-num" placeholder="Min lignes" />
            <span class="gf-sep">–</span>
            <input v-model.number="gf.maxRows" type="number" min="0" class="gf-input gf-num" placeholder="Max lignes" />
          </template>
          <span v-else class="gf-na">—</span>
        </div>

        <button class="gf-reset" @click="resetFilters" title="Réinitialiser les filtres">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="1 4 1 10 7 10"/><path d="M3.51 15a9 9 0 1 0 .49-4.5"/>
          </svg>
          Reset
        </button>

        <span class="gf-count">
          {{ activeFilterCount }} filtre(s) actif(s)
        </span>
      </div>

      <!-- ── USERS TAB ── -->
      <div v-if="activeTab === 'users'" class="tab-content">
        <div class="table-toolbar">
          <input v-model="searchUsers" class="search-input" placeholder="Rechercher un utilisateur..." />
          <button class="btn-add-user" @click="openAddUserModal">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
              <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
            </svg>
            Ajouter un utilisateur
          </button>
        </div>
        <div class="table-wrap">
          <table class="admin-table">
            <thead>
              <tr>
                <th>ID</th>
                <th>Email</th>
                <th>Rôle</th>
                <th>Dashboards</th>
                <th>Datasets</th>
                <th>Créé le</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="u in filteredUsers" :key="`user-${u.id}-${u.role}`">
                <td class="id-cell">#{{ u.id }}</td>
                <td>{{ u.email }}</td>
                <td>
                  <select
                    v-model="roleMap[u.id]"
                    class="role-select"
                    :class="roleMap[u.id]?.toLowerCase()"
                    @change="onRoleChange(u.id)"
                  >
                    <option value="Admin">Admin</option>
                    <option value="Editor">Editor</option>
                    <option value="Viewer">Viewer</option>
                  </select>
                </td>
                <td class="num-cell">{{ u.dashboardCount }}</td>
                <td class="num-cell">{{ u.datasetCount }}</td>
                <td class="date-cell">{{ formatDate(u.createdAt) }}</td>
                <td class="actions-cell">
                  <button class="btn-pwd" @click="openPwdModal(u.id, u.email)" title="Changer le mot de passe">
                    🔑
                  </button>
                  <button class="btn-delete" @click="confirmDelete('user', u.id, u.email)" title="Supprimer">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <polyline points="3,6 5,6 21,6"/><path d="M19,6l-1,14H6L5,6"/>
                      <path d="M10,11v6"/><path d="M14,11v6"/>
                      <path d="M9,6V4h6v2"/>
                    </svg>
                  </button>
                </td>
              </tr>
              <tr v-if="filteredUsers.length === 0">
                <td colspan="7" class="empty-row">Aucun utilisateur trouvé</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- ── DASHBOARDS TAB ── -->
      <div v-if="activeTab === 'dashboards'" class="tab-content">
        <div class="table-toolbar">
          <input v-model="searchDashboards" class="search-input" placeholder="Rechercher un dashboard..." />
        </div>
        <div class="table-wrap">
          <table class="admin-table">
            <thead>
              <tr>
                <th>ID</th>
                <th>Nom</th>
                <th>Propriétaire</th>
                <th>Widgets</th>
                <th>Public</th>
                <th>Créé le</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="d in filteredDashboards" :key="d.id">
                <td class="id-cell">#{{ d.id }}</td>
                <td>{{ d.name }}</td>
                <td class="email-cell">{{ d.ownerEmail }}</td>
                <td class="num-cell">{{ d.widgetCount }}</td>
                <td>
                  <span class="badge" :class="d.isPublic ? 'badge-green' : 'badge-gray'">
                    {{ d.isPublic ? 'Public' : 'Privé' }}
                  </span>
                </td>
                <td class="date-cell">{{ formatDate(d.createdAt) }}</td>
                <td>
                  <button class="btn-view" @click="$router.push(`/dashboard/${d.id}`)" title="Voir">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                      <circle cx="12" cy="12" r="3"/>
                    </svg>
                  </button>
                  <button class="btn-edit" @click="$router.push(`/builder/${d.id}`)" title="Éditer">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                      <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                    </svg>
                  </button>
                  <button class="btn-delete" @click="confirmDelete('dashboard', d.id, d.name)" title="Supprimer">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <polyline points="3,6 5,6 21,6"/><path d="M19,6l-1,14H6L5,6"/>
                      <path d="M10,11v6"/><path d="M14,11v6"/><path d="M9,6V4h6v2"/>
                    </svg>
                  </button>
                </td>
              </tr>
              <tr v-if="filteredDashboards.length === 0">
                <td colspan="7" class="empty-row">Aucun dashboard trouvé</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- ── WIDGETS TAB ── -->
      <div v-if="activeTab === 'widgets'" class="tab-content">
        <div class="table-toolbar">
          <input v-model="searchWidgets" class="search-input" placeholder="Rechercher un widget..." />
        </div>
        <div class="table-wrap">
          <table class="admin-table">
            <thead>
              <tr>
                <th>ID</th>
                <th>Titre</th>
                <th>Type</th>
                <th>Dashboard</th>
                <th>Propriétaire</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="w in filteredWidgets" :key="w.id">
                <td class="id-cell">#{{ w.id }}</td>
                <td>{{ w.title }}</td>
                <td><span class="badge badge-blue">{{ w.type }}</span></td>
                <td>{{ w.dashboardName }}</td>
                <td class="email-cell">{{ w.ownerEmail }}</td>
                <td>
                  <button class="btn-delete" @click="confirmDelete('widget', w.id, w.title)" title="Supprimer">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <polyline points="3,6 5,6 21,6"/><path d="M19,6l-1,14H6L5,6"/>
                      <path d="M10,11v6"/><path d="M14,11v6"/><path d="M9,6V4h6v2"/>
                    </svg>
                  </button>
                </td>
              </tr>
              <tr v-if="filteredWidgets.length === 0">
                <td colspan="6" class="empty-row">Aucun widget trouvé</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- ── DATASETS TAB ── -->
      <div v-if="activeTab === 'datasets'" class="tab-content">
        <div class="table-toolbar">
          <input v-model="searchDatasets" class="search-input" placeholder="Rechercher un dataset..." />
        </div>
        <div class="table-wrap">
          <table class="admin-table">
            <thead>
              <tr>
                <th>ID</th>
                <th>Fichier</th>
                <th>Lignes</th>
                <th>Propriétaire</th>
                <th>Uploadé le</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="ds in filteredDatasets" :key="ds.id">
                <td class="id-cell">#{{ ds.id }}</td>
                <td>{{ ds.fileName }}</td>
                <td class="num-cell">{{ ds.rowCount.toLocaleString() }}</td>
                <td class="email-cell">{{ ds.ownerEmail }}</td>
                <td class="date-cell">{{ formatDate(ds.uploadedAt) }}</td>
                <td>
                  <button class="btn-delete" @click="confirmDelete('dataset', ds.id, ds.fileName)" title="Supprimer">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <polyline points="3,6 5,6 21,6"/><path d="M19,6l-1,14H6L5,6"/>
                      <path d="M10,11v6"/><path d="M14,11v6"/><path d="M9,6V4h6v2"/>
                    </svg>
                  </button>
                </td>
              </tr>
              <tr v-if="filteredDatasets.length === 0">
                <td colspan="6" class="empty-row">Aucun dataset trouvé</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- ── STATS TAB ── -->
      <div v-if="activeTab === 'stats'" class="tab-content">
        <div v-if="stats.loading" class="loading-state">
          <div class="spinner"></div><span>Chargement des statistiques...</span>
        </div>
        <template v-else-if="stats.overview">

          <!-- KPIs overview -->
          <div class="stats-section-title">📊 Vue globale</div>
          <div class="stats-kpi-grid">
            <div class="stats-kpi"><span class="stats-kpi-val">{{ stats.overview.totals?.users }}</span><span class="stats-kpi-label">Utilisateurs</span></div>
            <div class="stats-kpi"><span class="stats-kpi-val">{{ stats.overview.totals?.dashboards }}</span><span class="stats-kpi-label">Dashboards</span></div>
            <div class="stats-kpi"><span class="stats-kpi-val">{{ stats.overview.totals?.widgets }}</span><span class="stats-kpi-label">Widgets</span></div>
            <div class="stats-kpi"><span class="stats-kpi-val">{{ stats.overview.totals?.datasets }}</span><span class="stats-kpi-label">Datasets</span></div>
            <div class="stats-kpi"><span class="stats-kpi-val">{{ stats.overview.totals?.dataSources }}</span><span class="stats-kpi-label">Sources</span></div>
            <div class="stats-kpi green"><span class="stats-kpi-val">{{ stats.overview.activity?.newUsersLast30Days }}</span><span class="stats-kpi-label">Nouveaux users (30j)</span></div>
            <div class="stats-kpi green"><span class="stats-kpi-val">{{ stats.overview.activity?.dashboardsCreatedLast7Days }}</span><span class="stats-kpi-label">Dashboards créés (7j)</span></div>
            <div class="stats-kpi green"><span class="stats-kpi-val">{{ stats.overview.activity?.activeShares }}</span><span class="stats-kpi-label">Partages actifs</span></div>
          </div>

          <div class="stats-two-cols">
            <!-- Utilisateurs par rôle -->
            <div class="stats-card">
              <div class="stats-section-title">👥 Utilisateurs par rôle</div>
              <div class="stats-bar-list">
                <div v-for="r in stats.users?.byRole" :key="r.role" class="stats-bar-row">
                  <span class="stats-bar-label">{{ r.role }}</span>
                  <div class="stats-bar-track">
                    <div class="stats-bar-fill" :style="{ width: Math.min(100, (r.count / (stats.overview.totals?.users || 1)) * 100) + '%' }"></div>
                  </div>
                  <span class="stats-bar-count">{{ r.count }}</span>
                </div>
              </div>
              <div class="stats-section-title" style="margin-top:18px">🕐 Derniers inscrits</div>
              <table class="admin-table">
                <thead><tr><th>Email</th><th>Rôle</th><th>Date</th></tr></thead>
                <tbody>
                  <tr v-for="u in stats.users?.recentUsers" :key="u.id">
                    <td class="email-cell">{{ u.email }}</td>
                    <td><span class="badge badge-blue">{{ u.role }}</span></td>
                    <td class="date-cell">{{ formatDate(u.createdAt) }}</td>
                  </tr>
                </tbody>
              </table>
            </div>

            <!-- Sources de données par type -->
            <div class="stats-card">
              <div class="stats-section-title">🗄 Sources par type</div>
              <div class="stats-bar-list">
                <div v-for="t in stats.datasources?.byType" :key="t.type" class="stats-bar-row">
                  <span class="stats-bar-label">{{ t.type }}</span>
                  <div class="stats-bar-track">
                    <div class="stats-bar-fill" style="background:#f59e0b" :style="{ width: Math.min(100, (t.count / (stats.overview.totals?.dataSources || 1)) * 100) + '%' }"></div>
                  </div>
                  <span class="stats-bar-count">{{ t.count }}</span>
                </div>
              </div>
              <div class="stats-section-title" style="margin-top:18px">⚠️ Sources non rafraîchies (+24h)</div>
              <table class="admin-table">
                <thead><tr><th>Nom</th><th>Type</th><th>Dernier refresh</th></tr></thead>
                <tbody>
                  <tr v-for="s in stats.datasources?.staleSources" :key="s.id">
                    <td>{{ s.name }}</td>
                    <td><span class="badge badge-blue">{{ s.type }}</span></td>
                    <td class="date-cell">{{ s.lastRefreshedAt ? formatDate(s.lastRefreshedAt) : '—' }}</td>
                  </tr>
                  <tr v-if="!stats.datasources?.staleSources?.length">
                    <td colspan="3" class="empty-row">✅ Toutes les sources sont à jour</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <div class="stats-two-cols">
            <!-- Widgets par type -->
            <div class="stats-card">
              <div class="stats-section-title">🧩 Widgets par type</div>
              <div class="stats-bar-list">
                <div v-for="w in stats.widgets?.byType" :key="w.type" class="stats-bar-row">
                  <span class="stats-bar-label">{{ w.type }}</span>
                  <div class="stats-bar-track">
                    <div class="stats-bar-fill" style="background:#8b5cf6" :style="{ width: Math.min(100, (w.count / (stats.overview.totals?.widgets || 1)) * 100) + '%' }"></div>
                  </div>
                  <span class="stats-bar-count">{{ w.count }}</span>
                </div>
              </div>
            </div>

            <!-- Audit — actions récentes -->
            <div class="stats-card">
              <div class="stats-section-title">📋 Actions les plus fréquentes (7j)</div>
              <div class="stats-bar-list">
                <div v-for="a in stats.audit?.topActions" :key="a.action" class="stats-bar-row">
                  <span class="stats-bar-label" style="font-size:11px">{{ a.action }}</span>
                  <div class="stats-bar-track">
                    <div class="stats-bar-fill" style="background:#ef4444" :style="{ width: Math.min(100, (a.count / (stats.audit?.totalActionsLast7Days || 1)) * 100) + '%' }"></div>
                  </div>
                  <span class="stats-bar-count">{{ a.count }}</span>
                </div>
              </div>
              <div class="stats-section-title" style="margin-top:18px">🕐 Logs récents</div>
              <table class="admin-table">
                <thead><tr><th>Action</th><th>Entité</th><th>IP</th><th>Date</th></tr></thead>
                <tbody>
                  <tr v-for="l in stats.audit?.recentLogs?.slice(0,8)" :key="l.id">
                    <td><span class="badge badge-blue" style="font-size:10px">{{ l.action }}</span></td>
                    <td class="email-cell">{{ l.entityType }} #{{ l.entityId }}</td>
                    <td class="date-cell">{{ l.ipAddress }}</td>
                    <td class="date-cell">{{ formatDate(l.createdAt) }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <!-- Top dashboards -->
          <div class="stats-card" style="margin-top:16px">
            <div class="stats-section-title">🏆 Top dashboards par nombre de widgets</div>
            <table class="admin-table">
              <thead><tr><th>ID</th><th>Nom</th><th>Public</th><th>Widgets</th><th>Créé le</th></tr></thead>
              <tbody>
                <tr v-for="d in stats.dashboards?.topByWidgets" :key="d.id">
                  <td class="id-cell">#{{ d.id }}</td>
                  <td>{{ d.name }}</td>
                  <td><span class="badge" :class="d.isPublic ? 'badge-green' : 'badge-gray'">{{ d.isPublic ? 'Public' : 'Privé' }}</span></td>
                  <td class="num-cell">{{ d.widgetCount }}</td>
                  <td class="date-cell">{{ formatDate(d.createdAt) }}</td>
                </tr>
              </tbody>
            </table>
          </div>

        </template>
        <div v-else class="empty-row" style="padding:60px;text-align:center;color:rgba(240,253,249,0.3)">
          Aucune donnée disponible
        </div>
      </div>

      <!-- ── NOTIFICATIONS TAB ── -->
      <div v-if="activeTab === 'notifications'" class="tab-content">
        <div class="notif-header">
          <span class="notif-count" v-if="notifications.length > 0">
            {{ notifications.length }} demande(s) en attente
          </span>
        </div>

        <div v-if="notifications.length === 0" class="empty-notif">
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="rgba(255,255,255,0.15)" stroke-width="1.5">
            <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/>
            <path d="M13.73 21a2 2 0 0 1-3.46 0"/>
          </svg>
          <p>Aucune notification en attente</p>
        </div>

        <div v-else class="notif-list">
          <div v-for="n in notifications" :key="n.id" class="notif-card">
            <div class="notif-icon">{{ n.action === 'ACCOUNT_REQUEST' ? '👤' : '🔑' }}</div>
            <div class="notif-body">
              <div class="notif-title">
                {{ n.action === 'ACCOUNT_REQUEST' ? 'Demande de création de compte' : 'Demande de réinitialisation de mot de passe' }}
              </div>
              <!-- Demande de compte : afficher nom + email depuis details -->
              <template v-if="n.action === 'ACCOUNT_REQUEST'">
                <div class="notif-email">{{ parseContactDetails(n.details).name }} — {{ parseContactDetails(n.details).email }}</div>
                <div v-if="parseContactDetails(n.details).message" class="notif-message">💬 {{ parseContactDetails(n.details).message }}</div>
              </template>
              <template v-else>
                <div class="notif-email">{{ n.userEmail }}</div>
              </template>
              <div class="notif-meta">
                <span>📅 {{ formatDateTime(n.createdAt) }}</span>
                <span>🌐 {{ n.ipAddress }}</span>
              </div>
            </div>
            <button class="notif-dismiss" @click="dismissNotification(n.id)" title="Marquer comme traité">
              ✓ Traité
            </button>
          </div>
        </div>
      </div>
    </template>

  <!-- Confirm Delete Modal -->
    <div v-if="deleteModal.show" class="modal-overlay" @click.self="deleteModal.show = false">
      <div class="confirm-modal">
        <div class="confirm-icon">
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#f87171" stroke-width="2">
            <circle cx="12" cy="12" r="10"/>
            <line x1="12" y1="8" x2="12" y2="12"/>
            <line x1="12" y1="16" x2="12.01" y2="16"/>
          </svg>
        </div>
        <h3>Confirmer la suppression</h3>
        <p>Voulez-vous vraiment supprimer <strong>{{ deleteModal.name }}</strong> ?</p>
        <p class="confirm-warn">Cette action est irréversible.</p>
        <div class="confirm-actions">
          <button class="btn-cancel" @click="deleteModal.show = false">Annuler</button>
          <button class="btn-confirm-delete" @click="executeDelete" :disabled="deleteModal.loading">
            {{ deleteModal.loading ? 'Suppression...' : 'Supprimer' }}
          </button>
        </div>
      </div>
    </div>
  <!-- Change Password Modal -->
    <div v-if="pwdModal.show" class="modal-overlay" @click.self="pwdModal.show = false">
      <div class="confirm-modal">
        <h3>🔑 Changer le mot de passe</h3>
        <p style="color:rgba(240,253,249,0.5); font-size:13px; margin-bottom:8px;">
          Utilisateur : <strong style="color:#10b981">{{ pwdModal.email }}</strong>
        </p>

        <div class="pwd-field">
          <input
            v-model="pwdModal.password"
            :type="pwdModal.show_pwd ? 'text' : 'password'"
            class="pwd-input"
            placeholder="Nouveau mot de passe (min. 6 caractères)"
            @keyup.enter="executeChangePassword"
          />
          <button type="button" class="pwd-toggle" @click="pwdModal.show_pwd = !pwdModal.show_pwd">
            {{ pwdModal.show_pwd ? '🙈' : '👁' }}
          </button>
        </div>
        <p v-if="pwdModal.error" class="pwd-error">{{ pwdModal.error }}</p>
        <p v-if="pwdModal.success" class="pwd-success">✅ Mot de passe modifié avec succès !</p>

        <div class="confirm-actions">
          <button class="btn-cancel" @click="pwdModal.show = false">Annuler</button>
          <button class="btn-confirm-delete" style="background:rgba(16,185,129,0.15); border-color:rgba(16,185,129,0.3); color:#10b981"
            @click="executeChangePassword" :disabled="pwdModal.loading">
            {{ pwdModal.loading ? 'Enregistrement...' : 'Confirmer' }}
          </button>
        </div>
      </div>
    </div>

  <!-- Add User Modal -->
  <div v-if="addUserModal.show" class="modal-overlay" @click.self="closeAddUserModal">
    <div class="confirm-modal" style="max-width:420px">
      <h3>👤 Ajouter un utilisateur</h3>

      <div class="add-user-fields">
        <div class="add-field-group">
          <label>Email</label>
          <input v-model="addUserModal.email" type="email" class="pwd-input" placeholder="exemple@email.com" @keyup.enter="executeAddUser" />
        </div>
        <div class="add-field-group">
          <label>Mot de passe</label>
          <div class="pwd-field">
            <input v-model="addUserModal.password" :type="addUserModal.showPwd ? 'text' : 'password'" class="pwd-input" placeholder="Min. 6 caractères" />
            <button type="button" class="pwd-toggle" @click="addUserModal.showPwd = !addUserModal.showPwd">
              {{ addUserModal.showPwd ? '🙈' : '👁' }}
            </button>
          </div>
        </div>
        <div class="add-field-group">
          <label>Rôle</label>
          <select v-model="addUserModal.role" class="role-select" :class="addUserModal.role.toLowerCase()" style="width:100%;padding:10px 12px">
            <option value="Viewer">Viewer</option>
            <option value="Editor">Editor</option>
            <option value="Admin">Admin</option>
          </select>
        </div>
      </div>

      <p v-if="addUserModal.error"   class="pwd-error">{{ addUserModal.error }}</p>
      <p v-if="addUserModal.success" class="pwd-success">✅ Utilisateur créé avec succès !</p>

      <div class="confirm-actions">
        <button class="btn-cancel" @click="closeAddUserModal">Annuler</button>
        <button class="btn-confirm-delete" style="background:rgba(16,185,129,0.15); border-color:rgba(16,185,129,0.3); color:#10b981"
          @click="executeAddUser" :disabled="addUserModal.loading">
          {{ addUserModal.loading ? 'Création...' : 'Créer l\'utilisateur' }}
        </button>
      </div>
    </div>
  </div>

  </div>
  </AppLayout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, reactive, watch } from 'vue'
import AppLayout from '@/components/layout/AppLayout.vue'
import adminService, {
  type AdminUser, type AdminDashboard, type AdminWidget, type AdminDataset, type AdminNotification
} from '@/services/adminService'

// ── State ──────────────────────────────────────────────────────────────────
const activeTab = ref<'users' | 'dashboards' | 'widgets' | 'datasets' | 'notifications' | 'stats'>('users')
const loading   = ref(false)
const error     = ref<string | null>(null)

const users         = ref<AdminUser[]>([])
const dashboards    = ref<AdminDashboard[]>([])
const widgets       = ref<AdminWidget[]>([])
const datasets      = ref<AdminDataset[]>([])
const notifications = ref<AdminNotification[]>([])

// Stats
const stats = reactive({
  loading:     false,
  overview:    null as any,
  users:       null as any,
  dashboards:  null as any,
  datasources: null as any,
  audit:       null as any,
  widgets:     null as any,
})

// Map userId → role (v-model sur chaque select)
const roleMap = reactive<Record<number, string>>({})

const searchUsers      = ref('')
const searchDashboards = ref('')
const searchWidgets    = ref('')
const searchDatasets   = ref('')

// ── Filtres globaux ───────────────────────────────────────────────
const gf = reactive({
  dateFrom:     '',
  dateTo:       '',
  role:         '',
  visibility:   '',
  widgetType:   '',
  minDashboards: null as number | null,
  maxDashboards: null as number | null,
  minWidgets:    null as number | null,
  maxWidgets:    null as number | null,
  minRows:       null as number | null,
  maxRows:       null as number | null,
})

function resetFilters() {
  Object.assign(gf, {
    dateFrom: '', dateTo: '', role: '', visibility: '', widgetType: '',
    minDashboards: null, maxDashboards: null,
    minWidgets: null, maxWidgets: null,
    minRows: null, maxRows: null,
  })
}

const activeFilterCount = computed(() => [
  gf.dateFrom, gf.dateTo, gf.role, gf.visibility, gf.widgetType,
  gf.minDashboards, gf.maxDashboards, gf.minWidgets, gf.maxWidgets,
  gf.minRows, gf.maxRows,
].filter(v => v !== '' && v !== null).length)

// Helper date
function inDateRange(dateStr: string) {
  if (!gf.dateFrom && !gf.dateTo) return true
  const d = new Date(dateStr).getTime()
  const from = gf.dateFrom ? new Date(gf.dateFrom).getTime() : -Infinity
  const to   = gf.dateTo   ? new Date(gf.dateTo).getTime() + 86_400_000 : Infinity
  return d >= from && d <= to
}

const deleteModal = ref({
  show:    false,
  loading: false,
  type:    '' as 'user' | 'dashboard' | 'widget' | 'dataset',
  id:      0,
  name:    '',
})

// ── Add User Modal ────────────────────────────────────────────────
const addUserModal = ref({
  show: false, loading: false,
  email: '', password: '', role: 'Viewer',
  showPwd: false, error: '', success: false,
})

function openAddUserModal() {
  addUserModal.value = { show: true, loading: false, email: '', password: '', role: 'Viewer', showPwd: false, error: '', success: false }
}

function closeAddUserModal() {
  addUserModal.value.show = false
}

async function executeAddUser() {
  addUserModal.value.error   = ''
  addUserModal.value.success = false
  if (!addUserModal.value.email.trim())         { addUserModal.value.error = 'Email requis.'; return }
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(addUserModal.value.email)) { addUserModal.value.error = 'Email invalide.'; return }
  if (addUserModal.value.password.length < 6)   { addUserModal.value.error = 'Mot de passe : 6 caractères minimum.'; return }

  addUserModal.value.loading = true
  try {
    const newUser = await adminService.createUser(addUserModal.value.email.trim(), addUserModal.value.password, addUserModal.value.role)
    users.value.unshift(newUser)
    roleMap[newUser.id] = newUser.role
    addUserModal.value.success = true
    setTimeout(() => closeAddUserModal(), 1500)
  } catch (e: any) {
    addUserModal.value.error = e?.message ?? 'Erreur lors de la création.'
  } finally {
    addUserModal.value.loading = false
  }
}

const pwdModal = ref({
  show:     false,
  loading:  false,
  userId:   0,
  email:    '',
  password: '',
  show_pwd: false,
  error:    '',
  success:  false,
})

// ── Tabs ───────────────────────────────────────────────────────────────────
const tabs = [
  {
    key: 'users', label: 'Utilisateurs',
    icon: `<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
      <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
      <circle cx="9" cy="7" r="4"/>
    </svg>`,
  },
  {
    key: 'dashboards', label: 'Dashboards',
    icon: `<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
      <rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/>
      <rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/>
    </svg>`,
  },
  {
    key: 'widgets', label: 'Widgets',
    icon: `<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
      <rect x="2" y="3" width="20" height="14" rx="2"/>
      <line x1="8" y1="21" x2="16" y2="21"/>
    </svg>`,
  },
  {
    key: 'datasets', label: 'Datasets',
    icon: `<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
      <ellipse cx="12" cy="5" rx="9" ry="3"/>
      <path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3"/>
      <path d="M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5"/>
    </svg>`,
  },
  {
    key: 'stats', label: 'Statistiques',
    icon: `<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
      <line x1="18" y1="20" x2="18" y2="10"/><line x1="12" y1="20" x2="12" y2="4"/>
      <line x1="6" y1="20" x2="6" y2="14"/>
    </svg>`,
  },
  {
    key: 'notifications', label: 'Notifications',
    icon: `<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
      <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/>
      <path d="M13.73 21a2 2 0 0 1-3.46 0"/>
    </svg>`,
  },
]

// ── Computed filters ───────────────────────────────────────────────────────
const filteredUsers = computed(() =>
  (users.value ?? []).filter(u => {
    const q = searchUsers.value.toLowerCase()
    if (q && !(u.email ?? '').toLowerCase().includes(q) && !(u.role ?? '').toLowerCase().includes(q)) return false
    if (gf.role && u.role !== gf.role) return false
    if (!inDateRange(u.createdAt)) return false
    if (gf.minDashboards !== null && u.dashboardCount < gf.minDashboards) return false
    if (gf.maxDashboards !== null && u.dashboardCount > gf.maxDashboards) return false
    return true
  })
)

const filteredDashboards = computed(() =>
  (dashboards.value ?? []).filter(d => {
    const q = searchDashboards.value.toLowerCase()
    if (q && !(d.name ?? '').toLowerCase().includes(q) && !(d.ownerEmail ?? '').toLowerCase().includes(q)) return false
    if (gf.visibility === 'public'  && !d.isPublic) return false
    if (gf.visibility === 'private' &&  d.isPublic) return false
    if (!inDateRange(d.createdAt)) return false
    if (gf.minWidgets !== null && d.widgetCount < gf.minWidgets) return false
    if (gf.maxWidgets !== null && d.widgetCount > gf.maxWidgets) return false
    return true
  })
)

const filteredWidgets = computed(() =>
  (widgets.value ?? []).filter(w => {
    const q = searchWidgets.value.toLowerCase()
    if (q && !(w.title ?? '').toLowerCase().includes(q) && !(w.type ?? '').toLowerCase().includes(q) && !(w.ownerEmail ?? '').toLowerCase().includes(q)) return false
    if (gf.widgetType && w.type !== gf.widgetType) return false
    return true
  })
)

const filteredDatasets = computed(() =>
  (datasets.value ?? []).filter(d => {
    const q = searchDatasets.value.toLowerCase()
    if (q && !(d.fileName ?? '').toLowerCase().includes(q) && !(d.ownerEmail ?? '').toLowerCase().includes(q)) return false
    if (!inDateRange(d.uploadedAt)) return false
    if (gf.minRows !== null && d.rowCount < gf.minRows) return false
    if (gf.maxRows !== null && d.rowCount > gf.maxRows) return false
    return true
  })
)

// Types de widgets disponibles (pour le filtre liste)
const widgetTypes = computed(() =>
  [...new Set((widgets.value ?? []).map(w => w.type).filter(Boolean))].sort()
)

// ── Load ───────────────────────────────────────────────────────────────────
async function loadAll() {
  loading.value = true
  error.value   = null
  try {
    const [u, d, w, ds, notifs] = await Promise.all([
      adminService.getUsers(),
      adminService.getDashboards(),
      adminService.getWidgets(),
      adminService.getDatasets(),
      adminService.getNotifications(),
    ])
    users.value         = Array.isArray(u)      ? u      : []
    dashboards.value    = Array.isArray(d)      ? d      : []
    widgets.value       = Array.isArray(w)      ? w      : []
    datasets.value      = Array.isArray(ds)     ? ds     : []
    notifications.value = Array.isArray(notifs) ? notifs : []
    // Initialiser la map des rôles
    users.value.forEach(usr => { roleMap[usr.id] = usr.role })
  } catch (e: any) {
    error.value = e?.response?.data?.message || 'Erreur de chargement'
  } finally {
    loading.value = false
  }
}

// ── Role change ────────────────────────────────────────────────────────────
async function onRoleChange(userId: number) {
  const newRole = roleMap[userId]
  if (!newRole) return
  const oldRole = users.value.find(u => u.id === userId)?.role ?? newRole
  try {
    await adminService.changeRole(userId, newRole)
    // Mettre à jour aussi le tableau users pour cohérence
    const idx = users.value.findIndex(u => u.id === userId)
    if (idx !== -1) users.value[idx].role = newRole
  } catch {
    // Rollback
    roleMap[userId] = oldRole
    alert('Erreur lors du changement de rôle')
  }
}

// ── Delete ─────────────────────────────────────────────────────────────────
function confirmDelete(type: 'user' | 'dashboard' | 'widget' | 'dataset', id: number, name: string) {
  deleteModal.value = { show: true, loading: false, type, id, name }
}

async function executeDelete() {
  deleteModal.value.loading = true
  const { type, id } = deleteModal.value
  try {
    if (type === 'user')      { await adminService.deleteUser(id);      users.value      = users.value.filter(u => u.id !== id) }
    if (type === 'dashboard') { await adminService.deleteDashboard(id); dashboards.value = dashboards.value.filter(d => d.id !== id) }
    if (type === 'widget')    { await adminService.deleteWidget(id);    widgets.value    = widgets.value.filter(w => w.id !== id) }
    if (type === 'dataset')   { await adminService.deleteDataset(id);   datasets.value   = datasets.value.filter(d => d.id !== id) }
    deleteModal.value.show = false
  } catch {
    alert('Erreur lors de la suppression')
  } finally {
    deleteModal.value.loading = false
  }
}

// ── Change password ────────────────────────────────────────────────────────
function openPwdModal(userId: number, email: string) {
  pwdModal.value = { show: true, loading: false, userId, email, password: '', show_pwd: false, error: '', success: false }
}

async function executeChangePassword() {
  pwdModal.value.error   = ''
  pwdModal.value.success = false
  if (pwdModal.value.password.length < 6) {
    pwdModal.value.error = 'Le mot de passe doit contenir au moins 6 caractères.'
    return
  }
  pwdModal.value.loading = true
  try {
    await adminService.changePassword(pwdModal.value.userId, pwdModal.value.password)
    pwdModal.value.success  = true
    pwdModal.value.password = ''
    setTimeout(() => { pwdModal.value.show = false }, 1500)
  } catch {
    pwdModal.value.error = 'Erreur lors du changement de mot de passe.'
  } finally {
    pwdModal.value.loading = false
  }
}

// ── Notifications ──────────────────────────────────────────────────────────
async function dismissNotification(id: number) {
  try {
    await adminService.dismissNotification(id)
    notifications.value = notifications.value.filter(n => n.id !== id)
  } catch {
    alert('Erreur lors de la suppression')
  }
}

// ── Utils ──────────────────────────────────────────────────────────────────
function formatDate(d: string) {
  return new Date(d).toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}
function formatDateTime(d: string) {
  return new Date(d).toLocaleString('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function parseContactDetails(details: string | null) {
  if (!details) return { name: '—', email: '—', message: '' }
  const name    = details.match(/name=([^|]*)/)?.[1]    ?? '—'
  const email   = details.match(/email=([^|]*)/)?.[1]   ?? '—'
  const message = details.match(/message=([^|]*)/)?.[1] ?? ''
  return { name, email, message }
}

async function loadStats() {
  if (stats.overview) return // déjà chargé
  stats.loading = true
  try {
    const [ov, us, da, ds, au, wi] = await Promise.all([
      adminService.getStatsOverview(),
      adminService.getStatsUsers(),
      adminService.getStatsDashboards(),
      adminService.getStatsDataSources(),
      adminService.getStatsAudit(),
      adminService.getStatsWidgets(),
    ])
    stats.overview    = ov
    stats.users       = us
    stats.dashboards  = da
    stats.datasources = ds
    stats.audit       = au
    stats.widgets     = wi
  } catch (e) {
    console.error('Erreur stats', e)
  } finally {
    stats.loading = false
  }
}

watch(activeTab, (tab) => { if (tab === 'stats') loadStats() })

onMounted(loadAll)
</script>

<style scoped>
.admin-view {
  padding: 28px 32px;
  min-height: 100vh;
  background: #060f0e;
  color: #f0fdf9;
  font-family: 'DM Sans', sans-serif;
}

/* Header */
.admin-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 28px;
}
.admin-header-left {
  display: flex;
  align-items: center;
  gap: 16px;
}
.admin-icon {
  width: 48px;
  height: 48px;
  background: rgba(239, 68, 68, 0.12);
  border: 1px solid rgba(239, 68, 68, 0.25);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #f87171;
}
.admin-title {
  font-size: 22px;
  font-weight: 700;
  color: #f0fdf9;
  margin: 0;
}
.admin-subtitle {
  font-size: 13px;
  color: rgba(240, 253, 249, 0.4);
  margin: 2px 0 0;
}

/* Stats */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
  margin-bottom: 28px;
}
.stat-card {
  background: rgba(255,255,255,0.03);
  border: 1px solid rgba(255,255,255,0.07);
  border-radius: 14px;
  padding: 18px 20px;
  display: flex;
  align-items: center;
  gap: 16px;
}
.stat-icon {
  width: 42px;
  height: 42px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}
.stat-icon.users     { background: rgba(99,102,241,0.15); color: #818cf8; }
.stat-icon.dashboards{ background: rgba(16,185,129,0.15); color: #34d399; }
.stat-icon.widgets   { background: rgba(245,158,11,0.15); color: #fbbf24; }
.stat-icon.datasets  { background: rgba(59,130,246,0.15); color: #60a5fa; }
.stat-info { display: flex; flex-direction: column; }
.stat-value { font-size: 26px; font-weight: 700; color: #f0fdf9; line-height: 1; }
.stat-label { font-size: 12px; color: rgba(240,253,249,0.4); margin-top: 4px; }

/* Tabs */
.tabs-bar {
  display: flex;
  gap: 4px;
  border-bottom: 1px solid rgba(255,255,255,0.08);
  margin-bottom: 24px;
}
.tab-btn {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 18px;
  background: none;
  border: none;
  border-bottom: 2px solid transparent;
  color: rgba(240,253,249,0.45);
  font-size: 13.5px;
  cursor: pointer;
  transition: all 0.2s;
  margin-bottom: -1px;
}
.tab-btn:hover { color: #f0fdf9; }
.tab-btn.active {
  color: #10b981;
  border-bottom-color: #10b981;
}

/* Toolbar */
/* ── Filtres globaux ─────────────────────────────────── */
.global-filters {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px 20px;
  background: rgba(255,255,255,0.03);
  border: 1px solid rgba(255,255,255,0.07);
  border-radius: 12px;
  padding: 12px 16px;
  margin-bottom: 16px;
}
.gf-group {
  display: flex;
  align-items: center;
  gap: 8px;
}
.gf-icon { color: #10b981; flex-shrink: 0; }
.gf-label {
  font-size: 11px;
  font-weight: 700;
  color: rgba(240,253,249,0.4);
  text-transform: uppercase;
  letter-spacing: .05em;
  white-space: nowrap;
}
.gf-input {
  background: rgba(255,255,255,0.05);
  border: 1px solid rgba(255,255,255,0.1);
  border-radius: 8px;
  color: rgba(240,253,249,0.85);
  font-size: 12px;
  padding: 6px 10px;
  outline: none;
  transition: border-color .2s;
}
.gf-input:focus { border-color: rgba(16,185,129,0.5); }
.gf-input[type="date"] { width: 130px; color-scheme: dark; }
.gf-num { width: 90px; }
.gf-select {
  background: rgba(255,255,255,0.05);
  border: 1px solid rgba(255,255,255,0.1);
  border-radius: 8px;
  color: rgba(240,253,249,0.85);
  font-size: 12px;
  padding: 6px 10px;
  outline: none;
  cursor: pointer;
  min-width: 130px;
}
.gf-select:focus { border-color: rgba(16,185,129,0.5); }
.gf-sep { font-size: 11px; color: rgba(240,253,249,0.3); }
.gf-na { font-size: 12px; color: rgba(240,253,249,0.25); }
.gf-reset {
  display: flex; align-items: center; gap: 5px;
  background: rgba(239,68,68,0.1);
  border: 1px solid rgba(239,68,68,0.25);
  border-radius: 8px;
  color: #f87171;
  font-size: 12px; font-weight: 600;
  padding: 6px 12px;
  cursor: pointer;
  transition: background .2s;
}
.gf-reset:hover { background: rgba(239,68,68,0.18); }
.gf-count {
  margin-left: auto;
  font-size: 11px;
  color: rgba(240,253,249,0.35);
}

.table-toolbar {
  margin-bottom: 14px;
  display: flex;
  align-items: center;
  gap: 12px;
}
.btn-add-user {
  display: flex; align-items: center; gap: 6px;
  padding: 8px 14px;
  background: rgba(16,185,129,0.12);
  border: 1px solid rgba(16,185,129,0.3);
  border-radius: 8px;
  color: #10b981;
  font-size: 13px; font-weight: 600;
  cursor: pointer; white-space: nowrap;
  transition: background .2s;
}
.btn-add-user:hover { background: rgba(16,185,129,0.2); }
.add-user-fields { display: flex; flex-direction: column; gap: 14px; margin: 16px 0; }
.add-field-group { display: flex; flex-direction: column; gap: 6px; }
.add-field-group label { font-size: 11px; font-weight: 600; color: rgba(240,253,249,0.45); text-transform: uppercase; letter-spacing: .04em; }
.search-input {
  background: rgba(255,255,255,0.04);
  border: 1px solid rgba(255,255,255,0.1);
  border-radius: 8px;
  color: #f0fdf9;
  padding: 8px 14px;
  font-size: 13px;
  width: 280px;
  outline: none;
  transition: border-color 0.2s;
}
.search-input::placeholder { color: rgba(240,253,249,0.3); }
.search-input:focus { border-color: rgba(16,185,129,0.5); }

/* Table */
.table-wrap { overflow-x: auto; }
.admin-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13.5px;
}
.admin-table th {
  text-align: left;
  padding: 10px 14px;
  color: rgba(240,253,249,0.35);
  font-size: 11px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  border-bottom: 1px solid rgba(255,255,255,0.07);
}
.admin-table td {
  padding: 11px 14px;
  border-bottom: 1px solid rgba(255,255,255,0.04);
  color: rgba(240,253,249,0.85);
  vertical-align: middle;
}
.admin-table tbody tr:hover td {
  background: rgba(255,255,255,0.025);
}
.id-cell   { color: rgba(240,253,249,0.3); font-size: 12px; font-family: monospace; }
.num-cell  { color: #10b981; font-weight: 600; }
.date-cell { color: rgba(240,253,249,0.45); font-size: 12px; }
.email-cell{ color: rgba(240,253,249,0.6); font-size: 12.5px; }
.empty-row { text-align: center; color: rgba(240,253,249,0.25); padding: 32px; }

/* Badge */
.badge {
  display: inline-block;
  padding: 2px 10px;
  border-radius: 20px;
  font-size: 11px;
  font-weight: 600;
}
.badge-green { background: rgba(16,185,129,0.15); color: #34d399; }
.badge-gray  { background: rgba(255,255,255,0.06); color: rgba(240,253,249,0.4); }
.badge-blue  { background: rgba(59,130,246,0.12); color: #60a5fa; }

/* Role select */
.role-select {
  border-radius: 20px;
  padding: 3px 10px;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  outline: none;
  border: 1px solid;
  appearance: none;
  -webkit-appearance: none;
  text-align: center;
}
.role-select.admin  { background: rgba(248,113,113,0.15); color: #f87171; border-color: rgba(248,113,113,0.3); }
.role-select.editor { background: rgba(16,185,129,0.15);  color: #34d399; border-color: rgba(16,185,129,0.3); }
.role-select.viewer { background: rgba(96,165,250,0.15);  color: #60a5fa; border-color: rgba(96,165,250,0.3); }
.role-select option { background: #0d1f1b; color: #f0fdf9; font-weight: 400; }

/* Action buttons */
.btn-delete, .btn-view, .btn-edit {
  background: none;
  border: none;
  cursor: pointer;
  padding: 6px;
  border-radius: 6px;
  transition: all 0.2s;
  color: rgba(240,253,249,0.35);
  display: inline-flex;
  align-items: center;
}
.btn-delete:hover { color: #f87171; background: rgba(248,113,113,0.1); }
.btn-view:hover   { color: #60a5fa; background: rgba(59,130,246,0.1); }
.btn-edit:hover   { color: #34d399; background: rgba(16,185,129,0.1); }

/* Loading / Error */
.loading-state, .error-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 12px;
  padding: 60px;
  color: rgba(240,253,249,0.4);
}
.spinner {
  width: 32px; height: 32px;
  border: 3px solid rgba(16,185,129,0.2);
  border-top-color: #10b981;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }
.error-state button {
  padding: 8px 18px;
  background: rgba(16,185,129,0.1);
  border: 1px solid rgba(16,185,129,0.3);
  border-radius: 8px;
  color: #10b981;
  cursor: pointer;
}

/* Confirm modal */
.modal-overlay {
  position: fixed; inset: 0;
  background: rgba(0,0,0,0.7);
  display: flex; align-items: center; justify-content: center;
  z-index: 1000;
}
.confirm-modal {
  background: #0d1f1b;
  border: 1px solid rgba(255,255,255,0.1);
  border-radius: 16px;
  padding: 28px 32px;
  width: 380px;
  text-align: center;
}
.confirm-icon { margin-bottom: 14px; }
.confirm-modal h3 { font-size: 17px; font-weight: 700; margin: 0 0 10px; color: #f0fdf9; }
.confirm-modal p  { font-size: 13.5px; color: rgba(240,253,249,0.65); margin: 0 0 6px; }
.confirm-warn { color: #f87171 !important; font-size: 12px !important; }
.confirm-actions { display: flex; gap: 10px; margin-top: 20px; }
.btn-cancel {
  flex: 1; padding: 10px;
  background: rgba(255,255,255,0.05);
  border: 1px solid rgba(255,255,255,0.1);
  border-radius: 8px;
  color: rgba(240,253,249,0.7);
  cursor: pointer; font-size: 13.5px;
  transition: all 0.2s;
}
.btn-cancel:hover { background: rgba(255,255,255,0.08); }
.btn-confirm-delete {
  flex: 1; padding: 10px;
  background: rgba(239,68,68,0.15);
  border: 1px solid rgba(239,68,68,0.3);
  border-radius: 8px;
  color: #f87171;
  cursor: pointer; font-size: 13.5px;
  transition: all 0.2s;
}
.btn-confirm-delete:hover:not(:disabled) { background: rgba(239,68,68,0.25); }
.btn-confirm-delete:disabled { opacity: 0.5; cursor: not-allowed; }

/* ── Change password ───────────────────────────────────────────── */
.btn-pwd {
  background: rgba(16,185,129,0.1);
  border: 1px solid rgba(16,185,129,0.2);
  border-radius: 6px; padding: 5px 8px;
  cursor: pointer; font-size: 13px;
  transition: all 0.15s; margin-right: 4px;
}
.btn-pwd:hover { background: rgba(16,185,129,0.2); }
.actions-cell { display: flex; align-items: center; gap: 4px; }

.pwd-field {
  position: relative; margin: 12px 0 4px;
}
.pwd-input {
  width: 100%; padding: 10px 42px 10px 12px;
  background: rgba(255,255,255,0.05);
  border: 1px solid rgba(255,255,255,0.1);
  border-radius: 8px; color: #f0fdf9;
  font-size: 13px; outline: none; box-sizing: border-box;
  transition: border-color 0.2s;
}
.pwd-input:focus { border-color: rgba(16,185,129,0.45); }
.pwd-toggle {
  position: absolute; right: 10px; top: 50%; transform: translateY(-50%);
  background: none; border: none; cursor: pointer; font-size: 14px;
}
.pwd-error   { font-size: 12px; color: #f87171; margin: 4px 0 0; }
.pwd-success { font-size: 12px; color: #10b981; margin: 4px 0 0; }

/* ── Notifications ─────────────────────────────────────────────── */
.notif-badge {
  display: inline-flex; align-items: center; justify-content: center;
  min-width: 18px; height: 18px;
  background: #ef4444; color: #fff;
  border-radius: 999px; font-size: 10px; font-weight: 700;
  padding: 0 5px; margin-left: 4px;
}
.notif-header {
  margin-bottom: 16px;
}
.notif-count {
  font-size: 13px; color: rgba(240,253,249,0.5);
}
.empty-notif {
  display: flex; flex-direction: column; align-items: center; gap: 12px;
  padding: 60px 20px; color: rgba(240,253,249,0.3); font-size: 13px;
}
.notif-list {
  display: flex; flex-direction: column; gap: 12px;
}
.notif-card {
  display: flex; align-items: flex-start; gap: 14px;
  background: rgba(255,255,255,0.03);
  border: 1px solid rgba(239,68,68,0.2);
  border-left: 3px solid #ef4444;
  border-radius: 12px; padding: 16px 18px;
  transition: background 0.15s;
}
.notif-card:hover { background: rgba(255,255,255,0.05); }
.notif-icon { font-size: 22px; flex-shrink: 0; }
.notif-body { flex: 1; display: flex; flex-direction: column; gap: 4px; }
.notif-title { font-size: 14px; font-weight: 600; color: rgba(240,253,249,0.9); }
.notif-email { font-size: 13px; color: #10b981; }
.notif-message { font-size: 12px; color: rgba(240,253,249,0.55); font-style: italic; }
.notif-meta {
  display: flex; gap: 16px; font-size: 11px; color: rgba(240,253,249,0.35);
  margin-top: 4px;
}
.notif-dismiss {
  background: rgba(16,185,129,0.1);
  border: 1px solid rgba(16,185,129,0.25);
  color: #10b981;
  border-radius: 8px; padding: 6px 14px;
  font-size: 12px; font-weight: 600; cursor: pointer;
  transition: all 0.15s; flex-shrink: 0;
}
.notif-dismiss:hover { background: rgba(16,185,129,0.2); }

/* ── STATS ── */
.stats-section-title {
  font-size: 13px; font-weight: 700;
  color: rgba(240,253,249,0.5);
  letter-spacing: 1px; text-transform: uppercase;
  margin-bottom: 12px; margin-top: 4px;
}
.stats-kpi-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
  gap: 12px; margin-bottom: 24px;
}
.stats-kpi {
  background: rgba(255,255,255,0.04);
  border: 1px solid rgba(255,255,255,0.08);
  border-radius: 12px; padding: 16px 14px;
  display: flex; flex-direction: column; gap: 4px; text-align: center;
}
.stats-kpi.green { border-color: rgba(16,185,129,0.25); }
.stats-kpi-val { font-size: 28px; font-weight: 700; color: #f0fdf9; }
.stats-kpi.green .stats-kpi-val { color: #34d399; }
.stats-kpi-label { font-size: 11px; color: rgba(240,253,249,0.4); }
.stats-two-cols {
  display: grid; grid-template-columns: 1fr 1fr;
  gap: 16px; margin-bottom: 16px;
}
.stats-card {
  background: rgba(255,255,255,0.03);
  border: 1px solid rgba(255,255,255,0.07);
  border-radius: 14px; padding: 18px 16px;
}
.stats-bar-list { display: flex; flex-direction: column; gap: 8px; }
.stats-bar-row { display: flex; align-items: center; gap: 8px; }
.stats-bar-label { font-size: 12px; color: rgba(240,253,249,0.6); width: 130px; flex-shrink: 0; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.stats-bar-track { flex: 1; height: 8px; background: rgba(255,255,255,0.07); border-radius: 4px; overflow: hidden; }
.stats-bar-fill { height: 100%; background: #3b82f6; border-radius: 4px; transition: width 0.4s ease; }
.stats-bar-count { font-size: 12px; color: rgba(240,253,249,0.5); width: 28px; text-align: right; flex-shrink: 0; }
</style>
