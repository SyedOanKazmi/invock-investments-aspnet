<template>
  <div class="auth">
    <div class="auth-card">
      <h1 class="brand">Invock <span>Investments</span></h1>
      <p class="sub">Create your account</p>
      <p v-if="error" class="error">{{ error }}</p>
      <input v-model="name" placeholder="Full Name" />
      <input v-model="email" placeholder="Email" />
      <input v-model="password" type="password" placeholder="Password (min 6 chars)" @keyup.enter="submit" />
      <button class="primary" @click="submit" :disabled="loading">
        {{ loading ? 'Creating...' : 'Create Account' }}
      </button>
      <p class="switch">Already have an account? <RouterLink to="/login">Sign In</RouterLink></p>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuth } from '../store/auth'

const name = ref('')
const email = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)
const auth = useAuth()
const router = useRouter()

async function submit() {
  if (password.value.length < 6) {
    error.value = 'Password must be at least 6 characters'
    return
  }
  loading.value = true
  error.value = ''
  try {
    await auth.register(name.value, email.value, password.value)
    router.push('/dashboard')
  } catch (e) {
    error.value = e.response?.data?.detail || 'Registration failed'
  } finally {
    loading.value = false
  }
}
</script>
