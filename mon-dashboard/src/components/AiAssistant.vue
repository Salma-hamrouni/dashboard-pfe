<template>
  <!-- Panel -->
  <Transition name="ai-anim">
    <div v-if="props.open" class="ai-panel">

      <!-- Header -->
      <div class="ai-hdr">
        <span class="ai-hdr-title">✨ Assistant IA</span>
        <span class="ai-hdr-badge">Gemini</span>
        <button class="ai-hdr-close" @click="emit('update:open', false)">✕</button>
      </div>

      <!-- No dataset -->
      <div v-if="!hasDataset" class="ai-empty">
        <span>⚠ Importez un dataset pour utiliser l'assistant IA</span>
      </div>

      <template v-else>
        <!-- Tabs -->
        <div class="ai-tabs">
          <button v-for="t in TABS" :key="t.id"
            class="ai-tab" :class="{ active: activeTab === t.id }"
            @click="activeTab = t.id as typeof activeTab">
            {{ t.icon }} {{ t.label }}
          </button>
        </div>

        <!-- ── CHAT ───────────────────────────────────────── -->
        <template v-if="activeTab === 'chat'">
          <div class="ai-messages" ref="messagesEl">
            <!-- Welcome chips -->
            <div v-if="!chatHistory.length" class="ai-welcome">
              <p>Décrivez ce que vous voulez visualiser :</p>
              <div class="ai-chips">
                <button v-for="s in CHIPS" :key="s" class="ai-chip" @click="sendChat(s)">{{ s }}</button>
              </div>
            </div>

            <template v-for="(msg, i) in chatHistory" :key="i">
              <div class="ai-bubble ai-bubble--user">{{ msg.text }}</div>

              <div v-if="msg.result" class="ai-bubble ai-bubble--bot">
                <div class="ai-bubble-head">
                  <span class="ai-badge" :class="`ai-type-${msg.result.type}`">{{ msg.result.type }}</span>
                  <strong>{{ msg.result.title }}</strong>
                </div>
                <p class="ai-bubble-explain">{{ msg.result.explanation }}</p>
                <div v-if="msg.result.xAxis || msg.result.yAxis" class="ai-bubble-axes">
                  <span v-if="msg.result.xAxis">X : <b>{{ msg.result.xAxis }}</b></span>
                  <span v-if="msg.result.yAxis">Y : <b>{{ msg.result.yAxis }}</b></span>
                  <span v-if="msg.result.aggregation && msg.result.aggregation !== 'none'">Agg : <b>{{ msg.result.aggregation }}</b></span>
                </div>
                <button class="ai-add-btn" @click="emit('create-widget', msg.result)">+ Ajouter au dashboard</button>
              </div>

              <div v-if="msg.error" class="ai-bubble ai-bubble--err">⚠ {{ msg.error }}</div>
            </template>

            <div v-if="chatLoading" class="ai-bubble ai-bubble--bot ai-loading">
              <span/><span/><span/>
            </div>
          </div>

          <div class="ai-input">
            <textarea
              v-model="chatInput"
              class="ai-textarea"
              placeholder="Ex : ventes par mois en courbe…"
              rows="2"
              @keydown.enter.exact.prevent="sendChat()"
            />
            <button class="ai-send" :disabled="!chatInput.trim() || chatLoading" @click="sendChat()">
              ➤
            </button>
          </div>
        </template>

        <!-- ── SUGGESTIONS ────────────────────────────────── -->
        <div v-else-if="activeTab === 'suggest'" class="ai-scroll">
          <button class="ai-run-btn" :disabled="suggestLoading" @click="runSuggestions">
            {{ suggestLoading ? '⏳ Analyse…' : '💡 Générer des suggestions' }}
          </button>

          <div v-if="suggestError" class="ai-err-box">{{ suggestError }}</div>

          <template v-if="chartReco">
            <div class="ai-sub">📊 Graphique recommandé</div>
            <div class="ai-card">
              <div class="ai-card-top">
                <span class="ai-badge" :class="`ai-type-${chartReco.recommendedChart}`">{{ chartReco.recommendedChart }}</span>
                <span class="ai-card-text">{{ chartReco.reason }}</span>
              </div>
              <div v-if="chartReco.xAxis || chartReco.yAxis" class="ai-card-axes">
                <span v-if="chartReco.xAxis">X : <b>{{ chartReco.xAxis }}</b></span>
                <span v-if="chartReco.yAxis">Y : <b>{{ chartReco.yAxis }}</b></span>
              </div>
              <div v-if="chartReco.alternatives?.length" class="ai-card-alts">
                <span v-for="a in chartReco.alternatives" :key="a" class="ai-alt">{{ a }}</span>
              </div>
              <button class="ai-add-btn" @click="emit('create-widget', recoToWidget(chartReco))">+ Ajouter</button>
            </div>
          </template>

          <template v-if="kpis.length">
            <div class="ai-sub">⚡ KPIs suggérés</div>
            <div v-for="(k, i) in kpis" :key="i" class="ai-card">
              <div class="ai-card-top">
                <strong>{{ k.title }}</strong>
                <span class="ai-pill">{{ k.aggregation }}</span>
              </div>
              <div class="ai-card-col">{{ k.column }}</div>
              <p v-if="k.description" class="ai-card-text">{{ k.description }}</p>
              <button class="ai-add-btn" @click="emit('create-widget', kpiToWidget(k))">+ Ajouter</button>
            </div>
          </template>
        </div>

        <!-- ── NARRATION ─────────────────────────────────── -->
        <div v-else-if="activeTab === 'narrative'" class="ai-scroll">
          <div class="ai-narr-intro">
            <p>Génère un rapport narratif complet sur votre dataset en langage naturel.</p>
          </div>

          <input
            v-model="narrativeFocus"
            class="ai-q"
            placeholder="Angle d'analyse (optionnel) — ex : focus sur les ventes…"
          />

          <button class="ai-run-btn" :disabled="narrativeLoading" @click="runNarrative">
            {{ narrativeLoading ? '⏳ Rédaction en cours…' : '📝 Générer l\'analyse narrative' }}
          </button>

          <div v-if="narrativeError" class="ai-err-box">{{ narrativeError }}</div>

          <div v-if="narrativeText" class="ai-narrative-body">
            <!-- Titre du rapport -->
            <div class="ai-narr-header">
              <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                <polyline points="14 2 14 8 20 8"/>
                <line x1="16" y1="13" x2="8" y2="13"/>
                <line x1="16" y1="17" x2="8" y2="17"/>
                <polyline points="10 9 9 9 8 9"/>
              </svg>
              <span>Rapport d'analyse — {{ new Date().toLocaleDateString('fr-FR') }}</span>
              <button class="ai-copy-btn" @click="copyNarrative" :title="narrativeCopied ? 'Copié !' : 'Copier le texte'">
                <svg v-if="!narrativeCopied" width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="9" y="9" width="13" height="13" rx="2"/>
                  <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/>
                </svg>
                <svg v-else width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="#1B6B3A" stroke-width="2.5">
                  <polyline points="20 6 9 17 4 12"/>
                </svg>
                {{ narrativeCopied ? 'Copié' : 'Copier' }}
              </button>
              <button class="ai-copy-btn ai-pdf-btn" @click="downloadNarrativePdf" title="Télécharger en PDF">
                <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                  <polyline points="14 2 14 8 20 8"/>
                  <line x1="12" y1="18" x2="12" y2="12"/>
                  <polyline points="9 15 12 18 15 15"/>
                </svg>
                PDF
              </button>
            </div>

            <!-- Texte rendu avec formatage markdown léger -->
            <div class="ai-narr-text" v-html="renderNarrative(narrativeText)"></div>

            <!-- Dataset info chips -->
            <div class="ai-narr-footer">
              <span class="ai-narr-chip">{{ columns.length }} colonne{{ columns.length > 1 ? 's' : '' }}</span>
              <span class="ai-narr-chip">{{ preview.length }} ligne{{ preview.length > 1 ? 's' : '' }} analysées</span>
              <span class="ai-narr-chip ai-narr-chip--ai">✨ Gemini 2.5</span>
            </div>
          </div>
        </div>

        <!-- ── ANALYSE ────────────────────────────────────── -->
        <div v-else class="ai-scroll">
          <input v-model="analyzeQ" class="ai-q" placeholder="Question spécifique (optionnel)…" />
          <button class="ai-run-btn" :disabled="analyzeLoading" @click="runAnalysis">
            {{ analyzeLoading ? '⏳ Analyse…' : '🔍 Analyser les données' }}
          </button>

          <div v-if="analyzeError" class="ai-err-box">{{ analyzeError }}</div>

          <template v-if="analysis">
            <div class="ai-summary">{{ analysis.summary }}</div>
            <template v-for="sec in SECTIONS" :key="sec.key">
              <template v-if="getSection(sec.key).length">
                <div class="ai-sub">{{ sec.icon }} {{ sec.label }}</div>
                <ul class="ai-ul">
                  <li v-for="(item, j) in getSection(sec.key)" :key="j">{{ item }}</li>
                </ul>
              </template>
            </template>
          </template>
        </div>
      </template>
    </div>
  </Transition>
