import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import OrderLayout from '@/app/layouts/admin/OrderLayout.vue'

const getOrdersMock = vi.hoisted(() =>
  vi.fn<typeof import('@/services/orderService').getOrders>(),
)

vi.mock('@/services/orderService', () => ({
  getOrders: getOrdersMock,
}))

const orderPage = {
  items: [
    {
      id: 15,
      orderCode: 'DH-20260831-ABC',
      customerName: 'Nguyễn Văn An',
      phone: '0912345678',
      deliveryAddress: '12 Lê Lợi, Quận 1, TP.HCM',
      paymentMethod: 'COD',
      status: 'PENDING',
      totalAmount: 10000,
      itemCount: 1,
      totalQuantity: 2,
      createdAt: '2026-08-31T12:00:00Z',
      items: [
        {
          productId: 1,
          productCode: 'SP001',
          productName: 'Mì Hảo Hảo',
          unitPrice: 5000,
          quantity: 2,
          lineTotal: 10000,
        },
      ],
    },
  ],
  totalCount: 1,
  page: 1,
  pageSize: 15,
  totalPages: 1,
}

function mountOrders() {
  return mount(OrderLayout, {
    global: {
      stubs: {
        Navbar: true,
        Sidebar: true,
      },
    },
  })
}

describe('OrderLayout', () => {
  beforeEach(() => {
    getOrdersMock.mockReset()
    getOrdersMock.mockResolvedValue(orderPage)
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('renders saved orders and expands their product details', async () => {
    const wrapper = mountOrders()
    await flushPromises()

    expect(getOrdersMock).toHaveBeenCalledWith({
      page: 1,
      pageSize: 15,
      search: '',
      status: 'all',
    })
    expect(wrapper.text()).toContain('DH-20260831-ABC')
    expect(wrapper.text()).toContain('Nguyễn Văn An')
    expect(wrapper.text()).toContain('Chờ xác nhận')
    expect(wrapper.text()).not.toContain('Mì Hảo Hảo')

    await wrapper.get('.detail-toggle').trigger('click')

    expect(wrapper.text()).toContain('Mì Hảo Hảo')
    expect(wrapper.text()).toContain('10.000 ₫')
  })

  it('debounces search and applies the selected status filter', async () => {
    vi.useFakeTimers()

    const wrapper = mountOrders()
    await flushPromises()

    await wrapper.get('#order-search').setValue('Nguyễn Văn An')
    await vi.advanceTimersByTimeAsync(350)
    await flushPromises()

    expect(getOrdersMock).toHaveBeenLastCalledWith({
      page: 1,
      pageSize: 15,
      search: 'Nguyễn Văn An',
      status: 'all',
    })

    await wrapper.get('#order-status').setValue('COMPLETED')
    await flushPromises()

    expect(getOrdersMock).toHaveBeenLastCalledWith({
      page: 1,
      pageSize: 15,
      search: 'Nguyễn Văn An',
      status: 'COMPLETED',
    })
  })
})
