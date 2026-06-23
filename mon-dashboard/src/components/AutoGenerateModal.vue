<template>
  <teleport to="body">
    <transition name="agm-fade">
      <div v-if="modelValue" class="agm-overlay" @click.self="onOverlayClick">
        <div class="agm-card" :class="{ 'agm-card--error': isError, 'agm-card--done': isDone }">

          <!-- Header -->
          <div class="agm-header">
            <div class="agm-icon-wrap" :class="{ spin: !isDone && !isError }">
              <svg v-if="!isError && !isDone" width="28" height="28" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 2L15.09 8.26L22 9.27L17 14.14L18.18 21.02L12 17.77L5.82 21.02L7 14.14L2 9.27L8.91 8.26L12 2Z"/>
              </svg>
              <svg v-else-if="isDone" width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                <polyline points="20 6 9 17 4 12"/>
              </svg>
              <svg v-else width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="12" cy="12" r="10"/>
                <line x1="12" y1="8" x2="12" y2="12"/>
                <line x1="12" y1="16" x2="12.01" y2="16"/>
              </svg>
            </div>
            <div class="agm-titles">
              <h2 class="agm-title">{{ isDone ? 'Dashboard généré !' : isError ? 'Génération échouée' : 'Génération automatique' }}</h2>
              <p class="agm-subtitle">
                {{ isDone
                  ? `${widgetCount} widget${widgetCount > 1 ? 's' : ''} créé${widgetCount > 1 ? 's' : ''} et positionnés`
                  : isError ? errorMessage
                  : 'L\'IA analyse votre dataset et construit le dashboard…' }}
              </p>
            </div>
          </div>

          <!-- Steps -->
          <div v-if="!isError" class="agm-steps">
            <div
              v-for="(step, i) in steps"
              :key="i"
              class="agm-step"
              :class="{
                'agm-step--done':    currentStep > i,
                'agm-step--active':  currentStep === i && !isDone,
                'agm-step--pending': currentStep < i,
              }"
            >
              <div class="agm-step-icon">
                <i v-if="currentStep > i" class="pi pi-check"/>
                <i v-else-if="currentStep === i && !isDone" class="pi pi-spin pi-spinner"/>
                <span v-else class="agm-step-num">{{ i + 1 }}</span>
              </div>
              <div class="agm-step-text">
                <span class="agm-step-label">{{ step.label }}</span>
                <span class="agm-step-desc">{{ step.desc }}</span>
              </div>
            </div>
          </div>

          <!-- Progress bar -->
          <div v-if="!isError" class="agm-progress-track">
            <div class="agm-progress-bar" :style="{ width: progressPct + '%' }"/>
          </div>

          <!-- Warning: existing widgets -->
          <div v-if="!isError && !isDone && hasExistingWidgets && currentStep === 0" class="agm-warning">
            <i class="pi pi-exclamation-triangle"/>
            <span>Le dashboard contient déjà <strong>{{ existingCount }} widget{{ existingCount > 1 ? 's' : '' }}</strong>. Ils seront remplacés par les widgets générés.</span>
          </div>

          <!-- Footer -->
          <div class="agm-footer">
            <button v-if="isDone || isError" class="agm-btn agm-btn--primary" @click="emit('update:modelValue', false)">
              <i class="pi pi-check" v-if="isDone"/>
              <i class="pi pi-times" v-else/>
              {{ isDone ? 'Parfait !' : 'Fermer' }}
            </button>
            <button v-else class="agm-btn agm-btn--ghost" @click="emit('cancel')">
              Annuler
            </button>
          </div>
        </div>
      </div>
    </transition>
  </teleport>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  modelValue:          boolean
  currentStep:         number     // 0-3
  isDone:              boolean
  isError:             boolean
  errorMessage?:       string
  widgetCount?:        number
  hasExistingWidgets?: boolean
  existingCount?:      number
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', v: boolean): void
  (e: 'cancel'): void
}>()

const steps = [
  { label: 'Analyse du schéma',          desc: 'Lecture des colonnes et types de données' },
  { label: 'Choix des visualisations',   desc: 'L\'IA sélectionne les graphiques pertinents' },
  { label: 'Positionnement sur la grille', desc: 'Calcul de la disposition optimale' },
  { label: 'Construction du dashboard', desc: 'Création des widgets et application des configs' },
]

const progressPct = computed(() => {
  if (props.isDone)  return 100
  if (props.isError) return 0
  return Math.round((props.currentStep / steps.length) * 100)
})

function onOverlayClick() {
  if (props.isDone || props.isError) emit('update:modelValue', false)
}
</script>

<style scoped>
/* Overlay */
.agm-overlay {
  position: fixed;
  inset: 0;
  z-index: 9999;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(0, 0, 0, 0.6);
  backdrop-filter: blur(6px);
}