</template>

<script setup lang="ts">
import { ref, computed, nextTick } from 'vue'
import { useDatasetStore } from '@/stores/dataset'
import aiService, {
  type AiChartRecommendation,
  type AiKpi,
  type AiAnalysis,
  type AiWidgetConfig,
} from '@/services/aiService'

const props = defineProps<{ open?: boolean }>()
const emit = defineEmits<{
  'create-widget': [cfg: AiWidgetConfig]
  'update:open':   [val: boolean]
}>()

const datasetStore = useDatasetStore()
const hasDataset   = computed(() => !!datasetStore.currentDataset)
const columns      = computed(() =>
  (datasetStore.currentDataset?.columns ?? []).map(c => ({ name: c.name, type: c.type }))
)
const preview = computed(() =>
  (datasetStore.currentDataset?.data ?? []).slice(0, 10) as Record<string, unknown>[]
)

// ── Panel — driven by parent via v-model:open ─────────────────────────────
const activeTab = ref<'chat' | 'suggest' | 'analyze' | 'narrative'>('chat')

const TABS = [
  { id: 'chat',      icon: '💬', label: 'Chat'       },
  { id: 'suggest',   icon: '💡', label: 'Suggestions'},
  { id: 'analyze',   icon: '🔍', label: 'Analyse'    },
  { id: 'narrative', icon: '📝', label: 'Narration'  },
]

