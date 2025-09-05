import { describe, it, expect, vi, beforeEach, afterEach } from "vitest"
import { mount } from "@vue/test-utils"
import { setActivePinia, createPinia } from "pinia"
import { Quasar } from "quasar"
import { usePermissionsStore } from "../stores/permissions"
import PermissionInfoBanner from "../components/PermissionInfoBanner.vue"
import { setupLocalStorageMock, mockLocalStorage } from "./test-utils"

// Mock the permissions store
vi.mock("../stores/permissions")

// Setup localStorage mock
setupLocalStorageMock()

const createWrapper = (props = {}) =>
    mount(PermissionInfoBanner, {
        props,
        global: {
            plugins: [[Quasar, {}]],
        },
    })

describe("PermissionInfoBanner - Display Logic", () => {
    let mockPermissionsStore: any = null

    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()

        mockPermissionsStore = {
            hasOnlyServiceSpecificPermissions: false,
            hasOnlyOwnSchedulePermission: false,
            hasLimitedPermissions: false,
            editableServiceCount: 0,
            getEditableServicesDisplay: vi.fn().mockReturnValue("None"),
        }

        vi.mocked(usePermissionsStore).mockReturnValue(mockPermissionsStore)
    })

    afterEach(() => {
        mockLocalStorage.clear()
    })

    describe("Banner Visibility", () => {
        it("shows banner for service-specific permissions", () => {
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = true
            mockPermissionsStore.hasLimitedPermissions = true
            mockPermissionsStore.editableServiceCount = 2
            mockPermissionsStore.getEditableServicesDisplay = vi.fn().mockReturnValue("Cardiology and Surgery")

            const wrapper = createWrapper()
            const banner = wrapper.findComponent({ name: "QBanner" })

            expect(banner.exists()).toBeTruthy()
            expect(banner.text()).toContain("Rotation-Specific Access")
            expect(banner.text()).toContain("You can manage schedules for 2 rotations: Cardiology and Surgery")
        })

        it("shows banner for own schedule permissions", () => {
            mockPermissionsStore.hasOnlyOwnSchedulePermission = true
            mockPermissionsStore.hasLimitedPermissions = true

            const wrapper = createWrapper()
            const banner = wrapper.findComponent({ name: "QBanner" })

            expect(banner.exists()).toBeTruthy()
            expect(banner.text()).toContain("Own Schedule Access")
            expect(banner.text()).toContain("You can only edit your own schedule entries")
        })

        it("does not show banner for full access users", () => {
            // Neither service-specific nor own-schedule-only permissions
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = false
            mockPermissionsStore.hasOnlyOwnSchedulePermission = false
            mockPermissionsStore.hasLimitedPermissions = false

            const wrapper = createWrapper()
            const banner = wrapper.findComponent({ name: "QBanner" })

            expect(banner.exists()).toBeFalsy()
        })

        it("shows combined message when both service-specific and own-schedule are true", () => {
            // New behavior: show both pieces of information
            // Use the conditions that the component checks for the combined case
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = false
            mockPermissionsStore.hasOnlyOwnSchedulePermission = false
            mockPermissionsStore.hasLimitedPermissions = true
            // Indicate both: user has some editable services AND own-schedule permission
            mockPermissionsStore.editableServiceCount = 1
            mockPermissionsStore.hasEditOwnSchedulePermission = true
            mockPermissionsStore.getEditableServicesDisplay = vi.fn().mockReturnValue("Cardiology")

            const wrapper = createWrapper()
            const banner = wrapper.findComponent({ name: "QBanner" })

            // Title reflects limited access
            expect(banner.text()).toContain("Limited Access")
            // Message includes both rotation-specific services and own-schedule note
            expect(banner.text()).toContain("You can manage schedules for 1 rotation (Cardiology)")
            expect(banner.text()).toContain("edit your own schedule entries")
        })
    })

    describe("Banner Styling", () => {
        it("displays correct icon for service-specific permissions", () => {
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = true
            mockPermissionsStore.hasLimitedPermissions = true

            const wrapper = createWrapper()
            const icon = wrapper.find("i")

            expect(icon.exists()).toBeTruthy()
            // Quasar renders icons with specific classes based on name
            expect(icon.classes()).toContain("notranslate")
        })
    })

    describe("Behavior Controls", () => {
        it("shows banner when service-specific permissions are active", () => {
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = true
            mockPermissionsStore.hasLimitedPermissions = true

            const wrapper = createWrapper()
            const banner = wrapper.findComponent({ name: "QBanner" })

            expect(banner.exists()).toBeTruthy()
        })

        it("hides banner when permissions change to full access", () => {
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = true
            mockPermissionsStore.hasLimitedPermissions = true

            const wrapper = createWrapper()
            expect(wrapper.findComponent({ name: "QBanner" }).exists()).toBeTruthy()

            // Simulate permission change
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = false
            mockPermissionsStore.hasOnlyOwnSchedulePermission = false
            mockPermissionsStore.hasLimitedPermissions = false

            // Remount to reflect new store values
            const wrapper2 = createWrapper()
            expect(wrapper2.findComponent({ name: "QBanner" }).exists()).toBeFalsy()
        })
    })
})
