export interface Product {
  id: number
  productCode: string
  name: string
  price: number
  stockQuantity: number
  imageUrl: string | null
  createdAt: string
}

export interface ProductRequest {
  productCode: string
  name: string
  price: number
  stockQuantity: number
}

export interface ProductPageResponse {
  items: Product[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface ProductImageOptions {
  image?: File | null
  removeImage?: boolean
}

export type ProductFileFormat = 'xlsx' | 'csv'