/* Card */
.agm-card {
  width: min(480px, 92vw);
  background: #0f1923;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 16px;
  padding: 28px;
  display: flex;
  flex-direction: column;
  gap: 20px;
  box-shadow: 0 24px 80px rgba(0, 0, 0, 0.7);
  transition: border-color 0.3s;
}
.agm-card--done  { border-color: rgba(27, 107, 58, 0.5); }
.agm-card--error { border-color: rgba(239, 68, 68, 0.5); }

/* Header */
.agm-header {
  display: flex;
  align-items: flex-start;
  gap: 14px;
}
.agm-icon-wrap {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  background: rgba(74, 108, 247, 0.15);
  border: 1px solid rgba(74, 108, 247, 0.3);
  display: flex;
  align-items: center;
  justify-content: center;
  color: #4A6CF7;
  flex-shrink: 0;
  transition: background 0.3s, border-color 0.3s, color 0.3s;
}
.agm-card--done  .agm-icon-wrap { background: rgba(27,107,58,0.15); border-color: rgba(27,107,58,0.3); color: #1B6B3A; }
.agm-card--error .agm-icon-wrap { background: rgba(239,68,68,0.15);  border-color: rgba(239,68,68,0.3);  color: #ef4444; }

.agm-icon-wrap.spin { animation: agm-rotate 2s linear infinite; }
@keyframes agm-rotate { to { transform: rotate(360deg); } }

.agm-titles { flex: 1; min-width: 0; }
.agm-title  { margin: 0 0 4px; font-size: 17px; font-weight: 700; color: #fff; }
.agm-subtitle { margin: 0; font-size: 12px; color: rgba(255,255,255,0.55); line-height: 1.5; }

/* Steps */
.agm-steps {
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.agm-step {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 12px;
  border-radius: 10px;
  border: 1px solid transparent;
  transition: background 0.3s, border-color 0.3s;
}
.agm-step--active  { background: rgba(74,108,247,0.1); border-color: rgba(74,108,247,0.25); }
.agm-step--done    { background: rgba(27,107,58,0.06); }
.agm-step--pending { opacity: 0.4; }

.agm-step-icon {
  width: 28px;
  height: 28px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 13px;
  flex-shrink: 0;
  background: rgba(27,107,58,0.06);
  color: rgba(255,255,255,0.6);
  transition: background 0.3s, color 0.3s;
}
.agm-step--done  .agm-step-icon { background: rgba(27,107,58,0.2); color: #1B6B3A; }
.agm-step--active .agm-step-icon { background: rgba(74,108,247,0.2); color: #4A6CF7; }

.agm-step-num  { font-size: 11px; font-weight: 700; }
.agm-step-text { display: flex; flex-direction: column; gap: 1px; }
.agm-step-label { font-size: 12px; font-weight: 600; color: rgba(255,255,255,0.85); }
.agm-step-desc  { font-size: 10px; color: rgba(255,255,255,0.4); }

/* Progress bar */
.agm-progress-track {
  height: 4px;
  background: rgba(255,255,255,0.08);
  border-radius: 4px;
  overflow: hidden;
}
.agm-progress-bar {
  height: 100%;
  background: linear-gradient(90deg, #4A6CF7, #7C3AED);
  border-radius: 4px;
  transition: width 0.5s ease;
}
.agm-card--done .agm-progress-bar { background: linear-gradient(90deg, #1B6B3A, #06b6d4); }

/* Warning */
.agm-warning {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  padding: 10px 12px;
  border-radius: 8px;
  background: rgba(251, 191, 36, 0.08);
  border: 1px solid rgba(251, 191, 36, 0.25);
  font-size: 11px;
  color: rgba(251, 191, 36, 0.9);
  line-height: 1.5;
}
.agm-warning .pi { margin-top: 1px; flex-shrink: 0; }

/* Footer */
.agm-footer { display: flex; justify-content: flex-end; gap: 10px; }

.agm-btn {
  padding: 8px 20px;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  border: none;
  display: flex;
  align-items: center;
  gap: 6px;
  transition: opacity 0.2s, transform 0.1s;
}
.agm-btn:hover   { opacity: 0.85; }
.agm-btn:active  { transform: scale(0.97); }
.agm-btn--primary { background: #4A6CF7; color: #fff; }
.agm-card--done .agm-btn--primary { background: #1B6B3A; }
.agm-card--error .agm-btn--primary { background: #ef4444; }
.agm-btn--ghost { background: rgba(255,255,255,0.08); color: rgba(255,255,255,0.7); border: 1px solid rgba(255,255,255,0.12); }

/* Transitions */
.agm-fade-enter-active,
.agm-fade-leave-active { transition: opacity 0.25s, transform 0.25s; }
.agm-fade-enter-from,
.agm-fade-leave-to { opacity: 0; }
.agm-fade-enter-from .agm-card,
.agm-fade-leave-to  .agm-card  { transform: scale(0.94) translateY(8px); }
</style>


