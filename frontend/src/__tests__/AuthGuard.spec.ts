import { defineComponent } from 'vue'
import { createMemoryHistory, createRouter } from 'vue-router'
import { describe, expect, it, vi } from 'vitest'
import { createAuthGuard, resolveSafeRedirect } from '@/app/router/authGuard'
import type { AuthUser } from '@/types/auth'

const PageStub = defineComponent({ template: '<div />' })

function guardStore(user: AuthUser | null) {
  return {
    user,
    initialize: vi.fn<() => Promise<void>>().mockResolvedValue(undefined),
  }
}

const customerUser: AuthUser = {
  id: 2,
  email: 'customer@cn25.vn',
  displayName: 'Khách hàng',
  role: 'CUSTOMER',
}

describe('auth route guard', () => {
  it('sends guests to the matching portal login and keeps the target', async () => {
    const adminStore = guardStore(null)
    const customerStore = guardStore(null)

    await expect(
      createAuthGuard(adminStore)({
        fullPath: '/orderPage?page=2',
        meta: { requiresAuth: true, role: 'ADMIN' },
      }),
    ).resolves.toEqual({
      name: 'admin-login',
      query: { redirect: '/orderPage?page=2' },
    })

    await expect(
      createAuthGuard(customerStore)({
        fullPath: '/checkout',
        meta: { requiresAuth: true, role: 'CUSTOMER' },
      }),
    ).resolves.toEqual({
      name: 'customer-login',
      query: { redirect: '/checkout' },
    })

    expect(adminStore.initialize).toHaveBeenCalledWith('ADMIN')
    expect(customerStore.initialize).toHaveBeenCalledWith('CUSTOMER')
  })

  it('checks only the target portal session before redirecting', async () => {
    const adminRouteStore = guardStore(customerUser)
    const customerLoginStore = guardStore(null)
    const activeCustomerStore = guardStore(customerUser)

    await expect(
      createAuthGuard(adminRouteStore)({
        fullPath: '/dashboard',
        meta: { requiresAuth: true, role: 'ADMIN' },
      }),
    ).resolves.toEqual({
      name: 'admin-login',
      query: { redirect: '/dashboard' },
    })

    await expect(
      createAuthGuard(customerLoginStore)({
        fullPath: '/login',
        meta: { guestOnly: true, loginRole: 'CUSTOMER' },
      }),
    ).resolves.toBe(true)

    await expect(
      createAuthGuard(activeCustomerStore)({
        fullPath: '/login',
        meta: { guestOnly: true, loginRole: 'CUSTOMER' },
      }),
    ).resolves.toEqual({ name: 'store' })

    expect(adminRouteStore.initialize).toHaveBeenCalledWith('ADMIN')
    expect(customerLoginStore.initialize).toHaveBeenCalledWith('CUSTOMER')
    expect(activeCustomerStore.initialize).toHaveBeenCalledWith('CUSTOMER')
  })
})

describe('safe post-login redirect', () => {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      {
        path: '/dashboard',
        name: 'dashboard',
        component: PageStub,
        meta: { requiresAuth: true, role: 'ADMIN' },
      },
      {
        path: '/store',
        name: 'store',
        component: PageStub,
        meta: { requiresAuth: true, role: 'CUSTOMER' },
      },
      {
        path: '/checkout',
        name: 'checkout',
        component: PageStub,
        meta: { requiresAuth: true, role: 'CUSTOMER' },
      },
    ],
  })

  it('accepts only an internal protected route for the logged-in role', () => {
    expect(resolveSafeRedirect('/checkout?step=delivery', 'CUSTOMER', router)).toBe(
      '/checkout?step=delivery',
    )
    expect(resolveSafeRedirect('/dashboard', 'CUSTOMER', router)).toBe('/store')
    expect(resolveSafeRedirect('//example.com/steal', 'ADMIN', router)).toBe('/dashboard')
    expect(resolveSafeRedirect('https://example.com', 'ADMIN', router)).toBe('/dashboard')
  })
})
