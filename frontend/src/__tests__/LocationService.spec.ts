import { afterEach, describe, expect, it, vi } from 'vitest'
import { getProvinces, getWardsByProvince } from '@/services/locationService'

describe('location service', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('loads the post-merger province and city list', async () => {
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(
      new Response(
        JSON.stringify([
          { code: 1, name: 'Thành phố Hà Nội' },
          { code: 79, name: 'Thành phố Hồ Chí Minh' },
        ]),
        { status: 200 },
      ),
    )
    vi.stubGlobal('fetch', fetchMock)

    await expect(getProvinces()).resolves.toEqual([
      { code: 1, name: 'Thành phố Hà Nội' },
      { code: 79, name: 'Thành phố Hồ Chí Minh' },
    ])
    expect(fetchMock).toHaveBeenCalledWith('https://provinces.open-api.vn/api/v2/p/', {
      headers: { Accept: 'application/json' },
      signal: undefined,
    })
  })

  it('loads wards for the selected province and removes mismatched data', async () => {
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(
      new Response(
        JSON.stringify([
          { code: 26734, name: 'Phường Bến Thành', province_code: 79 },
          { code: 4, name: 'Phường Ba Đình', province_code: 1 },
        ]),
        { status: 200 },
      ),
    )
    vi.stubGlobal('fetch', fetchMock)

    await expect(getWardsByProvince(79)).resolves.toEqual([
      { code: 26734, name: 'Phường Bến Thành' },
    ])
    expect(fetchMock).toHaveBeenCalledWith('https://provinces.open-api.vn/api/v2/w/?province=79', {
      headers: { Accept: 'application/json' },
      signal: undefined,
    })
  })

  it('rejects an invalid province code without calling the API', async () => {
    const fetchMock = vi.fn<typeof fetch>()
    vi.stubGlobal('fetch', fetchMock)

    await expect(getWardsByProvince(0)).rejects.toThrow('Mã tỉnh/thành phố không hợp lệ')
    expect(fetchMock).not.toHaveBeenCalled()
  })
})
