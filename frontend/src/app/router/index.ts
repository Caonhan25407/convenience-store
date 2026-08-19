import { createRouter, createWebHistory } from 'vue-router'
import AdminPage from '../layouts/AdminLayout.vue'


const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      redirect: '/adminPage'
    },
    {
      path: '/adminPage',
      name: 'adminPage',
      component: AdminPage
    }
  ],
})

export default router
