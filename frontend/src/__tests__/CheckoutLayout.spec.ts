import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import CheckoutLayout from '@/app/layouts/store/CheckoutLayout.vue'

const createOrderMock = vi.hoisted(() =>
  vi.fn<typeof import('@/services/orderService').createOrder>(),
)

vi.mock('@/services/orderService', () => ({
  createOrder: createOrderMock,
}))

const cartItem = {
  product: {
    id: 1,
    productCode: 'SP001',
    name: 'Mì Hảo Hảo',
    price: 5000,
    stockQuantity: 12,
    createdAt: '2026-08-31T00:00:00Z',
  },
  quantity: 2,
}

function mountCheckout() {
  return mount(CheckoutLayout, {
    global: {
      stubs: {
        RouterLink: {
          template: '<a><slot /></a>',
        },
      },
    },
  })
}

describe('CheckoutLayout', () => {
  beforeEach(() => {
    window.localStorage.clear()
    window.localStorage.setItem(
      'cn25-customer-cart',
      JSON.stringify([cartItem]),
    )
    Object.defineProperty(window, 'scrollTo', {
      configurable: true,
      value: vi.fn<() => void>(),
    })
    createOrderMock.mockReset()
    createOrderMock.mockResolvedValue({
      id: 15,
      orderCode: 'DH000015',
      totalAmount: 10000,
      paymentMethod: 'COD',
      status: 'pending',
      createdAt: '2026-08-31T12:00:00Z',
    })
  })

  it('validates recipient information before creating an order', async () => {
    const wrapper = mountCheckout()

    await wrapper.get('form').trigger('submit')

    expect(createOrderMock).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('Vui lòng nhập tên người nhận')
    expect(wrapper.text()).toContain('Nhập 10 số bắt đầu bằng 0')
    expect(wrapper.text()).toContain('Vui lòng nhập địa chỉ giao hàng đầy đủ')
  })

  it('creates a COD order, clears the cart and shows the order code', async () => {
    const wrapper = mountCheckout()

    await wrapper.get('#customer-name').setValue('Nguyễn Văn An')
    await wrapper.get('#customer-phone').setValue('0912 345 678')
    await wrapper.get('#delivery-address').setValue('12 Lê Lợi, Quận 1, TP.HCM')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(createOrderMock).toHaveBeenCalledWith({
      customerName: 'Nguyễn Văn An',
      phone: '0912345678',
      deliveryAddress: '12 Lê Lợi, Quận 1, TP.HCM',
      paymentMethod: 'COD',
      items: [{ productId: 1, quantity: 2 }],
    })
    expect(wrapper.text()).toContain('Đặt hàng thành công')
    expect(wrapper.text()).toContain('DH000015')
    expect(window.localStorage.getItem('cn25-customer-cart')).toBe('[]')
  })
})
