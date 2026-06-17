<template>
  <div class="page-head">
    <div>
      <h1 class="page-title">Stock Predictions</h1>
      <p class="page-sub">Forecast based on real PSX history</p>
    </div>
    <div class="controls">
      <select v-model="ticker" @change="load">
        <option v-for="s in stocks" :key="s.symbol" :value="s.symbol">{{ s.symbol }} — {{ s.name }}</option>
      </select>
    </div>
  </div>

  <div class="cards">
    <div class="card" v-if="isAdmin"><div class="label">Model Accuracy</div><div class="value green">{{ pred.accuracy }}%</div></div>
    <div class="card"><div class="label">Forecast End Price</div><div class="value">PKR {{ lastPrice }}</div></div>
    <div class="card"><div class="label">Forecast High</div><div class="value green">PKR {{ high }}</div></div>
    <div class="card"><div class="label">Forecast Low</div><div class="value red">PKR {{ low }}</div></div>
  </div>

  <div class="chart-card">
    <h3 class="chart-title">Actual vs Predicted</h3>
    <canvas ref="validCanvas" height="90"></canvas>
  </div>
  <div class="chart-card">
    <h3 class="chart-title">Forecast</h3>
    <canvas ref="forecastCanvas" height="90"></canvas>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import Chart from 'chart.js/auto'
import api from '../api'
import { useAuth } from '../store/auth'

const route = useRoute()
const auth = useAuth()
const isAdmin = computed(() => auth.role === 'admin')

const FORECAST_DAYS = 7
const stocks = ref([])
const ticker = ref('OGDC')
const pred = ref({ future: [] })
const validCanvas = ref(null)
const forecastCanvas = ref(null)
let validChart = null
let forecastChart = null

const prices = computed(() => pred.value.future.map((f) => f.price))
const lastPrice = computed(() => (prices.value.length ? prices.value[prices.value.length - 1] : '—'))
const high = computed(() => (prices.value.length ? Math.max(...prices.value) : '—'))
const low = computed(() => (prices.value.length ? Math.min(...prices.value) : '—'))

const axes = {
  x: { ticks: { maxTicksLimit: 8, color: '#8b949e' }, grid: { color: 'rgba(255,255,255,0.05)' } },
  y: { ticks: { color: '#8b949e' }, grid: { color: 'rgba(255,255,255,0.05)' } },
}

async function load() {
  const { data } = await api.get(`/api/predict/${ticker.value}?days=${FORECAST_DAYS}`)
  pred.value = data

  // Actual vs Predicted on the test set
  if (validChart) validChart.destroy()
  validChart = new Chart(validCanvas.value, {
    type: 'line',
    data: {
      labels: data.test_dates,
      datasets: [
        { label: 'Actual', data: data.test_actual, borderColor: '#00c896', borderWidth: 2, pointRadius: 0, tension: 0.3 },
        { label: 'Predicted', data: data.test_predicted, borderColor: '#f59e0b', borderWidth: 2, pointRadius: 0, tension: 0.3, borderDash: [5, 3] },
      ],
    },
    options: { plugins: { legend: { labels: { color: '#8b949e' } } }, scales: axes },
  })

  // Future forecast
  if (forecastChart) forecastChart.destroy()
  forecastChart = new Chart(forecastCanvas.value, {
    type: 'line',
    data: {
      labels: data.future.map((f) => f.date),
      datasets: [{
        label: 'Forecast', data: prices.value,
        borderColor: '#6366f1', backgroundColor: 'rgba(99,102,241,0.1)',
        borderWidth: 2.5, pointRadius: 3, fill: true, tension: 0.4,
      }],
    },
    options: { plugins: { legend: { display: false } }, scales: axes },
  })
}

onMounted(async () => {
  const { data } = await api.get('/api/stocks')
  stocks.value = data.stocks
  // If we arrived from the watchlist (?ticker=XYZ), open that stock; else the first.
  const wanted = (route.query.ticker || '').toString().toUpperCase()
  const match = data.stocks.find((s) => s.symbol === wanted)
  ticker.value = match ? match.symbol : data.stocks[0].symbol
  load()
})
</script>
