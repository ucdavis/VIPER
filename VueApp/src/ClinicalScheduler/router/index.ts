import { createRouter, createWebHistory } from "vue-router"
import { clinicalSchedulerRoutes as routes } from "./routes"
import { useRequireLogin } from "@/composables/RequireLogin"
import { useRouteFocus } from "@/composables/use-route-focus"

const baseUrl = import.meta.env.VITE_VIPER_HOME
const router = createRouter({
    scrollBehavior: () => ({ left: 0, top: 0 }),
    history: createWebHistory(baseUrl),
    routes,
})

router.beforeEach((to) => {
    const { requireLogin } = useRequireLogin(to)
    return requireLogin(true, "SVMSecure.ClnSched")
})

useRouteFocus(router)

export { router as clinicalSchedulerRouter }
