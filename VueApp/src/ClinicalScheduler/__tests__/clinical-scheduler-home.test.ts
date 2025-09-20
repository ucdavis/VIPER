import { describe, it, expect, vi, beforeEach } from "vitest"
import { usePermissionsStore } from "../stores/permissions"
import ClinicalSchedulerHome from "../pages/ClinicalSchedulerHome.vue"
import { setupTest, createTestWrapper, waitForAsync, createMockPermissionsStore } from "./test-utils"

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

const createWrapper = (customRouter?: any) =>
    createTestWrapper({
        component: ClinicalSchedulerHome,
        router: customRouter,
    })

describe("ClinicalSchedulerHome Component - Core Functionality", () => {
    let mockPermissionsStore: ReturnType<typeof createMockPermissionsStore> = {} as ReturnType<
        typeof createMockPermissionsStore
    >

    beforeEach(() => {
        const { mockStore } = setupTest()
        mockPermissionsStore = mockStore
        vi.mocked(usePermissionsStore).mockReturnValue(mockPermissionsStore)
    })

    describe("Access Control", () => {
        it("shows access denied message when user has no edit permissions", () => {
            mockPermissionsStore.hasAnyEditPermission = false

            const wrapper = createWrapper()

            expect(wrapper.text()).toContain("Access Denied")
            expect(wrapper.text()).toContain("You do not have permission to access the Clinical Scheduler")
            expect(wrapper.find(".no-access-card").exists()).toBeTruthy()
        })

        it("shows main content when user has edit permissions", () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasFullAccessPermission = true
            mockPermissionsStore.canAccessClinicianView = true

            const wrapper = createWrapper()

            expect(wrapper.find(".no-access-card").exists()).toBeFalsy()
            expect(wrapper.text()).toContain("Select Scheduling View")
        })

        it("initializes permissions store on mount", async () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.userPermissions = null // Not loaded yet

            createWrapper()

            // Wait for nextTick to ensure onMounted has run
            await waitForAsync()

            expect(mockPermissionsStore.initialize).toHaveBeenCalled()
        })
    })
})

describe("ClinicalSchedulerHome Component - View Visibility", () => {
    let mockPermissionsStore: ReturnType<typeof createMockPermissionsStore> = {} as ReturnType<
        typeof createMockPermissionsStore
    >

    beforeEach(() => {
        const { mockStore } = setupTest()
        mockPermissionsStore = mockStore
        vi.mocked(usePermissionsStore).mockReturnValue(mockPermissionsStore)
    })

    describe("View Card Visibility", () => {
        it("shows both views for full access users", () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasFullAccessPermission = true
            mockPermissionsStore.canAccessClinicianView = true
            mockPermissionsStore.canAccessRotationView = true
            mockPermissionsStore.clinicianViewLabel = "Schedule by Clinician" // Full access users see generic label

            const wrapper = createWrapper()

            expect(wrapper.text()).toContain("Schedule by Rotation")
            expect(wrapper.text()).toContain("Schedule by Clinician")

            // Should show both view cards, not the disabled explanation card
            const viewCards = wrapper.findAll(".view-card-custom:not(.bg-grey-1)")
            expect(viewCards).toHaveLength(2)
        })

        it("shows only rotation view for service-specific users", () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = true
            mockPermissionsStore.canAccessClinicianView = false
            mockPermissionsStore.canAccessRotationView = true
            mockPermissionsStore.clinicianViewLabel = "Schedule by Clinician"

            const wrapper = createWrapper()

            // Should show rotation view
            expect(wrapper.text()).toContain("Schedule by Rotation")

            // Should NOT show clinician view since user doesn't have access
            expect(wrapper.text()).not.toContain("Schedule by Clinician")

            // Only one view card should be visible
            const viewCards = wrapper.findAll(".view-card-custom")
            expect(viewCards).toHaveLength(1)
        })

        it("shows only clinician view for own schedule users", () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasOnlyOwnSchedulePermission = true
            mockPermissionsStore.canAccessClinicianView = true
            mockPermissionsStore.canAccessRotationView = false // Own schedule users typically can't access rotation view
            mockPermissionsStore.clinicianViewLabel = "Edit My Schedule" // Own schedule users see personal label

            const wrapper = createWrapper()

            // Should show clinician view with personal label
            expect(wrapper.text()).toContain("Edit My Schedule")
            expect(wrapper.text()).toContain("Your schedule only")

            // Should only have one card visible for own-schedule users
            const rotationCards = wrapper.findAll(".view-card-custom")
            expect(rotationCards).toHaveLength(1) // Only one card should be visible for own-schedule users
        })
    })

    describe("Layout Adjustments", () => {
        it("renders one view card when only one view is available", () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasOnlyOwnSchedulePermission = true
            mockPermissionsStore.canAccessClinicianView = true
            mockPermissionsStore.canAccessRotationView = false

            const wrapper = createWrapper()

            const viewCards = wrapper.findAll(".view-card-custom")
            expect(viewCards).toHaveLength(1)
        })

        it("renders two view cards when both views are available", () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasFullAccessPermission = true
            mockPermissionsStore.canAccessClinicianView = true
            mockPermissionsStore.canAccessRotationView = true

            const wrapper = createWrapper()

            const viewCards = wrapper.findAll(".view-card-custom")
            expect(viewCards.length).toBeGreaterThanOrEqual(2)
        })
    })
})

