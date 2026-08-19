import { createRouter, createWebHistory } from 'vue-router'
import AdminPage from '../layouts/AdminLayout.vue'
import ProductPage from '../layouts/ProductLayout.vue'

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
    },
    {
      path: '/productPage',
      name: 'productPage',
      component: ProductPage
    }
  ],
})

export default router
