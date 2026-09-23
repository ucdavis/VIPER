import { createSpaRouter } from "@/shared/create-spa-router"
import { routes } from "./routes"
import { useRequireLogin } from "@/composables/RequireLogin"
import { checkHasOnePermission } from "@/composables/CheckPagePermission"
import { useUserStore } from "@/store/UserStore"
import { ensurePermissions } from "./ensure-permissions"
import { CAREER_SELECTION_PERMISSION_PREFIX } from "@/Students/CareerSelection/constants/permissions"

const router = createSpaRouter(routes)

router.beforeEach(async (to, from) => {
    const userStore = useUserStore()

    // Skip re-authentication for in-app navigations. Re-calling requireLogin would
    // overwrite the permission array (including appended SIS permissions) and cause
    // a visible flash.
    const isInternalNavigation = from.matched.length > 0 && userStore.isLoggedIn
    if (!isInternalNavigation) {
        const { requireLogin } = useRequireLogin(to)
        const loginResult = await requireLogin(true, "SVMSecure.Students")
        if (!loginResult) {
            return false
        }

        // Emergency Contact routes grant access via SVMSecure.SIS.AllStudents, and Career
        // Selection routes via SVMSecure.CareerSelection; both areas are outside requireLogin.
        // Fetched together rather than in turn, to save a round trip: the store merges each
        // area's result into what is already held, so the order they arrive in does not matter.
        await Promise.all([ensurePermissions("SVMSecure.SIS"), ensurePermissions(CAREER_SELECTION_PERMISSION_PREFIX)])
    }

    if (to.meta.permissions !== undefined) {
        const hasPerm = checkHasOnePermission(to.meta.permissions as string[])
        if (!hasPerm) {
            return { name: "StudentsHome" }
        }
    }
})

export { router }
