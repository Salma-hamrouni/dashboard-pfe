<template>
  <div class="map-widget">
    <div v-if="!hasData" class="map-empty">
      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" width="28" height="28">
        <path d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 13l4.553 2.276A1 1 0 0021 21.382V10.618a1 1 0 00-.553-.894L15 7m0 13V7m0 0L9 4"/>
      </svg>
      <span>Ajoutez des localisations dans le panneau de configuration</span>
    </div>
    <div v-else ref="mapContainer" class="map-container" />
  </div>
</template>

<script setup lang="ts">
import { ref, watch, onMounted, onBeforeUnmount, computed, nextTick } from 'vue'

interface MapMarker {
  lat:    number
  lon:    number
  label?: string
  color?: string
  value?: number
}

const props = withDefaults(defineProps<{
  data?:        MapMarker[]
  markerColor?: string
  zoom?:        number
  centerLat?:   number
  centerLon?:   number
}>(), {
  data:        () => [],
  markerColor: '#3b82f6',
  zoom:        5,
  centerLat:   46.5,
  centerLon:   2.3,
})

const mapContainer = ref<HTMLElement | null>(null)
let leafletMap: any = null
let markersLayer: any = null

const hasData = computed(() =>
  props.data.length > 0 && props.data.some(p => !isNaN(p.lat) && !isNaN(p.lon))
)

async function initMap() {
  if (!mapContainer.value) return
  const L = (await import('leaflet')).default

  // Fix Vite asset paths for default icons
  delete (L.Icon.Default.prototype as any)._getIconUrl
  L.Icon.Default.mergeOptions({
    iconRetinaUrl: new URL('leaflet/dist/images/marker-icon-2x.png', import.meta.url).href,
    iconUrl:       new URL('leaflet/dist/images/marker-icon.png',    import.meta.url).href,
    shadowUrl:     new URL('leaflet/dist/images/marker-shadow.png',  import.meta.url).href,
  })

  if (leafletMap) { leafletMap.remove(); leafletMap = null }

  leafletMap = L.map(mapContainer.value, {
    center:             [props.centerLat, props.centerLon],
    zoom:               props.zoom,
    zoomControl:        true,
    attributionControl: false,
  })

  L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
    maxZoom: 19,
  }).addTo(leafletMap)

  markersLayer = L.layerGroup().addTo(leafletMap)
  renderMarkers(L)
}

function renderMarkers(L: any) {
  if (!markersLayer || !leafletMap) return
  markersLayer.clearLayers()

  const valid = props.data.filter(p => !isNaN(p.lat) && !isNaN(p.lon))
  if (!valid.length) return

  valid.forEach(p => {
    const color = p.color || props.markerColor
    const icon = L.divIcon({
      className: '',
      html: `<div style="
        width:16px;height:16px;
        background:${color};
        border:2.5px solid white;
        border-radius:50%;
        box-shadow:0 0 10px ${color}99,0 2px 6px rgba(0,0,0,0.4);
        transition:transform .15s;
      "></div>`,
      iconSize:   [16, 16],
      iconAnchor: [8,  8],
    })

    const marker = L.marker([p.lat, p.lon], { icon })
    if (p.label) {
      marker.bindTooltip(p.label, {
        direction:  'top',
        offset:     [0, -10],
        className:  'map-tooltip',
        permanent:  false,
      })
    }
    markersLayer.addLayer(marker)
  })

  if (valid.length > 1) {
    const bounds = L.latLngBounds(valid.map(p => [p.lat, p.lon]))
    leafletMap.fitBounds(bounds, { padding: [32, 32], maxZoom: 12 })
  } else if (valid.length === 1) {
    leafletMap.setView([valid[0]!.lat, valid[0]!.lon], props.zoom)
  }
}

onMounted(async () => {
  await import('leaflet/dist/leaflet.css')
  await nextTick()
  if (hasData.value) await initMap()
})

watch(() => [props.data, props.zoom, props.centerLat, props.centerLon], async () => {
  if (!hasData.value) return
  if (!leafletMap) {
    await nextTick()
    await initMap()
  } else {
    const L = (await import('leaflet')).default
    renderMarkers(L)
  }
}, { deep: true })

onBeforeUnmount(() => { leafletMap?.remove(); leafletMap = null })
</script>

<style scoped>
.map-widget {
  width: 100%; height: 100%;
  display: flex; flex-direction: column;
  overflow: hidden; border-radius: 8px;
}
.map-container {
  flex: 1; width: 100%; min-height: 0;
  border-radius: 8px; overflow: hidden;
}
.map-empty {
  flex: 1;
  display: flex; flex-direction: column;
  align-items: center; justify-content: center;
  gap: 10px;
  color: var(--color-text-muted);
  font-size: var(--text-xs);
  text-align: center;
  padding: 16px;
}
</style>

<style>
.map-tooltip {
  background: rgba(10,16,28,0.94) !important;
  border: 1px solid rgba(255,255,255,0.12) !important;
  color: #e2e8f0 !important;
  font-size: 12px !important;
  font-weight: 600 !important;
  border-radius: 6px !important;
  box-shadow: 0 4px 16px rgba(0,0,0,0.5) !important;
  padding: 4px 10px !important;
}
.map-tooltip::before { display: none !important; }
</style>