// ── Chat ─────────────────────────────────────────────────────────────────────
interface Msg { text: string; result?: AiWidgetConfig; error?: string }

const chatInput   = ref('')
const chatLoading = ref(false)
const chatHistory = ref<Msg[]>([])
const messagesEl  = ref<HTMLElement | null>(null)

/** Suggestions générées depuis les colonnes réelles du dataset */
const CHIPS = computed<string[]>(() => {
  const cols = datasetStore.currentDataset?.columns ?? []
  const nums  = cols.filter(c => c.type === 'number').map(c => c.name)
  const cats  = cols.filter(c => c.type === 'category' || c.type === 'string').map(c => c.name)
  const dates = cols.filter(c => c.type === 'date').map(c => c.name)

  const out: string[] = []

  // Courbe temporelle : date × numérique
  if (dates.length && nums.length)
    out.push(`${nums[0]} par ${dates[0]} en courbe`)

  // Barres : catégorie × numérique
  if (cats.length && nums.length)
    out.push(`${nums[0]} par ${cats[0]} en barres`)

  // Camembert : répartition par catégorie
  if (cats.length && nums.length)
    out.push(`Répartition de ${nums[0]} par ${cats[0]}`)

  // KPI : première colonne numérique
  if (nums.length)
    out.push(`KPI total de ${nums[0]}`)

  // Fallback si dataset vide ou sans colonnes utiles
  if (out.length === 0) {
    out.push('Montre-moi un résumé des données')
    out.push('Suggère le meilleur graphique')
  }

  return out.slice(0, 4)
})

async function sendChat(text?: string) {
  const msg = (text ?? chatInput.value).trim()
  if (!msg || chatLoading.value) return
  chatInput.value = ''
  chatHistory.value.push({ text: msg })
  chatLoading.value = true
  await nextTick()
  scrollMessages()
  try {
    const result = await aiService.chat(msg, columns.value)
    const last = chatHistory.value.at(-1)
    if (last) last.result = result
  } catch (e: any) {
    const last = chatHistory.value.at(-1)
    if (last) last.error = cleanError(e?.message)
  } finally {
    chatLoading.value = false
    await nextTick()
    scrollMessages()
  }
}

function scrollMessages() {
  messagesEl.value?.scrollTo({ top: messagesEl.value.scrollHeight, behavior: 'smooth' })
}

// ── Suggestions ──────────────────────────────────────────────────────────────
const suggestLoading = ref(false)
const suggestError   = ref('')
const chartReco      = ref<AiChartRecommendation | null>(null)
const kpis           = ref<AiKpi[]>([])

async function runSuggestions() {
  suggestLoading.value = true
  suggestError.value   = ''
  chartReco.value      = null
  kpis.value           = []
  try {
    const [reco, kpiRes] = await Promise.all([
      aiService.recommendChart(columns.value),
      aiService.suggestKpis(columns.value),
    ])
    chartReco.value = reco
    kpis.value      = kpiRes.kpis ?? []
  } catch (e: any) {
    suggestError.value = cleanError(e?.message)
  } finally {
    suggestLoading.value = false
  }
}

function recoToWidget(r: AiChartRecommendation): AiWidgetConfig {
  const title = (r.yAxis && r.xAxis)
    ? `${r.yAxis} par ${r.xAxis}`
    : r.yAxis || r.recommendedChart
  return { type: r.recommendedChart, title, xAxis: r.xAxis, yAxis: r.yAxis, aggregation: 'sum', filters: [], explanation: r.reason }
}
function kpiToWidget(k: AiKpi): AiWidgetConfig {
  return { type: 'kpi', title: k.title, xAxis: null, yAxis: k.column, aggregation: k.aggregation || 'sum', filters: [], explanation: k.description }
}

// ── Analyse ───────────────────────────────────────────────────────────────────
const analyzeLoading = ref(false)
const analyzeError   = ref('')
const analyzeQ       = ref('')
const analysis       = ref<AiAnalysis | null>(null)

