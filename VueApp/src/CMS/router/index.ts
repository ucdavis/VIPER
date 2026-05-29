import { createSpaRouter } from "@/shared/createSpaRouter"
import { routes } from "./routes"
import { useRequireLogin } from "@/composables/RequireLogin"
import { checkHasOnePermission } from "@/composables/CheckPagePermission"

const router = createSpaRouter(routes)

router.beforeEach(async (to) => {
    const { requireLogin } = useRequireLogin(to)
    const loginResult = await requireLogin(true, "SVMSecure.CMS")
    if (loginResult !== undefined && !loginResult) {
        return false
    }
    if (to.meta.permissions !== undefined) {
        const hasPerm = checkHasOnePermission(to.meta.permissions as string[])
        if (!hasPerm) {
            return { name: "CmsAuth" }
        }
    }
})

export { router as cmsRouter }
