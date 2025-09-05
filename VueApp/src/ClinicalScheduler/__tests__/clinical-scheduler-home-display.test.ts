import { describe, it, expect, vi, beforeEach } from "vitest"
import { computed } from "vue"
import { usePermissionsStore } from "../stores/permissions"
import ClinicalSchedulerHome from "../pages/ClinicalSchedulerHome.vue"
import { setupTest, createTestWrapper, createMockPermissionsStore } from "./test-utils"

// Mock the permissions store
vi.mock("../stores/permissions")

// Mock child components with inline definitions to avoid import hoisting issues
vi.mock("../components/PermissionInfoBanner.vue", () => ({
    default: {
        name: "PermissionInfoBanner",
        template: `<div v-if="shouldShow">Permission Info Banner</div>`,
        setup() {
            const permissionsStore = usePermissionsStore()
            const shouldShow = computed(
                () =>
                    permissionsStore.hasOnlyServiceSpecificPermissions || permissionsStore.hasOnlyOwnSchedulePermission,
            )
            return { shouldShow }
        },
    },
}))
vi.mock("../components/AccessDeniedCard.vue", () => ({
    default: {
        name: "AccessDeniedCard",
        props: ["message", "subtitle"],
        template: '<div class="no-access-card"><div>Access Denied</div><div>{{message}}</div></div>',
    },
}))

describe("ClinicalSchedulerHome - Display & Layout", () => {
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

    describe("View Card Visibility", () => {
        it("shows both views for full access users", () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasFullAccessPermission = true
            mockPermissionsStore.canAccessClinicianView = true
            mockPermissionsStore.canAccessRotationView = true

            const wrapper = createTestWrapper({
                component: ClinicalSchedulerHome,
                router,
            })

            expect(wrapper.text()).toContain("Schedule by Rotation")
            expect(wrapper.text()).toContain("Schedule by Clinician")

            // Should show both view cards, not the disabled explanation card
            const viewCards = wrapper.findAll(".view-card-custom")
            expect(viewCards).toHaveLength(2)
        })

        it("shows only rotation view for service-specific users", () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = true
            mockPermissionsStore.canAccessClinicianView = false
            mockPermissionsStore.canAccessRotationView = true

            const wrapper = createTestWrapper({
                component: ClinicalSchedulerHome,
                router,
            })

            // Should show rotation view
            expect(wrapper.text()).toContain("Schedule by Rotation")
            expect(wrapper.text()).toContain("Limited to your authorized rotations")

            // Should show disabled clinician view with explanation
            expect(wrapper.text()).toContain("Schedule by Clinician")
            expect(wrapper.text()).toContain("Not available with rotation-specific permissions")
            expect(wrapper.find("[disable]").exists()).toBeTruthy()
        })

        it("shows only clinician view for own schedule users", () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasOnlyOwnSchedulePermission = true
            mockPermissionsStore.canAccessClinicianView = true

            const wrapper = createTestWrapper({
                component: ClinicalSchedulerHome,
                router,
            })

            // Should show clinician view
            expect(wrapper.text()).toContain("Schedule by Clinician")
            expect(wrapper.text()).toContain("Your schedule only")

            // Should NOT show rotation view (own schedule users can't access rotation view)
            // Since canAccessRotationView is false for own schedule users, the rotation card won't be rendered
            expect(wrapper.text()).not.toContain("Schedule by Rotation")
        })
    })

    describe("Layout Adjustments", () => {
        it("renders one view card when only one view is available", () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasOnlyOwnSchedulePermission = true
            mockPermissionsStore.canAccessClinicianView = true

            const wrapper = createTestWrapper({
                component: ClinicalSchedulerHome,
                router,
            })

            // Only one view card should be visible
            const viewCards = wrapper.findAll(".view-card-custom")
            expect(viewCards).toHaveLength(1)
        })

        it("renders two view cards when both views are available", () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasFullAccessPermission = true
            mockPermissionsStore.canAccessClinicianView = true
            mockPermissionsStore.canAccessRotationView = true

            const wrapper = createTestWrapper({
                component: ClinicalSchedulerHome,
                router,
            })

            // Both view cards should be visible
            const viewCards = wrapper.findAll(".view-card-custom")
            expect(viewCards.length).toBeGreaterThanOrEqual(2)
        })
    })

    describe("Permission Indicators", () => {
        it("displays permission info banner for limited users", () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = true

            const wrapper = createTestWrapper({
                component: ClinicalSchedulerHome,
                router,
            })

            const permissionBanner = wrapper.findComponent({ name: "PermissionInfoBanner" })
            expect(permissionBanner.exists()).toBeTruthy()
        })

        it("hides permission banner for full access users", () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasFullAccessPermission = true
            // Ensure the limited permission flags are false to hide the banner
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = false
            mockPermissionsStore.hasOnlyOwnSchedulePermission = false

            const wrapper = createTestWrapper({
                component: ClinicalSchedulerHome,
                router,
            })

            // Check that the banner text is not present in the DOM for full access users
            expect(wrapper.text()).not.toContain("Permission Info Banner")
        })
    })

    describe("Content Visibility", () => {
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