const SECTIONS = [
  { key: 'insights'        as const, icon: '💡', label: 'Insights'        },
  { key: 'trends'          as const, icon: '📈', label: 'Tendances'        },
  { key: 'anomalies'       as const, icon: '⚠️',  label: 'Anomalies'        },
  { key: 'recommendations' as const, icon: '✅', label: 'Recommandations'  },
]

function getSection(key: keyof AiAnalysis): string[] {
  if (!analysis.value) return []
  const v = analysis.value[key]
  return Array.isArray(v) ? v : []
}

async function runAnalysis() {
  analyzeLoading.value = true
  analyzeError.value   = ''
  analysis.value       = null
  try {
    analysis.value = await aiService.analyze(columns.value, preview.value, analyzeQ.value.trim() || undefined)
  } catch (e: any) {
    analyzeError.value = cleanError(e?.message)
  } finally {
    analyzeLoading.value = false
  }
}

// ── Narration ─────────────────────────────────────────────────────────────────
const narrativeLoading = ref(false)
const narrativeError   = ref('')
const narrativeText    = ref('')
const narrativeFocus   = ref('')
const narrativeCopied  = ref(false)

async function runNarrative() {
  if (narrativeLoading.value) return
  narrativeLoading.value = true
  narrativeError.value   = ''
  narrativeText.value    = ''
  try {
    const result = await aiService.narrative(
      columns.value,
      preview.value,
      narrativeFocus.value.trim() || undefined
    )
    narrativeText.value = typeof result === 'string' ? result : String(result)
  } catch (e: any) {
    narrativeError.value = cleanError(e?.message)
  } finally {
    narrativeLoading.value = false
  }
}

async function copyNarrative() {
  if (!narrativeText.value) return
  await navigator.clipboard.writeText(narrativeText.value)
  narrativeCopied.value = true
  setTimeout(() => { narrativeCopied.value = false }, 2000)
}

function downloadNarrativePdf() {
  if (!narrativeText.value) return

  const html = renderNarrative(narrativeText.value)
  const date = new Date().toLocaleDateString('fr-FR', { day: 'numeric', month: 'long', year: 'numeric' })
  const dataset = (datasetStore.currentDataset?.name ?? 'Dataset')
  const cols = columns.value.length

  const printWindow = window.open('', '_blank', 'width=900,height=700')
  if (!printWindow) return

  printWindow.document.write(`<!DOCTYPE html>
<html lang="fr">
<head>
  <meta charset="UTF-8">
  <title>Analyse Narrative — ${dataset}</title>
  <style>
    * { box-sizing: border-box; margin: 0; padding: 0; }
    body {
      font-family: 'Segoe UI', system-ui, sans-serif;
      font-size: 13px;
      line-height: 1.75;
      color: #1e293b;
      background: #fff;
      padding: 48px 56px;
      max-width: 820px;
      margin: 0 auto;
    }

    /* Cover */
    .cover {
      border-bottom: 3px solid #1B6B3A;
      padding-bottom: 24px;
      margin-bottom: 32px;
    }
    .cover-tag {
      display: inline-block;
      background: #ecfdf5;
      color: #134E2A;
      border: 1px solid #134E2A;
      border-radius: 4px;
      font-size: 10px;
      font-weight: 700;
      letter-spacing: .08em;
      text-transform: uppercase;
      padding: 3px 10px;
      margin-bottom: 14px;
    }
    .cover h1 {
      font-size: 26px;
      font-weight: 800;
      color: #0f172a;
      line-height: 1.2;
      margin-bottom: 10px;
    }
    .cover-meta {
      display: flex;
      gap: 24px;
      font-size: 12px;
      color: #64748b;
      margin-top: 14px;
    }
    .cover-meta span::before { content: '• '; color: #1B6B3A; }

    /* Corps */
    p { margin-bottom: 14px; color: #334155; }

    h4 {
      display: flex;
      align-items: center;
      gap: 10px;
      font-size: 11px;
      font-weight: 800;
      text-transform: uppercase;
      letter-spacing: .08em;
      color: #134E2A;
      margin: 28px 0 10px;
      padding-bottom: 8px;
      border-bottom: 1px solid #C8D8CC;
    }
    .num {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 20px; height: 20px;
      background: #ecfdf5;
      border: 1px solid #134E2A;
      border-radius: 50%;
      font-size: 10px;
      color: #134E2A;
      flex-shrink: 0;
    }

    strong { color: #0f172a; font-weight: 600; }
    em { color: #64748b; font-style: italic; }

    ul { margin: 8px 0 14px 18px; }
    ul li {
      color: #475569;
      margin-bottom: 5px;
      line-height: 1.6;
    }

    /* Footer */
    .footer {
      margin-top: 40px;
      padding-top: 16px;
      border-top: 1px solid #e2e8f0;
      display: flex;
      justify-content: space-between;
      font-size: 10px;
      color: #94a3b8;
    }

    @media print {
      body { padding: 20px 28px; }
      @page { margin: 1.5cm; }
    }
  </style>
</head>
<body>
  <div class="cover">
    <div class="cover-tag">✨ Analyse IA — Gemini 2.5</div>
    <h1>Rapport d'Analyse Narrative<br><span style="color:#134E2A">${dataset}</span></h1>
    <div class="cover-meta">
      <span>Généré le ${date}</span>
      <span>${cols} colonne${cols > 1 ? 's' : ''} analysée${cols > 1 ? 's' : ''}</span>
      <span>DashGen BI</span>
    </div>
  </div>

  ${html}

  <div class="footer">
    <span>Généré par DashGen — Assistant IA Gemini 2.5</span>
    <span>${date}</span>
  </div>

  <script>
    window.onload = function() {
      window.print();
      setTimeout(function() { window.close(); }, 1000);
    };
  <\/script>
</body>
</html>`)

  printWindow.document.close()
}

