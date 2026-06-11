<template>
  <AppLayout>
    <div class="profile-page">

      <div class="profile-header">
        <div class="avatar-large">{{ initials }}</div>
        <div>
          <h1 class="profile-name">{{ user?.name || user?.email }}</h1>
          <p v-if="user?.name" class="profile-email-sub">{{ user?.email }}</p>
          <span class="role-badge" :class="user?.role?.toLowerCase()">{{ user?.role }}</span>
        </div>
      </div>

      <div class="profile-grid">

        <!-- ── Informations du compte ── -->
        <div class="profile-card ds-card">
          <h2 class="card-title">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/>
              <circle cx="12" cy="7" r="4"/>
            </svg>
            Informations du compte
          </h2>
          <div class="info-rows">
            <div class="info-row">
              <span class="info-label">Nom</span>
              <span class="info-value">{{ user?.name || '—' }}</span>
            </div>
            <div class="info-row">
              <span class="info-label">Email</span>
              <span class="info-value">{{ user?.email }}</span>
            </div>
            <div class="info-row">
              <span class="info-label">Rôle</span>
              <span class="role-badge" :class="user?.role?.toLowerCase()">{{ user?.role }}</span>
            </div>
            <div class="info-row">
              <span class="info-label">Membre depuis</span>
              <span class="info-value">{{ formatDate(user?.createdAt) }}</span>
            </div>
            <div class="info-row">
              <span class="info-label">ID utilisateur</span>
              <span class="info-value muted">#{{ user?.id }}</span>
            </div>
          </div>
        </div>

        <!-- ── Changer le nom ── -->
        <div class="profile-card ds-card">
          <h2 class="card-title">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
              <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
            </svg>
            Changer le nom
          </h2>
          <div class="form-fields">
            <div class="field-group">
              <label>Nom d'affichage</label>
              <div class="input-wrap">
                <input v-model="nameEdit.value" type="text" placeholder="Votre nom..." maxlength="100" />
              </div>
            </div>

            <div v-if="nameEdit.error"   class="alert alert-error">{{ nameEdit.error }}</div>
            <div v-if="nameEdit.success" class="alert alert-success">✅ Nom mis à jour avec succès !</div>

            <button class="ds-btn ds-btn-primary" @click="changeName" :disabled="nameEdit.loading">
              <span v-if="!nameEdit.loading">Mettre à jour</span>
              <span v-else>Enregistrement…</span>
            </button>
          </div>
        </div>

        <!-- ── Changer le mot de passe ── -->
        <div class="profile-card ds-card">
          <h2 class="card-title">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="3" y="11" width="18" height="11" rx="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/>
            </svg>
            Changer le mot de passe
          </h2>

          <div class="form-fields">
            <div class="field-group">
              <label>Mot de passe actuel</label>
              <div class="input-wrap">
                <input v-model="pwd.current" :type="pwd.showCurrent ? 'text' : 'password'" placeholder="••••••••" />
                <button type="button" class="eye-btn" @click="pwd.showCurrent = !pwd.showCurrent">
                  {{ pwd.showCurrent ? '🙈' : '👁' }}
                </button>
              </div>
            </div>
            <div class="field-group">
              <label>Nouveau mot de passe</label>
              <div class="input-wrap">
                <input v-model="pwd.new1" :type="pwd.showNew ? 'text' : 'password'" placeholder="••••••••" />
                <button type="button" class="eye-btn" @click="pwd.showNew = !pwd.showNew">
                  {{ pwd.showNew ? '🙈' : '👁' }}
                </button>
              </div>
            </div>
            <div class="field-group">
              <label>Confirmer le nouveau mot de passe</label>
              <div class="input-wrap">
                <input v-model="pwd.new2" :type="pwd.showNew ? 'text' : 'password'" placeholder="••••••••" />
              </div>
            </div>

            <div v-if="pwd.error"   class="alert alert-error">{{ pwd.error }}</div>
            <div v-if="pwd.success" class="alert alert-success">✅ Mot de passe modifié avec succès !</div>

            <!-- Indicateur de force -->
            <div v-if="pwd.new1" class="strength-bar">
              <div class="strength-fill" :class="pwdStrength.level" :style="{ width: pwdStrength.pct + '%' }"></div>
              <span class="strength-label" :class="pwdStrength.level">{{ pwdStrength.label }}</span>
            </div>

            <button class="ds-btn ds-btn-primary" @click="changePassword" :disabled="pwd.loading">
              <span v-if="!pwd.loading">Mettre à jour</span>
              <span v-else>Enregistrement…</span>
            </button>
          </div>
        </div>

        <!-- ── Statistiques ── -->
        <div class="profile-card ds-card stats-card">
          <h2 class="card-title">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="18" y1="20" x2="18" y2="10"/><line x1="12" y1="20" x2="12" y2="4"/>
              <line x1="6" y1="20" x2="6" y2="14"/>
            </svg>
            Mes statistiques
          </h2>
          <div class="stats-grid">
            <div class="stat-box">
              <span class="stat-num">{{ stats.dashboards }}</span>
              <span class="stat-lbl">Dashboards</span>
            </div>
            <div class="stat-box">
              <span class="stat-num">{{ stats.datasets }}</span>
              <span class="stat-lbl">Sources de données</span>
            </div>
          </div>
        </div>

      </div>
    </div>
  </AppLayout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, reactive } from 'vue'
