import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import OrderLayout from '@/app/layouts/admin/OrderLayout.vue'

const getOrdersMock = vi.hoisted(() => vi.fn<typeof import('@/services/orderService').getOrders>())
const confirmOrderMock = vi.hoisted(() =>
  vi.fn<typeof import('@/services/orderService').confirmOrder>(),
)

vi.mock('@/services/orderService', () => ({
  confirmOrder: confirmOrderMock,
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
    confirmOrderMock.mockReset()
    confirmOrderMock.mockResolvedValue({
      id: 15,
      orderCode: 'DH-20260831-ABC',
      status: 'CONFIRMED',
      message: 'Đã xác nhận đơn hàng DH-20260831-ABC.',
    })
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
    expect(wrapper.get('.confirm-order-button').text()).toBe('Xác nhận')
    expect(wrapper.text()).not.toContain('Mì Hảo Hảo')

    await wrapper.get('.detail-toggle').trigger('click')

    expect(wrapper.text()).toContain('Mì Hảo Hảo')
    expect(wrapper.text()).toContain('10.000 ₫')
  })

  it('confirms a pending order and reloads the current list', async () => {
    getOrdersMock.mockResolvedValueOnce(orderPage).mockResolvedValueOnce({
      ...orderPage,
      items: orderPage.items.map((order) => ({
        ...order,
        status: 'CONFIRMED',
      })),
    })
    const wrapper = mountOrders()
    await flushPromises()

    await wrapper.get('.confirm-order-button').trigger('click')
    await flushPromises()

    expect(confirmOrderMock).toHaveBeenCalledWith(15)
    expect(getOrdersMock).toHaveBeenCalledTimes(2)
    expect(wrapper.text()).toContain('Đã xác nhận đơn hàng DH-20260831-ABC.')
    expect(wrapper.text()).toContain('Đã xác nhận')
    expect(wrapper.find('.confirm-order-button').exists()).toBe(false)
  })

  it('shows the API error and keeps the confirm action available', async () => {
    confirmOrderMock.mockRejectedValueOnce(new Error('Đơn hàng đã được xác nhận trước đó.'))
    const wrapper = mountOrders()
    await flushPromises()

    await wrapper.get('.confirm-order-button').trigger('click')
    await flushPromises()

    expect(wrapper.get('.action-error').text()).toBe('Đơn hàng đã được xác nhận trước đó.')
    expect(wrapper.get('.confirm-order-button').attributes()).not.toHaveProperty('disabled')
  })

  it('returns to the last available page when confirming shrinks a filtered result', async () => {
    const firstPage = {
      ...orderPage,
      totalCount: 16,
      totalPages: 2,
    }
    const secondPage = {
      ...orderPage,
      page: 2,
      totalCount: 16,
      totalPages: 2,
    }
    const collapsedSecondPage = {
      ...secondPage,
      items: [],
      totalCount: 15,
      totalPages: 1,
    }
    const remainingFirstPage = {
      ...orderPage,
      items: orderPage.items.map((order) => ({
        ...order,
        id: 14,
        orderCode: 'DH-20260831-XYZ',
      })),
      totalCount: 15,
      totalPages: 1,
    }

    getOrdersMock
      .mockResolvedValueOnce(firstPage)
      .mockResolvedValueOnce(secondPage)
      .mockResolvedValueOnce(collapsedSecondPage)
      .mockResolvedValueOnce(remainingFirstPage)

    const wrapper = mountOrders()
    await flushPromises()

    await wrapper.get('.pagination button:last-child').trigger('click')
    await flushPromises()
    await wrapper.get('.confirm-order-button').trigger('click')
    await flushPromises()

    expect(getOrdersMock.mock.calls.map(([query]) => query.page)).toEqual([1, 2, 2, 1])
    expect(wrapper.text()).toContain('DH-20260831-XYZ')
    expect(wrapper.text()).not.toContain('Trang 2 / 1')
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
