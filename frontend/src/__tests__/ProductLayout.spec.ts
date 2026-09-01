import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import ProductLayout from '@/app/layouts/admin/ProductLayout.vue'

const getProductsMock = vi.hoisted(() =>
  vi.fn<typeof import('@/services/productService').getProducts>(),
)
const exportProductsMock = vi.hoisted(() =>
  vi.fn<typeof import('@/services/productService').exportProducts>(),
)
const importProductsMock = vi.hoisted(() =>
  vi.fn<typeof import('@/services/productService').importProducts>(),
)
const createProductMock = vi.hoisted(() =>
  vi.fn<typeof import('@/services/productService').createProduct>(),
)
const updateProductMock = vi.hoisted(() =>
  vi.fn<typeof import('@/services/productService').updateProduct>(),
)

vi.mock('@/services/productService', () => ({
  createProduct: createProductMock,
  deleteProduct: vi.fn<typeof import('@/services/productService').deleteProduct>(),
  exportProducts: exportProductsMock,
  getProducts: getProductsMock,
  importProducts: importProductsMock,
  updateProduct: updateProductMock,
}))

function mountProducts() {
  return mount(ProductLayout, {
    global: {
      stubs: {
        Navbar: true,
        Sidebar: true,
        Teleport: true,
      },
    },
  })
}

