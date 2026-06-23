<template>
  <div class="login-page">
    <!-- Animated background grid -->
    <div class="bg-grid"></div>
    <div class="bg-glow"></div>

    <!-- Floating data particles -->
    <div class="particles">
      <span v-for="i in 12" :key="i" class="particle" :style="particleStyle(i)"></span>
    </div>

    <div class="login-container">
      <!-- Left branding panel -->
      <div class="brand-panel">
        <div class="brand-content">
          <div class="logo-mark">
            <svg width="48" height="48" viewBox="0 0 48 48" fill="none">
              <rect x="4" y="24" width="8" height="16" rx="2" fill="#1B6B3A" />
              <rect x="16" y="16" width="8" height="24" rx="2" fill="#1B6B3A" />
              <rect x="28" y="8" width="8" height="32" rx="2" fill="#1B6B3A" />
              <rect x="40" y="18" width="8" height="22" rx="2" fill="#134E2A" />
              <path
                d="M6 20 L20 12 L32 18 L44 10"
                stroke="#1B6B3A"
                stroke-width="2"
                stroke-linecap="round"
                stroke-linejoin="round"
              />
              <circle cx="6" cy="20" r="2.5" fill="#1B6B3A" />
              <circle cx="20" cy="12" r="2.5" fill="#1B6B3A" />
              <circle cx="32" cy="18" r="2.5" fill="#1B6B3A" />
              <circle cx="44" cy="10" r="2.5" fill="#1B6B3A" />
            </svg>
          </div>
          <h1 class="brand-title">Dash<span class="accent">Gen</span></h1>
          <p class="brand-subtitle">Générateur de tableaux de bord interactifs avec IA</p>

          <div class="feature-list">
            <div
              class="feature-item"
              v-for="(f, i) in features"
              :key="i"
              :style="{ animationDelay: `${i * 0.15}s` }"
            >
              <span class="feature-icon">{{ f.icon }}</span>
              <span>{{ f.text }}</span>
            </div>
          </div>

          <div class="stat-row">
            <div class="stat" v-for="s in stats" :key="s.label">
              <span class="stat-value">{{ s.value }}</span>
              <span class="stat-label">{{ s.label }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Right login form panel -->
      <div class="form-panel">
        <div class="form-card">
          <div class="form-header">
            <h2>Connexion</h2>
            <p>Accédez à votre espace tableau de bord</p>
          </div>

          <form @submit.prevent="handleLogin" class="form-body" novalidate>
            <div class="field-group" :class="{ error: errors.email, focused: focused === 'email' }">
              <label for="email">Adresse email</label>
              <div class="input-wrap">
                <svg
                  class="input-icon"
                  width="18"
                  height="18"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="2"
                >
                  <rect x="2" y="4" width="20" height="16" rx="2" />
                  <polyline points="2,4 12,13 22,4" />
                </svg>
                <input
                  id="email"
                  v-model="form.email"
                  type="email"
                  placeholder="exemple@email.com"
                  autocomplete="email"
                  @focus="focused = 'email'"
                  @blur="onBlurEmail"
                />
              </div>
              <span class="error-msg" v-if="errors.email">{{ errors.email }}</span>
            </div>

            <div
              class="field-group"
              :class="{ error: errors.password, focused: focused === 'password' }"
            >
              <label for="password">Mot de passe</label>
              <div class="input-wrap">
                <svg
                  class="input-icon"
                  width="18"
                  height="18"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="2"
                >
                  <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
                  <path d="M7 11V7a5 5 0 0 1 10 0v4" />
                </svg>
                <input
                  id="password"
                  v-model="form.password"
                  :type="showPassword ? 'text' : 'password'"
                  placeholder="••••••••"
                  autocomplete="current-password"
                  @focus="focused = 'password'"
                  @blur="focused = null"
                />
                <button
                  type="button"
                  class="toggle-pwd"
                  @click="showPassword = !showPassword"
                  tabindex="-1"
                >
                  <svg
                    v-if="!showPassword"
                    width="18"
                    height="18"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2"
                  >
                    <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
                    <circle cx="12" cy="12" r="3" />
                  </svg>
                  <svg
                    v-else
                    width="18"
                    height="18"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2"
                  >
                    <path
                      d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94"
                    />
                    <path
                      d="M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19"
                    />
                    <line x1="1" y1="1" x2="23" y2="23" />
                  </svg>
                </button>
              </div>
              <span class="error-msg" v-if="errors.password">{{ errors.password }}</span>
            </div>

            <div class="form-options">
              <label class="remember-me">
                <input type="checkbox" v-model="form.remember" />
                <span class="checkmark"></span>
                Se souvenir de moi
              </label>
              <a href="#" class="forgot-link" @click.prevent="showForgot = true">Mot de passe oublié ?</a>
            </div>

            <button
              type="submit"
              class="submit-btn"
              :class="{ loading: isLoading }"
              :disabled="isLoading"
            >
              <span v-if="!isLoading" class="btn-content">
                <span>Se connecter</span>
                <svg
                  width="18"
                  height="18"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="2.5"
                >
                  <line x1="5" y1="12" x2="19" y2="12" />
                  <polyline points="12,5 19,12 12,19" />
                </svg>
              </span>
              <span v-else class="btn-loader">
                <span class="spinner"></span>
                Connexion en cours…
              </span>
            </button>

            <div class="api-error" v-if="apiError">
              <svg
                width="16"
                height="16"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
              >
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="8" x2="12" y2="12" />
                <line x1="12" y1="16" x2="12.01" y2="16" />
              </svg>
              {{ apiError }}
            </div>
          </form>

          <!-- ── Modal Mot de passe oublié ── -->
          <div v-if="showForgot" class="forgot-overlay" @click.self="closeForgot">
            <div class="forgot-modal">
              <button class="forgot-close" @click="closeForgot">✕</button>

              <!-- Formulaire -->
              <template v-if="!forgotSent">
                <div class="forgot-icon">🔑</div>
                <h3 class="forgot-title">Mot de passe oublié</h3>
                <p class="forgot-sub">Entrez votre adresse email. Contactez votre administrateur pour réinitialiser votre mot de passe.</p>
                <div class="field-group" :class="{ error: forgotError }">
                  <label>Adresse email</label>
                  <div class="input-wrap">
                    <svg class="input-icon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <rect x="2" y="4" width="20" height="16" rx="2"/><polyline points="2,4 12,13 22,4"/>
                    </svg>
                    <input
                      v-model="forgotEmail"
                      type="email"
                      placeholder="exemple@email.com"
                      class="forgot-input"
                      @keyup.enter="submitForgot"
                    />
                  </div>
                  <span class="error-msg" v-if="forgotError">{{ forgotError }}</span>
                </div>
                <button class="forgot-submit" @click="submitForgot" :disabled="forgotLoading">
                  <span v-if="!forgotLoading">Envoyer la demande</span>
                  <span v-else class="btn-loader"><span class="spinner"></span> Envoi…</span>
                </button>
              </template>

              <!-- Succès -->
              <template v-else>
                <div class="forgot-success-icon">✅</div>
                <h3 class="forgot-title">Demande envoyée</h3>
                <p class="forgot-sub">
                  Si l'adresse <strong>{{ forgotEmail }}</strong> est associée à un compte,
                  l'administrateur recevra une notification pour réinitialiser votre mot de passe.
                </p>
                <button class="forgot-submit" @click="closeForgot">Retour à la connexion</button>
              </template>
            </div>
          </div>

          <div class="form-footer">
            <p>Pas encore de compte ? <a href="#" class="forgot-link" @click.prevent="showContact = true">Contacter l'administrateur</a></p>
          </div>

          <!-- Modale contact admin -->
          <div v-if="showContact" class="forgot-overlay" @click.self="closeContact">
            <div class="forgot-modal">
              <button class="forgot-close" @click="closeContact">✕</button>

              <template v-if="!contactSent">
                <div class="forgot-icon">✉️</div>
                <h3 class="forgot-title">Demande de compte</h3>
                <p class="forgot-sub">Remplissez ce formulaire. L'administrateur recevra votre demande et créera votre compte.</p>

                <div class="field-group" :class="{ error: contactError && !contactName }">
                  <label>Nom complet</label>
                  <div class="input-wrap">
                    <svg class="input-icon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/>
                    </svg>
                    <input v-model="contactName" type="text" placeholder="Votre nom" class="forgot-input" />
                  </div>
                </div>

                <div class="field-group" :class="{ error: contactError && !contactEmail }">
                  <label>Adresse email</label>
                  <div class="input-wrap">
                    <svg class="input-icon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <rect x="2" y="4" width="20" height="16" rx="2"/><polyline points="2,4 12,13 22,4"/>
                    </svg>
                    <input v-model="contactEmail" type="email" placeholder="exemple@email.com" class="forgot-input" />
                  </div>
                </div>

                <div class="field-group">
                  <label>Message (optionnel)</label>
                  <textarea v-model="contactMessage" placeholder="Pourquoi souhaitez-vous accéder à la plateforme ?" class="forgot-input contact-textarea"></textarea>
                </div>

                <span class="error-msg" v-if="contactError">{{ contactError }}</span>

                <button class="forgot-submit" @click="submitContact" :disabled="contactLoading">
                  <span v-if="!contactLoading">Envoyer la demande</span>
                  <span v-else class="btn-loader"><span class="spinner"></span> Envoi…</span>
                </button>
              </template>

              <template v-else>
                <div class="forgot-success-icon">✅</div>
                <h3 class="forgot-title">Demande envoyée !</h3>
                <p class="forgot-sub">L'administrateur a reçu votre demande et vous contactera à l'adresse <strong>{{ contactEmail }}</strong>.</p>
                <button class="forgot-submit" @click="closeContact">Retour à la connexion</button>
              </template>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import api from '@/services/api'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

const form = reactive({
  email: '',
  password: '',
  remember: false,
})

const errors = reactive({ email: '', password: '' })
const focused = ref<string | null>(null)
const showPassword = ref(false)
const isLoading = ref(false)
const apiError = ref('')

// ── Contacter l'administrateur ───────────────────────────────────
const showContact    = ref(false)
const contactName    = ref('')
const contactEmail   = ref('')
const contactMessage = ref('')
const contactError   = ref('')
const contactSent    = ref(false)
const contactLoading = ref(false)

function closeContact() {
  showContact.value    = false
  contactName.value    = ''
  contactEmail.value   = ''
  contactMessage.value = ''
  contactError.value   = ''
  contactSent.value    = false
  contactLoading.value = false
}

async function submitContact() {
  contactError.value = ''
  if (!contactName.value.trim())  { contactError.value = 'Le nom est requis'; return }
  if (!contactEmail.value.trim()) { contactError.value = "L'email est requis"; return }
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(contactEmail.value)) { contactError.value = "Format d'email invalide"; return }

  contactLoading.value = true
  try {
    await api.post('/auth/contact-admin', {
      name:    contactName.value.trim(),
      email:   contactEmail.value.trim(),
      message: contactMessage.value.trim(),
    })
  } catch {
    // Affiche succès quand même
  } finally {
    contactLoading.value = false
    contactSent.value    = true
  }
}

