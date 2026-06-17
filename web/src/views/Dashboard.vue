<template>
  <div class="page-head">
    <div>
      <h1 class="page-title">Market Dashboard</h1>
      <p class="page-sub">{{ summary.ticker }} — market overview</p>
    </div>
    <select v-model="ticker" @change="load">
      <option v-for="s in stocks" :key="s.symbol" :value="s.symbol">{{ s.symbol }} — {{ s.name }}</option>
    </select>
  </div>

  <div class="cards">
    <div class="card">
      <div class="label">Latest Close</div>
      <div class="value">PKR {{ summary.latest_close }}</div>
      <div :class="summary.change >= 0 ? 'green' : 'red'">
        {{ summary.change >= 0 ? '+' : '' }}{{ summary.change }} ({{ summary.pct_change }}%)
      </div>
    </div>
    <div class="card"><div class="label">52-Week High</div><div class="value green">PKR {{ summary.high_52w }}</div></div>
    <div class="card"><div class="label">52-Week Low</div><div class="value red">PKR {{ summary.low_52w }}</div></div>
    <div class="card"><div class="label">Avg Volume (30d)</div><div class="value">{{ (summary.avg_volume / 1e6).toFixed(2) }}M</div></div>
  </div>

  <div class="chart-card">
    <h3 class="chart-title">Price History (1 Year)</h3>
    <canvas ref="priceCanvas" height="90"></canvas>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import Chart from 'chart.js/auto'
import api from '../api'

const stocks = ref([])
const ticker = ref('OGDC')
const summary = ref({})
const priceCanvas = ref(null)
let chart = null

async function load() {
  const [sum, hist] = await Promise.all([
    api.get(`/api/stocks/${ticker.value}/summary`),
    api.get(`/api/stocks/${ticker.value}/history`),
  ])
  summary.value = sum.data
  if (chart) chart.destroy()
  chart = new Chart(priceCanvas.value, {
    type: 'line',
    data: {
      labels: hist.data.dates,
      datasets: [{
        label: 'Close', data: hist.data.close,
        borderColor: '#00c896', backgroundColor: 'rgba(0,200,150,0.08)',
        borderWidth: 2, pointRadius: 0, fill: true, tension: 0.3,
      }],
    },
    options: {
      plugins: { legend: { display: false } },
      scales: {
        x: { ticks: { maxTicksLimit: 8, color: '#8b949e' }, grid: { color: 'rgba(255,255,255,0.05)' } },
        y: { ticks: { color: '#8b949e' }, grid: { color: 'rgba(255,255,255,0.05)' } },
      },
    },
  })
}

onMounted(async () => {
  const { data } = await api.get('/api/stocks')
  stocks.value = data.stocks
  ticker.value = data.stocks[0].symbol
  load()
})
</script>
