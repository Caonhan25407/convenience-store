import { describe, expect, it } from 'vitest'
import router from '@/app/router'

describe('customer registration route', () => {
  it('exposes the canonical path and store alias as customer-only guest pages', () => {
    const routes = router.getRoutes()
    const registration = routes.find((route) => route.path === '/register')
    const alias = routes.find((route) => route.path === '/store/register')

    expect(registration?.path).toBe('/register')
    expect(registration?.name).toBe('customer-register')
    expect(registration?.meta).toMatchObject({
      guestOnly: true,
      loginRole: 'CUSTOMER',
    })
    expect(alias?.aliasOf?.name).toBe('customer-register')
  })
})