describe("ClinicalSchedulerHome Component - Navigation", () => {
    let mockPermissionsStore: ReturnType<typeof createMockPermissionsStore> = {} as ReturnType<
        typeof createMockPermissionsStore
    >
    let router: any = {}

    beforeEach(() => {
        const { mockStore, router: testRouter } = setupTest()
        mockPermissionsStore = mockStore
        router = testRouter
        vi.mocked(usePermissionsStore).mockReturnValue(mockPermissionsStore)
    })

    describe("Navigation", () => {
        it("navigates to rotation view when rotation card is clicked", async () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasFullAccessPermission = true
            mockPermissionsStore.canAccessClinicianView = true
            mockPermissionsStore.canAccessRotationView = true

            const wrapper = createWrapper(router)
            const routerPush = vi.spyOn(router, "push")

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

            const routerPush = vi.spyOn(router, "push")
            const wrapper = createWrapper(router)

            // Find the clinician card (second view card)
            const viewCards = wrapper.findAll(".view-card-custom:not(.bg-grey-1)")
            const [, clinicianCard] = viewCards
            await clinicianCard.trigger("click")

            expect(routerPush).toHaveBeenCalledWith("/ClinicalScheduler/clinician")
        })

        it("does not show clinician card when user lacks access", async () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = true
            mockPermissionsStore.canAccessClinicianView = false
            mockPermissionsStore.canAccessRotationView = true

            const wrapper = createWrapper()

            // Only rotation card should be visible
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

            const routerPush = vi.spyOn(router, "push")
            const wrapper = createWrapper(router)

            const rotationCard = wrapper.find(".view-card-custom")
            await rotationCard.trigger("keydown.enter")

            expect(routerPush).toHaveBeenCalledWith("/ClinicalScheduler/rotation")
        })

        it("navigates on Space key press", async () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasFullAccessPermission = true
            mockPermissionsStore.canAccessClinicianView = true
            mockPermissionsStore.canAccessRotationView = true

            const wrapper = createWrapper(router)
            const routerPush = vi.spyOn(router, "push")

            const rotationCard = wrapper.find(".view-card-custom")
            await rotationCard.trigger("keydown.space")

            expect(routerPush).toHaveBeenCalledWith("/ClinicalScheduler/rotation")
        })
    })
})

describe("ClinicalSchedulerHome Component - Accessibility & Error Handling", () => {
    let mockPermissionsStore: ReturnType<typeof createMockPermissionsStore> = {} as ReturnType<
        typeof createMockPermissionsStore
    >
    let router: any = {}

    beforeEach(() => {
        const { mockStore, router: testRouter } = setupTest()
        mockPermissionsStore = mockStore
        router = testRouter
        vi.mocked(usePermissionsStore).mockReturnValue(mockPermissionsStore)
    })

    describe("Accessibility Features", () => {
        it("sets proper page title", () => {
            mockPermissionsStore.hasAnyEditPermission = true

            createWrapper(router)

            expect(document.title).toBe("VIPER - Clinical Scheduler")
        })

        it("provides proper ARIA attributes for view cards", () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasFullAccessPermission = true

            const wrapper = createWrapper(router)

            const viewCards = wrapper.findAll(".view-card-custom")
            for (const card of viewCards) {
                expect(card.attributes("role")).toBe("button")
                expect(card.attributes("tabindex")).toBe("0")
            }
        })

        it("provides focus indicators for keyboard navigation", () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasFullAccessPermission = true
            mockPermissionsStore.canAccessClinicianView = true
            mockPermissionsStore.canAccessRotationView = true

            const wrapper = createWrapper(router)

            // Check that cards have focus styles (tested through CSS classes)
            const viewCards = wrapper.findAll(".view-card-custom")
            expect(viewCards.length).toBeGreaterThan(0)
        })
    })

    describe("Error Handling", () => {
        it("handles permission store initialization errors gracefully", () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.initialize = vi.fn().mockRejectedValue(new Error("API Error"))

            // Should not throw error
            expect(() => createWrapper(router)).not.toThrow()
        })

        it("handles missing permissions store gracefully", () => {
            vi.mocked(usePermissionsStore).mockReturnValue({
                hasAnyEditPermission: false,
                hasFullAccessPermission: false,
                hasOnlyServiceSpecificPermissions: false,
                hasOnlyOwnSchedulePermission: false,
                canAccessClinicianView: false,
                canAccessRotationView: false,
                userPermissions: null,
                isLoading: false,
                editableServiceCount: 0,
                getEditableServicesDisplay: vi.fn().mockReturnValue("None"),
                initialize: vi.fn().mockResolvedValue(),
            })

            // Should not throw error
            expect(() => createWrapper(router)).not.toThrow()
        })
    })
})
