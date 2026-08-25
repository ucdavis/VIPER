import { createSpaRouter } from "@/shared/create-spa-router"
import { routes } from "./routes"
import { useRequireLogin } from "@/composables/RequireLogin"
import { useUserStore } from "@/store/UserStore"
import { useFetch } from "@/composables/ViperFetch"
import { checkHasOnePermission } from "@/composables/CheckPagePermission"

const router = createSpaRouter(routes)

// Dedup latch: reuse in-flight fetch so concurrent navigations don't fire multiple requests
let phonePermissionsPromise: Promise<void> | null = null

async function loadPhonePermissions() {
    try {
        const userStore = useUserStore()
        const existingPermissions = userStore.userInfo?.permissions ?? []
        const { get } = useFetch()
        const apiUrl = import.meta.env.VITE_API_URL
        const evalPerms = await get(`${apiUrl}loggedInUser/permissions?prefix=SVMSecure.PhoneLists`)
        if (evalPerms.success && Array.isArray(evalPerms.result)) {
            userStore.setPermissions([...existingPermissions, ...evalPerms.result])
        }
    } finally {
        // Reset latch so future session changes refetch instead of reusing the old resolved promise
        phonePermissionsPromise = null
    }
}

router.beforeEach(async (to, from) => {
    const userStore = useUserStore()

    // Skip re-authentication for in-app navigations (tab switches, course-to-course).
    // The user is already logged in and permissions are loaded; re-calling requireLogin
    // Would overwrite the permission array and cause a visible flash.
    const isInternalNavigation = from.matched.length > 0 && userStore.isLoggedIn
    if (!isInternalNavigation) {
        const { requireLogin } = useRequireLogin(to)
        const loginResult = await requireLogin(true, "SVMSecure.Personnel")
        if (loginResult !== null && !loginResult) {
            return false
        }

        // PhoneList permissions are in a separate area, so they aren't loaded by requireLogin
        if (!phonePermissionsPromise) {
            phonePermissionsPromise = loadPhonePermissions()
        }
        await phonePermissionsPromise
    }

    if (to.meta.permissions !== null && to.meta.permissions !== undefined) {
        const hasPerm = checkHasOnePermission(to.meta.permissions as string[])
        if (!hasPerm) {
            return { name: "PersonnelHome" }
        }
    }
})

export { router }
