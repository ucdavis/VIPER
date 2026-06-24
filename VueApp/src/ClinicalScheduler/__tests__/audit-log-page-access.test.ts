import { usePermissionsStore } from "../stores/permissions"
import { AuditLogService } from "../services/audit-log-service"
import { RotationService } from "../services/rotation-service"
import { PageDataService } from "../services/page-data-service"
import AuditLogPage from "../pages/AuditLogPage.vue"
import {
    setupTest,
    createTestWrapper,
    waitForAsync,
    createMockPermissionsStore,
    createMockUserPermissions,
} from "./test-utils"
import type { UserPermissions } from "../types"

type MockPermissionsStore = Omit<ReturnType<typeof createMockPermissionsStore>, "userPermissions"> & {
    hasManagePermission?: boolean
    userPermissions: UserPermissions | null
}

// Mock the permissions store and the services the page calls
vi.mock("../stores/permissions")
vi.mock("../services/audit-log-service")
vi.mock("../services/rotation-service")
vi.mock("../services/page-data-service")

// Stub child components that hit the network or rely on named routes
vi.mock("../components/AccessDeniedCard.vue", () => ({
    default: {
        name: "AccessDeniedCard",
        props: ["message", "subtitle"],
        template: '<div class="no-access-card"><div>Access Denied</div><div>{{message}}</div></div>',
    },
}))
vi.mock("../components/SchedulerNavigation.vue", () => ({
    default: { name: "SchedulerNavigation", template: "<div class='scheduler-nav'></div>" },
}))

describe("auditLogPage - Access Control", () => {
    let mockPermissionsStore: MockPermissionsStore = {} as never
    let router: any = {}

    beforeEach(() => {
        const { router: testRouter, mockStore } = setupTest()
        router = testRouter
        mockPermissionsStore = mockStore
        vi.mocked(usePermissionsStore).mockReturnValue(mockPermissionsStore as any)
        vi.mocked(AuditLogService.getAuditLog).mockResolvedValue({ result: [], success: true, errors: [] })
        vi.mocked(AuditLogService.getModifiers).mockResolvedValue({ result: [], success: true, errors: [] })
        vi.mocked(AuditLogService.getPersons).mockResolvedValue({ result: [], success: true, errors: [] })
        vi.mocked(RotationService.getRotations).mockResolvedValue({ result: [], success: true, errors: [] })
        vi.mocked(PageDataService.getPageData).mockResolvedValue({ currentGradYear: 2026, availableGradYears: [2026] })
    })

    it("shows access denied when user lacks the manage permission", () => {
        mockPermissionsStore.hasManagePermission = false
        mockPermissionsStore.userPermissions = createMockUserPermissions()

        const wrapper = createTestWrapper({ component: AuditLogPage, router })

        expect(wrapper.find(".no-access-card").exists()).toBeTruthy()
        // The results table (and its "Modified By" column) only renders for managers.
        expect(wrapper.text()).not.toContain("Modified By")
    })

    it("shows the audit trail and loads filter options for managers", async () => {
        mockPermissionsStore.hasManagePermission = true
        mockPermissionsStore.userPermissions = createMockUserPermissions({
            permissions: {
                hasAdminPermission: false,
                hasManagePermission: true,
                hasEditClnSchedulesPermission: false,
                hasEditOwnSchedulePermission: false,
                servicePermissions: {},
                editableServiceCount: 0,
            },
        })

        const wrapper = createTestWrapper({ component: AuditLogPage, router })
        await waitForAsync()

        expect(wrapper.find(".no-access-card").exists()).toBeFalsy()
        expect(wrapper.text()).toContain("Modified By")
        expect(RotationService.getRotations).toHaveBeenCalledWith()
        expect(AuditLogService.getModifiers).toHaveBeenCalledWith()
    })

    it("initializes the permissions store on mount when not yet loaded", async () => {
        mockPermissionsStore.hasManagePermission = true
        mockPermissionsStore.userPermissions = null

        createTestWrapper({ component: AuditLogPage, router })
        await waitForAsync()

        expect(mockPermissionsStore.initialize).toHaveBeenCalledWith()
    })
})
