import { createPinia, setActivePinia } from "pinia"
import { useUserStore } from "@/store/UserStore"
import { router } from "../router"

// The real beforeEach guard calls requireLogin, which hits the network and needs a Quasar/inject
// context. Stub it (Vitest hoists this above the imports) so the test exercises only the
// permission-driven redirect, not the auth plumbing.
vi.mock("@/composables/RequireLogin", () => ({
    useRequireLogin: () => ({ requireLogin: () => Promise.resolve(true) }),
    getLoginUrl: () => ({ value: "" }),
}))

// The guard also fetches SVMSecure.PhoneLists.* permissions on every non-internal navigation;
// stub it to resolve with no extra permissions so the test controls the permission set directly
// via the user store.
const mockGet = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({ get: (...args: unknown[]) => mockGet(...args) }),
}))

// Park on a neutral route first so the push to the target route is never a redundant
// navigation (which would resolve to a NavigationFailure and leave currentRoute unchanged).
async function goTo(path: string): Promise<void> {
    await router.push("/__reset__")
    await router.push(path)
}

function withPermissions(permissions: string[]) {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    mockGet.mockResolvedValue({ success: true, result: [] })
    useUserStore().setPermissions(permissions)
}

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
