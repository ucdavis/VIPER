import { createRouter, createWebHistory } from "vue-router"
import { routes } from "./routes"
import { useRequireLogin } from "@/composables/RequireLogin"

const baseUrl = import.meta.env.VITE_VIPER_HOME
const router = createRouter({
    scrollBehavior: () => ({ left: 0, top: 0 }),
    history: createWebHistory(baseUrl),
    routes,
})

router.beforeEach((to) => {
    const { requireLogin } = useRequireLogin(to)
    return requireLogin()
})

export { router }
