import { createPinia, setActivePinia } from "pinia"
import type { Router } from "vue-router"
import { useUserStore } from "@/store/UserStore"
import { createGuardedStubRouter } from "@/shared/__tests__/guarded-stub-router"
import { cmsGuard } from "@/CMS/router"
import { routes } from "@/CMS/router/routes"

// The real beforeEach guard calls requireLogin, which hits the network and needs a Quasar/inject
// context. Stub it (Vitest hoists this above the imports) so the test exercises only the
// permission-driven canonicalization, not the auth plumbing.
vi.mock("@/composables/RequireLogin", () => ({
    useRequireLogin: () => ({ requireLogin: () => Promise.resolve(true) }),
    getLoginUrl: () => ({ value: "" }),
}))

/** A fresh router and user store for a visitor holding these permissions. */
function routerFor(permissions: string[]): Router {
    setActivePinia(createPinia())
    useUserStore().setPermissions(permissions)
    return createGuardedStubRouter(routes, cmsGuard)
}

describe("cms router - area-root canonicalization", () => {
    it("redirects base SVMSecure.CMS users from /CMS/ to the Home hub", async () => {
        expect.hasAssertions()
        const router = routerFor(["SVMSecure.CMS"])

        await router.push("/CMS/")

        expect(router.currentRoute.value.name).toBe("CmsHome")
    })

    it("redirects granular-only users (no base SVMSecure.CMS) to the Home hub", async () => {
        expect.hasAssertions()
        // The regression: AllFiles/ManageNavigation/etc. users can enter the area but used to be
        // stranded on /CMS/ because canonicalization only checked the base permission.
        const router = routerFor(["SVMSecure.CMS.AllFiles"])

        await router.push("/CMS/")

        expect(router.currentRoute.value.name).toBe("CmsHome")
    })

    it("leaves visitors with no CMS permissions on the CmsAuth landing", async () => {
        expect.hasAssertions()
        const router = routerFor([])

        await router.push("/CMS/")

        expect(router.currentRoute.value.name).toBe("CmsAuth")
    })
})
