import { createSpaRouter } from "@/shared/createSpaRouter"
import { clinicalSchedulerRoutes as routes } from "./routes"
import { useRequireLogin } from "@/composables/RequireLogin"

const router = createSpaRouter(routes)

router.beforeEach((to) => {
    const { requireLogin } = useRequireLogin(to)
    return requireLogin(true, "SVMSecure.ClnSched")
})

export { router as clinicalSchedulerRouter }
