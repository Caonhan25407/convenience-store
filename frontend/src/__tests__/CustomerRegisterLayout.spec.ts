import { createPinia } from 'pinia'
import { defineComponent } from 'vue'
import { createMemoryHistory, createRouter } from 'vue-router'
import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import CustomerRegisterLayout from '@/app/layouts/store/CustomerRegisterLayout.vue'

const authApi = vi.hoisted(() => ({
  getCurrentUser: vi.fn<typeof import('@/services/authService').getCurrentUser>(),
  loginAdmin: vi.fn<typeof import('@/services/authService').loginAdmin>(),
  loginCustomer: vi.fn<typeof import('@/services/authService').loginCustomer>(),
  registerCustomer: vi.fn<typeof import('@/services/authService').registerCustomer>(),
  logoutSession: vi.fn<typeof import('@/services/authService').logoutSession>(),
}))

vi.mock('@/services/authService', () => authApi)

const PageStub = defineComponent({ template: '<div />' })

function createTestRouter() {
  return createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/register', component: CustomerRegisterLayout },
      { path: '/login', component: PageStub },
      {
        path: '/store',
        component: PageStub,
        meta: { requiresAuth: true, role: 'CUSTOMER' },
      },
      {
        path: '/checkout',
        component: PageStub,
        meta: { requiresAuth: true, role: 'CUSTOMER' },
      },
    ],
  })
}

describe('CustomerRegisterLayout', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    authApi.getCurrentUser.mockResolvedValue(null)
    authApi.logoutSession.mockResolvedValue(undefined)
  })

  it('validates required fields, password length and confirmation', async () => {
    const router = createTestRouter()
    await router.push('/register')
    await router.isReady()
    const wrapper = mount(CustomerRegisterLayout, {
      global: { plugins: [createPinia(), router] },
    })

    await wrapper.get('form').trigger('submit')
    expect(wrapper.text()).toContain('Vui lòng nhập tên hiển thị')
    expect(wrapper.text()).toContain('Vui lòng nhập email')
    expect(wrapper.text()).toContain('Vui lòng nhập mật khẩu')
    expect(wrapper.text()).toContain('Vui lòng xác nhận mật khẩu')

    await wrapper.get('#register-name').setValue('A')
    await wrapper.get('form').trigger('submit')

    expect(wrapper.text()).toContain('Tên hiển thị phải có ít nhất 2 ký tự')
    expect(authApi.registerCustomer).not.toHaveBeenCalled()

    await wrapper.get('#register-name').setValue('Nguyễn Văn An')
    await wrapper.get('#register-email').setValue('customer@cn25.vn')
    await wrapper.get('#register-password').setValue('short')
    await wrapper.get('#register-confirm-password').setValue('different')
    await wrapper.get('form').trigger('submit')

    expect(wrapper.text()).toContain('Mật khẩu phải có ít nhất 8 ký tự')
    expect(wrapper.text()).toContain('Mật khẩu xác nhận không khớp')
    expect(authApi.registerCustomer).not.toHaveBeenCalled()
  })

  it('registers the customer and follows a safe customer redirect', async () => {
    authApi.registerCustomer.mockResolvedValue({
      id: 2,
      email: 'customer@cn25.vn',
      displayName: 'Nguyễn Văn An',
      role: 'CUSTOMER',
    })
    const router = createTestRouter()
    await router.push('/register?redirect=/checkout')
    await router.isReady()
    const wrapper = mount(CustomerRegisterLayout, {
      global: { plugins: [createPinia(), router] },
    })

    await wrapper.get('#register-name').setValue('Nguyễn Văn An')
    await wrapper.get('#register-email').setValue('customer@cn25.vn')
    await wrapper.get('#register-password').setValue('secret123')
    await wrapper.get('#register-confirm-password').setValue('secret123')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(authApi.registerCustomer).toHaveBeenCalledWith({
      displayName: 'Nguyễn Văn An',
      email: 'customer@cn25.vn',
      password: 'secret123',
    })
    expect(router.currentRoute.value.fullPath).toBe('/checkout')
  })
})
