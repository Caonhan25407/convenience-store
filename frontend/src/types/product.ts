export interface Product {
  id: number
  productCode: string
  name: string
  price: number
  stockQuantity: number
  createdAt: string
}

export interface ProductRequest {
  productCode: string
  name: string
  price: number
  stockQuantity: number
}