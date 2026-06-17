<template>
  <div class="page-head">
    <div>
      <h1 class="page-title">Stocks &amp; Watchlist</h1>
      <p class="page-sub">Click a stock to view its forecast · star it to track it</p>
    </div>
  </div>

  <!-- Filter controls -->
  <div class="controls" style="margin-bottom:1.5rem; flex-wrap:wrap">
    <input v-model="search" placeholder="Search symbol or company..." style="flex:1; min-width:220px" />
    <select v-model="sector">
      <option value="">All Sectors</option>
      <option v-for="s in sectors" :key="s" :value="s">{{ s }}</option>
    </select>
    <label class="page-sub" style="display:flex; align-items:center; gap:.4rem; cursor:pointer">
      <input type="checkbox" v-model="watchOnly" style="width:auto" /> Watchlist only
    </label>
  </div>

  <div v-if="!filtered.length" class="page-sub">No stocks match your filters.</div>

  <div class="stocks">
    <div
      class="stock"
      v-for="s in filtered"
      :key="s.symbol"
      @click="openForecast(s)"
      style="cursor:pointer"
    >
      <div class="top">
        <div>
          <div class="sym">{{ s.symbol }}</div>
          <div class="nm">{{ s.name }}</div>
        </div>
        <button class="star" :class="{ on: s.tracked }" @click.stop="toggle(s)">{{ s.tracked ? '★' : '☆' }}</button>
      </div>
      <div style="margin-top:1rem; display:flex; justify-content:space-between; align-items:center;">
        <span class="nm">{{ s.sector }}</span>
        <div style="text-align:right">
          <div style="font-weight:600">PKR {{ s.price }}</div>
          <div :class="s.change >= 0 ? 'green' : 'red'" style="font-size:.8rem">
            {{ s.change >= 0 ? '+' : '' }}{{ s.change }}%
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import api from '../api'

const router = useRouter()
const stocks = ref([])
const search = ref('')
const sector = ref('')
const watchOnly = ref(false)

const sectors = computed(() => [...new Set(stocks.value.map((s) => s.sector))].sort())

const filtered = computed(() =>
  stocks.value.filter((s) => {
    const q = search.value.toLowerCase()
    const matchesSearch = !q || s.symbol.toLowerCase().includes(q) || s.name.toLowerCase().includes(q)
    const matchesSector = !sector.value || s.sector === sector.value
    const matchesWatch = !watchOnly.value || s.tracked
    return matchesSearch && matchesSector && matchesWatch
  })
)

async function loadStocks() {
  const { data } = await api.get('/api/stocks')
  stocks.value = data.stocks
}

// Go to the Predictions page for the clicked stock.
function openForecast(s) {
  router.push({ path: '/predictions', query: { ticker: s.symbol } })
}

async function toggle(s) {
  if (s.tracked) await api.delete(`/api/watchlist/${s.symbol}`)
  else await api.post(`/api/watchlist/${s.symbol}`)
  s.tracked = !s.tracked
}

onMounted(loadStocks)
</script>
