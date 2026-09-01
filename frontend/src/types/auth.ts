export type UserRole = 'ADMIN' | 'CUSTOMER'

export interface AuthUser {
  id: number
  email: string
  displayName: string
  role: UserRole
}

export interface LoginCredentials {
  email: string
  password: string
}

export interface CustomerRegistration {
  displayName: string
  email: string
  password: string
}
