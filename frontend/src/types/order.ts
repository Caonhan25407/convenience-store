export interface CreateOrderItemRequest {
  productId: number
  quantity: number
}

export interface CreateOrderRequest {
  customerName: string
  phone: string
  deliveryAddress: string
  paymentMethod: 'COD'
  items: CreateOrderItemRequest[]
}

export interface OrderResponse {
  id: number
  orderCode: string
  totalAmount: number
  paymentMethod: 'COD'
  status: string
  createdAt: string
}

export interface AdminOrderItem {
  productId: number | null
  productCode: string
  productName: string
  unitPrice: number
  quantity: number
  lineTotal: number
}

export interface AdminOrder {
  id: number
  orderCode: string
  customerName: string
  phone: string
  deliveryAddress: string
  paymentMethod: string
  status: string
  totalAmount: number
  itemCount: number
  totalQuantity: number
  createdAt: string
  items: AdminOrderItem[]
}

export interface OrderPageResponse {
  items: AdminOrder[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}
