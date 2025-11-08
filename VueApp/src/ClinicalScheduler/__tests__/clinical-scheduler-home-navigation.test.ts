import { describe, it, expect, vi, beforeEach } from "vitest"
import { usePermissionsStore } from "../stores/permissions"
import ClinicalSchedulerHome from "../pages/ClinicalSchedulerHome.vue"
import { setupTest, createTestWrapper, spyOnRouterPush, createMockPermissionsStore } from "./test-utils"

// Mock the permissions store
vi.mock("../stores/permissions")

// Mock child components with inline definitions to avoid import hoisting issues
vi.mock("../components/AccessDeniedCard.vue", () => ({
    default: {
        name: "AccessDeniedCard",
        props: ["message", "subtitle"],
        template: '<div class="no-access-card"><div>Access Denied</div><div>{{message}}</div></div>',
    },
}))

describe("ClinicalSchedulerHome - Navigation", () => {
    let mockPermissionsStore: ReturnType<typeof createMockPermissionsStore> = {} as ReturnType<
        typeof createMockPermissionsStore
    >
    let router: any = {}

    beforeEach(() => {
        const { router: testRouter, mockStore } = setupTest()
        router = testRouter
        mockPermissionsStore = mockStore
        vi.mocked(usePermissionsStore).mockReturnValue(mockPermissionsStore)
    })

    describe("Click Navigation", () => {
        it("navigates to rotation view when rotation card is clicked", async () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasFullAccessPermission = true
            mockPermissionsStore.canAccessClinicianView = true
            mockPermissionsStore.canAccessRotationView = true

            const wrapper = createTestWrapper({
                component: ClinicalSchedulerHome,
                router,
            })
            const routerPush = spyOnRouterPush(router)

            // Find and click the rotation view card
            const rotationCard = wrapper.find(".view-card-custom")
            await rotationCard.trigger("click")

            expect(routerPush).toHaveBeenCalledWith("/ClinicalScheduler/rotation")
        })

        it("navigates to clinician view when clinician card is clicked", async () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasFullAccessPermission = true
            mockPermissionsStore.canAccessClinicianView = true
            mockPermissionsStore.canAccessRotationView = true

            const wrapper = createTestWrapper({
                component: ClinicalSchedulerHome,
                router,
            })
            const routerPush = spyOnRouterPush(router)

            // Find the clinician card (second view card)
            const viewCards = wrapper.findAll(".view-card-custom")
            const [, clinicianCard] = viewCards
            await clinicianCard.trigger("click")

            expect(routerPush).toHaveBeenCalledWith("/ClinicalScheduler/clinician")
        })

        it("does not show clinician card when user lacks access", () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = true
            mockPermissionsStore.canAccessClinicianView = false
            mockPermissionsStore.canAccessRotationView = true

            const wrapper = createTestWrapper({
                component: ClinicalSchedulerHome,
                router,
            })

            // Only rotation card should be visible, no clinician card
            const viewCards = wrapper.findAll(".view-card-custom")
            expect(viewCards).toHaveLength(1)
            expect(wrapper.text()).not.toContain("Schedule by Clinician")
        })
    })

    describe("Keyboard Navigation", () => {
        it("navigates on Enter key press", async () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasFullAccessPermission = true
            mockPermissionsStore.canAccessClinicianView = true
            mockPermissionsStore.canAccessRotationView = true

            const wrapper = createTestWrapper({
                component: ClinicalSchedulerHome,
                router,
            })
            const routerPush = spyOnRouterPush(router)

            const rotationCard = wrapper.find(".view-card-custom")
            await rotationCard.trigger("keydown.enter")

            expect(routerPush).toHaveBeenCalledWith("/ClinicalScheduler/rotation")
        })

        it("navigates on Space key press", async () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasFullAccessPermission = true
            mockPermissionsStore.canAccessClinicianView = true
            mockPermissionsStore.canAccessRotationView = true

            const wrapper = createTestWrapper({
                component: ClinicalSchedulerHome,
                router,
            })
            const routerPush = spyOnRouterPush(router)

            const rotationCard = wrapper.find(".view-card-custom")
            await rotationCard.trigger("keydown.space")

            expect(routerPush).toHaveBeenCalledWith("/ClinicalScheduler/rotation")
        })

        it("does not navigate on other key presses", async () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasFullAccessPermission = true
            mockPermissionsStore.canAccessClinicianView = true
            mockPermissionsStore.canAccessRotationView = true

            const wrapper = createTestWrapper({
                component: ClinicalSchedulerHome,
                router,
            })
            const routerPush = spyOnRouterPush(router)

            const rotationCard = wrapper.find(".view-card-custom")
            await rotationCard.trigger("keydown.escape")
            await rotationCard.trigger("keydown.tab")

            expect(routerPush).not.toHaveBeenCalled()
        })

        it("only shows rotation card for users without clinician access", () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = true
            mockPermissionsStore.canAccessClinicianView = false
            mockPermissionsStore.canAccessRotationView = true

            const wrapper = createTestWrapper({
                component: ClinicalSchedulerHome,
                router,
            })

            // Only rotation card should be visible
            const viewCards = wrapper.findAll(".view-card-custom")
            expect(viewCards).toHaveLength(1)
            expect(wrapper.text()).toContain("Schedule by Rotation")
            expect(wrapper.text()).not.toContain("Schedule by Clinician")
        })
    })

    describe("Navigation State", () => {
        it("maintains router state during navigation", async () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasFullAccessPermission = true
            mockPermissionsStore.canAccessClinicianView = true
            mockPermissionsStore.canAccessRotationView = true

            const wrapper = createTestWrapper({
                component: ClinicalSchedulerHome,
                router,
            })
            const routerPush = spyOnRouterPush(router)

            const rotationCard = wrapper.find(".view-card-custom")
            await rotationCard.trigger("click")

            // Verify the call was made with correct parameters
            expect(routerPush).toHaveBeenCalledTimes(1)
            expect(routerPush).toHaveBeenCalledWith("/ClinicalScheduler/rotation")
        })

        it("handles multiple rapid clicks gracefully", async () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasFullAccessPermission = true
            mockPermissionsStore.canAccessClinicianView = true
            mockPermissionsStore.canAccessRotationView = true

            const wrapper = createTestWrapper({
                component: ClinicalSchedulerHome,
                router,
            })
            const routerPush = spyOnRouterPush(router)

            const rotationCard = wrapper.find(".view-card-custom")
            await rotationCard.trigger("click")
            await rotationCard.trigger("click")
            await rotationCard.trigger("click")

            // Should handle multiple clicks (router.push may be called multiple times)
            expect(routerPush).toHaveBeenCalled()
        })
    })
})
