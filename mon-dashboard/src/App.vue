<template>
  <RouterView v-slot="{ Component }">
    <transition name="page" mode="out-in">
      <component :is="Component" />
    </transition>
  </RouterView>
</template>

<script setup lang="ts">
import { RouterView } from 'vue-router'
</script>

<style>
/* Reset global styles if needed */
body {
  margin: 0;
  padding: 0;
  background-color: #050d0d;
}

.page-enter-active,
.page-leave-active {
  transition: opacity 0.22s ease, transform 0.22s ease;
}
.page-enter-from {
  opacity: 0;
  transform: translateY(10px);
}
.page-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}

/* ═══════════════════════════════════════════════════════════════
   THÈME CLAIR — overrides globaux (injectés après tous les scoped)
   Cible les couleurs hardcodées #f0fdf4 / #f0fdf9 / #e2e8f0 / #d1fae5
   qui deviennent invisibles sur fond blanc.
   ═══════════════════════════════════════════════════════════════ */

/* ── Fond et texte global ── */
[data-theme="white"] body,
[data-theme="white"] #app { background: #f1f5f9 !important; color: #1e293b !important; }

/* ── Toute surface dark → blanche ── */
[data-theme="white"] .admin-view,
[data-theme="white"] .dataset-page,
[data-theme="white"] .dashboard-list,
[data-theme="white"] .profile-view { background: #f1f5f9 !important; }

/* ── AdminView ── */
[data-theme="white"] .admin-title           { color: #0f172a !important; }
[data-theme="white"] .admin-subtitle        { color: #64748b !important; }
[data-theme="white"] .stat-card             { background: #fff !important; border-color: rgba(15,23,42,.1) !important; box-shadow: 0 2px 8px rgba(0,0,0,.06) !important; }
[data-theme="white"] .stat-value            { color: #0f172a !important; }
[data-theme="white"] .stat-label            { color: #64748b !important; }
[data-theme="white"] .tabs-bar              { background: rgba(15,23,42,.04) !important; border-color: rgba(15,23,42,.08) !important; }
[data-theme="white"] .tab-btn               { color: #64748b !important; }
[data-theme="white"] .tab-btn:hover         { color: #0f172a !important; }
[data-theme="white"] .tab-btn.active        { color: #059669 !important; border-bottom-color: #059669 !important; }
[data-theme="white"] .global-filters        { background: #fff !important; border-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .gf-label              { color: #64748b !important; }
[data-theme="white"] .gf-sep               { color: #94a3b8 !important; }
[data-theme="white"] .gf-na               { color: #94a3b8 !important; }
[data-theme="white"] .gf-input,
[data-theme="white"] .gf-select            { background: #f8fafc !important; border-color: rgba(15,23,42,.12) !important; color: #1e293b !important; }
[data-theme="white"] .gf-count             { color: #475569 !important; }
[data-theme="white"] .table-toolbar        { border-color: rgba(15,23,42,.08) !important; }
[data-theme="white"] .btn-add-user         { color: #059669 !important; border-color: rgba(5,150,105,.25) !important; background: rgba(5,150,105,.06) !important; }
[data-theme="white"] .data-table           { background: #fff !important; }
[data-theme="white"] .data-table thead th  { background: rgba(15,23,42,.03) !important; color: #64748b !important; border-color: rgba(15,23,42,.08) !important; }
[data-theme="white"] .data-table tbody td  { color: #334155 !important; border-color: rgba(15,23,42,.05) !important; }
[data-theme="white"] .data-table tbody tr:hover td { background: rgba(15,23,42,.02) !important; }
[data-theme="white"] .badge-gray           { background: rgba(15,23,42,.06) !important; color: #475569 !important; border-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .role-select          { background: #f8fafc !important; border-color: rgba(15,23,42,.12) !important; color: #334155 !important; }
[data-theme="white"] .role-select option   { background: #fff !important; color: #1e293b !important; }
[data-theme="white"] .btn-delete           { color: #94a3b8 !important; }
[data-theme="white"] .btn-delete:hover     { color: #ef4444 !important; background: rgba(239,68,68,.08) !important; }
[data-theme="white"] .confirm-modal        { background: #fff !important; border-color: rgba(15,23,42,.1) !important; box-shadow: 0 24px 60px rgba(0,0,0,.15) !important; }
[data-theme="white"] .confirm-modal h3     { color: #0f172a !important; }
[data-theme="white"] .confirm-modal p      { color: #475569 !important; }
[data-theme="white"] .pwd-input            { background: #f8fafc !important; border-color: rgba(15,23,42,.12) !important; color: #1e293b !important; }
[data-theme="white"] .pwd-input::placeholder { color: #94a3b8 !important; }
[data-theme="white"] .notification-item    { background: #fff !important; border-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .notif-action         { color: #0f172a !important; }
[data-theme="white"] .notif-meta           { color: #64748b !important; }
[data-theme="white"] .notif-details        { color: #475569 !important; }

/* ── DatasetUploadView ── */
[data-theme="white"] .dataset-page         { color: #1e293b !important; }
[data-theme="white"] .dataset-page .page-title   { color: #0f172a !important; }
[data-theme="white"] .dataset-page .page-subtitle { color: #64748b !important; }
[data-theme="white"] .upload-zone          { background: #fff !important; border-color: rgba(15,23,42,.15) !important; }
[data-theme="white"] .upload-zone:hover    { border-color: #10b981 !important; }
[data-theme="white"] .upload-title         { color: #0f172a !important; }
[data-theme="white"] .upload-sub           { color: #64748b !important; }
[data-theme="white"] .file-name            { color: #0f172a !important; }
[data-theme="white"] .file-size            { color: #64748b !important; }
[data-theme="white"] .result-info          { color: #475569 !important; }
[data-theme="white"] .result-info strong   { color: #0f172a !important; }
[data-theme="white"] .col-chip             { background: rgba(15,23,42,.06) !important; border-color: rgba(15,23,42,.1) !important; color: #334155 !important; }
[data-theme="white"] .preview-table        { background: #fff !important; }
[data-theme="white"] .preview-table th     { background: rgba(16,185,129,.06) !important; color: #059669 !important; border-color: rgba(15,23,42,.08) !important; }
[data-theme="white"] .preview-table td     { color: #334155 !important; border-color: rgba(15,23,42,.05) !important; }
[data-theme="white"] .result-badge         { color: #059669 !important; border-color: rgba(5,150,105,.3) !important; background: rgba(5,150,105,.08) !important; }
[data-theme="white"] .btn-secondary        { background: rgba(15,23,42,.05) !important; border-color: rgba(15,23,42,.12) !important; color: #334155 !important; }

/* ── DashboardListView ── */
[data-theme="white"] .page-title           { color: #0f172a !important; }
[data-theme="white"] .page-subtitle        { color: #64748b !important; }
[data-theme="white"] .count-pill           { background: rgba(5,150,105,.1) !important; color: #059669 !important; border-color: rgba(5,150,105,.2) !important; }
[data-theme="white"] .stats-bar            { background: #fff !important; border-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .stat-number          { color: #0f172a !important; }
[data-theme="white"] .stat-sep             { background: rgba(15,23,42,.1) !important; }
[data-theme="white"] .search-wrapper .search-input { background: #fff !important; border-color: rgba(15,23,42,.15) !important; color: #1e293b !important; }
[data-theme="white"] .search-input::placeholder     { color: #94a3b8 !important; }
[data-theme="white"] .filter-tabs          { background: rgba(15,23,42,.04) !important; border-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .tab-btn              { color: #64748b !important; }
[data-theme="white"] .tab-btn.active       { background: rgba(5,150,105,.1) !important; color: #059669 !important; }
[data-theme="white"] .sort-select          { background-color: #fff !important; border-color: rgba(15,23,42,.15) !important; color: #475569 !important; }
[data-theme="white"] .sort-select option   { background: #fff !important; color: #1e293b !important; }
[data-theme="white"] .view-toggle          { background: rgba(15,23,42,.04) !important; border-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .toggle-btn           { color: #94a3b8 !important; }
[data-theme="white"] .toggle-btn.active    { background: rgba(5,150,105,.1) !important; color: #059669 !important; }
[data-theme="white"] .filter-toggle-btn    { background: #fff !important; border-color: rgba(15,23,42,.12) !important; color: #475569 !important; }
[data-theme="white"] .filter-panel         { background: #fff !important; border-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .fp-label             { color: #64748b !important; }
[data-theme="white"] .fp-input             { background: #f8fafc !important; border-color: rgba(15,23,42,.12) !important; color: #1e293b !important; }
[data-theme="white"] .fp-pill              { background: #f8fafc !important; border-color: rgba(15,23,42,.1) !important; color: #64748b !important; }
[data-theme="white"] .fp-pill.active       { background: #10b981 !important; color: #fff !important; }
[data-theme="white"] .dash-card            { background: #fff !important; border-color: rgba(15,23,42,.1) !important; box-shadow: 0 2px 8px rgba(0,0,0,.06) !important; }
[data-theme="white"] .dash-card:hover      { border-color: rgba(5,150,105,.3) !important; box-shadow: 0 8px 24px rgba(0,0,0,.1) !important; }
[data-theme="white"] .card-title           { color: #0f172a !important; }
[data-theme="white"] .card-desc            { color: #64748b !important; }
[data-theme="white"] .widget-count         { color: #94a3b8 !important; }
[data-theme="white"] .dash-date            { color: #94a3b8 !important; }
[data-theme="white"] .tag                  { background: rgba(15,23,42,.06) !important; border-color: rgba(15,23,42,.1) !important; color: #475569 !important; }
[data-theme="white"] .card-actions         { border-top-color: rgba(15,23,42,.07) !important; }
[data-theme="white"] .action-btn           { background: rgba(15,23,42,.04) !important; border-color: rgba(15,23,42,.1) !important; color: #64748b !important; }
[data-theme="white"] .action-btn:hover     { background: rgba(15,23,42,.08) !important; color: #1e293b !important; }
[data-theme="white"] .list-view            { border-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .list-header          { background: rgba(15,23,42,.03) !important; color: #94a3b8 !important; }
[data-theme="white"] .list-row             { border-top-color: rgba(15,23,42,.06) !important; }
[data-theme="white"] .list-row:hover       { background: rgba(15,23,42,.02) !important; }
[data-theme="white"] .list-title           { color: #0f172a !important; }
[data-theme="white"] .list-desc-small      { color: #94a3b8 !important; }
[data-theme="white"] .list-cell            { color: #475569 !important; }
[data-theme="white"] .modal                { background: #fff !important; border-color: rgba(15,23,42,.12) !important; }
[data-theme="white"] .modal-header h2      { color: #0f172a !important; }
[data-theme="white"] .modal-footer         { border-top-color: rgba(15,23,42,.08) !important; }
[data-theme="white"] .form-group label     { color: #475569 !important; }
[data-theme="white"] .form-input           { background: #f8fafc !important; border-color: rgba(15,23,42,.12) !important; color: #1e293b !important; }
[data-theme="white"] .form-input::placeholder { color: #94a3b8 !important; }
[data-theme="white"] .modal-close          { color: #94a3b8 !important; }
[data-theme="white"] .delete-warning       { color: #475569 !important; }
[data-theme="white"] .delete-warning strong { color: #0f172a !important; }
[data-theme="white"] .btn-cancel           { border-color: rgba(15,23,42,.12) !important; color: #475569 !important; }

/* ── AppSidebar ── */
[data-theme="white"] .app-sidebar          { background: #fff !important; border-right-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .sidebar-logo-text    { color: #0f172a !important; }
[data-theme="white"] .sidebar-logo-tag     { color: #64748b !important; }
[data-theme="white"] .sidebar-nav-item     { color: #334155 !important; }
[data-theme="white"] .sidebar-nav-item:hover { background: rgba(15,23,42,.05) !important; color: #0f172a !important; }
[data-theme="white"] .sidebar-footer       { border-top-color: rgba(15,23,42,.08) !important; }
[data-theme="white"] .user-info .name      { color: #0f172a !important; }
[data-theme="white"] .user-info .email     { color: #64748b !important; }
[data-theme="white"] .section-title        { color: #94a3b8 !important; }
[data-theme="white"] .add-dash-btn         { background: rgba(15,23,42,.04) !important; border-color: rgba(15,23,42,.12) !important; color: #475569 !important; }
[data-theme="white"] .dashboard-item       { color: #334155 !important; }
[data-theme="white"] .dashboard-item:hover,
[data-theme="white"] .dashboard-item.active { background: rgba(15,23,42,.06) !important; color: #0f172a !important; }
[data-theme="white"] .palette-item         { background: rgba(15,23,42,.03) !important; border-color: rgba(15,23,42,.1) !important; }

/* ── AppNavbar ── */
[data-theme="white"] .navbar               { background: rgba(255,255,255,.95) !important; border-bottom-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .navbar .page-title   { color: #0f172a !important; }
[data-theme="white"] .page-breadcrumb      { color: #64748b !important; }
[data-theme="white"] .date-chip            { background: rgba(15,23,42,.05) !important; border-color: rgba(15,23,42,.1) !important; color: #475569 !important; }
[data-theme="white"] .user-email           { color: #334155 !important; }
[data-theme="white"] .d-name               { color: #334155 !important; }
[data-theme="white"] .dropdown             { background: #fff !important; border-color: rgba(15,23,42,.1) !important; box-shadow: 0 8px 24px rgba(0,0,0,.1) !important; }
[data-theme="white"] .dropdown-item        { color: #334155 !important; }
[data-theme="white"] .dropdown-item:hover  { background: rgba(15,23,42,.05) !important; color: #0f172a !important; }
[data-theme="white"] .dropdown-sep         { background: rgba(15,23,42,.08) !important; }

/* ── ProfileView ── */
[data-theme="white"] .profile-header       { background: #fff !important; border-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .profile-name         { color: #0f172a !important; }
[data-theme="white"] .profile-email-sub    { color: #64748b !important; }
[data-theme="white"] .info-card,
[data-theme="white"] .change-card          { background: #fff !important; border-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .card-title-row h2    { color: #0f172a !important; }
[data-theme="white"] .info-label           { color: #64748b !important; }
[data-theme="white"] .info-value           { color: #1e293b !important; }
[data-theme="white"] .change-input         { background: #f8fafc !important; border-color: rgba(15,23,42,.12) !important; color: #1e293b !important; }
[data-theme="white"] .change-input::placeholder { color: #94a3b8 !important; }

/* ── VersionHistoryModal ── */
[data-theme="white"] .vh-panel             { background: #fff !important; border-color: rgba(15,23,42,.1) !important; box-shadow: -8px 0 32px rgba(0,0,0,.1) !important; }
[data-theme="white"] .vh-header            { border-bottom-color: rgba(15,23,42,.08) !important; }
[data-theme="white"] .vh-header-title      { color: #0f172a !important; }
[data-theme="white"] .vh-close-btn         { color: #94a3b8 !important; }
[data-theme="white"] .vh-item              { background: #f8fafc !important; border-color: rgba(15,23,42,.08) !important; }
[data-theme="white"] .vh-item:hover        { border-color: rgba(5,150,105,.25) !important; }
[data-theme="white"] .vh-version-num,
[data-theme="white"] .vh-label             { color: #0f172a !important; }
[data-theme="white"] .vh-meta             { color: #94a3b8 !important; }
[data-theme="white"] .vh-widget-count,
[data-theme="white"] .vh-widget-type       { color: #64748b !important; }
[data-theme="white"] .vh-restore-btn       { color: #059669 !important; }

/* ── ShareModal ── */
[data-theme="white"] .sm-panel             { background: #fff !important; border-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .sm-title             { color: #0f172a !important; }
[data-theme="white"] .sm-subtitle          { color: #64748b !important; }
[data-theme="white"] .sm-close             { color: #94a3b8 !important; }
[data-theme="white"] .sm-label,
[data-theme="white"] .sm-result-label      { color: #475569 !important; }
[data-theme="white"] .sm-pub-url           { background: #f8fafc !important; border-color: rgba(15,23,42,.12) !important; color: #334155 !important; }
[data-theme="white"] .sm-perm-btn          { background: #f8fafc !important; border-color: rgba(15,23,42,.1) !important; color: #475569 !important; }
[data-theme="white"] .sm-new-btn           { color: #64748b !important; border-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .expiry               { background: rgba(15,23,42,.05) !important; color: #64748b !important; border-color: rgba(15,23,42,.1) !important; }

/* ── SqlConnect / RestConnect modals ── */
[data-theme="white"] .sql-modal,
[data-theme="white"] .rest-modal           { background: #fff !important; color: #1e293b !important; border-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .sql-label,
[data-theme="white"] .rest-label           { color: #475569 !important; }
[data-theme="white"] .sql-input,
[data-theme="white"] .sql-select,
[data-theme="white"] .rest-input           { background: #f8fafc !important; border-color: rgba(15,23,42,.12) !important; color: #1e293b !important; }
[data-theme="white"] .sql-input::placeholder,
[data-theme="white"] .rest-input::placeholder { color: #94a3b8 !important; }
[data-theme="white"] .sql-close,
[data-theme="white"] .rest-close           { color: #94a3b8 !important; }
[data-theme="white"] .sql-quick            { color: #475569 !important; border-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .sql-btn--ghost       { color: #475569 !important; }
[data-theme="white"] .sql-col-name         { color: #1e293b !important; }
[data-theme="white"] .sql-col-type         { color: #64748b !important; }

/* ── TableWidget ── */
[data-theme="white"] .tbl-search           { background: rgba(15,23,42,.04) !important; border-color: rgba(15,23,42,.1) !important; color: #1e293b !important; }
[data-theme="white"] .tbl-search::placeholder { color: #94a3b8 !important; }
[data-theme="white"] .data-table           { background: #fff !important; }
[data-theme="white"] .data-table th        { background: rgba(15,23,42,.03) !important; color: #64748b !important; border-color: rgba(15,23,42,.08) !important; }
[data-theme="white"] .data-table td        { color: #334155 !important; border-color: rgba(15,23,42,.05) !important; }
[data-theme="white"] .data-table tr:hover td { background: rgba(15,23,42,.02) !important; }
[data-theme="white"] .tbl-page-info        { color: #64748b !important; }
[data-theme="white"] .tbl-pagination-btn   { color: #475569 !important; border-color: rgba(15,23,42,.1) !important; }

/* ── KpiCardWidget ── */
[data-theme="white"] .kpi-card             { background: #fff !important; border-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .kpi-title            { color: #64748b !important; }
[data-theme="white"] .kpi-value            { color: #0f172a !important; }
[data-theme="white"] .kpi-unit             { color: #94a3b8 !important; }

/* ── Login / Register ── */
[data-theme="white"] .auth-box             { background: #fff !important; border-color: rgba(15,23,42,.1) !important; box-shadow: 0 12px 40px rgba(0,0,0,.1) !important; }
[data-theme="white"] .auth-box h1,
[data-theme="white"] .auth-box h2          { color: #0f172a !important; }
[data-theme="white"] .auth-box p           { color: #64748b !important; }
[data-theme="white"] .auth-box label       { color: #475569 !important; }
[data-theme="white"] .auth-box input       { background: #f8fafc !important; border-color: rgba(15,23,42,.12) !important; color: #1e293b !important; }
[data-theme="white"] .auth-box input::placeholder { color: #94a3b8 !important; }
[data-theme="white"] .auth-box a           { color: #059669 !important; }

/* ── Builder : Properties Panel (.pp) ── */
[data-theme="white"] .pp                        { border-left-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .pp-header                 { border-bottom-color: rgba(15,23,42,.08) !important; }
[data-theme="white"] .pp-idle-title             { color: #1e293b !important; }
[data-theme="white"] .pp-title-input            { color: #1e293b !important; background: rgba(15,23,42,.04) !important; border-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .pp-title-input::placeholder { color: rgba(15,23,42,.3) !important; }
[data-theme="white"] .pp-close-btn              { color: rgba(15,23,42,.5) !important; }
[data-theme="white"] .pp-close-btn:hover        { background: rgba(239,68,68,.1) !important; color: #ef4444 !important; }
[data-theme="white"] .pp-tabs                   { border-bottom-color: rgba(15,23,42,.08) !important; background: rgba(15,23,42,.02) !important; }
[data-theme="white"] .pp-tab                    { color: rgba(15,23,42,.5) !important; }
[data-theme="white"] .pp-tab:hover              { color: #1e293b !important; background: rgba(15,23,42,.04) !important; }
[data-theme="white"] .pp-tab.active             { color: #059669 !important; border-bottom-color: #059669 !important; background: rgba(5,150,105,.05) !important; }
[data-theme="white"] .pp-body                   { background: #f8fafc !important; }
[data-theme="white"] .pp-body::-webkit-scrollbar-thumb { background: rgba(15,23,42,.1) !important; }
[data-theme="white"] .pp-toggle                 { border-color: rgba(15,23,42,.12) !important; background: rgba(15,23,42,.03) !important; color: rgba(15,23,42,.4) !important; }
[data-theme="white"] .pp-section                { border-bottom-color: rgba(15,23,42,.07) !important; }
[data-theme="white"] .pp-section-toggle         { color: #334155 !important; }
[data-theme="white"] .pp-section-toggle:hover   { background: rgba(15,23,42,.04) !important; }
[data-theme="white"] .pp-section-toggle i       { color: rgba(15,23,42,.3) !important; }
[data-theme="white"] .pp-collapsed-hint         { color: rgba(15,23,42,.3) !important; }
[data-theme="white"] .pp-footer                 { border-top-color: rgba(15,23,42,.08) !important; }
[data-theme="white"] .pp-section-badge          { background: rgba(15,23,42,.06) !important; color: #475569 !important; }

/* ── Builder : Config Drawer (.cfg-drawer) ── */
[data-theme="white"] .cfg-drawer                { background: #ffffff !important; border-color: rgba(15,23,42,.1) !important; color: #1e293b !important; }
[data-theme="white"] .cfg-header                { border-bottom-color: rgba(15,23,42,.08) !important; }
[data-theme="white"] .cfg-type-badge            { background: rgba(15,23,42,.06) !important; color: #475569 !important; }
[data-theme="white"] .cfg-title-input           { background: rgba(15,23,42,.04) !important; border-color: rgba(15,23,42,.1) !important; color: #1e293b !important; }
[data-theme="white"] .cfg-title-input::placeholder { color: rgba(15,23,42,.3) !important; }
[data-theme="white"] .cfg-close                 { color: rgba(15,23,42,.5) !important; }
[data-theme="white"] .cfg-close:hover           { background: rgba(239,68,68,.1) !important; color: #ef4444 !important; }
[data-theme="white"] .cfg-tabs                  { border-bottom-color: rgba(15,23,42,.08) !important; background: rgba(15,23,42,.02) !important; }
[data-theme="white"] .cfg-tab                   { color: rgba(15,23,42,.55) !important; }
[data-theme="white"] .cfg-tab:hover             { color: #1e293b !important; }
[data-theme="white"] .cfg-tab.active            { color: #059669 !important; border-bottom-color: #059669 !important; }
[data-theme="white"] .cfg-body                  { color: #1e293b !important; }
[data-theme="white"] .cfg-body::-webkit-scrollbar-thumb { background: rgba(15,23,42,.1) !important; }
[data-theme="white"] .cfg-section               { border-bottom-color: rgba(15,23,42,.07) !important; }
[data-theme="white"] .cfg-section-title         { color: rgba(15,23,42,.45) !important; }
[data-theme="white"] .cfg-field label           { color: rgba(15,23,42,.6) !important; }
[data-theme="white"] .cfg-field label strong    { color: rgba(15,23,42,.85) !important; }
[data-theme="white"] .cfg-hint                  { color: rgba(15,23,42,.4) !important; }
[data-theme="white"] .cfg-toggle-sub            { color: rgba(15,23,42,.4) !important; }
[data-theme="white"] .pie-type-hint             { color: rgba(15,23,42,.35) !important; border-top-color: rgba(15,23,42,.07) !important; }
[data-theme="white"] .pie-fields-banner         { background: rgba(99,102,241,.06) !important; border-color: rgba(99,102,241,.18) !important; color: rgba(15,23,42,.6) !important; }
[data-theme="white"] .pie-fields-banner strong  { color: rgba(15,23,42,.85) !important; }
[data-theme="white"] .pie-info-banner           { background: rgba(245,158,11,.06) !important; border-color: rgba(245,158,11,.18) !important; color: rgba(15,23,42,.6) !important; }
[data-theme="white"] .pie-info-banner strong    { color: rgba(15,23,42,.8) !important; }
[data-theme="white"] .cfg-input,
[data-theme="white"] .cfg-select,
[data-theme="white"] .cfg-textarea              { background: rgba(15,23,42,.04) !important; border-color: rgba(15,23,42,.12) !important; color: #1e293b !important; }
[data-theme="white"] .cfg-input::placeholder,
[data-theme="white"] .cfg-textarea::placeholder { color: rgba(15,23,42,.3) !important; }
[data-theme="white"] .cfg-select option         { background: #f8fafc !important; color: #1e293b !important; }
[data-theme="white"] .cfg-row-sep               { background: rgba(15,23,42,.07) !important; }
[data-theme="white"] .cfg-micro-btn             { color: rgba(15,23,42,.5) !important; border-color: rgba(15,23,42,.1) !important; background: rgba(15,23,42,.03) !important; }
[data-theme="white"] .cfg-micro-btn:hover       { background: rgba(15,23,42,.08) !important; color: #1e293b !important; }
[data-theme="white"] .cfg-micro-btn.active      { background: rgba(5,150,105,.1) !important; color: #059669 !important; }
[data-theme="white"] .cfg-btn-group button      { color: rgba(15,23,42,.5) !important; border-color: rgba(15,23,42,.1) !important; background: rgba(15,23,42,.03) !important; }
[data-theme="white"] .cfg-btn-group button:hover { background: rgba(15,23,42,.08) !important; color: #1e293b !important; }
[data-theme="white"] .cfg-btn-group button.active { background: rgba(5,150,105,.1) !important; border-color: rgba(5,150,105,.3) !important; color: #059669 !important; }
[data-theme="white"] .cfg-style-btn             { border-color: rgba(15,23,42,.1) !important; color: rgba(15,23,42,.5) !important; }
[data-theme="white"] .cfg-style-btn:hover       { border-color: rgba(15,23,42,.2) !important; color: #1e293b !important; }
[data-theme="white"] .cfg-icon-btn              { border-color: rgba(15,23,42,.1) !important; color: rgba(15,23,42,.5) !important; }
[data-theme="white"] .cfg-checkbox-item:hover   { background: rgba(15,23,42,.04) !important; }
[data-theme="white"] .cfg-btn-cancel            { background: rgba(15,23,42,.05) !important; border-color: rgba(15,23,42,.1) !important; color: rgba(15,23,42,.7) !important; }
[data-theme="white"] .cfg-btn-cancel:hover      { background: rgba(15,23,42,.09) !important; color: #1e293b !important; }
[data-theme="white"] .cfg-presets-label         { color: rgba(15,23,42,.5) !important; }
[data-theme="white"] .cfg-preset-btn            { border-color: rgba(15,23,42,.1) !important; color: rgba(15,23,42,.6) !important; background: rgba(15,23,42,.03) !important; }
[data-theme="white"] .cfg-preset-btn:hover      { border-color: rgba(5,150,105,.3) !important; background: rgba(5,150,105,.06) !important; }
[data-theme="white"] .cfg-preset-btn.active     { border-color: #059669 !important; background: rgba(5,150,105,.1) !important; color: #059669 !important; }
[data-theme="white"] .cfg-unit-preview          { color: rgba(15,23,42,.5) !important; }

/* ── Builder : Fields Panel ── */
[data-theme="white"] .fields-panel              { border-left-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .fields-panel-title        { color: #0f172a !important; }
[data-theme="white"] .fields-panel-subtitle     { color: #64748b !important; }
[data-theme="white"] .fstat-val                 { color: #059669 !important; }
[data-theme="white"] .fstat-lbl                 { color: rgba(15,23,42,.4) !important; }
[data-theme="white"] .fields-empty-icon         { color: rgba(15,23,42,.15) !important; }
[data-theme="white"] .fields-empty-title        { color: rgba(15,23,42,.45) !important; }
[data-theme="white"] .fields-empty-sub          { color: rgba(15,23,42,.25) !important; }
[data-theme="white"] .field-item                { border-color: rgba(15,23,42,.07) !important; background: rgba(15,23,42,.02) !important; }
[data-theme="white"] .field-item:hover          { background: rgba(15,23,42,.06) !important; }
/* ★ NOM des colonnes : c'est .fi-name qui était blanc sur blanc */
[data-theme="white"] .fi-name                   { color: #1e293b !important; }
[data-theme="white"] .fi-col-name               { color: #1e293b !important; }
[data-theme="white"] .fi-col-type               { color: rgba(15,23,42,.45) !important; }
[data-theme="white"] .fi-axis-hint              { color: rgba(15,23,42,.35) !important; }
[data-theme="white"] .fi-delete-btn             { color: rgba(15,23,42,.35) !important; }
[data-theme="white"] .col-name                  { color: #1e293b !important; }
[data-theme="white"] .cfg-col-name              { color: #1e293b !important; }

/* ── Builder : Axis Wells ── */
[data-theme="white"] .axis-well                 { background: rgba(15,23,42,.03) !important; border-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .axis-well-label           { color: rgba(15,23,42,.6) !important; }
[data-theme="white"] .axis-well-placeholder     { color: rgba(15,23,42,.3) !important; }
[data-theme="white"] .axis-well-header          { border-bottom-color: rgba(15,23,42,.08) !important; }
[data-theme="white"] .aChip-name                { color: #1e293b !important; }
[data-theme="white"] .axis-clear                { color: rgba(15,23,42,.4) !important; }

/* ── Builder : Format Toolbar (.ftb) ── */
[data-theme="white"] .format-toolbar            { background: #ffffff !important; border-bottom-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .ftb-info,
[data-theme="white"] .ftb-title                 { color: #334155 !important; }
[data-theme="white"] .ftb-type-badge            { color: #475569 !important; background: rgba(15,23,42,.06) !important; }
[data-theme="white"] .ftb-btn                   { color: rgba(15,23,42,.55) !important; border-color: rgba(15,23,42,.1) !important; background: rgba(15,23,42,.03) !important; }
[data-theme="white"] .ftb-btn:hover             { background: rgba(15,23,42,.08) !important; color: #1e293b !important; }
[data-theme="white"] .ftb-btn.active            { background: rgba(5,150,105,.1) !important; color: #059669 !important; border-color: rgba(5,150,105,.3) !important; }
[data-theme="white"] .ftb-sep                   { background: rgba(15,23,42,.1) !important; }
[data-theme="white"] .ftb-close-btn             { color: rgba(15,23,42,.4) !important; }

/* ── Builder : Canvas header ── */
[data-theme="white"] .canvas-header             { background: rgba(248,250,252,.95) !important; border-bottom-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .status-badge              { background: rgba(5,150,105,.08) !important; border-color: rgba(5,150,105,.2) !important; color: #059669 !important; }
[data-theme="white"] .header-sep                { background: rgba(15,23,42,.12) !important; }
[data-theme="white"] .header-meta-item          { color: rgba(15,23,42,.45) !important; }
[data-theme="white"] .icon-btn                  { color: rgba(15,23,42,.5) !important; border-color: rgba(15,23,42,.1) !important; background: rgba(15,23,42,.03) !important; }
[data-theme="white"] .icon-btn:hover            { background: rgba(15,23,42,.08) !important; color: #1e293b !important; }
[data-theme="white"] .export-btn                { background: rgba(15,23,42,.04) !important; border-color: rgba(15,23,42,.12) !important; color: #334155 !important; }
[data-theme="white"] .version-topbar-btn        { background: rgba(99,102,241,.06) !important; border-color: rgba(99,102,241,.25) !important; color: #4338ca !important; }
[data-theme="white"] .share-topbar-btn          { background: rgba(74,108,247,.07) !important; border-color: rgba(74,108,247,.25) !important; color: #3b5bdb !important; }

/* ── Builder : Widget card ── */
[data-theme="white"] .widget-card               { background: #ffffff !important; border-color: rgba(15,23,42,.1) !important; box-shadow: 0 2px 12px rgba(0,0,0,.06) !important; }
[data-theme="white"] .widget-header             { border-bottom-color: rgba(15,23,42,.07) !important; }
[data-theme="white"] .widget-title              { color: #1e293b !important; }
[data-theme="white"] .widget-description        { color: rgba(15,23,42,.5) !important; }
[data-theme="white"] .wbtn-config,
[data-theme="white"] .wbtn-delete,
[data-theme="white"] .wbtn-labels,
[data-theme="white"] .wbtn-export               { color: rgba(15,23,42,.4) !important; }

/* ── Builder : Empty state ── */
[data-theme="white"] .empty-hero h2             { color: #1e293b !important; }
[data-theme="white"] .empty-hero p              { color: rgba(15,23,42,.5) !important; }
[data-theme="white"] .fields-hint-bar           { color: rgba(15,23,42,.4) !important; }
[data-theme="white"] .new-measure-form          { background: rgba(15,23,42,.04) !important; border-color: rgba(15,23,42,.1) !important; }
[data-theme="white"] .nm-label,
[data-theme="white"] .nm-hint                   { color: rgba(15,23,42,.55) !important; }
[data-theme="white"] .nm-empty-hint             { color: rgba(15,23,42,.35) !important; }
</style>
