import { createSpaRouter } from "@/shared/create-spa-router"
import type { RouteLocationNormalized } from "vue-router"
import { routes } from "./routes"
import { useRequireLogin } from "@/composables/RequireLogin"
import { useUserStore } from "@/store/UserStore"
import { checkHasOnePermission } from "@/composables/CheckPagePermission"

const router = createSpaRouter(routes)

/**
 * Signs the visitor in and loads the SVMSecure.PhoneLists permissions in the same request.
 * Those roles are the only permissions anything in this SPA reads, so asking requireLogin for
 * them directly leaves no second permission set to fetch or keep in sync.
 * A null result means requireLogin reached no verdict, which is not a refusal.
 */
async function authenticate(to: RouteLocationNormalized): Promise<boolean> {
    const { requireLogin } = useRequireLogin(to)
    const loginResult = await requireLogin(true, "SVMSecure.PhoneLists")
    return loginResult === null || loginResult
}

/**
 * An in-app navigation (tab switch, list to list) is already signed in and already holds its
 * permissions, so repeating the login round-trip would only cost a request and flash the page.
 */
function needsAuthentication(from: RouteLocationNormalized): boolean {
    return from.matched.length === 0 || !useUserStore().isLoggedIn
}

router.beforeEach(async (to, from) => {
    if (needsAuthentication(from) && !(await authenticate(to))) {
        return false
    }

    const required = to.meta.permissions as string[] | null | undefined
    if (required !== null && required !== undefined && !checkHasOnePermission(required)) {
        return { name: "PersonnelHome" }
    }
})

export { router }
