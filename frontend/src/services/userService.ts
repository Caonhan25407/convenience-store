import type { UserPageResponse, UserRole } from '@/types/user'

const API_URL = '/api/users'

export interface UserQuery {
  page: number
  pageSize: number
  search?: string
  role?: UserRole | 'all'
}

async function getErrorMessage(response: Response) {
  try {
    const error = await response.json() as { message?: unknown }
    return typeof error.message === 'string'
      ? error.message
      : 'Không thể tải danh sách người dùng'
  } catch {
    return 'Không thể tải danh sách người dùng'
  }
}

export async function getUsers(query: UserQuery): Promise<UserPageResponse> {
  const params = new URLSearchParams({
    page: String(query.page),
    pageSize: String(query.pageSize),
    role: query.role ?? 'all',
  })

  if (query.search?.trim()) {
    params.set('search', query.search.trim())
  }

  const response = await fetch(`${API_URL}?${params.toString()}`, {
    credentials: 'include',
  })

  if (!response.ok) {
    throw new Error(await getErrorMessage(response))
  }

  return response.json() as Promise<UserPageResponse>
}