describe('ProductLayout file and image operations', () => {
  const createObjectUrlMock = vi.fn<(value: Blob | MediaSource) => string>(
    () => 'blob:product-export',
  )
  const revokeObjectUrlMock = vi.fn<(url: string) => void>()

  beforeEach(() => {
    const savedProduct = {
      id: 1,
      productCode: 'SP001',
      name: 'Mì Hảo Hảo',
      price: 5000,
      stockQuantity: 12,
      imageUrl: '/api/products/1/image?v=one',
      createdAt: '2026-09-02T00:00:00Z',
    }

    getProductsMock.mockReset()
    getProductsMock.mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
      totalPages: 0,
    })

    exportProductsMock.mockReset()
    exportProductsMock.mockResolvedValue({
      blob: new Blob([new Uint8Array([0x50, 0x4b])], {
        type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      }),
      fileName: 'san-pham-20260901.xlsx',
    })

    importProductsMock.mockReset()
    importProductsMock.mockResolvedValue({
      message: 'Import thành công 1 sản phẩm',
      successCount: 1,
      failedCount: 0,
      conflictedProducts: [],
    })

    createProductMock.mockReset()
    createProductMock.mockResolvedValue(savedProduct)
    updateProductMock.mockReset()
    updateProductMock.mockResolvedValue(savedProduct)

    createObjectUrlMock.mockReset()
    createObjectUrlMock.mockReturnValue('blob:product-export')
    revokeObjectUrlMock.mockReset()

    Object.defineProperty(URL, 'createObjectURL', {
      configurable: true,
      value: createObjectUrlMock,
    })
    Object.defineProperty(URL, 'revokeObjectURL', {
      configurable: true,
      value: revokeObjectUrlMock,
    })
  })

  afterEach(() => {
    vi.restoreAllMocks()
    document.body.innerHTML = ''
  })

  it('opens the export modal and downloads the selected format', async () => {
    let downloadedFileName = ''
    let downloadedUrl = ''
    exportProductsMock.mockResolvedValueOnce({
      blob: new Blob(['Mã sản phẩm,Tên sản phẩm,Giá,Số lượng'], { type: 'text/csv' }),
      fileName: 'san-pham-20260901.csv',
    })
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (
      this: HTMLAnchorElement,
    ) {
      downloadedFileName = this.download
      downloadedUrl = this.href
    })

    const wrapper = mountProducts()
    await flushPromises()

    const fileActions = wrapper.get('.file-actions')
    expect(fileActions.get('.btn-import-form').text()).toContain('Import file')
    expect(fileActions.get('.btn-export-form').text()).toContain('Export file')

    await fileActions.get('.btn-export-form').trigger('click')
    const formatSelect = wrapper.get<HTMLSelectElement>('#exportFormat')
    expect(formatSelect.findAll('option').map((option) => option.attributes('value'))).toEqual([
      'xlsx',
      'csv',
    ])
    await formatSelect.setValue('csv')
    await wrapper.get('.export-modal .modal-form').trigger('submit')
    await flushPromises()

    expect(exportProductsMock).toHaveBeenCalledWith('csv')
    expect(createObjectUrlMock).toHaveBeenCalledOnce()
    expect(downloadedFileName).toBe('san-pham-20260901.csv')
    expect(downloadedUrl).toBe('blob:product-export')
    expect(revokeObjectUrlMock).toHaveBeenCalledWith('blob:product-export')
    expect(wrapper.text()).toContain('Đã export file san-pham-20260901.csv')
  })

  it('switches the import chooser to CSV, uploads the file and reloads products', async () => {
    const wrapper = mountProducts()
    await flushPromises()

    await wrapper.get('.btn-import-form').trigger('click')

    await wrapper.get('#importFormat').setValue('csv')

    const fileInput = wrapper.get<HTMLInputElement>('#importFile')
    const file = new File(['SP001,Mì Hảo Hảo,5000,12'], 'san-pham.csv', {
      type: 'text/csv',
    })

    expect(fileInput.attributes('accept')).toBe('.csv,text/csv')

    Object.defineProperty(fileInput.element, 'files', {
      configurable: true,
      value: [file],
    })
    await fileInput.trigger('change')
    await wrapper.get('.modal-form').trigger('submit')
    await flushPromises()

    expect(importProductsMock).toHaveBeenCalledWith(file)
    expect(getProductsMock).toHaveBeenCalledTimes(2)
    expect(wrapper.text()).toContain('Import thành công 1 sản phẩm')
  })

  it('previews a valid image and sends it when creating a product', async () => {
    createObjectUrlMock.mockReturnValueOnce('blob:product-image')
    const wrapper = mountProducts()
    await flushPromises()

    await wrapper.get('.btn-open-form').trigger('click')
    await wrapper.get('#productCode').setValue('SP001')
    await wrapper.get('#productName').setValue('Mì Hảo Hảo')
    await wrapper.get('#price').setValue(5000)
    await wrapper.get('#stockQuantity').setValue(12)

    const imageInput = wrapper.get<HTMLInputElement>('#productImage')
    const image = new File([new Uint8Array([0xff, 0xd8, 0xff])], 'mi.jpg', {
      type: 'image/jpeg',
    })

    expect(imageInput.attributes('accept')).toContain('image/webp')
    Object.defineProperty(imageInput.element, 'files', {
      configurable: true,
      value: [image],
    })
    await imageInput.trigger('change')

    expect(wrapper.get('.product-image-preview img').attributes('src')).toBe('blob:product-image')

    await wrapper.get('.product-modal .modal-form').trigger('submit')
    await flushPromises()

    expect(createProductMock).toHaveBeenCalledWith(
      {
        productCode: 'SP001',
        name: 'Mì Hảo Hảo',
        price: 5000,
        stockQuantity: 12,
      },
      { image, removeImage: false },
    )
    expect(revokeObjectUrlMock).toHaveBeenCalledWith('blob:product-image')
  })

  it('shows an existing image in edit mode and sends the explicit remove flag', async () => {
    const product = {
      id: 1,
      productCode: 'SP001',
      name: 'Mì Hảo Hảo',
      price: 5000,
      stockQuantity: 12,
      imageUrl: '/api/products/1/image?v=one',
      createdAt: '2026-09-02T00:00:00Z',
    }
    getProductsMock.mockResolvedValue({
      items: [product],
      totalCount: 1,
      page: 1,
      pageSize: 20,
      totalPages: 1,
    })

    const wrapper = mountProducts()
    await flushPromises()

    expect(wrapper.get('.product-image-thumbnail img').attributes('src')).toBe(product.imageUrl)
    await wrapper.get('.btn-edit').trigger('click')
    expect(wrapper.get('.product-image-preview img').attributes('src')).toBe(product.imageUrl)

    await wrapper.get('.btn-remove-image').trigger('click')
    expect(wrapper.find('.product-image-preview img').exists()).toBe(false)
    expect(wrapper.find('.btn-restore-image').exists()).toBe(true)

    await wrapper.get('.product-modal .modal-form').trigger('submit')
    await flushPromises()

    expect(updateProductMock).toHaveBeenCalledWith(
      1,
      {
        productCode: 'SP001',
        name: 'Mì Hảo Hảo',
        price: 5000,
        stockQuantity: 12,
      },
      { image: null, removeImage: true },
    )
  })

  it('rejects an image larger than 5 MB before creating a preview', async () => {
    const wrapper = mountProducts()
    await flushPromises()
    await wrapper.get('.btn-open-form').trigger('click')

    const imageInput = wrapper.get<HTMLInputElement>('#productImage')
    const image = new File(['image'], 'too-large.png', { type: 'image/png' })
    Object.defineProperty(image, 'size', {
      configurable: true,
      value: 5 * 1024 * 1024 + 1,
    })
    Object.defineProperty(imageInput.element, 'files', {
      configurable: true,
      value: [image],
    })
    await imageInput.trigger('change')

    expect(wrapper.text()).toContain('Ảnh sản phẩm không được vượt quá 5 MB')
    expect(wrapper.find('.product-image-preview img').exists()).toBe(false)
    expect(createObjectUrlMock).not.toHaveBeenCalled()
  })
})
