import type { AuthUser, CustomerRegistration, LoginCredentials, UserRole } from '@/types/auth'

const AUTH_URL = '/api/auth'

function portalForRole(role: UserRole) {
  return role === 'ADMIN' ? 'admin' : 'customer'
}

function isAuthUser(value: unknown): value is AuthUser {
  if (!value || typeof value !== 'object') {
    return false
  }

  const user = value as Partial<AuthUser>

  return Boolean(
    Number.isInteger(user.id) &&
    typeof user.email === 'string' &&
    typeof user.displayName === 'string' &&
    (user.role === 'ADMIN' || user.role === 'CUSTOMER'),
  )
}

async function getErrorMessage(response: Response, fallback: string) {
  try {
    const error = (await response.json()) as { message?: unknown }
    return typeof error.message === 'string' ? error.message : fallback
  } catch {
    return fallback
  }
}

async function readUser(response: Response): Promise<AuthUser> {
  const data: unknown = await response.json()

  if (!isAuthUser(data)) {
    throw new Error('Phản hồi đăng nhập không hợp lệ')
  }

  return data
}

async function login(
  portal: 'admin' | 'customer',
  credentials: LoginCredentials,
): Promise<AuthUser> {
  const response = await fetch(`${AUTH_URL}/${portal}/login`, {
    method: 'POST',
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json; charset=utf-8',
    },
    body: JSON.stringify({
      email: credentials.email.trim(),
      password: credentials.password,
    }),
  })

  if (!response.ok) {
    throw new Error(await getErrorMessage(response, 'Không thể đăng nhập lúc này'))
  }

  return readUser(response)
}

export function loginAdmin(credentials: LoginCredentials) {
  return login('admin', credentials)
}

export function loginCustomer(credentials: LoginCredentials) {
  return login('customer', credentials)
}

export async function registerCustomer(registration: CustomerRegistration): Promise<AuthUser> {
  const response = await fetch(`${AUTH_URL}/customer/register`, {
    method: 'POST',
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json; charset=utf-8',
    },
    body: JSON.stringify({
      displayName: registration.displayName.trim(),
      email: registration.email.trim(),
      password: registration.password,
    }),
  })

  if (!response.ok) {
    throw new Error(await getErrorMessage(response, 'Không thể đăng ký lúc này'))
  }

  return readUser(response)
}

export async function getCurrentUser(role: UserRole): Promise<AuthUser | null> {
  const response = await fetch(`${AUTH_URL}/${portalForRole(role)}/me`, {
    credentials: 'include',
  })

  if (response.status === 401 || response.status === 403) {
    return null
  }

  if (!response.ok) {
    throw new Error(await getErrorMessage(response, 'Không thể kiểm tra phiên đăng nhập'))
  }

  return readUser(response)
}

export async function logoutSession(role: UserRole): Promise<void> {
  const response = await fetch(`${AUTH_URL}/${portalForRole(role)}/logout`, {
    method: 'POST',
    credentials: 'include',
  })

  if (!response.ok && response.status !== 401 && response.status !== 403) {
    throw new Error(await getErrorMessage(response, 'Không thể đăng xuất lúc này'))
  }
}
