import { setActivePinia, createPinia } from "pinia"
import { usePermissionsStore } from "../stores/permissions"
import { PermissionService, createMockUserPermissions } from "./test-utils"

// Mock the permission service
vi.mock("../services/permission-service", () => ({
    PermissionService: {
        getUserPermissions: vi.fn(),
        getPermissionSummary: vi.fn(),
        canEditService: vi.fn(),
        canEditRotation: vi.fn(),
        canEditOwnSchedule: vi.fn(),
    },
    permissionService: {},
}))

describe("Permissions Store - Error Handling & Hierarchy", () => {
    beforeEach(() => {
        // Create fresh pinia instance for each test
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    describe("Error Handling", () => {
        it("handles null userPermissions gracefully", () => {
            const store = usePermissionsStore()

            expect(store.hasFullAccessPermission).toBeFalsy()
            expect(store.hasAnyEditPermission).toBeFalsy()
            expect(store.hasOnlyServiceSpecificPermissions).toBeFalsy()
            expect(store.hasOnlyOwnSchedulePermission).toBeFalsy()
            expect(store.canAccessClinicianView).toBeFalsy()
            expect(store.permissionLevel).toBe("none")
            expect(store.getEditableServiceNames()).toEqual([])
            expect(store.getEditableServicesDisplay()).toBe("None")
        })

        it("handles invalid service IDs in canEditService", async () => {
            const store = usePermissionsStore()
            const mockPermissions = createMockUserPermissions()

            vi.mocked(PermissionService.getUserPermissions).mockResolvedValue(mockPermissions)
            await store.fetchUserPermissions()

            expect(store.canEditService(0)).toBeFalsy()
            expect(store.canEditService(-1)).toBeFalsy()
            expect(store.canEditService(Number.NaN)).toBeFalsy()
        })
    })

    describe("Permission Hierarchy", () => {
        it("admin permission overrides all others", async () => {
            const store = usePermissionsStore()
            const mockPermissions = createMockUserPermissions({
                permissions: {
                    hasAdminPermission: true,
                    hasManagePermission: true,
                    hasEditClnSchedulesPermission: true,
                    hasEditOwnSchedulePermission: true,
                    servicePermissions: { 1: true },
                    editableServiceCount: 1,
                },
            })

            // Mock the API call
            vi.mocked(PermissionService.getUserPermissions).mockResolvedValue(mockPermissions)
            await store.fetchUserPermissions()

            expect(store.permissionLevel).toBe("admin")
            expect(store.hasFullAccessPermission).toBeTruthy()
            expect(store.canAccessClinicianView).toBeTruthy()
            expect(store.hasOnlyServiceSpecificPermissions).toBeFalsy()
            expect(store.hasOnlyOwnSchedulePermission).toBeFalsy()
        })

        it("manage permission overrides edit_all permission", async () => {
            const store = usePermissionsStore()
            const mockPermissions = createMockUserPermissions({
                permissions: {
                    hasAdminPermission: false,
                    hasManagePermission: true,
                    hasEditClnSchedulesPermission: true,
                    hasEditOwnSchedulePermission: false,
                    servicePermissions: {},
                    editableServiceCount: 0,
                },
            })

            vi.mocked(PermissionService.getUserPermissions).mockResolvedValue(mockPermissions)
            await store.fetchUserPermissions()
            expect(store.permissionLevel).toBe("manage")
        })

        it("edit_all permission overrides edit_own permission", async () => {
            const store = usePermissionsStore()
            const mockPermissions = createMockUserPermissions({
                permissions: {
                    hasAdminPermission: false,
                    hasManagePermission: false,
                    hasEditClnSchedulesPermission: true,
                    hasEditOwnSchedulePermission: true,
                    servicePermissions: {},
                    editableServiceCount: 0,
                },
            })

            vi.mocked(PermissionService.getUserPermissions).mockResolvedValue(mockPermissions)
            await store.fetchUserPermissions()
            expect(store.permissionLevel).toBe("edit_all")
        })

        it("edit_own permission overrides service_specific permission", async () => {
            const store = usePermissionsStore()
            const mockPermissions = createMockUserPermissions({
                permissions: {
                    hasAdminPermission: false,
                    hasManagePermission: false,
                    hasEditClnSchedulesPermission: false,
                    hasEditOwnSchedulePermission: true,
                    servicePermissions: { 1: true },
                    editableServiceCount: 1,
                },
            })

            vi.mocked(PermissionService.getUserPermissions).mockResolvedValue(mockPermissions)
            await store.fetchUserPermissions()
            expect(store.permissionLevel).toBe("edit_own")
        })
    })
})
