import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import UsersLayout from '@/app/layouts/admin/UsersLayout.vue'

const getUsersMock = vi.hoisted(() =>
  vi.fn<typeof import('@/services/userService').getUsers>(),
)

vi.mock('@/services/userService', () => ({
  getUsers: getUsersMock,
}))

const userPage = {
  items: [
    {
      id: 1,
      displayName: 'Quản trị CN25',
      email: 'admin@cn25.vn',
      phone: null,
      role: 'ADMIN' as const,
      isActive: true,
      createdAt: '2026-08-30T08:00:00Z',
      lastLoginAt: '2026-08-31T12:00:00Z',
    },
    {
      id: 2,
      displayName: 'Nguyễn Văn An',
      email: 'customer@cn25.vn',
      phone: '0912345678',
      role: 'CUSTOMER' as const,
      isActive: false,
      createdAt: '2026-08-31T08:00:00Z',
      lastLoginAt: null,
    },
  ],
  totalCount: 16,
  page: 1,
  pageSize: 15,
  totalPages: 2,
}

function mountUsers() {
  return mount(UsersLayout, {
    global: {
      stubs: {
        Navbar: true,
        Sidebar: true,
      },
    },
  })
}

describe('UsersLayout', () => {
  beforeEach(() => {
    getUsersMock.mockReset()
    getUsersMock.mockResolvedValue(userPage)
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('renders paginated admin and customer accounts', async () => {
    const wrapper = mountUsers()
    await flushPromises()

    expect(getUsersMock).toHaveBeenCalledWith({
      page: 1,
      pageSize: 15,
      search: '',
      role: 'all',
    })
    expect(wrapper.text()).toContain('16')
    expect(wrapper.text()).toContain('Quản trị CN25')
    expect(wrapper.text()).toContain('admin@cn25.vn')
    expect(wrapper.text()).toContain('Nguyễn Văn An')
    expect(wrapper.text()).toContain('0912345678')
    expect(wrapper.text()).toContain('Quản trị viên')
    expect(wrapper.text()).toContain('Khách hàng')
    expect(wrapper.text()).toContain('Hoạt động')
    expect(wrapper.text()).toContain('Ngừng hoạt động')
    expect(wrapper.text()).toContain('Chưa đăng nhập')
  })

  it('debounces search and applies the role filter', async () => {
    vi.useFakeTimers()
    const wrapper = mountUsers()
    await flushPromises()

    await wrapper.get('#user-search').setValue('  Nguyễn Văn An  ')
    await vi.advanceTimersByTimeAsync(350)
    await flushPromises()

    expect(getUsersMock).toHaveBeenLastCalledWith({
      page: 1,
      pageSize: 15,
      search: 'Nguyễn Văn An',
      role: 'all',
    })

    await wrapper.get('#user-role').setValue('CUSTOMER')
    await flushPromises()

    expect(getUsersMock).toHaveBeenLastCalledWith({
      page: 1,
      pageSize: 15,
      search: 'Nguyễn Văn An',
      role: 'CUSTOMER',
    })
  })
})