function renderNarrative(text: string): string {
  return text
    // Titres de sections : **Titre** en début de ligne
    .replace(/^\*\*(.+?)\*\*\s*$/gm, '<h4 class="ai-narr-section">$1</h4>')
    // Gras inline
    .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
    // Italique
    .replace(/\*(.*?)\*/g, '<em>$1</em>')
    // Numéros de section "1. Titre"
    .replace(/^(\d+)\.\s+\*\*(.+?)\*\*/gm, '<h4 class="ai-narr-section"><span class="ai-narr-num">$1</span>$2</h4>')
    // Puces
    .replace(/^[-•]\s+(.+)$/gm, '<li>$1</li>')
    .replace(/(<li>.*<\/li>\n?)+/g, '<ul class="ai-narr-ul">$&</ul>')
    // Sauts de ligne → paragraphes (groupes de lignes non-HTML)
    .split(/\n{2,}/)
    .map(block => block.trim().startsWith('<') ? block : `<p>${block.replace(/\n/g, '<br>')}</p>`)
    .join('\n')
}

// ── Utils ─────────────────────────────────────────────────────────────────────
function cleanError(msg?: string): string {
  if (!msg) return 'Erreur IA inconnue.'

  const m = msg.toLowerCase()

  if (m.includes('quota') || m.includes('resource_exhausted'))
    return '⚠ Quota Gemini dépassé. Attendez la réinitialisation (généralement 1 min ou 1 jour selon le plan) ou activez la facturation sur aistudio.google.com.'

  if (m.includes('permission_denied') || m.includes('api key') || m.includes('clé api') || m.includes('révoquée') || m.includes('invalide'))
    return '🔑 Clé API Gemini invalide ou révoquée. Mettez-la à jour dans appsettings.json.'

  if (m.includes('timeout') || m.includes('timed out'))
    return '⏱ Délai dépassé. Gemini met parfois plus de 60 s — réessayez dans quelques instants.'

  if (m.includes('unavailable') || m.includes('indisponible') || m.includes('503'))
    return '🔄 Gemini est temporairement indisponible. Réessayez dans quelques secondes.'

  // Hide raw JSON or long stack traces
  if (msg.length > 140) return msg.slice(0, 140) + '…'
  return msg
}
</script>

<style scoped>
/* ── Panel ───────────────────────────────────────────────────────────────── */
.ai-panel {
  position: fixed; top: 72px; right: 24px; z-index: 499;
  width: 420px;
  max-height: calc(100vh - 96px);
  min-height: 480px;
  background: #FFFFFF;
  border: 1px solid rgba(99,102,241,0.45);
  border-radius: 18px;
  box-shadow:
    0 0 0 1px rgba(99,102,241,0.15),
    0 20px 60px rgba(0,0,0,0.7),
    0 0 40px rgba(99,102,241,0.15);
  display: flex; flex-direction: column; overflow: hidden;
  font-family: var(--font-sans, sans-serif);
}
.ai-anim-enter-active, .ai-anim-leave-active { transition: opacity 0.2s, transform 0.2s; }
.ai-anim-enter-from, .ai-anim-leave-to { opacity: 0; transform: translateY(-10px) scale(0.97); }

/* ── Header ──────────────────────────────────────────────────────────────── */
.ai-hdr {
  display: flex; align-items: center; gap: 8px;
  padding: 11px 14px 10px; flex-shrink: 0;
  background: linear-gradient(135deg, rgba(99,102,241,0.14), rgba(139,92,246,0.08));
  border-bottom: 1px solid rgba(27,107,58,0.06);
}
.ai-hdr-title { font-size: 13px; font-weight: 600; color: rgba(255,255,255,0.9); flex: 1; }
.ai-hdr-badge {
  font-size: 9px; padding: 2px 6px; border-radius: 4px; font-weight: 700;
  background: rgba(99,102,241,0.2); border: 1px solid rgba(99,102,241,0.35);
  color: #a5b4fc; letter-spacing: 0.05em;
}
.ai-hdr-close {
  background: none; border: none; color: rgba(255,255,255,0.3);
  font-size: 14px; cursor: pointer; padding: 2px 6px; border-radius: 4px;
  transition: color 0.15s, background 0.15s;
}
.ai-hdr-close:hover { color: rgba(255,255,255,0.8); background: rgba(255,255,255,0.07); }

