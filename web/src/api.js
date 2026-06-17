import axios from 'axios'

// All requests go to the ASP.NET backend.
// - Local dev: VITE_API_URL is unset -> talk to localhost:8002.
// - Hosted: set VITE_API_URL to the deployed API URL.
const apiBase = import.meta.env.VITE_API_URL ?? 'http://127.0.0.1:8002'
const api = axios.create({ baseURL: apiBase })

// Attach the JWT (if we have one) to every request.
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// If the token is rejected, clear the session and send the user to login.
api.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err.response && err.response.status === 401) {
      localStorage.removeItem('token')
      localStorage.removeItem('user')
      if (!location.hash.includes('/login')) location.hash = '#/login'
    }
    return Promise.reject(err)
  }
)

export default api
