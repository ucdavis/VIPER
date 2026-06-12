import { createSpaRouter } from "@/shared/create-spa-router"
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
    // Canonicalize the area root for CMS users so the left nav "Home" link
    // highlights; unauthenticated and permission-less visitors stay on the
    // CmsAuth landing (redirecting them would force a login or loop).
    if (to.name === "CmsAuth" && checkHasOnePermission(["SVMSecure.CMS"])) {
        return { name: "CmsHome" }
    }
})

export { router as cmsRouter }