/* ── Empty / no dataset ──────────────────────────────────────────────────── */
.ai-empty {
  flex: 1; display: flex; align-items: center; justify-content: center;
  font-size: 12px; color: rgba(255,255,255,0.35); padding: 24px; text-align: center;
}

/* ── Tabs ────────────────────────────────────────────────────────────────── */
.ai-tabs {
  display: flex; flex-shrink: 0;
  border-bottom: 1px solid rgba(27,107,58,0.06);
}
.ai-tab {
  flex: 1; padding: 8px 4px; font-size: 11px; background: none; border: none;
  color: rgba(255,255,255,0.4); cursor: pointer;
  border-bottom: 2px solid transparent; margin-bottom: -1px;
  transition: color 0.15s, border-color 0.15s;
  font-family: var(--font-sans, sans-serif);
}
.ai-tab:hover  { color: rgba(255,255,255,0.7); }
.ai-tab.active { color: #a5b4fc; border-bottom-color: #6366f1; }

/* ── Chat layout ─────────────────────────────────────────────────────────── */
.ai-messages {
  flex: 1; overflow-y: auto; padding: 12px 12px 8px;
  display: flex; flex-direction: column; gap: 8px;
  min-height: 0; scrollbar-width: thin;
}
.ai-input {
  flex-shrink: 0; display: flex; gap: 8px; align-items: flex-end;
  padding: 8px 12px 10px; border-top: 1px solid rgba(27,107,58,0.06);
}
.ai-textarea {
  flex: 1; background: rgba(27,107,58,0.06); border: 1px solid rgba(255,255,255,0.1);
  border-radius: 8px; color: rgba(255,255,255,0.88); font-size: 12px;
  padding: 8px 10px; resize: none; outline: none;
  font-family: var(--font-sans, sans-serif); transition: border-color 0.15s;
}
.ai-textarea:focus { border-color: rgba(99,102,241,0.5); }
.ai-send {
  width: 36px; height: 36px; flex-shrink: 0;
  background: #6366f1; border: none; border-radius: 8px;
  color: white; font-size: 15px; cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  transition: background 0.15s, opacity 0.15s;
}
.ai-send:hover:not(:disabled) { background: #4f46e5; }
.ai-send:disabled { opacity: 0.35; cursor: not-allowed; }

/* ── Welcome / chips ─────────────────────────────────────────────────────── */
.ai-welcome { font-size: 12px; color: rgba(255,255,255,0.4); }
.ai-welcome p { margin: 0 0 10px; }
.ai-chips { display: flex; flex-wrap: wrap; gap: 6px; }
.ai-chip {
  padding: 4px 10px; font-size: 10px; border-radius: 999px; cursor: pointer;
  background: rgba(99,102,241,0.12); border: 1px solid rgba(99,102,241,0.28); color: #a5b4fc;
  transition: background 0.15s; font-family: var(--font-sans, sans-serif);
}
.ai-chip:hover { background: rgba(99,102,241,0.25); }

/* ── Bubbles ─────────────────────────────────────────────────────────────── */
.ai-bubble {
  max-width: 90%; padding: 8px 12px; border-radius: 12px;
  font-size: 12px; line-height: 1.5; word-break: break-word;
}
.ai-bubble--user {
  align-self: flex-end;
  background: rgba(99,102,241,0.22); border: 1px solid rgba(99,102,241,0.35);
  color: rgba(255,255,255,0.88);
}
.ai-bubble--bot {
  align-self: flex-start; max-width: 100%;
  background: rgba(255,255,255,0.04); border: 1px solid rgba(255,255,255,0.08);
  color: rgba(255,255,255,0.8);
}
.ai-bubble--err {
  align-self: flex-start; max-width: 100%;
  background: rgba(239,68,68,0.1); border: 1px solid rgba(239,68,68,0.22); color: #fca5a5;
}
.ai-bubble-head { display: flex; align-items: center; gap: 8px; margin-bottom: 5px; }
.ai-bubble-explain { margin: 0 0 6px; font-size: 11px; color: rgba(255,255,255,0.55); }
.ai-bubble-axes {
  display: flex; flex-wrap: wrap; gap: 10px;
  font-size: 10px; color: rgba(255,255,255,0.45); margin-bottom: 8px;
}
.ai-bubble-axes b { color: rgba(255,255,255,0.75); font-weight: 600; }

/* Loading dots */
.ai-loading { display: flex; gap: 5px; padding: 12px 14px; }
.ai-loading span {
  width: 7px; height: 7px; border-radius: 50%;
  background: rgba(99,102,241,0.7);
  animation: ai-dot 0.9s infinite ease-in-out;
}
.ai-loading span:nth-child(2) { animation-delay: 0.15s; }
.ai-loading span:nth-child(3) { animation-delay: 0.30s; }
@keyframes ai-dot {
  0%, 80%, 100% { transform: scale(0.75); opacity: 0.35; }
  40%           { transform: scale(1.1);  opacity: 1;    }
}

/* ── Scrollable tab body (suggest + analyze) ─────────────────────────────── */
.ai-scroll {
  flex: 1; overflow-y: auto; padding: 12px;
  display: flex; flex-direction: column; gap: 10px;
  min-height: 0; scrollbar-width: thin;
}

/* ── Shared elements ─────────────────────────────────────────────────────── */
.ai-run-btn {
  width: 100%; padding: 8px 0; border-radius: 8px; border: none; cursor: pointer;
  background: rgba(99,102,241,0.15); border: 1px solid rgba(99,102,241,0.3);
  color: #a5b4fc; font-size: 12px; font-weight: 500;
  transition: background 0.15s; font-family: var(--font-sans, sans-serif);
}
.ai-run-btn:hover:not(:disabled) { background: rgba(99,102,241,0.28); }
.ai-run-btn:disabled { opacity: 0.45; cursor: not-allowed; }

.ai-err-box {
  padding: 8px 10px; border-radius: 8px; font-size: 11px;
  background: rgba(239,68,68,0.1); border: 1px solid rgba(239,68,68,0.2); color: #fca5a5;
}

.ai-sub {
  font-size: 10px; font-weight: 700; text-transform: uppercase;
  letter-spacing: 0.07em; color: rgba(255,255,255,0.4); margin-top: 4px;
}

.ai-card {
  background: rgba(255,255,255,0.03); border: 1px solid rgba(255,255,255,0.08);
  border-radius: 10px; padding: 10px 12px; display: flex; flex-direction: column; gap: 5px;
}
.ai-card-top  { display: flex; align-items: flex-start; gap: 8px; }
.ai-card-text { font-size: 11px; color: rgba(255,255,255,0.5); flex: 1; margin: 0; }
.ai-card-axes { font-size: 11px; color: rgba(255,255,255,0.4); display: flex; gap: 12px; }
.ai-card-axes b { color: rgba(255,255,255,0.75); }
.ai-card-alts { display: flex; flex-wrap: wrap; gap: 4px; }
.ai-card-col  { font-size: 11px; color: rgba(255,255,255,0.45); }
.ai-alt {
  padding: 1px 7px; font-size: 10px; border-radius: 4px;
  background: rgba(27,107,58,0.06); border: 1px solid rgba(255,255,255,0.1);
  color: rgba(255,255,255,0.5);
}
.ai-pill {
  font-size: 9px; padding: 1px 6px; border-radius: 4px; font-weight: 700;
  text-transform: uppercase; letter-spacing: 0.05em;
  background: rgba(139,92,246,0.18); border: 1px solid rgba(139,92,246,0.3); color: #c4b5fd;
}

.ai-add-btn {
  align-self: flex-start; margin-top: 2px;
  padding: 4px 10px; font-size: 10px; border-radius: 6px; cursor: pointer;
  background: rgba(27,107,58,0.12); border: 1px solid rgba(27,107,58,0.3);
  color: #1B6B3A; transition: background 0.15s;
  font-family: var(--font-sans, sans-serif);
}
.ai-add-btn:hover { background: rgba(27,107,58,0.24); }

/* ── Type badges ─────────────────────────────────────────────────────────── */
.ai-badge {
  display: inline-block; padding: 2px 7px; border-radius: 4px; font-size: 9px;
  font-weight: 700; text-transform: uppercase; letter-spacing: 0.06em; white-space: nowrap;
  background: rgba(99,102,241,0.2); border: 1px solid rgba(99,102,241,0.3); color: #a5b4fc;
}
.ai-type-bar    { background: rgba(59,130,246,0.18); border-color: rgba(59,130,246,0.3); color: #93c5fd; }
.ai-type-line   { background: rgba(27,107,58,0.18); border-color: rgba(27,107,58,0.3); color: #1B6B3A; }
.ai-type-pie,.ai-type-doughnut { background: rgba(245,158,11,0.18); border-color: rgba(245,158,11,0.3); color: #fcd34d; }
.ai-type-kpi    { background: rgba(139,92,246,0.18); border-color: rgba(139,92,246,0.3); color: #c4b5fd; }
.ai-type-scatter { background: rgba(236,72,153,0.18); border-color: rgba(236,72,153,0.3); color: #f9a8d4; }
.ai-type-table  { background: rgba(148,163,184,0.18); border-color: rgba(148,163,184,0.3); color: #cbd5e1; }

/* ── Analyse ─────────────────────────────────────────────────────────────── */
.ai-q {
  width: 100%; box-sizing: border-box;
  background: rgba(255,255,255,0.04); border: 1px solid rgba(255,255,255,0.1);
  border-radius: 8px; color: rgba(255,255,255,0.8); font-size: 12px;
  padding: 7px 10px; outline: none; font-family: var(--font-sans, sans-serif);
}
.ai-q:focus { border-color: rgba(99,102,241,0.5); }

.ai-summary {
  padding: 10px 12px; border-radius: 10px; font-size: 12px; line-height: 1.6;
  background: rgba(99,102,241,0.08); border: 1px solid rgba(99,102,241,0.18);
  color: rgba(255,255,255,0.75);
}
.ai-ul { margin: 0; padding: 0 0 0 16px; display: flex; flex-direction: column; gap: 4px; }
.ai-ul li { font-size: 11px; color: rgba(255,255,255,0.6); line-height: 1.5; }

/* ── Narration ───────────────────────────────────────────────────────────── */
.ai-narr-intro {
  padding: 8px 10px;
  background: rgba(99,102,241,0.07);
  border: 1px solid rgba(99,102,241,0.15);
  border-radius: 8px;
  font-size: 11px;
  color: rgba(255,255,255,0.45);
}
.ai-narr-intro p { margin: 0; }

.ai-narrative-body {
  display: flex;
  flex-direction: column;
  gap: 0;
  background: rgba(255,255,255,0.025);
  border: 1px solid rgba(255,255,255,0.07);
  border-radius: 12px;
  overflow: visible;
}

.ai-narr-header {
  display: flex;
  align-items: center;
  gap: 7px;
  padding: 9px 12px;
  background: rgba(99,102,241,0.1);
  border-bottom: 1px solid rgba(99,102,241,0.15);
  font-size: 10px;
  color: rgba(255,255,255,0.45);
  font-weight: 600;
  letter-spacing: 0.04em;
}
.ai-narr-header svg { color: #a5b4fc; flex-shrink: 0; }
.ai-narr-header span { flex: 1; }

.ai-copy-btn {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  background: rgba(27,107,58,0.06);
  border: 1px solid rgba(255,255,255,0.1);
  border-radius: 5px;
  color: rgba(255,255,255,0.5);
  font-size: 10px;
  padding: 3px 7px;
  cursor: pointer;
  transition: background 0.15s, color 0.15s;
  font-family: var(--font-sans, sans-serif);
}
.ai-copy-btn:hover { background: rgba(255,255,255,0.12); color: rgba(255,255,255,0.8); }
.ai-pdf-btn { color: #fca5a5; border-color: rgba(239,68,68,0.25); background: rgba(239,68,68,0.07); }
.ai-pdf-btn:hover { background: rgba(239,68,68,0.18); color: #f87171; border-color: rgba(239,68,68,0.4); }

.ai-narr-text {
  padding: 12px 14px;
  font-size: 12px;
  line-height: 1.75;
  color: rgba(255,255,255,0.72);
}
.ai-narr-text :deep(p) {
  margin: 0 0 10px;
}
.ai-narr-text :deep(h4.ai-narr-section) {
  display: flex;
  align-items: center;
  gap: 7px;
  font-size: 11px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.07em;
  color: #a5b4fc;
  margin: 14px 0 6px;
  padding-bottom: 5px;
  border-bottom: 1px solid rgba(99,102,241,0.2);
}
.ai-narr-text :deep(.ai-narr-num) {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 18px;
  height: 18px;
  background: rgba(99,102,241,0.25);
  border-radius: 50%;
  font-size: 10px;
  color: #a5b4fc;
  flex-shrink: 0;
}
.ai-narr-text :deep(strong) {
  color: rgba(255,255,255,0.88);
  font-weight: 600;
}
.ai-narr-text :deep(em) {
  color: rgba(255,255,255,0.6);
  font-style: italic;
}
.ai-narr-text :deep(ul.ai-narr-ul) {
  margin: 4px 0 10px;
  padding: 0 0 0 14px;
  display: flex;
  flex-direction: column;
  gap: 3px;
}
.ai-narr-text :deep(ul.ai-narr-ul li) {
  font-size: 11.5px;
  color: rgba(255,255,255,0.62);
  line-height: 1.5;
}

.ai-narr-footer {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
  padding: 8px 12px;
  border-top: 1px solid rgba(27,107,58,0.06);
}
.ai-narr-chip {
  font-size: 9.5px;
  padding: 2px 8px;
  border-radius: 4px;
  background: rgba(27,107,58,0.06);
  border: 1px solid rgba(255,255,255,0.08);
  color: rgba(255,255,255,0.4);
}
.ai-narr-chip--ai {
  background: rgba(99,102,241,0.12);
  border-color: rgba(99,102,241,0.25);
  color: #a5b4fc;
}
</style>


