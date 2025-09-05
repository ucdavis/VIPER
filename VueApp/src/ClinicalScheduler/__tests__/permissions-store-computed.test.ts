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

describe("Permissions Store - Permission Level Detection", () => {
    beforeEach(() => {
        // Create fresh pinia instance for each test
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    describe("Admin and Manager Detection", () => {
        it("hasFullAccessPermission detects admin users", async () => {
            const store = usePermissionsStore()
            const mockPermissions = createMockUserPermissions({
                permissions: {
                    hasAdminPermission: true,
                    hasManagePermission: false,
                    hasEditClnSchedulesPermission: false,
                    hasEditOwnSchedulePermission: false,
                    servicePermissions: {},
                    editableServiceCount: 0,
                },
            })

            // Mock the API call
            vi.mocked(permissionService.getUserPermissions).mockResolvedValue(mockPermissions)

            // Trigger the fetch
            await store.fetchUserPermissions()

            expect(store.hasFullAccessPermission).toBeTruthy()
            expect(store.permissionLevel).toBe("admin")
        })

        it("hasFullAccessPermission detects manage users", async () => {
            const store = usePermissionsStore()
            const mockPermissions = createMockUserPermissions({
                permissions: {
                    hasAdminPermission: false,
                    hasManagePermission: true,
                    hasEditClnSchedulesPermission: false,
                    hasEditOwnSchedulePermission: false,
                    servicePermissions: {},
                    editableServiceCount: 0,
                },
            })

            // Mock the API call
            vi.mocked(permissionService.getUserPermissions).mockResolvedValue(mockPermissions)

            // Trigger the fetch
            await store.fetchUserPermissions()

            expect(store.hasFullAccessPermission).toBeTruthy()
            expect(store.permissionLevel).toBe("manage")
        })

        it("hasFullAccessPermission detects edit all users", async () => {
            const store = usePermissionsStore()
            const mockPermissions = createMockUserPermissions({
                permissions: {
                    hasAdminPermission: false,
                    hasManagePermission: false,
                    hasEditClnSchedulesPermission: true,
                    hasEditOwnSchedulePermission: false,
                    servicePermissions: {},
                    editableServiceCount: 0,
                },
            })

            // Mock the API call
            vi.mocked(permissionService.getUserPermissions).mockResolvedValue(mockPermissions)

            // Trigger the fetch
            await store.fetchUserPermissions()

            expect(store.hasFullAccessPermission).toBeTruthy()
            expect(store.permissionLevel).toBe("edit_all")
        })
    })
})

describe("Permissions Store - Service-Specific Permissions", () => {
    beforeEach(() => {
        // Create fresh pinia instance for each test
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    describe("Service Permission Detection", () => {
        it("hasOnlyServiceSpecificPermissions detects service-specific users", async () => {
            const store = usePermissionsStore()
            const mockPermissions = createMockUserPermissions({
                permissions: {
                    hasAdminPermission: false,
                    hasManagePermission: false,
                    hasEditClnSchedulesPermission: false,
                    hasEditOwnSchedulePermission: false,
                    servicePermissions: { 1: true, 2: true },
                    editableServiceCount: 2,
                },
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

            // Trigger the fetch
            await store.fetchUserPermissions()

            expect(store.hasOnlyServiceSpecificPermissions).toBeTruthy()
            expect(store.hasFullAccessPermission).toBeFalsy()
            expect(store.canAccessClinicianView).toBeFalsy()
            expect(store.permissionLevel).toBe("service_specific")
        })
    })
})

describe("Permissions Store - Own Schedule Permissions", () => {
    beforeEach(() => {
        // Create fresh pinia instance for each test
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    describe("Own Schedule Detection", () => {
        it("hasOnlyOwnSchedulePermission detects own schedule users", async () => {
            const store = usePermissionsStore()
            const mockPermissions = createMockUserPermissions({
                permissions: {
                    hasAdminPermission: false,
                    hasManagePermission: false,
                    hasEditClnSchedulesPermission: false,
                    hasEditOwnSchedulePermission: true,
                    servicePermissions: {},
                    editableServiceCount: 0,
                },
            })

            // Mock the API call
            vi.mocked(permissionService.getUserPermissions).mockResolvedValue(mockPermissions)

            // Trigger the fetch
            await store.fetchUserPermissions()

            expect(store.hasOnlyOwnSchedulePermission).toBeTruthy()
            expect(store.hasFullAccessPermission).toBeFalsy()
            expect(store.canAccessClinicianView).toBeTruthy() // Own schedule users CAN access clinician view
            expect(store.permissionLevel).toBe("edit_own")
        })
    })
})

describe("Permissions Store - General Permission Detection", () => {
    beforeEach(() => {
        // Create fresh pinia instance for each test
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    describe("Any Edit Permission Detection", () => {
        it("hasAnyEditPermission detects any editing capability", async () => {
            const store = usePermissionsStore()

            // Test with service-specific permissions
            const serviceSpecificPermissions = createMockUserPermissions({
                permissions: {
                    hasAdminPermission: false,
                    hasManagePermission: false,
                    hasEditClnSchedulesPermission: false,
                    hasEditOwnSchedulePermission: false,
                    servicePermissions: { 1: true },
                    editableServiceCount: 1,
                },
                editableServices: [
                    {
                        serviceId: 1,
                        serviceName: "Cardiology",
                        shortName: "Cardio",
                    },
                ],
            })

            // Mock the API call for service permissions
            vi.mocked(permissionService.getUserPermissions).mockResolvedValue(serviceSpecificPermissions)
            await store.fetchUserPermissions()
            expect(store.hasAnyEditPermission).toBeTruthy()

            // Test with no permissions - create new store instance
            const storeNoPerms = usePermissionsStore()
            const noPermissions = createMockUserPermissions()
            vi.mocked(permissionService.getUserPermissions).mockResolvedValue(noPermissions)
            await storeNoPerms.fetchUserPermissions()
            expect(storeNoPerms.hasAnyEditPermission).toBeFalsy()
        })
    })
})

describe("Permissions Store - Clinician View Access", () => {
    beforeEach(() => {
        // Create fresh pinia instance for each test
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    describe("Clinician View Permission", () => {
        it("canAccessClinicianView allows admin access", async () => {
            const store = usePermissionsStore()
            const mockPermissions = createMockUserPermissions({
                permissions: {
                    hasAdminPermission: true,
                    hasManagePermission: false,
                    hasEditClnSchedulesPermission: false,
                    hasEditOwnSchedulePermission: false,
                    servicePermissions: {},
                    editableServiceCount: 0,
                },
            })

            vi.mocked(permissionService.getUserPermissions).mockResolvedValue(mockPermissions)
            await store.fetchUserPermissions()
            expect(store.canAccessClinicianView).toBeTruthy()
        })

        it("canAccessClinicianView allows own schedule users", async () => {
            const store = usePermissionsStore()
            const mockPermissions = createMockUserPermissions({
                permissions: {
                    hasAdminPermission: false,
                    hasManagePermission: false,
                    hasEditClnSchedulesPermission: false,
                    hasEditOwnSchedulePermission: true,
                    servicePermissions: {},
                    editableServiceCount: 0,
                },
            })

            vi.mocked(permissionService.getUserPermissions).mockResolvedValue(mockPermissions)
            await store.fetchUserPermissions()
            expect(store.canAccessClinicianView).toBeTruthy()
        })

        it("canAccessClinicianView blocks service-specific users", async () => {
            const store = usePermissionsStore()
            const mockPermissions = createMockUserPermissions({
                permissions: {
                    hasAdminPermission: false,
                    hasManagePermission: false,
                    hasEditClnSchedulesPermission: false,
                    hasEditOwnSchedulePermission: false,
                    servicePermissions: { 1: true },
                    editableServiceCount: 1,
                },
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
            expect(store.canAccessClinicianView).toBeFalsy()
        })
    })
})
