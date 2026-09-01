import type {
  Product,
  ProductFileFormat,
  ProductImageOptions,
  ProductPageResponse,
  ProductRequest,
} from '@/types/product'

const API_URL = '/api/products'

async function getErrorMessage(response: Response, fallback: string) {
  try {
    const error = await response.json()
    return error.message ?? fallback
  } catch {
    return fallback
  }
}

// Xem danh sách
export interface ProductQuery {
  page: number
  pageSize: number
  search?: string
  minPrice?: string
  maxPrice?: string
  minStock?: string
  maxStock?: string
  stockStatus?: string
}

export async function getProducts(query: ProductQuery): Promise<ProductPageResponse> {
  const params = new URLSearchParams({
    page: String(query.page),
    pageSize: String(query.pageSize),
  })

  const optionalParams = {
    search: query.search,
    minPrice: query.minPrice,
    maxPrice: query.maxPrice,
    minStock: query.minStock,
    maxStock: query.maxStock,
    stockStatus: query.stockStatus,
  }

  Object.entries(optionalParams).forEach(([key, value]) => {
    if (value) {
      params.set(key, value)
    }
  })

  const response = await fetch(`${API_URL}?${params.toString()}`, {
    credentials: 'include',
  })

  if (!response.ok) {
    throw new Error('Không lấy được danh sách sản phẩm')
  }

  return response.json() as Promise<ProductPageResponse>
}

// Thêm
function createProductFormData(data: ProductRequest, imageOptions: ProductImageOptions) {
  const formData = new FormData()

  formData.append('productCode', data.productCode)
  formData.append('name', data.name)
  formData.append('price', String(data.price))
  formData.append('stockQuantity', String(data.stockQuantity))

  if (imageOptions.image) {
    formData.append('image', imageOptions.image)
  }

  formData.append('removeImage', String(Boolean(imageOptions.removeImage)))

  return formData
}

export async function createProduct(
  data: ProductRequest,
  imageOptions: ProductImageOptions = {},
): Promise<Product> {
  const response = await fetch(API_URL, {
    method: 'POST',
    credentials: 'include',
    body: createProductFormData(data, imageOptions),
  })

  if (!response.ok) {
    throw new Error(await getErrorMessage(response, 'Thêm sản phẩm thất bại'))
  }

  return response.json()
}

// Sửa
export async function updateProduct(
  id: number,
  data: ProductRequest,
  imageOptions: ProductImageOptions = {},
): Promise<Product> {
  const response = await fetch(`${API_URL}/${id}`, {
    method: 'PUT',
    credentials: 'include',
    body: createProductFormData(data, imageOptions),
  })

  if (!response.ok) {
    throw new Error(await getErrorMessage(response, 'Cập nhật sản phẩm thất bại'))
  }

  return response.json()
}

// Xóa
export async function deleteProduct(id: number): Promise<void> {
  const response = await fetch(`${API_URL}/${id}`, {
    method: 'DELETE',
    credentials: 'include',
  })

  if (!response.ok) {
    throw new Error('Xóa sản phẩm thất bại')
  }
}

export interface ImportResponse {
  message: string
  successCount: number
  failedCount: number
  conflictedProducts: string[]
}

export interface ProductExport {
  blob: Blob
  fileName: string
}

function getExportFileName(response: Response, format: ProductFileFormat): string {
  const contentDisposition = response.headers.get('Content-Disposition') ?? ''
  const encodedFileName = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i)?.[1]

  if (encodedFileName) {
    try {
      return decodeURIComponent(encodedFileName)
    } catch {
      // Fall back to the plain filename below.
    }
  }

  const plainFileName = contentDisposition.match(/filename="?([^";]+)"?/i)?.[1]
  return plainFileName ?? `san-pham.${format}`
}

export async function exportProducts(format: ProductFileFormat = 'xlsx'): Promise<ProductExport> {
  const params = new URLSearchParams({ format })
  const response = await fetch(`${API_URL}/export?${params.toString()}`, {
    credentials: 'include',
  })

  if (!response.ok) {
    throw new Error(await getErrorMessage(response, 'Export sản phẩm thất bại'))
  }

  return {
    blob: await response.blob(),
    fileName: getExportFileName(response, format),
  }
}

export async function importProducts(file: File): Promise<ImportResponse> {
  const formData = new FormData()
  formData.append('file', file)

  const response = await fetch(`${API_URL}/import`, {
    method: 'POST',
    credentials: 'include',
    body: formData,
  })

  if (!response.ok) {
    throw new Error(await getErrorMessage(response, 'Import sản phẩm thất bại'))
  }

  return response.json() as Promise<ImportResponse>
}
