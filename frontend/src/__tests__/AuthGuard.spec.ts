import { defineComponent } from 'vue'
import { createMemoryHistory, createRouter } from 'vue-router'
import { describe, expect, it, vi } from 'vitest'
import {
  createAuthGuard,
  resolveSafeRedirect,
} from '@/app/router/authGuard'
import type { AuthUser } from '@/types/auth'

const PageStub = defineComponent({ template: '<div />' })

function guardStore(user: AuthUser | null) {
  return {
    user,
    initialize: vi.fn<() => Promise<void>>().mockResolvedValue(undefined),
  }
}

const adminUser: AuthUser = {
  id: 1,
  email: 'admin@cn25.vn',
  displayName: 'Quản trị viên',
  role: 'ADMIN',
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

    await expect(createAuthGuard(adminStore)({
      fullPath: '/orderPage?page=2',
      meta: { requiresAuth: true, role: 'ADMIN' },
    })).resolves.toEqual({
      name: 'admin-login',
      query: { redirect: '/orderPage?page=2' },
    })

    await expect(createAuthGuard(customerStore)({
      fullPath: '/checkout',
      meta: { requiresAuth: true, role: 'CUSTOMER' },
    })).resolves.toEqual({
      name: 'customer-login',
      query: { redirect: '/checkout' },
    })

    expect(adminStore.initialize).toHaveBeenCalledOnce()
    expect(customerStore.initialize).toHaveBeenCalledOnce()
  })

  it('keeps roles separated and moves logged-in users away from login pages', async () => {
    await expect(createAuthGuard(guardStore(customerUser))({
      fullPath: '/dashboard',
      meta: { requiresAuth: true, role: 'ADMIN' },
    })).resolves.toEqual({ name: 'store' })

    await expect(createAuthGuard(guardStore(adminUser))({
      fullPath: '/login',
      meta: { guestOnly: true, loginRole: 'CUSTOMER' },
    })).resolves.toEqual({ name: 'dashboard' })
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
    expect(resolveSafeRedirect('/checkout?step=delivery', 'CUSTOMER', router))
      .toBe('/checkout?step=delivery')
    expect(resolveSafeRedirect('/dashboard', 'CUSTOMER', router)).toBe('/store')
    expect(resolveSafeRedirect('//example.com/steal', 'ADMIN', router)).toBe('/dashboard')
    expect(resolveSafeRedirect('https://example.com', 'ADMIN', router)).toBe('/dashboard')
  })
})