import AppLayout from '@/components/layout/AppLayout.vue'
import { useAuthStore } from '@/stores/auth'
import { useDashboardStore } from '@/stores/dashboard'
import api from '@/services/api'

const authStore      = useAuthStore()
const dashboardStore = useDashboardStore()
const user = computed(() => authStore.user)

const initials = computed(() => {
  const name = user.value?.name
  if (name && name.trim()) {
    return name.trim().slice(0, 2).toUpperCase()
  }
  const email = user.value?.email ?? ''
  return email.slice(0, 2).toUpperCase()
})

// ── Nom ─────────────────────────────────────────────────────────
const nameEdit = reactive({
  value: user.value?.name ?? '',
  loading: false, error: '', success: false,
})

async function changeName() {
  nameEdit.error   = ''
  nameEdit.success = false
  if (!nameEdit.value.trim()) { nameEdit.error = 'Le nom ne peut pas être vide.'; return }

  nameEdit.loading = true
  try {
    await api.put('/auth/update-name', { name: nameEdit.value.trim() })
    authStore.updateName(nameEdit.value.trim())
    nameEdit.success = true
    setTimeout(() => { nameEdit.success = false }, 3000)
  } catch (e: any) {
    nameEdit.error = e?.message ?? 'Erreur lors de la mise à jour.'
  } finally {
    nameEdit.loading = false
  }
}

// ── Mot de passe ────────────────────────────────────────────────
const pwd = reactive({
  current: '', new1: '', new2: '',
  showCurrent: false, showNew: false,
  loading: false, error: '', success: false,
})

const pwdStrength = computed(() => {
  const p = pwd.new1
  if (!p) return { level: '', pct: 0, label: '' }
  let score = 0
  if (p.length >= 8)          score++
  if (/[A-Z]/.test(p))        score++
  if (/[0-9]/.test(p))        score++
  if (/[^A-Za-z0-9]/.test(p)) score++
  const levels = [
    { level: 'weak',   pct: 25,  label: 'Faible' },
    { level: 'fair',   pct: 50,  label: 'Moyen' },
    { level: 'good',   pct: 75,  label: 'Bon' },
    { level: 'strong', pct: 100, label: 'Fort' },
  ]
  return levels[score - 1] ?? levels[0]
})

async function changePassword() {
  pwd.error   = ''
  pwd.success = false
  if (!pwd.current)           { pwd.error = 'Entrez votre mot de passe actuel.'; return }
  if (pwd.new1.length < 6)    { pwd.error = 'Le nouveau mot de passe doit contenir au moins 6 caractères.'; return }
  if (pwd.new1 !== pwd.new2)  { pwd.error = 'Les mots de passe ne correspondent pas.'; return }

  pwd.loading = true
  try {
    await api.put('/auth/change-password', {
      currentPassword: pwd.current,
      newPassword:     pwd.new1,
    })
    pwd.success = true
    pwd.current = ''; pwd.new1 = ''; pwd.new2 = ''
  } catch (e: any) {
    pwd.error = e?.response?.data?.error ?? 'Mot de passe actuel incorrect.'
  } finally {
    pwd.loading = false
  }
}

// ── Stats ────────────────────────────────────────────────────────
const stats = reactive({ dashboards: 0, datasets: 0 })

onMounted(async () => {
  try {
    // Charger les dashboards si pas encore dans le store
    if (dashboardStore.dashboards.length === 0) {
      await dashboardStore.fetchDashboards()
    }
    stats.dashboards = dashboardStore.dashboards.length

    // Charger les sources de données
    const ds = await api.get('/datasource')
    const dsRaw = ds.data
    stats.datasets = Array.isArray(dsRaw) ? dsRaw.length
                   : Array.isArray(dsRaw?.data) ? dsRaw.data.length
                   : 0
  } catch { /* ignore */ }
})

function formatDate(iso?: string) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('fr-FR', { day: '2-digit', month: 'long', year: 'numeric' })
}
</script>

<style scoped>
.profile-page {
  padding: var(--space-8) var(--space-10);
  display: flex; flex-direction: column; gap: var(--space-6);
  max-width: 960px;
}

