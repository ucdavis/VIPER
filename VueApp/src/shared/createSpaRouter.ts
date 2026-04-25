import { createRouter, createWebHistory } from "vue-router"
import type { RouteRecordRaw, Router } from "vue-router"
import { useRouteFocus } from "@/composables/use-route-focus"

/**
 * Standard VIPER SPA router: web history rooted at VITE_VIPER_HOME,
 * scroll-to-top on every navigation, and route-change focus management
 * for accessibility. Callers wire their own `beforeEach` guard since
 * auth/permission rules vary per SPA.
 */
export function createSpaRouter(routes: RouteRecordRaw[]): Router {
    const baseUrl = import.meta.env.VITE_VIPER_HOME
    const router = createRouter({
        scrollBehavior: () => ({ left: 0, top: 0 }),
        history: createWebHistory(baseUrl),
        routes,
    })
    useRouteFocus(router)
    return router
}
