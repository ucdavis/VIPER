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

describe("PermissionInfoBanner - Content & Messages", () => {
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

    describe("Message Content", () => {
        it("displays correct message for service-specific permissions with single service", () => {
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = true
            mockPermissionsStore.hasLimitedPermissions = true
            mockPermissionsStore.editableServiceCount = 1
            mockPermissionsStore.getEditableServicesDisplay = vi.fn().mockReturnValue("Cardiology")

            const wrapper = createWrapper()

            expect(wrapper.text()).toContain("You can manage schedules for 1 service: Cardiology")
        })

        it("displays correct message for service-specific permissions with multiple services", () => {
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = true
            mockPermissionsStore.hasLimitedPermissions = true
            mockPermissionsStore.editableServiceCount = 3
            mockPermissionsStore.getEditableServicesDisplay = vi
                .fn()
                .mockReturnValue("Cardiology, Surgery, and Internal Medicine")

            const wrapper = createWrapper()

            expect(wrapper.text()).toContain(
                "You can manage schedules for 3 services: Cardiology, Surgery, and Internal Medicine",
            )
        })

        it("displays correct message for own schedule permissions", () => {
            mockPermissionsStore.hasOnlyOwnSchedulePermission = true
            mockPermissionsStore.hasLimitedPermissions = true

            const wrapper = createWrapper()

            expect(wrapper.text()).toContain("You can only edit your own schedule entries")
        })

        it("displays correct combined message for service-specific and own-schedule permissions", () => {
            // Test the specific case of users with both permissions
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = false
            mockPermissionsStore.hasOnlyOwnSchedulePermission = false
            mockPermissionsStore.hasLimitedPermissions = true
            mockPermissionsStore.editableServiceCount = 1
            mockPermissionsStore.hasEditOwnSchedulePermission = true
            mockPermissionsStore.getEditableServicesDisplay = vi.fn().mockReturnValue("Internal Medicine")

            const wrapper = createWrapper()

            expect(wrapper.text()).toContain("Limited Access")
            expect(wrapper.text()).toContain(
                "You can manage schedules for 1 service (Internal Medicine) and edit your own schedule entries",
            )
        })

        it("displays correct combined message with multiple services", () => {
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = false
            mockPermissionsStore.hasOnlyOwnSchedulePermission = false
            mockPermissionsStore.hasLimitedPermissions = true
            mockPermissionsStore.editableServiceCount = 2
            mockPermissionsStore.hasEditOwnSchedulePermission = true
            mockPermissionsStore.getEditableServicesDisplay = vi.fn().mockReturnValue("Internal Medicine, Surgery")

            const wrapper = createWrapper()

            expect(wrapper.text()).toContain("Limited Access")
            expect(wrapper.text()).toContain(
                "You can manage schedules for 2 services (Internal Medicine, Surgery) and edit your own schedule entries",
            )
        })
    })

    describe("Styling Classes", () => {
        it("applies correct classes for service-specific permissions", () => {
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = true
            mockPermissionsStore.hasLimitedPermissions = true

            const wrapper = createWrapper()
            const banner = wrapper.findComponent({ name: "QBanner" })

            expect(banner.classes()).toContain("q-mb-md")
            expect(banner.classes()).toContain("text-positive")
        })

        it("applies correct classes for own schedule permissions", () => {
            mockPermissionsStore.hasOnlyOwnSchedulePermission = true
            mockPermissionsStore.hasLimitedPermissions = true

            const wrapper = createWrapper()
            const banner = wrapper.findComponent({ name: "QBanner" })

            expect(banner.classes()).toContain("q-mb-md")
            expect(banner.classes()).toContain("text-info")
        })

        it("uses rounded and dense props", () => {
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = true
            mockPermissionsStore.hasLimitedPermissions = true

            const wrapper = createWrapper()
            const banner = wrapper.findComponent({ name: "QBanner" })

            expect(banner.props("rounded")).toBeTruthy()
            expect(banner.props("dense")).toBeTruthy()
        })

        it("applies correct classes for combined permissions", () => {
            // Users with both service-specific and own-schedule should show positive color (service takes precedence)
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = false
            mockPermissionsStore.hasOnlyOwnSchedulePermission = false
            mockPermissionsStore.hasLimitedPermissions = true
            mockPermissionsStore.editableServiceCount = 1
            mockPermissionsStore.hasEditOwnSchedulePermission = true

            const wrapper = createWrapper()
            const banner = wrapper.findComponent({ name: "QBanner" })

            expect(banner.classes()).toContain("q-mb-md")
            expect(banner.classes()).toContain("text-positive")
        })
    })

    describe("Accessibility Features", () => {
        it("provides proper icon for service-specific permissions", () => {
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = true
            mockPermissionsStore.hasLimitedPermissions = true

            const wrapper = createWrapper()
            const icon = wrapper.find("i")

            expect(icon.exists()).toBeTruthy()
        })

        it("provides proper icon for own schedule permissions", () => {
            mockPermissionsStore.hasOnlyOwnSchedulePermission = true
            mockPermissionsStore.hasLimitedPermissions = true

            const wrapper = createWrapper()
            const icon = wrapper.find("i")

            expect(icon.exists()).toBeTruthy()
        })
    })

    describe("Edge Cases & Error Handling", () => {
        it("handles empty editable services display", () => {
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = true
            mockPermissionsStore.hasLimitedPermissions = true
            mockPermissionsStore.editableServiceCount = 0
            mockPermissionsStore.getEditableServicesDisplay = vi.fn().mockReturnValue("None")

            const wrapper = createWrapper()

            expect(wrapper.text()).toContain("You can manage schedules for 0 service: None")
        })

        it("handles missing permissions store data gracefully", () => {
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = true
            mockPermissionsStore.getEditableServicesDisplay = vi.fn().mockReturnValue("")

            // Should not throw error
            expect(() => createWrapper()).not.toThrow()
        })

        it("handles undefined service count", () => {
            mockPermissionsStore.hasOnlyServiceSpecificPermissions = true
            mockPermissionsStore.editableServiceCount = undefined
            mockPermissionsStore.getEditableServicesDisplay = vi.fn().mockReturnValue("Cardiology")

            // Should not throw error and handle gracefully
            expect(() => createWrapper()).not.toThrow()
        })
    })
})
