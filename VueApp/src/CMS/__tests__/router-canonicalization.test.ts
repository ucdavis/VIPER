import { createPinia, setActivePinia } from "pinia"
import { useUserStore } from "@/store/UserStore"
import { cmsRouter } from "@/CMS/router"

// The real beforeEach guard calls requireLogin, which hits the network and needs a Quasar/inject
// context. Stub it (Vitest hoists this above the imports) so the test exercises only the
// permission-driven canonicalization, not the auth plumbing.
vi.mock("@/composables/RequireLogin", () => ({
    useRequireLogin: () => ({ requireLogin: () => Promise.resolve(true) }),
    getLoginUrl: () => ({ value: "" }),
}))

// Park on a neutral route first so the push to the area root is never a redundant navigation
// (which would resolve to a NavigationFailure and leave currentRoute unchanged).
async function goToAreaRoot(): Promise<void> {
    await cmsRouter.push("/__reset__")
    await cmsRouter.push("/CMS/")
}

describe("CMS area-root canonicalization", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
    })

    it("redirects base SVMSecure.CMS users from /CMS/ to the Home hub", async () => {
        useUserStore().setPermissions(["SVMSecure.CMS"])

        await goToAreaRoot()

        expect(cmsRouter.currentRoute.value.name).toBe("CmsHome")
    })

    it("redirects granular-only users (no base SVMSecure.CMS) to the Home hub", async () => {
        // The regression: AllFiles/ManageNavigation/etc. users can enter the area but used to be
        // stranded on /CMS/ because canonicalization only checked the base permission.
        useUserStore().setPermissions(["SVMSecure.CMS.AllFiles"])

        await goToAreaRoot()

        expect(cmsRouter.currentRoute.value.name).toBe("CmsHome")
    })

    it("leaves visitors with no CMS permissions on the CmsAuth landing", async () => {
        useUserStore().setPermissions([])

        await goToAreaRoot()

        expect(cmsRouter.currentRoute.value.name).toBe("CmsAuth")
    })
})
