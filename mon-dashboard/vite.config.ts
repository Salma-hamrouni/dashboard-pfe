import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'

// https://vite.dev/config/
export default defineConfig(({ mode }) => ({
  plugins: [
    vue(),
    // DevTools uniquement en développement — ne jamais embarquer en prod
    ...(mode === 'development' ? [vueDevTools()] : []),
  ],

  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },

  // ── Serveur de développement ─────────────────────────────────────────────
  server: {
    // Warm-up des modules les plus lourds dès le démarrage (évite la lenteur au 1er accès)
    warmup: {
      clientFiles: [
        './src/views/builder/BuilderView.vue',
        './src/views/dashboard/DashboardListView.vue',
        './src/views/dashboard/DashboardViewerView.vue',
      ],
    },
  },

  // ── Build de production ──────────────────────────────────────────────────
  build: {
    // Seuil d'avertissement chunk : 600 kB (défaut : 500 kB)
    chunkSizeWarningLimit: 600,

    rollupOptions: {
      output: {
        // Séparation manuelle des vendors pour un meilleur cache navigateur
        manualChunks: (id: string) => {
          if (id.includes('node_modules/vue') || id.includes('node_modules/vue-router') || id.includes('node_modules/pinia')) return 'vue-core'
          if (id.includes('node_modules/axios')) return 'vendor-ui'
        },
      },
    },
  },
}))
