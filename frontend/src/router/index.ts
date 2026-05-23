import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import DashboardView from '../views/DashboardView.vue'
import AllPhotosView from '../views/AllPhotosView.vue'
import AlbumPhotosView from '../views/AlbumPhotosView.vue'
import LoginView from '../views/LoginView.vue'
import RegisterView from '../views/RegisterView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', redirect: '/app' },
    { path: '/login', component: LoginView },
    { path: '/register', component: RegisterView },
    { path: '/app', component: DashboardView, meta: { requiresAuth: true } },
    { path: '/photos', component: AllPhotosView, meta: { requiresAuth: true } },
    { path: '/albums/:albumId/photos', component: AlbumPhotosView, meta: { requiresAuth: true }, props: true },
  ],
})

router.beforeEach((to) => {
  const authStore = useAuthStore()

  if (!authStore.initialized) {
    authStore.initialize()
  }

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    return '/login'
  }

  if ((to.path === '/login' || to.path === '/register') && authStore.isAuthenticated) {
    return '/app'
  }

  return true
})

export default router