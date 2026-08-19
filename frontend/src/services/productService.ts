import type {
  Product,
  ProductRequest,
} from '@/types/product'

const API_URL = 'http://localhost:5000/api/products'

export async function getProducts(): Promise<Product[]> {
  const response = await fetch(API_URL)

  if (!response.ok) {
    throw new Error('Không lấy được danh sách sản phẩm')
  }

  return response.json()
}

export async function createProduct(
  data: ProductRequest,
): Promise<Product> {
  const response = await fetch(API_URL, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json; charset=utf-8',
    },
    body: JSON.stringify(data),
  })

  if (!response.ok) {
    const error = await response.json().catch(() => null)

    throw new Error(
      error?.message ?? 'Thêm sản phẩm thất bại',
    )
  }

  return response.json()
}

export async function updateProduct(
  id: number,
  data: ProductRequest,
): Promise<Product> {
  const response = await fetch(
    `${API_URL}/${id}`,
    {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json; charset=utf-8',
      },
      body: JSON.stringify(data),
    },
  )

  if (!response.ok) {
    throw new Error('Cập nhật sản phẩm thất bại')
  }

  return response.json()
}

export async function deleteProduct(
  id: number,
): Promise<void> {
  const response = await fetch(
    `${API_URL}/${id}`,
    {
      method: 'DELETE',
    },
  )

  if (!response.ok) {
    throw new Error('Xóa sản phẩm thất bại')
  }
}