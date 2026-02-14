import { createRouter, createWebHistory } from "vue-router"
import { effortRoutes as routes } from "./routes"
import { useRequireLogin } from "@/composables/RequireLogin"
import checkHasOnePermission from "@/composables/CheckPagePermission"
import { useFetch } from "@/composables/ViperFetch"
import { useUserStore } from "@/store/UserStore"

const baseUrl = import.meta.env.VITE_VIPER_HOME
const router = createRouter({
    scrollBehavior: () => ({ left: 0, top: 0 }),
    history: createWebHistory(baseUrl),
    routes,
})

// Dedup latch: reuse in-flight fetch so concurrent navigations don't fire multiple requests
let evalPermissionsPromise: Promise<void> | null = null

async function loadEvalPermissions() {
    const userStore = useUserStore()
    const existingPermissions = userStore.userInfo?.permissions ?? []
    const { get } = useFetch()
    const apiUrl = import.meta.env.VITE_API_URL
    const evalPerms = await get(`${apiUrl}loggedInUser/permissions?prefix=SVMSecure.Eval`)
    if (evalPerms.success && Array.isArray(evalPerms.result)) {
        userStore.setPermissions([...existingPermissions, ...evalPerms.result])
    } else {
        // Reset latch so the next navigation retries the fetch
        evalPermissionsPromise = null
    }
}

router.beforeEach(async (to) => {
    const { requireLogin } = useRequireLogin(to)
    const loginResult = await requireLogin(true, "SVMSecure.Effort")
    if (loginResult !== null && !loginResult) {
        return false
    }

    // Eval permissions are in a separate area, so they aren't loaded by requireLogin
    const userStore = useUserStore()
    const existingPermissions = userStore.userInfo?.permissions ?? []
    const hasEvalPermissions = existingPermissions.some((p: string) => p.startsWith("SVMSecure.Eval"))
    if (!hasEvalPermissions) {
        if (!evalPermissionsPromise) {
            evalPermissionsPromise = loadEvalPermissions()
        }
        await evalPermissionsPromise
    }

    if (to.meta.permissions !== null && to.meta.permissions !== undefined) {
        const hasPerm = checkHasOnePermission(to.meta.permissions as string[])
        if (!hasPerm) {
            return { name: "EffortHome" }
        }
    }
})

export { router as effortRouter }
