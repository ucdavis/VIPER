import { createSpaRouter } from "@/shared/create-spa-router"
import type { NavigationGuard, RouteLocationNormalized } from "vue-router"
import { routes } from "./routes"
import { useRequireLogin } from "@/composables/RequireLogin"
import { useUserStore } from "@/store/UserStore"
import { checkHasOnePermission } from "@/composables/CheckPagePermission"

const router = createSpaRouter(routes)

/**
 * Signs the visitor in. Which reports they may see is decided by the API, report by report, so
 * the only permissions loaded are the Report Builder's own (SVMSecure.Reports.*).
 * A null result means requireLogin reached no verdict, which is not a refusal.
 */
async function authenticate(to: RouteLocationNormalized): Promise<boolean> {
    const { requireLogin } = useRequireLogin(to)
    const loginResult = await requireLogin(true, "SVMSecure.Reports")
    return loginResult === null || loginResult
}

// Exported so tests can run the guard over a router whose page components are stubs.
const reportsGuard: NavigationGuard = async (to, from) => {
    // An in-app navigation is already signed in, so the login round-trip would only cost a request.
    const needsAuthentication = from.matched.length === 0 || !useUserStore().isLoggedIn
    if (needsAuthentication && !(await authenticate(to))) {
        return false
    }

    const required = to.meta.permissions as string[] | undefined
    if (required !== undefined && !checkHasOnePermission(required)) {
        return { name: "ReportsHome" }
    }
}

router.beforeEach(reportsGuard)

export { router, reportsGuard }
