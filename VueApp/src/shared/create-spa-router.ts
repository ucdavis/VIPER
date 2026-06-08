import { createRouter, createWebHistory } from "vue-router"
import type { RouteRecordRaw, Router } from "vue-router"
import { useRouteFocus } from "@/composables/use-route-focus"
import { applicationBase } from "./application-base"

/**
 * Standard VIPER SPA router: web history rooted at VITE_VIPER_HOME,
 * scroll restoration on popstate (browser back/forward), scroll-to-top on
 * page navigation (query-only changes keep the scroll position), hash-fragment
 * scrolling for router-driven anchor links, and route-change focus management
 * for accessibility.
 * Callers wire their own `beforeEach` guard since auth/permission rules
 * vary per SPA.
 */
export function createSpaRouter(routes: RouteRecordRaw[]): Router {
    // An explicit "/" is required at the domain root, where applicationBase() (built for
    // concatenation) is the empty string.
    const baseUrl = applicationBase() || "/"
    const router = createRouter({
        scrollBehavior: (to, from, savedPosition) => {
            // Browser back/forward restores the position the user left this entry at;
            // honour it before any of the path/hash defaults below.
            if (savedPosition) {
                return savedPosition
            }
            // Scroll to the fragment target on router-driven hash navigation,
            // but not on query-only changes where the hash merely persists.
            if (to.hash && (to.path !== from.path || to.hash !== from.hash)) {
                return { el: to.hash }
            }
            // Same page, same fragment: a query-only change, so hold the reader's place.
            // Dropping a fragment (/page#section -> /page) falls through to the top instead,
            // which would otherwise leave the page parked at the old anchor.
            if (to.path === from.path && to.hash === from.hash) {
                return false
            }
            return { left: 0, top: 0 }
        },
        history: createWebHistory(baseUrl),
        routes,
    })
    useRouteFocus(router)
    return router
}