// ── Mot de passe oublié ──────────────────────────────────────────
const showForgot   = ref(false)
const forgotEmail  = ref('')
const forgotError  = ref('')
const forgotSent   = ref(false)
const forgotLoading = ref(false)

function closeForgot() {
  showForgot.value   = false
  forgotEmail.value  = ''
  forgotError.value  = ''
  forgotSent.value   = false
  forgotLoading.value = false
}

async function submitForgot() {
  forgotError.value = ''
  if (!forgotEmail.value.trim()) {
    forgotError.value = "L'email est requis"
    return
  }
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(forgotEmail.value)) {
    forgotError.value = "Format d'email invalide"
    return
  }
  forgotLoading.value = true
  try {
    await api.post('/auth/forgot-password', { email: forgotEmail.value.trim() })
  } catch {
    // On affiche succès même en cas d'erreur (sécurité : ne pas révéler si email existe)
  } finally {
    forgotLoading.value = false
    forgotSent.value = true
  }
}

const features = [
  { icon: '⚡', text: 'Drag & drop intuitif' },
  { icon: '🤖', text: 'IA générative intégrée' },
  { icon: '📊', text: 'Graphiques interactifs' },
  { icon: '🔗', text: 'Import CSV, SQL, API REST' },
]

const stats = [
  { value: '12+', label: 'Types de widgets' },
  { value: 'IA', label: 'Insights auto' },
  { value: '∞', label: 'Dashboards' },
]

