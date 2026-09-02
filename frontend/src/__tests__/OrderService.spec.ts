import { afterEach, describe, expect, it, vi } from 'vitest'
import { confirmOrder } from '@/services/orderService'

describe('order service confirmation', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('confirms an order through the admin endpoint with the session cookie', async () => {
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(
      new Response(
        JSON.stringify({
          id: 15,
          orderCode: 'DH-20260831-ABC',
          status: 'CONFIRMED',
          message: 'Đã xác nhận đơn hàng DH-20260831-ABC.',
        }),
        {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        },
      ),
    )
    vi.stubGlobal('fetch', fetchMock)

    await expect(confirmOrder(15)).resolves.toMatchObject({
      id: 15,
      status: 'CONFIRMED',
    })
    expect(fetchMock).toHaveBeenCalledWith('/api/orders/15/confirm', {
      method: 'PATCH',
      credentials: 'include',
    })
  })

  it('surfaces the backend message when confirmation is rejected', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn<typeof fetch>().mockResolvedValue(
        new Response(JSON.stringify({ message: 'Đơn hàng đã được xác nhận trước đó.' }), {
          status: 409,
          headers: { 'Content-Type': 'application/json' },
        }),
      ),
    )

    await expect(confirmOrder(15)).rejects.toThrow('Đơn hàng đã được xác nhận trước đó.')
  })
})
