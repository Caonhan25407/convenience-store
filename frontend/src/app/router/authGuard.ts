import type {
  LocationQueryValue,
  RouteLocationRaw,
  RouteMeta,
  Router,
} from 'vue-router'
import type { AuthUser, UserRole } from '@/types/auth'

interface AuthGuardStore {
  readonly user: AuthUser | null
  initialize: () => Promise<void>
}

interface GuardTarget {
  fullPath: string
  meta: RouteMeta
}

function routeRole(meta: RouteMeta): UserRole | undefined {
  return meta.role === 'ADMIN' || meta.role === 'CUSTOMER'
    ? meta.role
    : undefined
}

export function roleLanding(role: UserRole): RouteLocationRaw {
  return { name: role === 'ADMIN' ? 'dashboard' : 'store' }
}

export function createAuthGuard(auth: AuthGuardStore) {
  return async (to: GuardTarget): Promise<true | RouteLocationRaw> => {
    await auth.initialize()

    if (to.meta.guestOnly) {
      return auth.user ? roleLanding(auth.user.role) : true
    }

    if (!to.meta.requiresAuth) {
      return true
    }

    const requiredRole = routeRole(to.meta)

    if (!auth.user) {
      return {
        name: requiredRole === 'ADMIN' ? 'admin-login' : 'customer-login',
        query: { redirect: to.fullPath },
      }
    }

    if (requiredRole && auth.user.role !== requiredRole) {
      return roleLanding(auth.user.role)
    }

    return true
  }
}

export function resolveSafeRedirect(
  rawRedirect: LocationQueryValue | LocationQueryValue[] | undefined,
  role: UserRole,
  router: Pick<Router, 'resolve'>,
) {
  const redirect = Array.isArray(rawRedirect) ? rawRedirect[0] : rawRedirect
  const fallback = role === 'ADMIN' ? '/dashboard' : '/store'

  if (
    typeof redirect !== 'string' ||
    !redirect.startsWith('/') ||
    redirect.startsWith('//')
  ) {
    return fallback
  }

  try {
    const resolved = router.resolve(redirect)
    const requiredRole = routeRole(resolved.meta)
    const isProtectedMatch = resolved.matched.some(
      (record) => record.meta.requiresAuth && record.meta.role === role,
    )

    if (!isProtectedMatch || requiredRole !== role) {
      return fallback
    }

    return resolved.fullPath
  } catch {
    return fallback
  }
}
