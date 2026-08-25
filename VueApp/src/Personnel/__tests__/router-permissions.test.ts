import { createPinia, setActivePinia } from "pinia"
import { useUserStore } from "@/store/UserStore"
import { router } from "../router"

// The real beforeEach guard calls requireLogin, which hits the network and needs a Quasar/inject
// context. Stub it (Vitest hoists this above the imports) so the test exercises only the
// permission-driven redirect, not the auth plumbing. The spy is kept so the tests below can
// assert which permission prefix the guard asks for — that single call is the only thing that
// populates the permission set the route gate then reads.
const { mockRequireLogin } = vi.hoisted(() => ({
    mockRequireLogin: vi.fn<(...args: unknown[]) => unknown>(),
}))
vi.mock("@/composables/RequireLogin", () => ({
    useRequireLogin: () => ({ requireLogin: (...args: unknown[]) => mockRequireLogin(...args) }),
    getLoginUrl: () => ({ value: "" }),
}))

// Park on a neutral route first so the push to the target route is never a redundant
// navigation (which would resolve to a NavigationFailure and leave currentRoute unchanged).
async function goTo(path: string): Promise<void> {
    await router.push("/__reset__")
    await router.push(path)
}

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

function withPermissions(permissions: string[]) {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    mockRequireLogin.mockResolvedValue(true)
    useUserStore().setPermissions(permissions)
}

describe("personnel router - permission loading", () => {
    it("asks for the phone-list permissions the routes actually gate on", async () => {
        expect.hasAssertions()
        // SVMSecure.PhoneLists.* is the only permission set anything in this SPA reads, so the
        // guard requests it directly. Asking for the area's own SVMSecure.Personnel prefix
        // instead would leave the route gate below with nothing to match and need a second
        // request to repair it.
        withPermissions([])

        await goTo("/Personnel/PhoneList/VMDO")

        expect(mockRequireLogin).toHaveBeenCalledWith(true, "SVMSecure.PhoneLists")
    })

    it("loads permissions in a single request per external navigation", async () => {
        expect.hasAssertions()
        // Only requireLogin populates permissions now. A second call here would mean the guard
        // had gone back to topping the set up with its own extra fetch.
        withPermissions([])
        await router.push("/__reset__")
        mockRequireLogin.mockClear()

        await router.push("/Personnel/PhoneList/VMDO")

        expect(mockRequireLogin).toHaveBeenCalledExactlyOnceWith(true, "SVMSecure.PhoneLists")
    })

    it("skips re-authentication once the user is already logged in and navigating in-app", async () => {
        expect.hasAssertions()
        // Re-calling requireLogin on a tab switch would overwrite the permission array and
        // flash the page, so an in-app navigation must not reach it again.
        withPermissions(["SVMSecure.PhoneLists.SVMMaintain"])
        await goTo("/Personnel/PhoneList/VMDO")
        signIn()
        mockRequireLogin.mockClear()

        await router.push("/Personnel/SVMPhonesMaintain")

        expect(mockRequireLogin).not.toHaveBeenCalled()
    })

    it("abandons the navigation when login fails", async () => {
        expect.hasAssertions()
        withPermissions([])
        mockRequireLogin.mockResolvedValue(false)

        await goTo("/Personnel/PhoneList/VMDO")

        expect(router.currentRoute.value.path).not.toBe("/Personnel/PhoneList/VMDO")
    })
})

describe("personnel router - permission gating", () => {
    it("redirects a caller without SVMMaintain away from SVMPhonesMaintain", async () => {
        expect.hasAssertions()
        withPermissions([])

        await goTo("/Personnel/SVMPhonesMaintain")

        expect(router.currentRoute.value.name).toBe("PersonnelHome")
    })

    it("allows a caller with SVMMaintain onto SVMPhonesMaintain", async () => {
        expect.hasAssertions()
        withPermissions(["SVMSecure.PhoneLists.SVMMaintain"])

        await goTo("/Personnel/SVMPhonesMaintain")

        expect(router.currentRoute.value.name).toBe("MaintainSchoolwidePhones")
    })

    it("allows navigation to unrestricted routes regardless of phone-list permissions", async () => {
        expect.hasAssertions()
        withPermissions([])

        await goTo("/Personnel/PhoneList/VMDO")

        expect(router.currentRoute.value.name).toBe("PhoneList")
    })

    it("routes any list code through the one generic phone-list page", async () => {
        expect.hasAssertions()
        withPermissions([])

        await goTo("/Personnel/PhoneList/SOMEOTHER")

        expect(router.currentRoute.value.name).toBe("PhoneList")
        expect(router.currentRoute.value.params.code).toBe("SOMEOTHER")
    })

    it("lets the maintain route resolve without a static permission", async () => {
        expect.hasAssertions()
        // The required role is the list's own MaintainRole, so it cannot be known before the
        // list is fetched. The page redirects on canMaintain=false and the API rejects writes;
        // the router deliberately does not gate here.
        withPermissions([])

        await goTo("/Personnel/PhoneList/VMDO/Maintain")

        expect(router.currentRoute.value.name).toBe("MaintainPhoneList")
    })

    it("redirects the pre-Code VMDO paths so published links keep working", async () => {
        expect.hasAssertions()
        withPermissions([])

        await goTo("/Personnel/VMDOPhones")

        expect(router.currentRoute.value.name).toBe("PhoneList")
        expect(router.currentRoute.value.params.code).toBe("VMDO")

        await goTo("/Personnel/VMDOPhonesMaintain")

        expect(router.currentRoute.value.name).toBe("MaintainPhoneList")
        expect(router.currentRoute.value.params.code).toBe("VMDO")
    })
})
