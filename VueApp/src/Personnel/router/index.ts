import { createSpaRouter } from "@/shared/create-spa-router"
import type { NavigationGuard, RouteLocationNormalized } from "vue-router"
import { routes } from "./routes"
import { createAuthenticator } from "@/composables/RequireLogin"
import { useUserStore } from "@/store/UserStore"
import { checkHasOnePermission } from "@/composables/CheckPagePermission"

const router = createSpaRouter(routes)

// Those are the only permissions anything in this SPA reads, so asking requireLogin for them
// directly leaves no second permission set to fetch or keep in sync.
const authenticate = createAuthenticator("SVMSecure.PhoneLists")

/**
 * An in-app navigation (tab switch, list to list) is already signed in and already holds its
 * permissions, so repeating the login round-trip would only cost a request and flash the page.
 */
function needsAuthentication(from: RouteLocationNormalized): boolean {
    return from.matched.length === 0 || !useUserStore().isLoggedIn
}

// Exported so tests can run the guard over a router whose page components are stubs.
const personnelGuard: NavigationGuard = async (to, from) => {
    if (needsAuthentication(from) && !(await authenticate(to))) {
        return false
    }

    const required = to.meta.permissions as string[] | null | undefined
    if (required !== null && required !== undefined && !checkHasOnePermission(required)) {
        return { name: "PersonnelHome" }
    }
}

router.beforeEach(personnelGuard)

export { router, personnelGuard }
