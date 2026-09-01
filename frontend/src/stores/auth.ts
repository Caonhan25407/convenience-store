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
import type { AuthUser, CustomerRegistration, LoginCredentials, UserRole } from '@/types/auth'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<AuthUser | null>(null)
  const initialized = ref(false)
  const initializedRole = ref<UserRole | null>(null)
  let initializationPromise: Promise<void> | null = null

  const isAuthenticated = computed(() => user.value !== null)
  const isAdmin = computed(() => user.value?.role === 'ADMIN')
  const isCustomer = computed(() => user.value?.role === 'CUSTOMER')

  async function initialize(role: UserRole) {
    if (initializedRole.value === role) {
      return
    }

    while (initializationPromise) {
      await initializationPromise

      if (initializedRole.value === role) {
        return
      }
    }

    const pendingInitialization = (async () => {
      try {
        const currentUser = await getCurrentUser(role)
        user.value = currentUser?.role === role ? currentUser : null

        if (role === 'CUSTOMER' && !user.value) {
          clearStoredCart()
        }
      } catch {
        user.value = null
      } finally {
        initialized.value = true
        initializedRole.value = role
      }
    })()

    initializationPromise = pendingInitialization

    try {
      await pendingInitialization
    } finally {
      if (initializationPromise === pendingInitialization) {
        initializationPromise = null
      }
    }
  }

  async function login(role: UserRole, credentials: LoginCredentials): Promise<AuthUser> {
    const nextUser =
      role === 'ADMIN'
        ? await requestAdminLogin(credentials)
        : await requestCustomerLogin(credentials)

    if (nextUser.role !== role) {
      try {
        await logoutSession(role)
      } finally {
        user.value = null
        initialized.value = true
        initializedRole.value = role
      }

      throw new Error('Tài khoản không có quyền truy cập khu vực này')
    }

    user.value = nextUser
    initialized.value = true
    initializedRole.value = role
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
      await logoutSession('CUSTOMER')
      user.value = null
      initialized.value = true
      initializedRole.value = 'CUSTOMER'
      throw new Error('Tài khoản không có quyền truy cập cửa hàng')
    }

    user.value = nextUser
    initialized.value = true
    initializedRole.value = 'CUSTOMER'
    return nextUser
  }

  async function logout() {
    const previousRole = user.value?.role

    if (!previousRole) {
      return
    }

    await logoutSession(previousRole)
    user.value = null
    initialized.value = true
    initializedRole.value = previousRole

    if (previousRole === 'CUSTOMER') {
      clearStoredCart()
    }
  }

  return {
    user,
    initialized,
    initializedRole,
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
