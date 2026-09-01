import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  createProduct,
  exportProducts,
  importProducts,
  updateProduct,
} from '@/services/productService'

describe('product service file operations', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('downloads the admin Excel export with cookies and the server filename', async () => {
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(
      new Response(new Uint8Array([0x50, 0x4b]), {
        status: 200,
        headers: {
          'Content-Disposition': "attachment; filename*=UTF-8''san-pham-20260901.xlsx",
          'Content-Type': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
        },
      }),
    )
    vi.stubGlobal('fetch', fetchMock)

    const result = await exportProducts()

    expect(fetchMock).toHaveBeenCalledWith('/api/products/export?format=xlsx', {
      credentials: 'include',
    })
    expect(result.fileName).toBe('san-pham-20260901.xlsx')
    expect(result.blob.type).toBe(
      'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    )
  })

  it('surfaces the API error when export fails', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn<typeof fetch>().mockResolvedValue(
        new Response(JSON.stringify({ message: 'Không thể export lúc này' }), {
          status: 500,
          headers: { 'Content-Type': 'application/json' },
        }),
      ),
    )

    await expect(exportProducts()).rejects.toThrow('Không thể export lúc này')
  })

  it('uploads the selected Excel file as multipart form data', async () => {
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(
      new Response(
        JSON.stringify({
          message: 'Import thành công 1 sản phẩm',
          successCount: 1,
          failedCount: 0,
          conflictedProducts: [],
        }),
        {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        },
      ),
    )
    vi.stubGlobal('fetch', fetchMock)

    const file = new File([new Uint8Array([0x50, 0x4b])], 'san-pham.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })

    await importProducts(file)

    expect(fetchMock).toHaveBeenCalledOnce()
    const [url, request] = fetchMock.mock.calls[0] ?? []
    expect(url).toBe('/api/products/import')
    expect(request?.method).toBe('POST')
    expect(request?.credentials).toBe('include')
    const requestBody = request?.body
    expect(requestBody).toBeInstanceOf(FormData)
    expect((requestBody as FormData).get('file')).toBe(file)
    expect(request?.headers).toBeUndefined()
  })

  it('requests a CSV export and uses a CSV fallback filename', async () => {
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(
      new Response('Mã sản phẩm,Tên sản phẩm,Giá,Số lượng', {
        status: 200,
        headers: { 'Content-Type': 'text/csv; charset=utf-8' },
      }),
    )
    vi.stubGlobal('fetch', fetchMock)

    await expect(exportProducts('csv')).resolves.toMatchObject({ fileName: 'san-pham.csv' })
    expect(fetchMock).toHaveBeenCalledWith('/api/products/export?format=csv', {
      credentials: 'include',
    })
  })

  it('creates and updates products as multipart form data without overriding the boundary', async () => {
    const responseProduct = {
      id: 1,
      productCode: 'SP001',
      name: 'Mì Hảo Hảo',
      price: 5000,
      stockQuantity: 12,
      imageUrl: '/api/products/1/image?v=one',
      createdAt: '2026-09-02T00:00:00Z',
    }
    const fetchMock = vi.fn<typeof fetch>().mockImplementation(
      async () =>
        new Response(JSON.stringify(responseProduct), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
    )
    vi.stubGlobal('fetch', fetchMock)

    const data = {
      productCode: 'SP001',
      name: 'Mì Hảo Hảo',
      price: 5000,
      stockQuantity: 12,
    }
    const image = new File([new Uint8Array([0xff, 0xd8, 0xff])], 'mi.jpg', {
      type: 'image/jpeg',
    })

    await createProduct(data, { image })
    await updateProduct(1, data, { removeImage: true })

    const [createUrl, createRequest] = fetchMock.mock.calls[0] ?? []
    const createBody = createRequest?.body as FormData
    expect(createUrl).toBe('/api/products')
    expect(createRequest?.method).toBe('POST')
    expect(createRequest?.headers).toBeUndefined()
    expect(createBody.get('productCode')).toBe('SP001')
    expect(createBody.get('name')).toBe('Mì Hảo Hảo')
    expect(createBody.get('price')).toBe('5000')
    expect(createBody.get('stockQuantity')).toBe('12')
    expect(createBody.get('image')).toBe(image)
    expect(createBody.get('removeImage')).toBe('false')

    const [updateUrl, updateRequest] = fetchMock.mock.calls[1] ?? []
    const updateBody = updateRequest?.body as FormData
    expect(updateUrl).toBe('/api/products/1')
    expect(updateRequest?.method).toBe('PUT')
    expect(updateRequest?.headers).toBeUndefined()
    expect(updateBody.get('image')).toBeNull()
    expect(updateBody.get('removeImage')).toBe('true')
  })

  it('falls back to an Excel filename when the server omits Content-Disposition', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn<typeof fetch>().mockResolvedValue(
        new Response(new Uint8Array([0x50, 0x4b]), {
          status: 200,
          headers: {
            'Content-Type': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
          },
        }),
      ),
    )

    await expect(exportProducts()).resolves.toMatchObject({ fileName: 'san-pham.xlsx' })
  })
})
