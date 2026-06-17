import { defineStore } from 'pinia'
import api from '../api'

// Holds the logged-in user + JWT, persisted in localStorage.
export const useAuth = defineStore('auth', {
  state: () => ({
    user: JSON.parse(localStorage.getItem('user') || 'null'),
    token: localStorage.getItem('token') || null,
  }),
  getters: {
    isAuthed: (s) => !!s.token,
    role: (s) => (s.user ? s.user.role : null),
  },
  actions: {
    async login(email, password) {
      const { data } = await api.post('/api/auth/login', { email, password })
      this.setSession(data)
    },
    async register(name, email, password) {
      const { data } = await api.post('/api/auth/register', { name, email, password })
      this.setSession(data)
    },
    setSession(data) {
      this.token = data.access_token
      this.user = data.user
      localStorage.setItem('token', data.access_token)
      localStorage.setItem('user', JSON.stringify(data.user))
    },
    logout() {
      this.token = null
      this.user = null
      localStorage.removeItem('token')
      localStorage.removeItem('user')
    },
  },
})
