<template>
  <div class="page-head">
    <div>
      <h1 class="page-title">Admin Panel</h1>
      <p class="page-sub">Manage users, announcements and feedback</p>
    </div>
  </div>

  <div class="cards">
    <div class="card"><div class="label">Total Users</div><div class="value">{{ users.length }}</div></div>
    <div class="card"><div class="label">Investors</div><div class="value">{{ count('investor') }}</div></div>
    <div class="card"><div class="label">Experts</div><div class="value">{{ count('expert') }}</div></div>
    <div class="card"><div class="label">Admins</div><div class="value">{{ count('admin') }}</div></div>
  </div>

  <div class="chart-card">
    <h3 class="chart-title">Broadcast Announcement</h3>
    <p v-if="annOk" class="success">{{ annOk }}</p>
    <textarea v-model="announcement" class="full" rows="2" placeholder="Sent to every user as a notification..."></textarea>
    <button class="primary" style="margin-top:.75rem" @click="sendAnnouncement" :disabled="!announcement.trim()">Send Announcement</button>
  </div>

  <div class="chart-card">
    <h3 class="chart-title">User Management</h3>
    <div class="table-wrap">
      <table>
        <thead><tr><th>Name</th><th>Email</th><th>Role</th><th></th></tr></thead>
        <tbody>
          <tr v-for="u in users" :key="u.email">
            <td>{{ u.name }}</td>
            <td>{{ u.email }}</td>
            <td>
              <select v-if="u.email !== me" :value="u.role" @change="setRole(u, $event.target.value)">
                <option value="investor">investor</option>
                <option value="expert">expert</option>
                <option value="admin">admin</option>
              </select>
              <span v-else class="badge admin">you</span>
            </td>
            <td>
              <button v-if="u.email !== me" class="del" @click="removeUser(u.email)">Delete</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>

  <div class="chart-card">
    <h3 class="chart-title">Feedback Inbox</h3>
    <div v-if="!feedback.length" class="page-sub">No feedback yet.</div>
    <div class="fb-item" v-for="f in feedback" :key="f.id">
      <div class="fb-top">
        <span><span class="fb-cat">{{ f.category }}</span> <span class="qa-date">{{ f.name }}</span></span>
        <span style="display:flex; align-items:center; gap:.5rem">
          <span class="status" :class="f.status">{{ f.status }}</span>
          <button class="del" @click="removeFeedback(f.id)">Delete</button>
        </span>
      </div>
      <p>{{ f.message }}</p>
      <span class="qa-date">{{ f.created_at }}</span>
      <div v-if="f.admin_response" class="fb-response"><strong>You:</strong> {{ f.admin_response }}</div>
      <div v-else style="margin-top:.6rem; display:flex; gap:.5rem">
        <input v-model="responses[f.id]" class="full" placeholder="Write a response..." />
        <button class="primary" @click="respond(f.id)" :disabled="!(responses[f.id] || '').trim()">Respond</button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import api from '../api'
import { useAuth } from '../store/auth'

const auth = useAuth()
const me = auth.user.email
const users = ref([])
const feedback = ref([])
const announcement = ref('')
const annOk = ref('')
const responses = reactive({})

const count = (role) => users.value.filter((u) => u.role === role).length

async function setRole(u, role) {
  await api.post(`/api/admin/users/${encodeURIComponent(u.email)}/role`, { role })
  load()
}

async function load() {
  const [u, f] = await Promise.all([api.get('/api/admin/users'), api.get('/api/feedback')])
  users.value = u.data
  feedback.value = f.data
}
async function removeUser(email) {
  if (!confirm(`Delete ${email}?`)) return
  await api.delete(`/api/admin/users/${encodeURIComponent(email)}`)
  load()
}
async function sendAnnouncement() {
  await api.post('/api/notifications/announce', { message: announcement.value.trim() })
  announcement.value = ''
  annOk.value = 'Announcement sent to all users.'
  setTimeout(() => (annOk.value = ''), 4000)
}
async function respond(id) {
  await api.post(`/api/feedback/${id}/respond`, { response: responses[id].trim() })
  responses[id] = ''
  load()
}
async function removeFeedback(id) {
  if (!confirm('Delete this feedback?')) return
  await api.delete(`/api/feedback/${id}`)
  load()
}
onMounted(load)
</script>
