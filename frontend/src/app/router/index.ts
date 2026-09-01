import { createRouter, createWebHistory } from 'vue-router'
import Home from '../layouts/admin/HomeLayout.vue'
import Dashboard from '../layouts/admin/DashboardLayout.vue'
import ProductPage from '../layouts/admin/ProductLayout.vue'
import ClassifyPage from '../layouts/admin/ClassifyLayout.vue'
import OrderPage from '../layouts/admin/OrderLayout.vue'
import UsersPage from '../layouts/admin/UsersLayout.vue'
import AdminLogin from '../layouts/admin/AdminLoginLayout.vue'
import CustomerStore from '../layouts/store/CustomerStoreLayout.vue'
import Checkout from '../layouts/store/CheckoutLayout.vue'
import CustomerLogin from '../layouts/store/CustomerLoginLayout.vue'
import CustomerRegister from '../layouts/store/CustomerRegisterLayout.vue'
import { useAuthStore } from '@/stores/auth'
import { pinia } from '@/stores/pinia'
import { createAuthGuard } from './authGuard'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      redirect: '/store',
    },
    {
      path: '/admin/login',
      name: 'admin-login',
      component: AdminLogin,
      meta: { guestOnly: true, loginRole: 'ADMIN' },
    },
    {
      path: '/login',
      alias: '/store/login',
      name: 'customer-login',
      component: CustomerLogin,
      meta: { guestOnly: true, loginRole: 'CUSTOMER' },
    },
    {
      path: '/register',
      alias: '/store/register',
      name: 'customer-register',
      component: CustomerRegister,
      meta: { guestOnly: true, loginRole: 'CUSTOMER' },
    },
    {
      path: '/store',
      name: 'store',
      component: CustomerStore,
      meta: { requiresAuth: true, role: 'CUSTOMER' },
    },
    {
      path: '/checkout',
      name: 'checkout',
      component: Checkout,
      meta: { requiresAuth: true, role: 'CUSTOMER' },
    },
    {
      path: '/home',
      name: 'home',
      component: Home,
      meta: { requiresAuth: true, role: 'ADMIN' },
    },
    {
      path: '/dashboard',
      name: 'dashboard',
      component: Dashboard,
      meta: { requiresAuth: true, role: 'ADMIN' },
    },
    {
      path: '/productPage',
      name: 'productPage',
      component: ProductPage,
      meta: { requiresAuth: true, role: 'ADMIN' },
    },
    {
      path: '/classifyPage',
      name: 'classifyPage',
      component: ClassifyPage,
      meta: { requiresAuth: true, role: 'ADMIN' },
    },
    {
      path: '/orderPage',
      name: 'orderPage',
      component: OrderPage,
      meta: { requiresAuth: true, role: 'ADMIN' },
    },
    {
      path: '/usersPage',
      name: 'usersPage',
      component: UsersPage,
      meta: { requiresAuth: true, role: 'ADMIN' },
    },
  ],
})

router.beforeEach(createAuthGuard(useAuthStore(pinia)))

export default router
