import type { NavigationGuard, RouteRecordRaw, Router } from "vue-router"
import { createSpaRouter } from "../create-spa-router"

/**
 * Navigating a real SPA router loads every lazy page component on the matched records before a
 * push settles, importing whole Quasar pages just to check a redirect. This runs the real guard
 * over the real route table with the components stubbed out instead. Build one per test: a fresh
 * router starts at START_LOCATION, so the first push is never a redundant navigation (which would
 * resolve to a NavigationFailure and leave currentRoute unchanged).
 */
export function createGuardedStubRouter(routes: RouteRecordRaw[], guard: NavigationGuard): Router {
    const router = createSpaRouter(routes.map((route) => (route.component ? { ...route, component: {} } : route)))
    router.beforeEach(guard)
    return router
}