function particleStyle(i: number) {
  const positions = [
    { left: '10%', top: '20%' },
    { left: '25%', top: '60%' },
    { left: '45%', top: '15%' },
    { left: '60%', top: '75%' },
    { left: '80%', top: '35%' },
    { left: '15%', top: '85%' },
    { left: '70%', top: '10%' },
    { left: '35%', top: '90%' },
    { left: '90%', top: '55%' },
    { left: '5%', top: '45%' },
    { left: '55%', top: '50%' },
    { left: '85%', top: '80%' },
  ]
  const index = (i - 1) % positions.length
  const p = positions[index] || { left: '0%', top: '0%' }
  return {
    left: p.left,
    top: p.top,
    animationDelay: `${(i * 0.7) % 4}s`,
    animationDuration: `${3 + (i % 3)}s`,
  }
}

function validateEmail() {
  if (!form.email) {
    errors.email = "L'email est requis"
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) {
    errors.email = "Format d'email invalide"
  } else {
    errors.email = ''
  }
}

function onBlurEmail() {
  focused.value = null
  validateEmail()
}

async function handleLogin() {
  errors.email = ''
  errors.password = ''
  apiError.value = ''

  validateEmail()
  if (!form.password) {
    errors.password = 'Le mot de passe est requis'
  }

  if (errors.email || errors.password) return

  isLoading.value = true
  try {
    await authStore.login(form.email, form.password)
    // Rediriger vers la page demandée (si redirect query param) ou le dashboard
    const redirect = route.query.redirect as string | undefined
    router.push(redirect && redirect.startsWith('/') ? redirect : '/dashboard')
  } catch (err: unknown) {
    if (err instanceof Error) {
      apiError.value = err.message
    } else {
      apiError.value = 'Identifiants incorrects ou erreur serveur.'
    }
  } finally {
    isLoading.value = false
  }
}
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Syne:wght@400;500;600;700;800&family=DM+Sans:wght@300;400;500&display=swap');

