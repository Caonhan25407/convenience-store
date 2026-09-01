import { afterEach, describe, expect, it, vi } from 'vitest'
import { getCurrentUser, loginAdmin, logoutSession, registerCustomer } from '@/services/authService'

const admin = {
  id: 1,
  email: 'admin@cn25.vn',
  displayName: 'Quản trị viên',
  role: 'ADMIN' as const,
}

const customer = {
  id: 2,
  email: 'customer@cn25.vn',
  displayName: 'Nguyễn Văn An',
  role: 'CUSTOMER' as const,
}

describe('auth service', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('logs into the admin endpoint with credentials-enabled cookies', async () => {
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(
      new Response(JSON.stringify(admin), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    )
    vi.stubGlobal('fetch', fetchMock)

    await expect(
      loginAdmin({
        email: ' admin@cn25.vn ',
        password: 'secret123',
      }),
    ).resolves.toEqual(admin)

    expect(fetchMock).toHaveBeenCalledWith('/api/auth/admin/login', {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json; charset=utf-8',
      },
      body: JSON.stringify({
        email: 'admin@cn25.vn',
        password: 'secret123',
      }),
    })
  })

  it('treats an unauthorized session lookup as a guest', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn<typeof fetch>().mockResolvedValue(new Response(null, { status: 401 })),
    )

    await expect(getCurrentUser('CUSTOMER')).resolves.toBeNull()
    expect(fetch).toHaveBeenCalledWith('/api/auth/customer/me', {
      credentials: 'include',
    })
  })

  it('logs out only the requested portal session', async () => {
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(new Response(null, { status: 204 }))
    vi.stubGlobal('fetch', fetchMock)

    await logoutSession('ADMIN')

    expect(fetchMock).toHaveBeenCalledWith('/api/auth/admin/logout', {
      method: 'POST',
      credentials: 'include',
    })
  })

  it('registers a customer with trimmed public fields and an HttpOnly-cookie session', async () => {
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(
      new Response(JSON.stringify(customer), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    )
    vi.stubGlobal('fetch', fetchMock)

    await expect(
      registerCustomer({
        displayName: ' Nguyễn Văn An ',
        email: ' customer@cn25.vn ',
        password: 'secret123',
      }),
    ).resolves.toEqual(customer)

    expect(fetchMock).toHaveBeenCalledWith('/api/auth/customer/register', {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json; charset=utf-8',
      },
      body: JSON.stringify({
        displayName: 'Nguyễn Văn An',
        email: 'customer@cn25.vn',
        password: 'secret123',
      }),
    })
  })
})
