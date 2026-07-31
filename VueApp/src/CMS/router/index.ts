import { createSpaRouter } from "@/shared/create-spa-router"
import { routes, cmsHomePermissions } from "./routes"
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
    // CmsAuth landing (redirecting them would force a login or loop). Gate on
    // the same set as the CmsHome route so granular-only users (no base
    // SVMSecure.CMS) canonicalize too, instead of being stranded on /CMS/.
    if (to.name === "CmsAuth" && checkHasOnePermission(cmsHomePermissions)) {
        return { name: "CmsHome" }
    }
})

export { router as cmsRouter }
