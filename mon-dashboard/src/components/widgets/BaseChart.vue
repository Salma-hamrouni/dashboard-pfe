<template>
  <div class="chart-container">
    <canvas ref="chartCanvas"></canvas>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, onBeforeUnmount } from 'vue'
import Chart from 'chart.js/auto'

const props = defineProps<{
  type: string
  data: any
  options?: any
  color?: string
  theme?: string
  textColor?: string
  mutedColor?: string
}>()

const chartCanvas = ref<HTMLCanvasElement | null>(null)
let chartInstance: Chart | null = null

const initChart = () => {
  if (!chartCanvas.value) return
  if (chartInstance) {
    chartInstance.destroy()
  }

  const ctx = chartCanvas.value.getContext('2d')
  if (!ctx) return

  const chartType = props.type === 'area' ? 'line' : props.type

  // Basic data transformation
  let chartData: any = { datasets: [] }

  if (props.type === 'scatter') {
    chartData = {
      datasets: [
        {
          label: 'Données',
          data: props.data.map((d: any) => ({ x: d.x, y: d.y })),
          backgroundColor: props.color || '#10B981',
        },
      ],
    }
  } else if (Array.isArray(props.data)) {
    chartData = {
      labels: props.data.map((d: any) => d.label),
      datasets: [
        {
          label: 'Valeur',
          data: props.data.map((d: any) => d.value),
          backgroundColor:
            props.type === 'pie' || props.type === 'doughnut' || props.type === 'radar'
              ? [props.color || '#10B981', '#3b82f6', '#ec4899', '#f59e0b', '#6366f1', '#8b5cf6']
              : props.color || '#10B981',
          borderColor: props.color || '#10B981',
          borderWidth: 1,
          fill: props.type === 'area',
          tension: 0.4,
        },
      ],
    }
  } else {
    chartData = props.data
  }

  chartInstance = new Chart(ctx, {
    type: chartType as any,
    data: chartData,
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          display: props.type === 'pie' || props.type === 'doughnut' || props.type === 'radar',
          position: 'bottom',
          labels: {
            color: props.textColor || '#f0fdf9',
            font: { size: 10 },
          },
        },
      },
      scales:
        props.type === 'pie' || props.type === 'doughnut' || props.type === 'radar'
          ? {}
          : {
              y: {
                beginAtZero: true,
                grid: { color: props.mutedColor || 'rgba(255, 255, 255, 0.05)' },
                ticks: {
                  color: props.mutedColor || 'rgba(240, 253, 249, 0.5)',
                  font: { size: 10 },
                },
              },
              x: {
                grid: { display: false },
                ticks: {
                  color: props.mutedColor || 'rgba(240, 253, 249, 0.5)',
                  font: { size: 10 },
                },
              },
            },
      ...props.options,
    },
  })
}

onMounted(initChart)

watch(
  () => [props.data, props.type, props.color, props.theme, props.textColor, props.mutedColor],
  initChart,
  { deep: true },
)

onBeforeUnmount(() => {
  if (chartInstance) {
    chartInstance.destroy()
  }
})
</script>

<style scoped>
.chart-container {
  width: 100%;
  height: 100%;
  position: relative;
}
</style>
