import { createRouter, createWebHashHistory } from 'vue-router'
import { useAuth } from '../store/auth'
import Login from '../views/Login.vue'
import Register from '../views/Register.vue'
import Dashboard from '../views/Dashboard.vue'
import Predictions from '../views/Predictions.vue'
import Watchlist from '../views/Watchlist.vue'
import QA from '../views/QA.vue'
import Feedback from '../views/Feedback.vue'
import Admin from '../views/Admin.vue'

const routes = [
  { path: '/login', component: Login, meta: { public: true } },
  { path: '/register', component: Register, meta: { public: true } },
  { path: '/', redirect: '/dashboard' },
  { path: '/dashboard', component: Dashboard },
  { path: '/predictions', component: Predictions },
  { path: '/watchlist', component: Watchlist },
  { path: '/qa', component: QA },
  { path: '/feedback', component: Feedback },
  { path: '/admin', component: Admin, meta: { admin: true } },
]

const router = createRouter({ history: createWebHashHistory(), routes })

// Route guard: protect private pages, gate admin pages, and keep logged-in
// users out of login/register.
router.beforeEach((to) => {
  const auth = useAuth()
  if (!to.meta.public && !auth.isAuthed) return '/login'
  if (to.meta.public && auth.isAuthed) return '/dashboard'
  if (to.meta.admin && auth.role !== 'admin') return '/dashboard'
})

export default router
