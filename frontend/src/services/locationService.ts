const LOCATION_API_URL = 'https://provinces.open-api.vn/api/v2'

export interface LocationOption {
  code: number
  name: string
}

function parseLocationOptions(
  data: unknown,
  errorMessage: string,
  expectedProvinceCode?: number,
): LocationOption[] {
  if (!Array.isArray(data)) {
    throw new Error(errorMessage)
  }

  const options = data.map((item) => {
    if (typeof item !== 'object' || item === null) {
      throw new Error(errorMessage)
    }

    const code = Reflect.get(item, 'code')
    const name = Reflect.get(item, 'name')
    const provinceCode = Reflect.get(item, 'province_code')

    if (
      typeof code !== 'number' ||
      typeof name !== 'string' ||
      (expectedProvinceCode !== undefined && typeof provinceCode !== 'number')
    ) {
      throw new Error(errorMessage)
    }

    return {
      code,
      name: name.trim(),
      provinceCode,
    }
  })

  if (options.some((option) => !Number.isInteger(option.code) || !option.name)) {
    throw new Error(errorMessage)
  }

  return options
    .filter(
      (option) =>
        expectedProvinceCode === undefined || option.provinceCode === expectedProvinceCode,
    )
    .map(({ code, name }) => ({ code, name }))
}

async function getLocationOptions(
  url: string,
  errorMessage: string,
  signal?: AbortSignal,
  expectedProvinceCode?: number,
) {
  const response = await fetch(url, {
    headers: {
      Accept: 'application/json',
    },
    signal,
  })

  if (!response.ok) {
    throw new Error(errorMessage)
  }

  return parseLocationOptions(await response.json(), errorMessage, expectedProvinceCode)
}

export function getProvinces(signal?: AbortSignal) {
  return getLocationOptions(
    `${LOCATION_API_URL}/p/`,
    'Không thể tải danh sách tỉnh/thành phố',
    signal,
  )
}

export function getWardsByProvince(provinceCode: number, signal?: AbortSignal) {
  if (!Number.isInteger(provinceCode) || provinceCode <= 0) {
    return Promise.reject(new Error('Mã tỉnh/thành phố không hợp lệ'))
  }

  const params = new URLSearchParams({
    province: String(provinceCode),
  })

  return getLocationOptions(
    `${LOCATION_API_URL}/w/?${params.toString()}`,
    'Không thể tải danh sách phường/xã',
    signal,
    provinceCode,
  )
}
