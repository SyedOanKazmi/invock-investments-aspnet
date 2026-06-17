<template>
  <div class="auth">
    <div class="auth-card">
      <h1 class="brand">Invock <span>Investments</span></h1>
      <p class="sub">Sign in to your account</p>
      <p v-if="error" class="error">{{ error }}</p>
      <input v-model="email" placeholder="Email" />
      <input v-model="password" type="password" placeholder="Password" @keyup.enter="submit" />
      <button class="primary" @click="submit" :disabled="loading">
        {{ loading ? 'Signing in...' : 'Sign In' }}
      </button>
      <p class="switch">No account? <RouterLink to="/register">Register</RouterLink></p>
      <p class="hint">Demo: user@psx.com / user123</p>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuth } from '../store/auth'

const email = ref('user@psx.com')
const password = ref('user123')
const error = ref('')
const loading = ref(false)
const auth = useAuth()
const router = useRouter()

async function submit() {
  loading.value = true
  error.value = ''
  try {
    await auth.login(email.value, password.value)
    router.push('/dashboard')
  } catch (e) {
    error.value = e.response?.data?.detail || 'Login failed'
  } finally {
    loading.value = false
  }
}
</script>
