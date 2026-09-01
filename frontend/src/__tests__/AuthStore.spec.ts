import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useAuthStore } from '@/stores/auth'

const authApi = vi.hoisted(() => ({
  getCurrentUser: vi.fn<typeof import('@/services/authService').getCurrentUser>(),
  loginAdmin: vi.fn<typeof import('@/services/authService').loginAdmin>(),
  loginCustomer: vi.fn<typeof import('@/services/authService').loginCustomer>(),
  registerCustomer: vi.fn<typeof import('@/services/authService').registerCustomer>(),
  logoutSession: vi.fn<typeof import('@/services/authService').logoutSession>(),
}))

vi.mock('@/services/authService', () => authApi)

const customer = {
  id: 2,
  email: 'customer@cn25.vn',
  displayName: 'Nguyễn Văn An',
  role: 'CUSTOMER' as const,
}

const admin = {
  id: 1,
  email: 'admin@cn25.vn',
  displayName: 'Quản trị viên',
  role: 'ADMIN' as const,
}

describe('auth store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    window.localStorage.clear()
    vi.clearAllMocks()
    authApi.getCurrentUser.mockResolvedValue(null)
    authApi.logoutSession.mockResolvedValue(undefined)
  })

  it('restores the HttpOnly-cookie session only once', async () => {
    authApi.getCurrentUser.mockResolvedValue(customer)
    const auth = useAuthStore()

    await Promise.all([auth.initialize('CUSTOMER'), auth.initialize('CUSTOMER')])
    await auth.initialize('CUSTOMER')

    expect(authApi.getCurrentUser).toHaveBeenCalledExactlyOnceWith('CUSTOMER')
    expect(auth.user).toEqual(customer)
    expect(auth.isCustomer).toBe(true)
  })

  it('uses the customer portal and clears its local cart on logout', async () => {
    authApi.loginCustomer.mockResolvedValue(customer)
    window.localStorage.setItem('cn25-customer-cart', '[{"quantity":1}]')
    const auth = useAuthStore()

    await auth.loginCustomer({
      email: ' customer@cn25.vn ',
      password: 'secret123',
    })
    await auth.logout()

    expect(authApi.loginCustomer).toHaveBeenCalledWith({
      email: ' customer@cn25.vn ',
      password: 'secret123',
    })
    expect(authApi.logoutSession).toHaveBeenCalledWith('CUSTOMER')
    expect(auth.user).toBeNull()
    expect(window.localStorage.getItem('cn25-customer-cart')).toBeNull()
  })

  it('stores the customer returned by registration', async () => {
    authApi.registerCustomer.mockResolvedValue(customer)
    const auth = useAuthStore()

    await auth.registerCustomer({
      displayName: 'Nguyễn Văn An',
      email: 'customer@cn25.vn',
      password: 'secret123',
    })

    expect(authApi.registerCustomer).toHaveBeenCalledWith({
      displayName: 'Nguyễn Văn An',
      email: 'customer@cn25.vn',
      password: 'secret123',
    })
    expect(auth.user).toEqual(customer)
    expect(auth.isCustomer).toBe(true)
  })

  it('keeps the current session when the logout request fails', async () => {
    authApi.loginCustomer.mockResolvedValue(customer)
    authApi.logoutSession.mockRejectedValue(new Error('Máº¡ng khÃ´ng kháº£ dá»¥ng'))
    window.localStorage.setItem('cn25-customer-cart', '[{"quantity":1}]')
    const auth = useAuthStore()

    await auth.loginCustomer({
      email: 'customer@cn25.vn',
      password: 'secret123',
    })

    await expect(auth.logout()).rejects.toThrow('Máº¡ng khÃ´ng kháº£ dá»¥ng')
    expect(auth.user).toEqual(customer)
    expect(window.localStorage.getItem('cn25-customer-cart')).not.toBeNull()
  })

  it('loads each portal independently without clearing the customer cart for admin', async () => {
    authApi.getCurrentUser.mockImplementation(async (role) => (role === 'ADMIN' ? admin : customer))
    window.localStorage.setItem('cn25-customer-cart', '[{"quantity":1}]')
    const auth = useAuthStore()

    await auth.initialize('ADMIN')

    expect(auth.user).toEqual(admin)
    expect(window.localStorage.getItem('cn25-customer-cart')).not.toBeNull()

    await auth.initialize('CUSTOMER')

    expect(auth.user).toEqual(customer)
    expect(authApi.getCurrentUser).toHaveBeenNthCalledWith(1, 'ADMIN')
    expect(authApi.getCurrentUser).toHaveBeenNthCalledWith(2, 'CUSTOMER')
  })
})
