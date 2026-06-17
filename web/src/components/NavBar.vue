<template>
  <nav class="nav">
    <div class="brand">Invock <span>Investments</span></div>
    <div class="links">
      <RouterLink to="/dashboard">Dashboard</RouterLink>
      <RouterLink to="/predictions">Predictions</RouterLink>
      <RouterLink to="/watchlist">Watchlist</RouterLink>
      <RouterLink to="/qa">Expert Q&amp;A</RouterLink>
      <RouterLink to="/feedback">Feedback</RouterLink>
      <RouterLink v-if="auth.role === 'admin'" to="/admin">Admin</RouterLink>
    </div>
    <div class="user">
      <div class="notif" v-click-outside="() => (open = false)">
        <button class="bell" @click="open = !open">
          🔔<span v-if="unread" class="count">{{ unread }}</span>
        </button>
        <div v-if="open" class="notif-panel">
          <div class="notif-head">
            <span>Notifications</span>
            <button class="link-btn" @click="markRead">Mark all read</button>
          </div>
          <div v-if="!items.length" class="notif-empty">No notifications</div>
          <div v-for="n in items" :key="n.id" class="notif-item" :class="{ unread: !n.is_read }">
            <span>{{ icon(n.type) }}</span>
            <div>
              <p class="notif-msg">{{ n.message }}</p>
              <span class="qa-date">{{ n.created_at }}</span>
            </div>
          </div>
        </div>
      </div>
      <span class="badge" :class="auth.user.role">{{ auth.user.role }}</span>
      <span class="name">{{ auth.user.name }}</span>
      <button class="logout" @click="logout">Logout</button>
    </div>
  </nav>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuth } from '../store/auth'
import api from '../api'

const auth = useAuth()
const router = useRouter()
const items = ref([])
const unread = ref(0)
const open = ref(false)
let timer = null

const ICONS = { announcement: '📢', answer: '💬', feedback: '📝', info: 'ℹ️' }
const icon = (t) => ICONS[t] || 'ℹ️'

async function loadNotifs() {
  try {
    const { data } = await api.get('/api/notifications')
    items.value = data.notifications
    unread.value = data.unread
  } catch (e) { /* ignore transient errors */ }
}
async function markRead() {
  await api.post('/api/notifications/read')
  loadNotifs()
}
function logout() {
  auth.logout()
  router.push('/login')
}

onMounted(() => {
  loadNotifs()
  timer = setInterval(loadNotifs, 30000)
})
onUnmounted(() => clearInterval(timer))
</script>