* {
  box-sizing: border-box;
  margin: 0;
  padding: 0;
}

.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #F5F6F5;
  font-family: 'DM Sans', sans-serif;
  overflow: hidden;
  position: relative;
}

/* Animated background */
.bg-grid {
  position: fixed;
  inset: 0;
  z-index: 0;
  background-image:
    linear-gradient(rgba(27, 107, 58, 0.06) 1px, transparent 1px),
    linear-gradient(90deg, rgba(27, 107, 58, 0.06) 1px, transparent 1px);
  background-size: 40px 40px;
  mask-image: radial-gradient(ellipse 80% 80% at 50% 50%, black 30%, transparent 100%);
}

.bg-glow {
  position: fixed;
  inset: 0;
  z-index: 0;
  background:
    radial-gradient(ellipse 60% 50% at 25% 50%, rgba(27, 107, 58, 0.12) 0%, transparent 60%),
    radial-gradient(ellipse 40% 40% at 75% 50%, rgba(6, 182, 212, 0.08) 0%, transparent 60%);
}

/* Particles */
.particles {
  position: fixed;
  inset: 0;
  z-index: 0;
  pointer-events: none;
}
.particle {
  position: absolute;
  width: 3px;
  height: 3px;
  border-radius: 50%;
  background: #1B6B3A;
  opacity: 0;
  animation: floatParticle linear infinite;
}
.particle:nth-child(even) {
  background: #06b6d4;
  width: 2px;
  height: 2px;
}

