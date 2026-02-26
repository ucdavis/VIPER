import { usePermissionsStore } from "../stores/permissions"
import ClinicalSchedulerHome from "../pages/ClinicalSchedulerHome.vue"
import { setupTest, createTestWrapper, waitForAsync, createMockPermissionsStore } from "./test-utils"

// Mock the permissions store
vi.mock("../stores/permissions")

vi.mock("../components/AccessDeniedCard.vue", () => ({
    default: {
        name: "AccessDeniedCard",
        props: ["message", "subtitle"],
        template: '<div class="no-access-card"><div>Access Denied</div><div>{{message}}</div></div>',
    },
}))

describe("ClinicalSchedulerHome - Access Control", () => {
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

    describe("Access Denied Scenarios", () => {
        it("shows access denied message when user has no edit permissions", () => {
            mockPermissionsStore.hasAnyEditPermission = false

            const wrapper = createTestWrapper({
                component: ClinicalSchedulerHome,
                router,
            })

            expect(wrapper.text()).toContain("Access Denied")
            expect(wrapper.text()).toContain("You do not have permission to access the Clinical Scheduler")
            expect(wrapper.find(".no-access-card").exists()).toBeTruthy()
        })

        it("hides main content when user lacks permissions", () => {
            mockPermissionsStore.hasAnyEditPermission = false

            const wrapper = createTestWrapper({
                component: ClinicalSchedulerHome,
                router,
            })

            expect(wrapper.find(".no-access-card").exists()).toBeTruthy()
            expect(wrapper.text()).not.toContain("Select Scheduling View")
        })
    })

    describe("Access Granted Scenarios", () => {
        it("shows main content when user has edit permissions", () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.hasFullAccessPermission = true
            mockPermissionsStore.canAccessClinicianView = true

            const wrapper = createTestWrapper({
                component: ClinicalSchedulerHome,
                router,
            })

            expect(wrapper.find(".no-access-card").exists()).toBeFalsy()
            expect(wrapper.text()).toContain("Select Scheduling View")
        })

        it("initializes permissions store on mount", async () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.userPermissions = null // Not loaded yet

            createTestWrapper({
                component: ClinicalSchedulerHome,
                router,
            })

            // Wait for component to mount and initialize
            await waitForAsync()

            expect(mockPermissionsStore.initialize).toHaveBeenCalled()
        })
    })

    describe("Permission States", () => {
        it("handles loading state properly", () => {
            mockPermissionsStore.hasAnyEditPermission = true
            mockPermissionsStore.isLoading = true

            const wrapper = createTestWrapper({
                component: ClinicalSchedulerHome,
                router,
            })

            // Should still show content even during loading
            expect(wrapper.find(".no-access-card").exists()).toBeFalsy()
        })

        it("handles null permissions gracefully", () => {
            mockPermissionsStore.hasAnyEditPermission = false
            mockPermissionsStore.userPermissions = null

            const wrapper = createTestWrapper({
                component: ClinicalSchedulerHome,
                router,
            })

            expect(wrapper.find(".no-access-card").exists()).toBeTruthy()
        })
    })
})
