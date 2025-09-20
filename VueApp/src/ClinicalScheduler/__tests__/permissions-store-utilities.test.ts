import { describe, it, expect, vi, beforeEach } from "vitest"
import { setActivePinia, createPinia } from "pinia"
import { usePermissionsStore } from "../stores/permissions"
import { permissionService, createMockUserPermissions } from "./test-utils"

// Mock the permission service
vi.mock("../services/permission-service", () => ({
    permissionService: {
        getUserPermissions: vi.fn(),
        getPermissionSummary: vi.fn(),
        canEditService: vi.fn(),
        canEditRotation: vi.fn(),
        canEditOwnSchedule: vi.fn(),
    },
}))

describe("Permissions Store - Utility Methods", () => {
    beforeEach(() => {
        // Create fresh pinia instance for each test
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    describe("Service Name Utilities", () => {
        it("getEditableServiceNames returns correct service names", async () => {
            const store = usePermissionsStore()
            const mockPermissions = createMockUserPermissions({
                editableServices: [
                    {
                        serviceId: 1,
                        serviceName: "Cardiology",
                        shortName: "Cardio",
                    },
                    {
                        serviceId: 2,
                        serviceName: "Internal Medicine",
                        shortName: "IntMed",
                    },
                ],
            })

            // Mock the API call
            vi.mocked(permissionService.getUserPermissions).mockResolvedValue(mockPermissions)
            await store.fetchUserPermissions()

            expect(store.getEditableServiceNames()).toEqual(["Cardiology", "Internal Medicine"])
        })
    })

    describe("Service Display Formatting", () => {
        it("getEditableServicesDisplay formats no services correctly", async () => {
            const store = usePermissionsStore()
            const mockPermissions = createMockUserPermissions({
                editableServices: [],
            })

            vi.mocked(permissionService.getUserPermissions).mockResolvedValue(mockPermissions)
            await store.fetchUserPermissions()
            expect(store.getEditableServicesDisplay()).toBe("None")
        })

        it("getEditableServicesDisplay formats one service correctly", async () => {
            const store = usePermissionsStore()
            const mockPermissions = createMockUserPermissions({
                editableServices: [
                    {
                        serviceId: 1,
                        serviceName: "Cardiology",
                        shortName: "Cardio",
                    },
                ],
            })

            vi.mocked(permissionService.getUserPermissions).mockResolvedValue(mockPermissions)
            await store.fetchUserPermissions()
            expect(store.getEditableServicesDisplay()).toBe("Cardiology")
        })

        it("getEditableServicesDisplay formats multiple services correctly", async () => {
            const store = usePermissionsStore()
            const mockPermissions = createMockUserPermissions({
                editableServices: [
                    {
                        serviceId: 1,
                        serviceName: "Cardiology",
                        shortName: "Cardio",
                    },
                    {
                        serviceId: 2,
                        serviceName: "Internal Medicine",
                        shortName: "IntMed",
                    },
                    {
                        serviceId: 3,
                        serviceName: "Surgery",
                        shortName: "Surg",
                    },
                ],
            })

            vi.mocked(permissionService.getUserPermissions).mockResolvedValue(mockPermissions)
            await store.fetchUserPermissions()
            expect(store.getEditableServicesDisplay()).toBe("Cardiology, Internal Medicine, and Surgery")
        })
    })

    describe("Service Permission Checking", () => {
        it("canEditService checks service permissions correctly", async () => {
            const store = usePermissionsStore()
            const mockPermissions = createMockUserPermissions({
                permissions: {
                    hasAdminPermission: false,
                    hasManagePermission: false,
                    hasEditClnSchedulesPermission: false,
                    hasEditOwnSchedulePermission: false,
                    servicePermissions: { 1: true, 2: false, 3: true },
                    editableServiceCount: 2,
                },
            })

            // Mock the API call
            vi.mocked(permissionService.getUserPermissions).mockResolvedValue(mockPermissions)
            await store.fetchUserPermissions()

            expect(store.canEditService(1)).toBeTruthy()
            expect(store.canEditService(2)).toBeFalsy()
            expect(store.canEditService(3)).toBeTruthy()
            expect(store.canEditService(999)).toBeFalsy()
        })
    })
})
