import { createSpaRouter } from "@/shared/create-spa-router"
import type { NavigationGuard } from "vue-router"
import { routes } from "./routes"
import { createAuthenticator } from "@/composables/RequireLogin"
import { useUserStore } from "@/store/UserStore"
import { checkHasOnePermission } from "@/composables/CheckPagePermission"

const router = createSpaRouter(routes)

// Which reports a visitor may see is decided by the API, report by report, so the only
// permissions loaded here are the Report Builder's own (SVMSecure.Reports.*).
const authenticate = createAuthenticator("SVMSecure.Reports")

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
