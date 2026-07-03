import { createSpaRouter } from "@/shared/create-spa-router"
import { routes } from "./routes"
import { useRequireLogin } from "@/composables/RequireLogin"

const router = createSpaRouter(routes)

router.beforeEach((to) => {
    const { requireLogin } = useRequireLogin(to)
    return requireLogin()
})

export { router }
