import { describe, it, expect, vi, beforeEach } from "vitest"
import { usePermissionsStore } from "../stores/permissions"
import ClinicalSchedulerHome from "../pages/ClinicalSchedulerHome.vue"
import { setupTest, createTestWrapper, createMockPermissionsStore } from "./test-utils"

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

describe("ClinicalSchedulerHome - Display Content", () => {
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

    describe("Content Display", () => {
        it("shows view selection when user has permissions", () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasFullAccessPermission = true

            const wrapper = createTestWrapper({
                component: ClinicalSchedulerHome,
                router,
            })

            expect(wrapper.text()).toContain("Select Scheduling View")
        })

        it("displays appropriate view descriptions", () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasFullAccessPermission = true
            mockPermissionsStore.canAccessClinicianView = true
            mockPermissionsStore.canAccessRotationView = true

            const wrapper = createTestWrapper({
                component: ClinicalSchedulerHome,
                router,
            })

            expect(wrapper.text()).toContain("Schedule clinicians for a specific rotation")
            expect(wrapper.text()).toContain("Schedule rotations for a specific clinician")
        })
    })
})
