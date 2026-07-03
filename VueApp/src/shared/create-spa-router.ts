import { createRouter, createWebHistory } from "vue-router"
import type { RouteRecordRaw, Router } from "vue-router"
import { useRouteFocus } from "@/composables/use-route-focus"

/**
 * Standard VIPER SPA router: web history rooted at VITE_VIPER_HOME,
 * scroll-to-top on page navigation (query-only changes keep the scroll
 * position), hash-fragment scrolling for router-driven anchor links, and
 * route-change focus management for accessibility.
 * Callers wire their own `beforeEach` guard since auth/permission rules
 * vary per SPA.
 */
export function createSpaRouter(routes: RouteRecordRaw[]): Router {
    // Same normalization as bootstrap-spa: tolerate a missing env var (tests,
    // misconfigured builds) and a trailing slash.
    const baseUrl = (import.meta.env.VITE_VIPER_HOME ?? "/").replace(/\/$/, "")
    const router = createRouter({
        scrollBehavior: (to, from) => {
            // Scroll to the fragment target on router-driven hash navigation,
            // but not on query-only changes where the hash merely persists.
            if (to.hash && (to.path !== from.path || to.hash !== from.hash)) {
                return { el: to.hash }
            }
            return to.path === from.path ? false : { left: 0, top: 0 }
        },
        history: createWebHistory(baseUrl),
        routes,
    })
    useRouteFocus(router)
    return router
}
