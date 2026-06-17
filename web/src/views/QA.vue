<template>
  <div class="page-head">
    <div>
      <h1 class="page-title">Expert Q&amp;A</h1>
      <p class="page-sub">Ask questions, get answers from verified experts</p>
    </div>
  </div>

  <div class="chart-card">
    <h3 class="chart-title">Ask a Question</h3>
    <textarea v-model="newQuestion" class="full" rows="3" placeholder="E.g. Is OGDC a good buy right now?"></textarea>
    <button class="primary" style="margin-top:.75rem" @click="ask" :disabled="!newQuestion.trim()">Submit Question</button>
  </div>

  <div v-if="!posts.length" class="page-sub">No questions yet. Be the first to ask!</div>

  <div class="qa-card" v-for="p in posts" :key="p.id">
    <div class="qa-top">
      <div>
        <span class="qa-author">{{ p.author }}</span>
        <span class="badge" :class="p.role">{{ p.role }}</span>
        <span class="qa-date">{{ p.date }}</span>
      </div>
      <button v-if="canModerate" class="del" @click="remove(p.id)">✕</button>
    </div>
    <p class="qa-q">{{ p.question }}</p>

    <div v-if="p.answer" class="qa-answer">
      <div class="answer-tag">Expert Answer</div>
      <p>{{ p.answer }}</p>
      <span class="qa-date">— {{ p.answered_by }}</span>
    </div>
    <div v-else class="qa-pending">
      <span class="pending">Awaiting expert response</span>
      <div v-if="canModerate" style="margin-top:.6rem">
        <textarea v-model="drafts[p.id]" class="full" rows="2" placeholder="Write your answer..."></textarea>
        <button class="primary" style="margin-top:.5rem" @click="answer(p.id)" :disabled="!(drafts[p.id] || '').trim()">Post Answer</button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import api from '../api'
import { useAuth } from '../store/auth'

const auth = useAuth()
const posts = ref([])
const newQuestion = ref('')
const drafts = reactive({})
const canModerate = computed(() => ['expert', 'admin'].includes(auth.role))

async function load() {
  const { data } = await api.get('/api/qa')
  posts.value = data.slice().reverse()
}
async function ask() {
  await api.post('/api/qa', { question: newQuestion.value.trim() })
  newQuestion.value = ''
  load()
}
async function answer(id) {
  await api.post(`/api/qa/${id}/answer`, { answer: drafts[id].trim() })
  drafts[id] = ''
  load()
}
async function remove(id) {
  if (!confirm('Delete this question?')) return
  await api.delete(`/api/qa/${id}`)
  load()
}
onMounted(load)
</script>
