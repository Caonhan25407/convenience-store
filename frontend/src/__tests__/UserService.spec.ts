import { afterEach, describe, expect, it, vi } from 'vitest'
import { getUsers } from '@/services/userService'

describe('user service', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sends pagination, trimmed search, role and the admin cookie', async () => {
    const response = {
      items: [],
      totalCount: 0,
      page: 2,
      pageSize: 15,
      totalPages: 0,
    }
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(
      new Response(JSON.stringify(response), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    )
    vi.stubGlobal('fetch', fetchMock)

    await expect(getUsers({
      page: 2,
      pageSize: 15,
      search: '  Nguyễn Văn An  ',
      role: 'CUSTOMER',
    })).resolves.toEqual(response)

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/users?page=2&pageSize=15&role=CUSTOMER&search=Nguy%E1%BB%85n+V%C4%83n+An',
      { credentials: 'include' },
    )
  })
})
