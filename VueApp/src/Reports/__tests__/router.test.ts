import { createPinia, setActivePinia } from "pinia"
import type { Router } from "vue-router"
import { useUserStore } from "@/store/UserStore"
import { createGuardedStubRouter } from "@/shared/__tests__/guarded-stub-router"
import { reportsGuard } from "../router"
import { routes } from "../router/routes"

const { mockRequireLogin } = vi.hoisted(() => ({
    mockRequireLogin: vi.fn<(...args: unknown[]) => unknown>(),
}))
vi.mock("@/composables/RequireLogin", () => ({
    useRequireLogin: () => ({ requireLogin: (...args: unknown[]) => mockRequireLogin(...args) }),
    getLoginUrl: () => ({ value: "" }),
    createAuthenticator: (permission: string) => async () => {
        const loginResult = await mockRequireLogin(true, permission)
        return loginResult === null || loginResult
    },
}))

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

function routerFor(loginResult: boolean | null): Router {
    setActivePinia(createPinia())
    mockRequireLogin.mockResolvedValue(loginResult)
    return createGuardedStubRouter(routes, reportsGuard)
}

describe("reports routes", () => {
    it("signs the visitor in with the Report Builder's permissions", async () => {
        expect.hasAssertions()
        const router = routerFor(true)

        await router.push("/Reports/8a6f1d2e-0b7c-4a59-9d3e-1f2a3b4c5d6e")

        expect(mockRequireLogin).toHaveBeenCalledExactlyOnceWith(true, "SVMSecure.Reports")
        expect(router.currentRoute.value.name).toBe("Report")
    })

    it("opens the list of reports", async () => {
        expect.hasAssertions()
        const router = routerFor(null)

        await router.push("/Reports/")

        expect(router.currentRoute.value.name).toBe("ReportsHome")
    })

    it.each([
        "/Reports/Other",
        "/Reports/Admin/x",
        "/Reports/8a6f1d2e",
        "/Reports/8a6f1d2e-0b7c-4a59-9d3e-1f2a3b4c5d6e-x",
    ])("sends a path that isn't a report's slug to the 404 page (%s)", async (path) => {
        expect.hasAssertions()
        const router = routerFor(true)

        await router.push(path)

        expect(router.currentRoute.value.matched[0]?.path).toBe("/:catchAll(.*)*")
    })

    it.each([
        { path: "/Reports/Admin/4", name: "ReportAdminEdit" },
        { path: "/Reports/Admin/Problems", name: "ReportAdminProblems" },
    ])("lets admins into $path", async ({ path, name }) => {
        expect.hasAssertions()
        const router = routerFor(true)
        useUserStore().setPermissions(["SVMSecure.Reports.Admin"])

        await router.push(path)

        expect(router.currentRoute.value.name).toBe(name)
    })

    it.each(["/Reports/Admin", "/Reports/Admin/New", "/Reports/Admin/4", "/Reports/Admin/Problems"])(
        "sends others from %s to the list of reports",
        async (path) => {
            expect.hasAssertions()
            const router = routerFor(true)

            await router.push(path)

            expect(router.currentRoute.value.name).toBe("ReportsHome")
        },
    )

    it("stays put when sign-in is refused", async () => {
        expect.hasAssertions()
        const router = routerFor(false)

        await router.push("/Reports/")

        expect(router.currentRoute.value.matched).toHaveLength(0)
    })

    it("doesn't sign in again when moving within the app", async () => {
        expect.hasAssertions()
        const router = routerFor(true)
        await router.push("/Reports/")
        signIn()

        await router.push("/Reports/8a6f1d2e-0b7c-4a59-9d3e-1f2a3b4c5d6e")

        expect(mockRequireLogin).toHaveBeenCalledOnce()
    })
})
