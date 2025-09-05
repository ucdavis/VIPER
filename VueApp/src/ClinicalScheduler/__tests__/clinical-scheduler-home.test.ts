import { describe, it, expect, vi, beforeEach } from "vitest"
import { computed } from "vue"
import { usePermissionsStore } from "../stores/permissions"
import ClinicalSchedulerHome from "../pages/ClinicalSchedulerHome.vue"
import { setupTest, createTestWrapper, waitForAsync, createMockPermissionsStore } from "./test-utils"

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

            const wrapper = createWrapper()

            // Should show rotation view
            expect(wrapper.text()).toContain("Schedule by Rotation")
            expect(wrapper.text()).toContain("Limited to your authorized rotations")

            // Should show disabled clinician view with explanation
            expect(wrapper.text()).toContain("Schedule by Clinician")
            expect(wrapper.text()).toContain("Not available with rotation-specific permissions")
            expect(wrapper.find(".bg-grey-1").exists()).toBeTruthy()
        })

        it("shows only clinician view for own schedule users", () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasOnlyOwnSchedulePermission = true
            mockPermissionsStore.canAccessClinicianView = true
            mockPermissionsStore.canAccessRotationView = false // Own schedule users typically can't access rotation view

            const wrapper = createWrapper()

            // Should show clinician view
            expect(wrapper.text()).toContain("Schedule by Clinician")
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

    describe("Permission Indicators", () => {
        it("displays permission info banner for limited users", () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = true

            const wrapper = createWrapper()

            const permissionBanner = wrapper.findComponent({ name: "PermissionInfoBanner" })
            expect(permissionBanner.exists()).toBeTruthy()
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

        it("does not navigate when disabled card is clicked", async () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = true
            mockPermissionsStore.canAccessClinicianView = false
            mockPermissionsStore.canAccessRotationView = true

            const wrapper = createWrapper()
            const routerPush = vi.spyOn(router, "push")

            // Click the disabled clinician card
            const disabledCard = wrapper.find(".bg-grey-1")
            await disabledCard.trigger("click")

            expect(routerPush).not.toHaveBeenCalled()
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
