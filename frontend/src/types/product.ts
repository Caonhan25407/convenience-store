export interface Product {
  id: number
  name: string
  price: number
  stockQuantity: number
  createdAt: string
}

export interface ProductRequest {
  name: string
  price: number
  stockQuantity: number
}