import { setActivePinia, createPinia } from "pinia"
import { useUserStore } from "@/store/UserStore"
import { ensurePermissions } from "../router/ensure-permissions"

/**
 * Tests for ensurePermissions: the router's top-up of permissions from areas outside
 * requireLogin, such as SIS for emergency contacts and CareerSelection for career selection.
 */

const PREFIX = "SVMSecure.CareerSelection"

const mockGet = vi.fn<(...args: unknown[]) => unknown>()

vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({ get: (...args: unknown[]) => mockGet(...args) }),
}))

/** Starts a session holding the given permissions, as a fresh navigation would. */
function setUser(permissions: string[]): void {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    useUserStore().setPermissions(permissions)
}

describe("ensure permissions", () => {
    it("fetches the area's permissions and keeps the ones already held", async () => {
        expect.hasAssertions()
        setUser(["SVMSecure.Students"])
        mockGet.mockResolvedValue({ success: true, result: [`${PREFIX}.Admin`] })

        await ensurePermissions(PREFIX)

        expect(mockGet).toHaveBeenCalledWith(expect.stringContaining(`permissions?prefix=${PREFIX}`))
        expect(useUserStore().userInfo.permissions).toStrictEqual(["SVMSecure.Students", `${PREFIX}.Admin`])
    })

    it("does not fetch when the area's permissions are already loaded", async () => {
        expect.hasAssertions()
        setUser([`${PREFIX}.ReadOnly`])

        await ensurePermissions(PREFIX)

        expect(mockGet).not.toHaveBeenCalled()
    })

    it("leaves the permissions alone when the request fails", async () => {
        expect.hasAssertions()
        setUser(["SVMSecure.Students"])
        mockGet.mockResolvedValue({ success: false, result: null })

        await ensurePermissions(PREFIX)

        expect(useUserStore().userInfo.permissions).toStrictEqual(["SVMSecure.Students"])
    })

    it("shares one request between concurrent navigations", async () => {
        expect.hasAssertions()
        setUser([])
        let resolvePending!: (value: unknown) => void
        mockGet.mockReturnValue(new Promise((resolve) => (resolvePending = resolve)))

        const first = ensurePermissions(PREFIX)
        const second = ensurePermissions(PREFIX)
        resolvePending({ success: true, result: [`${PREFIX}.Admin`] })
        await Promise.all([first, second])

        expect(mockGet).toHaveBeenCalledOnce()
    })

    it("fetches again after a failed attempt rather than reusing it", async () => {
        expect.hasAssertions()
        // The latch resets after each attempt so a later re-auth can pick up new roles.
        setUser([])
        mockGet.mockResolvedValue({ success: false, result: null })

        await ensurePermissions(PREFIX)
        await ensurePermissions(PREFIX)

        expect(mockGet).toHaveBeenCalledTimes(2)
    })

    it("keeps both areas' permissions when they load at the same time", async () => {
        expect.hasAssertions()
        // The router fetches the two areas together, so each has to merge into what is already
        // held rather than replace it, whichever order they come back in.
        setUser(["SVMSecure.Students"])
        const byPrefix: Record<string, string[]> = {
            "SVMSecure.SIS": ["SVMSecure.SIS.AllStudents"],
            [PREFIX]: [`${PREFIX}.Admin`],
        }
        mockGet.mockImplementation((url: unknown) =>
            Promise.resolve({ success: true, result: byPrefix[String(url).split("prefix=")[1]] }),
        )

        await Promise.all([ensurePermissions("SVMSecure.SIS"), ensurePermissions(PREFIX)])

        expect(useUserStore().userInfo.permissions).toStrictEqual([
            "SVMSecure.Students",
            "SVMSecure.SIS.AllStudents",
            `${PREFIX}.Admin`,
        ])
    })

    it("does not add a permission the user already holds", async () => {
        expect.hasAssertions()
        setUser(["SVMSecure.Students"])
        mockGet.mockResolvedValue({ success: true, result: ["SVMSecure.Students", `${PREFIX}.Admin`] })

        await ensurePermissions(PREFIX)

        expect(useUserStore().userInfo.permissions).toStrictEqual(["SVMSecure.Students", `${PREFIX}.Admin`])
    })

    it("keeps each area's fetch separate", async () => {
        expect.hasAssertions()
        setUser([])
        mockGet.mockResolvedValue({ success: true, result: [] })

        await ensurePermissions(PREFIX)
        await ensurePermissions("SVMSecure.SIS")

        expect(mockGet).toHaveBeenNthCalledWith(1, expect.stringContaining(PREFIX))
        expect(mockGet).toHaveBeenNthCalledWith(2, expect.stringContaining("SVMSecure.SIS"))
    })
})
