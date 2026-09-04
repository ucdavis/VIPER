import { createPinia, setActivePinia } from "pinia"
import { START_LOCATION } from "vue-router"
import type { Router } from "vue-router"
import { useUserStore } from "@/store/UserStore"
import { createGuardedStubRouter } from "@/shared/__tests__/guarded-stub-router"
import { personnelGuard } from "../router"
import { routes } from "../router/routes"

// The real beforeEach guard calls requireLogin, which hits the network and needs a Quasar/inject
// context. Stub it (Vitest hoists this above the imports) so the test exercises only the
// permission-driven redirect, not the auth plumbing. The spy is kept so the tests below can
// assert which permission prefix the guard asks for: that single call is the only thing that
// populates the permission set the route gate then reads.
const { mockRequireLogin } = vi.hoisted(() => ({
    mockRequireLogin: vi.fn<(...args: unknown[]) => unknown>(),
}))
vi.mock("@/composables/RequireLogin", () => ({
    useRequireLogin: () => ({ requireLogin: (...args: unknown[]) => mockRequireLogin(...args) }),
    getLoginUrl: () => ({ value: "" }),
}))

/**
 * Signs the caller in as far as the guard is concerned. isLoggedIn reads loginId, so that is the
 * field that has to be set - a user object without it leaves the store logged out.
 */
function signIn() {
    useUserStore().loadUser({
        firstName: "Test",
        lastName: "Caller",
        mailId: "caller",
        loginId: "caller",
        mothraId: "caller01",
        userId: 1,
        token: "",
        emulating: false,
        permissions: [],
    })
}

/** A fresh router, user store, and login spy for a visitor holding these permissions. */
function routerFor(permissions: string[]): Router {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    mockRequireLogin.mockResolvedValue(true)
    useUserStore().setPermissions(permissions)
    return createGuardedStubRouter(routes, personnelGuard)
}

describe("personnel router - permission loading", () => {
    it("asks for the phone-list permissions in a single request when entering from outside", async () => {
        expect.hasAssertions()
        // SVMSecure.PhoneLists.* is the only permission set anything in this SPA reads, so the
        // guard requests it directly. Asking for the area's own SVMSecure.Personnel prefix
        // instead would leave the route gate below with nothing to match and need a second
        // request to repair it. A second call would mean the guard had gone back to topping the
        // set up with its own extra fetch.
        const router = routerFor([])

        await router.push("/Personnel/PhoneList/VMDO")

        expect(mockRequireLogin).toHaveBeenCalledExactlyOnceWith(true, "SVMSecure.PhoneLists")
    })

    it("authenticates a navigation from outside even when the store already says signed in", async () => {
        expect.hasAssertions()
        // Only an in-app navigation may trust the store's session; entering the app re-validates
        // whatever the store carried in.
        const router = routerFor([])
        signIn()

        await router.push("/Personnel/PhoneList/VMDO")

        expect(mockRequireLogin).toHaveBeenCalledOnce()
    })

    it("skips re-authentication once the user is already logged in and navigating in-app", async () => {
        expect.hasAssertions()
        // Re-calling requireLogin on a tab switch would overwrite the permission array and
        // flash the page, so an in-app navigation must not reach it again.
        const router = routerFor(["SVMSecure.PhoneLists.SVMMaintain"])
        await router.push("/Personnel/PhoneList/VMDO")
        signIn()
        mockRequireLogin.mockClear()

        await router.push("/Personnel/SVMPhonesMaintain")

        expect(mockRequireLogin).not.toHaveBeenCalled()
    })

    it("abandons the navigation when login fails", async () => {
        expect.hasAssertions()
        const router = routerFor([])
        mockRequireLogin.mockResolvedValue(false)

        await router.push("/Personnel/PhoneList/VMDO")

        expect(router.currentRoute.value).toBe(START_LOCATION)
    })
})

describe("personnel router - permission gating", () => {
    it("redirects a caller without SVMMaintain away from SVMPhonesMaintain", async () => {
        expect.hasAssertions()
        const router = routerFor([])

        await router.push("/Personnel/SVMPhonesMaintain")

        expect(router.currentRoute.value.name).toBe("PersonnelHome")
    })

    it("allows a caller with SVMMaintain onto SVMPhonesMaintain", async () => {
        expect.hasAssertions()
        const router = routerFor(["SVMSecure.PhoneLists.SVMMaintain"])

        await router.push("/Personnel/SVMPhonesMaintain")

        expect(router.currentRoute.value.name).toBe("MaintainSchoolwidePhones")
    })

    it("allows navigation to unrestricted routes regardless of phone-list permissions", async () => {
        expect.hasAssertions()
        const router = routerFor([])

        await router.push("/Personnel/PhoneList/VMDO")

        expect(router.currentRoute.value.name).toBe("PhoneList")
    })

    it("routes any list code through the one generic phone-list page", async () => {
        expect.hasAssertions()
        const router = routerFor([])

        await router.push("/Personnel/PhoneList/SOMEOTHER")

        expect(router.currentRoute.value.name).toBe("PhoneList")
        expect(router.currentRoute.value.params.code).toBe("SOMEOTHER")
    })

    it("lets the maintain route resolve without a static permission", async () => {
        expect.hasAssertions()
        // The required role is the list's own MaintainRole, so it cannot be known before the
        // list is fetched. The page redirects on canMaintain=false and the API rejects writes;
        // the router deliberately does not gate here.
        const router = routerFor([])

        await router.push("/Personnel/PhoneList/VMDO/Maintain")

        expect(router.currentRoute.value.name).toBe("MaintainPhoneList")
    })

    it("redirects the pre-Code VMDO paths so published links keep working", async () => {
        expect.hasAssertions()
        const router = routerFor([])

        await router.push("/Personnel/VMDOPhones")

        expect(router.currentRoute.value.name).toBe("PhoneList")
        expect(router.currentRoute.value.params.code).toBe("VMDO")

        await router.push("/Personnel/VMDOPhonesMaintain")

        expect(router.currentRoute.value.name).toBe("MaintainPhoneList")
        expect(router.currentRoute.value.params.code).toBe("VMDO")
    })
})
