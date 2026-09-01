import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import CheckoutLayout from '@/app/layouts/store/CheckoutLayout.vue'

const createOrderMock = vi.hoisted(() =>
  vi.fn<typeof import('@/services/orderService').createOrder>(),
)
const getProvincesMock = vi.hoisted(() =>
  vi.fn<typeof import('@/services/locationService').getProvinces>(),
)
const getWardsByProvinceMock = vi.hoisted(() =>
  vi.fn<typeof import('@/services/locationService').getWardsByProvince>(),
)

vi.mock('@/services/orderService', () => ({
  createOrder: createOrderMock,
}))
vi.mock('@/services/locationService', () => ({
  getProvinces: getProvincesMock,
  getWardsByProvince: getWardsByProvinceMock,
}))

const provinceOptions = [
  { code: 79, name: 'Thành phố Hồ Chí Minh' },
  { code: 75, name: 'Tỉnh Đồng Nai' },
]

const wardOptions = [
  { code: 26734, name: 'Phường Bến Thành' },
  { code: 26737, name: 'Phường Sài Gòn' },
]

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
    window.localStorage.setItem('cn25-customer-cart', JSON.stringify([cartItem]))
    Object.defineProperty(window, 'scrollTo', {
      configurable: true,
      value: vi.fn<() => void>(),
    })
    createOrderMock.mockReset()
    getProvincesMock.mockReset()
    getWardsByProvinceMock.mockReset()
    getProvincesMock.mockResolvedValue(provinceOptions)
    getWardsByProvinceMock.mockResolvedValue(wardOptions)
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
    await flushPromises()

    await wrapper.get('form').trigger('submit')

    expect(createOrderMock).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('Vui lòng nhập tên người nhận')
    expect(wrapper.text()).toContain('Nhập 10 số bắt đầu bằng 0')
    expect(wrapper.text()).toContain('Vui lòng nhập địa chỉ')
    expect(wrapper.text()).toContain('Vui lòng chọn phường/xã/đặc khu')
    expect(wrapper.text()).toContain('Vui lòng chọn tỉnh/thành phố')
    expect(wrapper.get('#street-address').attributes('aria-invalid')).toBe('true')
    expect(wrapper.get('#ward-city').attributes('aria-describedby')).toBe('ward-city-error')
    expect(wrapper.get('#province').attributes('aria-describedby')).toBe('province-error')
  })

  it('creates a COD order, clears the cart and shows the order code', async () => {
    const wrapper = mountCheckout()
    await flushPromises()

    expect(wrapper.get('.summary-thumb').text()).toBe('2')

    await wrapper.get('#customer-name').setValue('Nguyễn Văn An')
    await wrapper.get('#customer-phone').setValue('0912 345 678')
    await wrapper.get('#street-address').setValue('  12   Lê Lợi  ')
    await wrapper.get('#province').setValue('79')
    await flushPromises()
    await wrapper.get('#ward-city').setValue('26734')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(getWardsByProvinceMock.mock.calls[0]?.[0]).toBe(79)
    expect(createOrderMock).toHaveBeenCalledWith({
      customerName: 'Nguyễn Văn An',
      phone: '0912345678',
      deliveryAddress: '12 Lê Lợi, Phường Bến Thành, Thành phố Hồ Chí Minh',
      paymentMethod: 'COD',
      items: [{ productId: 1, quantity: 2 }],
    })
    expect(wrapper.text()).toContain('Đặt hàng thành công')
    expect(wrapper.text()).toContain('DH000015')
    expect(window.localStorage.getItem('cn25-customer-cart')).toBe('[]')
  })

  it('shows an API error and retries loading provinces', async () => {
    getProvincesMock
      .mockRejectedValueOnce(new Error('API unavailable'))
      .mockResolvedValueOnce(provinceOptions)
    const wrapper = mountCheckout()
    await flushPromises()

    expect(wrapper.text()).toContain('Không thể tải danh sách tỉnh/thành phố')
    expect(wrapper.get('#province').attributes()).toHaveProperty('disabled')

    await wrapper.get('#province-load-error button').trigger('click')
    await flushPromises()

    expect(getProvincesMock).toHaveBeenCalledTimes(2)
    expect(wrapper.find('#province-load-error').exists()).toBe(false)
    expect(wrapper.get('#province').attributes()).not.toHaveProperty('disabled')
  })
})
