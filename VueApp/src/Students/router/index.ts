import { createRouter, createWebHistory } from "vue-router"
import { routes } from "./routes"
import { useRequireLogin } from "@/composables/RequireLogin"
import { useRouteFocus } from "@/composables/use-route-focus"
import { checkHasOnePermission } from "@/composables/CheckPagePermission"
import { useFetch } from "@/composables/ViperFetch"
import { useUserStore } from "@/store/UserStore"

const baseUrl = import.meta.env.VITE_VIPER_HOME
const router = createRouter({
    scrollBehavior: () => ({ left: 0, top: 0 }),
    history: createWebHistory(baseUrl),
    routes,
})

// In-flight latch: dedups concurrent navigations but resets after each attempt so later
// sessions (e.g. re-auth into an SIS role) can re-fetch instead of reusing a stale resolution.
let sisPermissionsPromise: Promise<void> | null = null

async function loadSisPermissions() {
    try {
        const userStore = useUserStore()
        const { get } = useFetch()
        const apiUrl = import.meta.env.VITE_API_URL
        const sisPerms = await get(`${apiUrl}loggedInUser/permissions?prefix=SVMSecure.SIS`)
        if (sisPerms.success && Array.isArray(sisPerms.result)) {
            const currentPermissions = userStore.userInfo?.permissions ?? []
            userStore.setPermissions([...currentPermissions, ...sisPerms.result])
        }
    } finally {
        sisPermissionsPromise = null
    }
}

router.beforeEach(async (to) => {
    const { requireLogin } = useRequireLogin(to)
    const loginResult = await requireLogin(true, "SVMSecure.Students")
    if (!loginResult) {
        return false
    }

    // SIS permissions are in a separate area, so they aren't loaded by requireLogin.
    // Emergency Contact routes grant access via SVMSecure.SIS.AllStudents.
    const userStore = useUserStore()
    const existingPermissions = userStore.userInfo?.permissions ?? []
    const hasSisPermissions = existingPermissions.some((p: string) => p.startsWith("SVMSecure.SIS"))
    if (!hasSisPermissions) {
        if (!sisPermissionsPromise) {
            sisPermissionsPromise = loadSisPermissions()
        }
        await sisPermissionsPromise
    }

    if (to.meta.permissions !== undefined) {
        const hasPerm = checkHasOnePermission(to.meta.permissions as string[])
        if (!hasPerm) {
            return { name: "StudentsHome" }
        }
    }
})

useRouteFocus(router)

export { router }
