import type {
  CreateOrderRequest,
  OrderPageResponse,
  OrderResponse,
} from '@/types/order'

const API_URL = '/api/orders'

async function getErrorMessage(response: Response) {
  try {
    const error = await response.json() as { message?: string }
    return error.message ?? 'Không thể tạo đơn hàng'
  } catch {
    return 'Không thể tạo đơn hàng'
  }
}

export async function createOrder(
  request: CreateOrderRequest,
): Promise<OrderResponse> {
  const response = await fetch(API_URL, {
    method: 'POST',
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json; charset=utf-8',
    },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error(await getErrorMessage(response))
  }

  return response.json() as Promise<OrderResponse>
}

export interface OrderQuery {
  page: number
  pageSize: number
  search?: string
  status?: string
}

export async function getOrders(
  query: OrderQuery,
): Promise<OrderPageResponse> {
  const params = new URLSearchParams({
    page: String(query.page),
    pageSize: String(query.pageSize),
  })

  if (query.search?.trim()) {
    params.set('search', query.search.trim())
  }

  if (query.status && query.status !== 'all') {
    params.set('status', query.status)
  }

  const response = await fetch(`${API_URL}?${params.toString()}`, {
    credentials: 'include',
  })

  if (!response.ok) {
    throw new Error(await getErrorMessage(response))
  }

  return response.json() as Promise<OrderPageResponse>
}