/* Header */
.profile-header {
  display: flex; align-items: center; gap: var(--space-5);
}
.avatar-large {
  width: 72px; height: 72px; border-radius: 50%;
  background: linear-gradient(135deg, var(--color-primary), #6366f1);
  color: #fff; font-size: 26px; font-weight: 700;
  display: flex; align-items: center; justify-content: center;
  flex-shrink: 0;
}
.profile-name {
  font-size: var(--text-2xl); font-weight: var(--weight-bold);
  color: var(--color-text); margin: 0 0 4px;
}
.profile-email-sub {
  font-size: var(--text-sm); color: var(--color-text-muted);
  margin: 0 0 6px;
}

/* Role badge */
.role-badge {
  display: inline-block; padding: 3px 10px;
  border-radius: var(--radius-full); font-size: var(--text-xs);
  font-weight: var(--weight-bold); text-transform: uppercase; letter-spacing: .05em;
}
.role-badge.admin  { background: rgba(239,68,68,.15);  color: #f87171; border: 1px solid rgba(239,68,68,.25); }
.role-badge.editor { background: rgba(16,185,129,.15); color: #34d399; border: 1px solid rgba(16,185,129,.25); }
.role-badge.viewer { background: rgba(99,102,241,.15); color: #818cf8; border: 1px solid rgba(99,102,241,.25); }

/* Grid */
.profile-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-5);
}
.stats-card { grid-column: 1 / -1; }

/* Cards */
.profile-card { padding: var(--space-5); }
.card-title {
  display: flex; align-items: center; gap: var(--space-2);
  font-size: var(--text-base); font-weight: var(--weight-semibold);
  color: var(--color-text); margin: 0 0 var(--space-5);
}

/* Info rows */
.info-rows { display: flex; flex-direction: column; gap: var(--space-3); }
.info-row {
  display: flex; justify-content: space-between; align-items: center;
  padding: var(--space-3) 0;
  border-bottom: 1px solid var(--color-border);
}
.info-row:last-child { border-bottom: none; }
.info-label { font-size: var(--text-sm); color: var(--color-text-muted); }
.info-value { font-size: var(--text-sm); color: var(--color-text); font-weight: var(--weight-medium); }
.info-value.muted { color: var(--color-text-muted); }

/* Form */
.form-fields { display: flex; flex-direction: column; gap: var(--space-4); }
.field-group { display: flex; flex-direction: column; gap: var(--space-2); }
.field-group label {
  font-size: var(--text-xs); font-weight: var(--weight-semibold);
  color: var(--color-text-secondary); text-transform: uppercase; letter-spacing: .04em;
}
.input-wrap { position: relative; }
.input-wrap input {
  width: 100%; padding: 10px 40px 10px 12px;
  background: var(--color-surface-2);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  color: var(--color-text); font-size: var(--text-sm);
  outline: none; transition: border-color .2s; box-sizing: border-box;
}
.input-wrap input:focus { border-color: var(--color-primary); }
.eye-btn {
  position: absolute; right: 10px; top: 50%; transform: translateY(-50%);
  background: none; border: none; cursor: pointer; font-size: 14px;
}

/* Alerts */
.alert { padding: 10px 14px; border-radius: var(--radius-md); font-size: var(--text-sm); }
.alert-error   { background: rgba(239,68,68,.1);  color: #f87171; border: 1px solid rgba(239,68,68,.2); }
.alert-success { background: rgba(16,185,129,.1); color: #34d399; border: 1px solid rgba(16,185,129,.2); }

/* Strength bar */
.strength-bar {
  display: flex; align-items: center; gap: var(--space-3);
  background: var(--color-surface-2);
  border-radius: var(--radius-full);
  height: 6px; overflow: hidden; position: relative;
}
.strength-fill {
  height: 100%; border-radius: var(--radius-full);
  transition: width .3s, background .3s;
}
.strength-fill.weak   { background: #ef4444; }
.strength-fill.fair   { background: #f59e0b; }
.strength-fill.good   { background: #3b82f6; }
.strength-fill.strong { background: #10b981; }
.strength-label {
  position: absolute; right: 0; font-size: 10px; font-weight: 600;
  background: var(--color-surface-2); padding: 0 6px;
}
.strength-label.weak   { color: #ef4444; }
.strength-label.fair   { color: #f59e0b; }
.strength-label.good   { color: #3b82f6; }
.strength-label.strong { color: #10b981; }

/* Stats */
.stats-grid { display: flex; gap: var(--space-5); }
.stat-box {
  flex: 1; background: var(--color-surface-2);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg); padding: var(--space-5);
  display: flex; flex-direction: column; align-items: center; gap: var(--space-2);
}
.stat-num {
  font-size: var(--text-3xl); font-weight: var(--weight-bold);
  color: var(--color-primary);
}
.stat-lbl { font-size: var(--text-sm); color: var(--color-text-muted); }

@media (max-width: 640px) {
  .profile-grid { grid-template-columns: 1fr; }
  .stats-card { grid-column: 1; }
  .profile-page { padding: var(--space-5); }
}
</style>
