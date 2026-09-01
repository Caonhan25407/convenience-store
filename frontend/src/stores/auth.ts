import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import {
  getCurrentUser,
  loginAdmin as requestAdminLogin,
  loginCustomer as requestCustomerLogin,
  logoutSession,
  registerCustomer as requestCustomerRegistration,
} from '@/services/authService'
import { clearStoredCart } from '@/composables/useCart'
import type {
  AuthUser,
  CustomerRegistration,
  LoginCredentials,
  UserRole,
} from '@/types/auth'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<AuthUser | null>(null)
  const initialized = ref(false)
  let initializationPromise: Promise<void> | null = null

  const isAuthenticated = computed(() => user.value !== null)
  const isAdmin = computed(() => user.value?.role === 'ADMIN')
  const isCustomer = computed(() => user.value?.role === 'CUSTOMER')

  async function initialize() {
    if (initialized.value) {
      return
    }

    if (!initializationPromise) {
      initializationPromise = (async () => {
        try {
          const currentUser = await getCurrentUser()
          user.value = currentUser

          if (!currentUser) {
            clearStoredCart()
          }
        } catch {
          user.value = null
        } finally {
          initialized.value = true
          initializationPromise = null
        }
      })()
    }

    await initializationPromise
  }

  async function login(
    role: UserRole,
    credentials: LoginCredentials,
  ): Promise<AuthUser> {
    const nextUser = role === 'ADMIN'
      ? await requestAdminLogin(credentials)
      : await requestCustomerLogin(credentials)

    if (nextUser.role !== role) {
      try {
        await logoutSession()
      } finally {
        user.value = null
        initialized.value = true
      }

      throw new Error('Tài khoản không có quyền truy cập khu vực này')
    }

    user.value = nextUser
    initialized.value = true
    return nextUser
  }

  function loginAdmin(credentials: LoginCredentials) {
    return login('ADMIN', credentials)
  }

  function loginCustomer(credentials: LoginCredentials) {
    return login('CUSTOMER', credentials)
  }

  async function registerCustomer(registration: CustomerRegistration) {
    const nextUser = await requestCustomerRegistration(registration)

    if (nextUser.role !== 'CUSTOMER') {
      await logoutSession()
      user.value = null
      initialized.value = true
      throw new Error('Tài khoản không có quyền truy cập cửa hàng')
    }

    user.value = nextUser
    initialized.value = true
    return nextUser
  }

  async function logout() {
    const previousRole = user.value?.role

    await logoutSession()
    user.value = null
    initialized.value = true

    if (previousRole === 'CUSTOMER') {
      clearStoredCart()
    }
  }

  return {
    user,
    initialized,
    isAuthenticated,
    isAdmin,
    isCustomer,
    initialize,
    loginAdmin,
    loginCustomer,
    registerCustomer,
    logout,
  }
})
