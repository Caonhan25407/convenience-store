export type UserRole = 'ADMIN' | 'CUSTOMER'

export interface AdminUser {
  id: number
  displayName: string
  email: string
  phone: string | null
  role: UserRole
  isActive: boolean
  createdAt: string
  lastLoginAt: string | null
}

export interface UserPageResponse {
  items: AdminUser[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}
