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

router.beforeEach(async (to) => {
    const { requireLogin } = useRequireLogin(to)
    const loginResult = await requireLogin(true, "SVMSecure.Effort")
    if (loginResult !== null && !loginResult) {
        return false
    }

    // Also load Eval permissions (needed for evaluation data display)
    const { get } = useFetch()
    const apiUrl = import.meta.env.VITE_API_URL
    const evalPerms = await get(`${apiUrl}loggedInUser/permissions?prefix=SVMSecure.Eval`)
    if (evalPerms.success && Array.isArray(evalPerms.result)) {
        const userStore = useUserStore()
        userStore.setPermissions([...userStore.userInfo.permissions, ...evalPerms.result])
    }

    if (to.meta.permissions !== null && to.meta.permissions !== undefined) {
        const hasPerm = checkHasOnePermission(to.meta.permissions as string[])
        if (!hasPerm) {
            return { name: "EffortHome" }
        }
    }
})

export { router as effortRouter }
