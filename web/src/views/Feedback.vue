<template>
  <div class="page-head">
    <div>
      <h1 class="page-title">Feedback</h1>
      <p class="page-sub">Share suggestions, report issues or tell us what you think</p>
    </div>
  </div>

  <div class="feedback-grid">
    <div class="chart-card">
      <h3 class="chart-title">Submit Feedback</h3>
      <p v-if="success" class="success">{{ success }}</p>
      <label class="lbl">Category</label>
      <select v-model="category" class="full">
        <option>General</option>
        <option>Suggestion</option>
        <option>Bug Report</option>
        <option>Complaint</option>
        <option>Feature Request</option>
      </select>
      <label class="lbl" style="margin-top:.75rem">Your Message</label>
      <textarea v-model="message" class="full" rows="4" placeholder="Tell us what's on your mind..."></textarea>
      <button class="primary" style="margin-top:.75rem" @click="submit" :disabled="!message.trim()">Send Feedback</button>
    </div>

    <div class="chart-card">
      <h3 class="chart-title">Your Submissions</h3>
      <div v-if="!items.length" class="page-sub">No feedback submitted yet.</div>
      <div class="fb-item" v-for="f in items" :key="f.id">
        <div class="fb-top">
          <span class="fb-cat">{{ f.category }}</span>
          <span class="status" :class="f.status">{{ f.status }}</span>
        </div>
        <p>{{ f.message }}</p>
        <span class="qa-date">{{ f.created_at }}</span>
        <div v-if="f.admin_response" class="fb-response"><strong>Admin:</strong> {{ f.admin_response }}</div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '../api'

const category = ref('General')
const message = ref('')
const success = ref('')
const items = ref([])

async function load() {
  const { data } = await api.get('/api/feedback')
  items.value = data
}
async function submit() {
  const { data } = await api.post('/api/feedback', { category: category.value, message: message.value.trim() })
  success.value = data.message
  message.value = ''
  setTimeout(() => (success.value = ''), 4000)
  load()
}
onMounted(load)
</script>