@keyframes floatParticle {
  0% {
    opacity: 0;
    transform: translateY(0) scale(0);
  }
  20% {
    opacity: 0.6;
    transform: translateY(-10px) scale(1);
  }
  80% {
    opacity: 0.3;
    transform: translateY(-40px) scale(0.8);
  }
  100% {
    opacity: 0;
    transform: translateY(-80px) scale(0);
  }
}

/* Layout */
.login-container {
  position: relative;
  z-index: 1;
  display: flex;
  width: 100%;
  max-width: 960px;
  min-height: 580px;
  margin: 24px;
  border-radius: 24px;
  overflow: hidden;
  box-shadow:
    0 40px 120px rgba(0, 0, 0, 0.6),
    0 0 0 1px rgba(27, 107, 58, 0.15);
  animation: slideUp 0.7s cubic-bezier(0.16, 1, 0.3, 1) both;
}

@keyframes slideUp {
  from {
    opacity: 0;
    transform: translateY(32px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* Brand panel */
.brand-panel {
  flex: 1.1;
  background: linear-gradient(145deg, #EEF7F1 0%, #F5F6F5 50%, #FFFFFF 100%);
  border-right: 1px solid rgba(27, 107, 58, 0.12);
  padding: 48px 40px;
  display: flex;
  align-items: center;
  position: relative;
  overflow: hidden;
}

.brand-panel::before {
  content: '';
  position: absolute;
  top: -60px;
  right: -60px;
  width: 300px;
  height: 300px;
  background: radial-gradient(circle, rgba(27, 107, 58, 0.15) 0%, transparent 70%);
  pointer-events: none;
}

.brand-panel::after {
  content: '';
  position: absolute;
  bottom: -40px;
  left: -40px;
  width: 200px;
  height: 200px;
  background: radial-gradient(circle, rgba(6, 182, 212, 0.1) 0%, transparent 70%);
  pointer-events: none;
}

.brand-content {
  position: relative;
  z-index: 1;
  width: 100%;
}

.logo-mark {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 64px;
  height: 64px;
  background: rgba(27, 107, 58, 0.1);
  border: 1px solid rgba(27, 107, 58, 0.25);
  border-radius: 16px;
  margin-bottom: 20px;
}

.brand-title {
  font-family: 'Syne', sans-serif;
  font-size: 42px;
  font-weight: 800;
  color: #111714;
  letter-spacing: -1px;
  line-height: 1;
  margin-bottom: 10px;
}

.brand-title .accent {
  color: #1B6B3A;
}

.brand-subtitle {
  font-size: 14px;
  color: #4B5E52;
  line-height: 1.5;
  margin-bottom: 36px;
  max-width: 260px;
}

.feature-list {
  display: flex;
  flex-direction: column;
  gap: 14px;
  margin-bottom: 36px;
}

.feature-item {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 13.5px;
  color: #4B5E52;
  animation: fadeInLeft 0.5s cubic-bezier(0.16, 1, 0.3, 1) both;
}

@keyframes fadeInLeft {
  from {
    opacity: 0;
    transform: translateX(-12px);
  }
  to {
    opacity: 1;
    transform: translateX(0);
  }
}

.feature-icon {
  font-size: 16px;
  width: 30px;
  height: 30px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(27, 107, 58, 0.08);
  border-radius: 8px;
  flex-shrink: 0;
}

.stat-row {
  display: flex;
  gap: 24px;
  padding-top: 28px;
  border-top: 1px solid rgba(27, 107, 58, 0.1);
}

.stat {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.stat-value {
  font-family: 'Syne', sans-serif;
  font-size: 22px;
  font-weight: 700;
  color: #1B6B3A;
}

.stat-label {
  font-size: 11px;
  color: #94A99A;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

/* Form panel */
.form-panel {
  flex: 1;
  background: #FFFFFF;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 48px 40px;
}

.form-card {
  width: 100%;
  max-width: 360px;
}

.form-header {
  margin-bottom: 36px;
}

.form-header h2 {
  font-family: 'Syne', sans-serif;
  font-size: 28px;
  font-weight: 700;
  color: #111714;
  margin-bottom: 6px;
}

.form-header p {
  font-size: 13.5px;
  color: #94A99A;
}

.form-body {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

/* Fields */
.field-group {
  display: flex;
  flex-direction: column;
  gap: 7px;
}

.field-group label {
  font-size: 12.5px;
  font-weight: 500;
  color: rgba(240, 253, 249, 0.55);
  letter-spacing: 0.3px;
  transition: color 0.2s;
}

.field-group.focused label {
  color: #1B6B3A;
}
.field-group.error label {
  color: #f87171;
}

.input-wrap {
  position: relative;
  display: flex;
  align-items: center;
}

.input-icon {
  position: absolute;
  left: 14px;
  color: #C4CFC7;
  pointer-events: none;
  transition: color 0.2s;
}

.field-group.focused .input-icon {
  color: #1B6B3A;
}
.field-group.error .input-icon {
  color: #f87171;
}

.input-wrap input {
  width: 100%;
  padding: 13px 14px 13px 44px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 12px;
  color: #111714;
  font-size: 14px;
  font-family: 'DM Sans', sans-serif;
  outline: none;
  transition: all 0.2s;
}

.input-wrap input::placeholder {
  color: rgba(240, 253, 249, 0.2);
}

.input-wrap input:focus {
  border-color: rgba(27, 107, 58, 0.45);
  background: rgba(27, 107, 58, 0.05);
  box-shadow: 0 0 0 3px rgba(27, 107, 58, 0.08);
}

.field-group.error input {
  border-color: rgba(248, 113, 113, 0.45);
  background: rgba(248, 113, 113, 0.04);
}

.toggle-pwd {
  position: absolute;
  right: 14px;
  background: none;
  border: none;
  cursor: pointer;
  color: #94A99A;
  display: flex;
  align-items: center;
  transition: color 0.2s;
  padding: 4px;
}

.toggle-pwd:hover {
  color: #4B5E52;
}

.error-msg {
  font-size: 12px;
  color: #f87171;
  display: flex;
  align-items: center;
  gap: 4px;
}

.form-options {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin: 4px 0;
}

.remember-me {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  color: #94A99A;
  cursor: pointer;
  user-select: none;
}

.remember-me input {
  display: none;
}

.checkmark {
  width: 18px;
  height: 18px;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 5px;
  background: rgba(255, 255, 255, 0.03);
  position: relative;
  transition: all 0.2s;
}

.remember-me input:checked ~ .checkmark {
  background: #1B6B3A;
  border-color: #1B6B3A;
}

.remember-me input:checked ~ .checkmark::after {
  content: '';
  position: absolute;
  left: 6px;
  top: 2px;
  width: 4px;
  height: 8px;
  border: solid white;
  border-width: 0 2px 2px 0;
  transform: rotate(45deg);
}

.forgot-link {
  font-size: 13px;
  color: rgba(27, 107, 58, 0.8);
  text-decoration: none;
  transition: color 0.2s;
}

.forgot-link:hover {
  color: #1B6B3A;
}

.submit-btn {
  position: relative;
  margin-top: 12px;
  padding: 14px;
  background: #1B6B3A;
  border: none;
  border-radius: 14px;
  color: #fff;
  font-size: 15px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
  overflow: hidden;
}

.submit-btn::before {
  content: '';
  position: absolute;
  inset: 0;
  background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.2), transparent);
  transform: translateX(-100%);
  transition: transform 0.5s;
}

.submit-btn:hover::before {
  transform: translateX(100%);
}

.submit-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 12px 24px rgba(27, 107, 58, 0.3);
}

.btn-content,
.btn-loader {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
}

.spinner {
  width: 18px;
  height: 18px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: #fff;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.api-error {
  margin-top: 16px;
  padding: 12px;
  background: rgba(248, 113, 113, 0.1);
  border: 1px solid rgba(248, 113, 113, 0.2);
  border-radius: 10px;
  color: #f87171;
  font-size: 13px;
  display: flex;
  align-items: center;
  gap: 8px;
}

.form-footer {
  margin-top: 32px;
  text-align: center;
  font-size: 13px;
  color: #94A99A;
}

.form-footer a {
  color: #1B6B3A;
  text-decoration: none;
  font-weight: 500;
}

@media (max-width: 680px) {
  .brand-panel {
    display: none;
  }
  .login-container {
    margin: 16px;
    border-radius: 20px;
  }
}

/* ── Modal mot de passe oublié ─────────────────────────────────── */
.forgot-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.6);
  backdrop-filter: blur(4px);
  z-index: 9999;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 16px;
  animation: fadeIn 0.2s ease;
}

@keyframes fadeIn {
  from { opacity: 0; }
  to   { opacity: 1; }
}

.forgot-modal {
  position: relative;
  background: #0d1f1c;
  border: 1px solid rgba(27, 107, 58, 0.2);
  border-radius: 20px;
  padding: 36px 32px;
  width: 100%;
  max-width: 380px;
  box-shadow: 0 24px 60px rgba(0, 0, 0, 0.5);
  display: flex;
  flex-direction: column;
  gap: 14px;
  animation: slideUp 0.25s cubic-bezier(0.16, 1, 0.3, 1);
}

.forgot-close {
  position: absolute;
  top: 14px;
  right: 16px;
  background: none;
  border: none;
  color: #94A99A;
  font-size: 14px;
  cursor: pointer;
  padding: 4px 6px;
  border-radius: 6px;
  transition: all 0.15s;
}
.forgot-close:hover {
  color: #111714;
  background: rgba(255, 255, 255, 0.08);
}

.forgot-icon, .forgot-success-icon {
  font-size: 32px;
  text-align: center;
}

.forgot-title {
  font-family: 'Syne', sans-serif;
  font-size: 20px;
  font-weight: 700;
  color: #111714;
  margin: 0;
  text-align: center;
}

.forgot-sub {
  font-size: 13px;
  color: #4B5E52;
  text-align: center;
  line-height: 1.6;
  margin: 0;
}
.forgot-sub strong {
  color: #1B6B3A;
}

.forgot-input {
  width: 100%;
  padding: 13px 14px 13px 44px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 12px;
  color: #111714;
  font-size: 14px;
  font-family: 'DM Sans', sans-serif;
  outline: none;
  transition: all 0.2s;
  box-sizing: border-box;
}
.forgot-input:focus {
  border-color: rgba(27, 107, 58, 0.45);
  background: rgba(27, 107, 58, 0.05);
  box-shadow: 0 0 0 3px rgba(27, 107, 58, 0.08);
}
.contact-textarea {
  padding: 13px 14px;
  min-height: 90px;
  resize: vertical;
  font-family: inherit;
}

.forgot-submit {
  margin-top: 4px;
  padding: 13px;
  background: #1B6B3A;
  border: none;
  border-radius: 12px;
  color: #fff;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
}
.forgot-submit:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 8px 20px rgba(27, 107, 58, 0.3);
}
.forgot-submit:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
</style>


