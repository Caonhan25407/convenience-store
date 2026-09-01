import type {
  Product,
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

export async function getProducts(
  query: ProductQuery,
): Promise<ProductPageResponse> {
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
export async function createProduct(
  data: ProductRequest,
): Promise<Product> {
  const response = await fetch(API_URL, {
    method: 'POST',
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json; charset=utf-8',
    },
    body: JSON.stringify(data),
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
): Promise<Product> {
  const response = await fetch(`${API_URL}/${id}`, {
    method: 'PUT',
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json; charset=utf-8',
    },
    body: JSON.stringify(data),
  })

  if (!response.ok) {
    throw new Error(await getErrorMessage(response, 'Cập nhật sản phẩm thất bại'))
  }

  return response.json()
}

// Xóa
export async function deleteProduct(
  id: number,
): Promise<void> {
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

export async function importProducts(
  file: File,
): Promise<ImportResponse> {
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
