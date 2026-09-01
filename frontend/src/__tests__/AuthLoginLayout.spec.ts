import { createPinia } from 'pinia'
import { defineComponent } from 'vue'
import { createMemoryHistory, createRouter } from 'vue-router'
import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import AdminLoginLayout from '@/app/layouts/admin/AdminLoginLayout.vue'
import CustomerLoginLayout from '@/app/layouts/store/CustomerLoginLayout.vue'

const authApi = vi.hoisted(() => ({
  getCurrentUser: vi.fn<typeof import('@/services/authService').getCurrentUser>(),
  loginAdmin: vi.fn<typeof import('@/services/authService').loginAdmin>(),
  loginCustomer: vi.fn<typeof import('@/services/authService').loginCustomer>(),
  registerCustomer: vi.fn<typeof import('@/services/authService').registerCustomer>(),
  logoutSession: vi.fn<typeof import('@/services/authService').logoutSession>(),
}))

vi.mock('@/services/authService', () => authApi)

const PageStub = defineComponent({ template: '<div>Dashboard</div>' })

function createTestRouter() {
  return createRouter({
    history: createMemoryHistory(),
    routes: [
      {
        path: '/admin/login',
        component: AdminLoginLayout,
      },
      {
        path: '/login',
        component: CustomerLoginLayout,
      },
      {
        path: '/register',
        component: PageStub,
      },
      {
        path: '/dashboard',
        component: PageStub,
        meta: { requiresAuth: true, role: 'ADMIN' },
      },
      {
        path: '/store',
        component: PageStub,
        meta: { requiresAuth: true, role: 'CUSTOMER' },
      },
    ],
  })
}

describe('separate login layouts', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    authApi.getCurrentUser.mockResolvedValue(null)
    authApi.logoutSession.mockResolvedValue(undefined)
  })

  it('submits the admin portal and follows its safe redirect', async () => {
    authApi.loginAdmin.mockResolvedValue({
      id: 1,
      email: 'admin@cn25.vn',
      displayName: 'Quản trị viên',
      role: 'ADMIN',
    })
    const router = createTestRouter()
    await router.push('/admin/login?redirect=/dashboard')
    await router.isReady()
    const wrapper = mount(AdminLoginLayout, {
      global: { plugins: [createPinia(), router] },
    })

    expect(wrapper.find('.register-link').exists()).toBe(false)

    await wrapper.get('#admin-email').setValue('admin@cn25.vn')
    await wrapper.get('#admin-password').setValue('secret123')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(authApi.loginAdmin).toHaveBeenCalledWith({
      email: 'admin@cn25.vn',
      password: 'secret123',
    })
    expect(router.currentRoute.value.fullPath).toBe('/dashboard')
  })

  it('renders a distinct customer portal and validates empty fields', async () => {
    const router = createTestRouter()
    await router.push('/login?redirect=/store')
    await router.isReady()
    const wrapper = mount(CustomerLoginLayout, {
      global: { plugins: [createPinia(), router] },
    })

    expect(wrapper.text()).toContain('Chào mừng bạn trở lại')
    expect(wrapper.text()).toContain('Đăng nhập quản trị')
    expect(wrapper.get('.register-link').attributes('href'))
      .toBe('/register?redirect=/store')

    await wrapper.get('form').trigger('submit')

    expect(wrapper.text()).toContain('Vui lòng nhập email')
    expect(wrapper.text()).toContain('Vui lòng nhập mật khẩu')
    expect(authApi.loginCustomer).not.toHaveBeenCalled()
  })
})
