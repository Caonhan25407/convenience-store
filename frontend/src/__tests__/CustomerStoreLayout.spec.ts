import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import CustomerStoreLayout from '@/app/layouts/store/CustomerStoreLayout.vue'

const getProductsMock = vi.hoisted(() =>
  vi.fn<typeof import('@/services/productService').getProducts>(),
)

vi.mock('@/services/productService', () => ({
  getProducts: getProductsMock,
}))

const productPage = {
  items: [
    {
      id: 1,
      productCode: 'SP001',
      name: 'Mì Hảo Hảo',
      price: 5000,
      stockQuantity: 12,
      imageUrl: '/api/products/1/image?v=one',
      createdAt: '2026-08-31T00:00:00Z',
    },
    {
      id: 2,
      productCode: 'SP002',
      name: 'Coca Cola',
      price: 12000,
      stockQuantity: 0,
      imageUrl: null,
      createdAt: '2026-08-31T00:00:00Z',
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 12,
  totalPages: 1,
}

function mountStore() {
  return mount(CustomerStoreLayout, {
    global: {
      stubs: {
        RouterLink: {
          template: '<a><slot /></a>',
        },
        StoreAccountControls: true,
      },
    },
  })
}

describe('CustomerStoreLayout', () => {
  beforeEach(() => {
    getProductsMock.mockReset()
    getProductsMock.mockResolvedValue(productPage)
    window.localStorage.clear()
    document.body.style.overflow = ''
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('loads the same product API and renders customer-facing product cards', async () => {
    const wrapper = mountStore()

    await flushPromises()

    const backToProducts = wrapper.get('.back-to-products')
    expect(backToProducts.attributes('href')).toBe('#products')
    expect(backToProducts.attributes('aria-label')).toBe('Quay lại danh sách sản phẩm')
    expect(backToProducts.attributes('title')).toBe('Quay lại danh sách sản phẩm')
    expect(backToProducts.text()).toBe('')
    expect(backToProducts.find('svg').exists()).toBe(true)
    expect(wrapper.find('.store-footer .back-to-products').exists()).toBe(false)

    expect(getProductsMock).toHaveBeenCalledWith({
      page: 1,
      pageSize: 12,
      search: '',
      stockStatus: 'all',
    })
    const productVisuals = wrapper.findAll('.product-visual')
    expect(productVisuals).toHaveLength(2)
    expect(productVisuals.every((visual) => visual.text() === '')).toBe(true)
    expect(productVisuals[0]?.get('img').attributes('src')).toBe('/api/products/1/image?v=one')
    expect(productVisuals[1]?.find('img').exists()).toBe(false)
    expect(wrapper.find('.stock-badge').exists()).toBe(false)
    expect(wrapper.find('.product-monogram').exists()).toBe(false)
    expect(wrapper.text()).toContain('Mì Hảo Hảo')
    expect(wrapper.text()).toContain('5.000 ₫')
    expect(wrapper.text()).toContain('Tạm thời hết hàng')
  })

  it('shows a recoverable error state when the API is unavailable', async () => {
    getProductsMock.mockRejectedValueOnce(new Error('Không lấy được danh sách sản phẩm'))

    const wrapper = mountStore()
    await flushPromises()

    expect(wrapper.text()).toContain('Chưa thể tải sản phẩm')
    expect(wrapper.text()).toContain('Không lấy được danh sách sản phẩm')

    await wrapper.get('.error-panel button').trigger('click')
    await flushPromises()

    expect(getProductsMock).toHaveBeenCalledTimes(2)
    expect(wrapper.text()).toContain('Mì Hảo Hảo')
  })

  it('debounces searches before requesting matching products', async () => {
    vi.useFakeTimers()

    const wrapper = mountStore()
    await flushPromises()

    await wrapper.get('#store-search').setValue('Coca')
    await vi.advanceTimersByTimeAsync(350)
    await flushPromises()

    expect(getProductsMock).toHaveBeenLastCalledWith({
      page: 1,
      pageSize: 12,
      search: 'Coca',
      stockStatus: 'all',
    })
  })

  it('adds products to a persistent cart and updates quantities', async () => {
    const wrapper = mountStore()
    await flushPromises()

    const addButtons = wrapper.findAll('.add-cart')

    expect(addButtons).toHaveLength(2)
    expect(addButtons[1]?.attributes('disabled')).toBeDefined()

    await addButtons[0]?.trigger('click')

    expect(wrapper.get('.cart-trigger').text()).toContain('1')
    expect(wrapper.text()).toContain('1 sản phẩm')
    expect(wrapper.get('.cart-thumb').text()).toBe('')
    expect(document.body.style.overflow).toBe('hidden')

    await wrapper.get('.quantity-control button:last-child').trigger('click')

    expect(wrapper.get('.cart-trigger').text()).toContain('2')
    expect(wrapper.text()).toContain('10.000 ₫')
    expect(JSON.parse(window.localStorage.getItem('cn25-customer-cart') ?? '[]')).toMatchObject([
      {
        product: { imageUrl: '/api/products/1/image?v=one' },
        quantity: 2,
      },
    ])

    await wrapper.get('.cart-header > button').trigger('click')

    expect(wrapper.find('.cart-drawer').exists()).toBe(false)
    expect(document.body.style.overflow).toBe('')
  })
})
