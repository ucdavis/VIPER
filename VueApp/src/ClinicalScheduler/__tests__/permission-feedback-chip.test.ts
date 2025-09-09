import { describe, it, expect, vi, afterEach } from "vitest"
import { mount } from "@vue/test-utils"
import { createTestingPinia } from "@pinia/testing"
import { Quasar } from "quasar"
import PermissionFeedbackChip from "../components/PermissionFeedbackChip.vue"
import { usePermissionsStore } from "../stores/permissions"

// Test setup helper
// Initialize to a typed placeholder to satisfy init-declarations rule
let wrapper = null as unknown as ReturnType<typeof mount>

const mountComponent = (props = {}, permissionOverrides = {}) => {
    const pinia = createTestingPinia({
        createSpy: vi.fn,
        stubActions: false,
    })

    // Set up permissions store with overrides
    const permissionsStore = usePermissionsStore(pinia)
    Object.assign(permissionsStore, {
        hasFullAccessPermission: false,
        editableServiceCount: 0,
        getEditableServicesDisplay: vi.fn(() => ""),
        ...permissionOverrides,
    })

    wrapper = mount(PermissionFeedbackChip, {
        props: {
            visible: true,
            showServiceSpecific: true,
            showFullAccess: true,
            ...props,
        },
        global: {
            plugins: [pinia, Quasar],
        },
    })

    return wrapper
}

afterEach(() => {
    wrapper?.unmount()
})

describe("PermissionFeedbackChip - Visibility Logic", () => {
    it("should not show when visible prop is false", () => {
        mountComponent({ visible: false })
        expect(wrapper.find(".permission-feedback-chip").exists()).toBeFalsy()
    })

    it("should show custom message when provided", () => {
        mountComponent({ customMessage: "Test message" })
        expect(wrapper.find(".permission-feedback-chip").exists()).toBeTruthy()
        expect(wrapper.text()).toContain("Test message")
    })

    it("should not show when user has no permissions", () => {
        mountComponent()
        expect(wrapper.find(".permission-feedback-chip").exists()).toBeFalsy()
    })
})

describe("PermissionFeedbackChip - Service-Specific Permissions", () => {
    it("should show for users with only service-specific permissions", () => {
        mountComponent(
            {
                filteredCount: 5,
                totalCount: 10,
            },
            {
                editableServiceCount: 1,
                hasFullAccessPermission: false,
                getEditableServicesDisplay: vi.fn(() => "Internal Medicine"),
            },
        )

        expect(wrapper.find(".permission-feedback-chip").exists()).toBeTruthy()
        expect(wrapper.text()).toContain("Showing 5 of 10 items (Internal Medicine)")
    })

    it("should show for users with BOTH service-specific AND edit-own permissions", () => {
        // This is the bug we fixed - users with both permissions should see the chip
        mountComponent(
            {
                filteredCount: 3,
                totalCount: 15,
            },
            {
                editableServiceCount: 1, // Has service permissions
                hasFullAccessPermission: false,
                hasEditOwnSchedulePermission: true, // Also has edit-own permission
                getEditableServicesDisplay: vi.fn(() => "Internal Medicine"),
            },
        )

        expect(wrapper.find(".permission-feedback-chip").exists()).toBeTruthy()
        expect(wrapper.text()).toContain("Showing 3 of 15 items (Internal Medicine)")
    })

    it("should show multiple services correctly", () => {
        mountComponent(
            {
                filteredCount: 8,
                totalCount: 20,
            },
            {
                editableServiceCount: 2,
                hasFullAccessPermission: false,
                getEditableServicesDisplay: vi.fn(() => "Internal Medicine, Surgery"),
            },
        )

        expect(wrapper.find(".permission-feedback-chip").exists()).toBeTruthy()
        expect(wrapper.text()).toContain("Showing 8 of 20 items (Internal Medicine, Surgery)")
    })

    it("should show limited message when counts are not provided", () => {
        mountComponent(
            {},
            {
                editableServiceCount: 1,
                hasFullAccessPermission: false,
                getEditableServicesDisplay: vi.fn(() => "Internal Medicine"),
            },
        )

        expect(wrapper.find(".permission-feedback-chip").exists()).toBeTruthy()
        expect(wrapper.text()).toContain("Limited to Internal Medicine")
    })

    it("should not show service-specific chip when showServiceSpecific is false", () => {
        mountComponent(
            {
                showServiceSpecific: false,
            },
            {
                editableServiceCount: 1,
                hasFullAccessPermission: false,
            },
        )

        expect(wrapper.find(".permission-feedback-chip").exists()).toBeFalsy()
    })
})

describe("PermissionFeedbackChip - Full Access Permissions", () => {
    it("should show for users with full access permissions", () => {
        mountComponent(
            {
                filteredCount: 25,
            },
            {
                hasFullAccessPermission: true,
                editableServiceCount: 0,
            },
        )

        expect(wrapper.find(".permission-feedback-chip").exists()).toBeTruthy()
        expect(wrapper.text()).toContain("Showing all 25 available items")
    })

    it("should not show full access chip when filteredCount is 0", () => {
        mountComponent(
            {
                filteredCount: 0,
            },
            {
                hasFullAccessPermission: true,
            },
        )

        expect(wrapper.find(".permission-feedback-chip").exists()).toBeFalsy()
    })

    it("should not show full access chip when showFullAccess is false", () => {
        mountComponent(
            {
                showFullAccess: false,
                filteredCount: 10,
            },
            {
                hasFullAccessPermission: true,
            },
        )

        expect(wrapper.find(".permission-feedback-chip").exists()).toBeFalsy()
    })

    it("should prioritize full access over service-specific when user has both", () => {
        mountComponent(
            {
                filteredCount: 20,
            },
            {
                hasFullAccessPermission: true,
                editableServiceCount: 2, // Also has service permissions
            },
        )

        expect(wrapper.find(".permission-feedback-chip").exists()).toBeTruthy()
        // Should show full access message, not service-specific
        expect(wrapper.text()).toContain("Showing all 20 available items")
        expect(wrapper.text()).not.toContain("items (")
    })
})

describe("PermissionFeedbackChip - Custom Messages", () => {
    it("should use custom service-specific message when provided", () => {
        mountComponent(
            {
                serviceSpecificMessage: "Custom service message",
            },
            {
                editableServiceCount: 1,
                hasFullAccessPermission: false,
            },
        )

        expect(wrapper.text()).toContain("Custom service message")
    })

    it("should use custom full access message when provided", () => {
        mountComponent(
            {
                fullAccessMessage: "Custom full access message",
                filteredCount: 10,
            },
            {
                hasFullAccessPermission: true,
            },
        )

        expect(wrapper.text()).toContain("Custom full access message")
    })

    it("should show custom message with custom icon and color", () => {
        mountComponent({
            customMessage: "Special message",
            customIcon: "warning",
            customColor: "negative",
        })

        const chip = wrapper.find(".q-chip")
        expect(chip.exists()).toBeTruthy()
        expect(wrapper.text()).toContain("Special message")
    })
})

describe("PermissionFeedbackChip - Edge Cases", () => {
    it("should handle undefined filteredCount gracefully", () => {
        mountComponent(
            {
                filteredCount: undefined,
                totalCount: undefined,
            },
            {
                editableServiceCount: 1,
                hasFullAccessPermission: false,
                getEditableServicesDisplay: vi.fn(() => "Internal Medicine"),
            },
        )

        expect(wrapper.find(".permission-feedback-chip").exists()).toBeTruthy()
        expect(wrapper.text()).toContain("Limited to Internal Medicine")
    })

    it("should handle zero service count correctly", () => {
        mountComponent(
            {},
            {
                editableServiceCount: 0,
                hasFullAccessPermission: false,
            },
        )

        expect(wrapper.find(".permission-feedback-chip").exists()).toBeFalsy()
    })

    it("should handle complex permission combinations correctly", () => {
        // User with service permissions, edit-own, but not full access
        mountComponent(
            {
                filteredCount: 7,
                totalCount: 30,
            },
            {
                editableServiceCount: 3,
                hasFullAccessPermission: false,
                hasEditOwnSchedulePermission: true,
                hasManagePermission: false,
                hasAdminPermission: false,
                getEditableServicesDisplay: vi.fn(() => "Internal Medicine, Surgery, Pediatrics"),
            },
        )

        expect(wrapper.find(".permission-feedback-chip").exists()).toBeTruthy()
        expect(wrapper.text()).toContain("Showing 7 of 30 items (Internal Medicine, Surgery, Pediatrics)")
    })
})

describe("PermissionFeedbackChip - Chip Appearance", () => {
    it("should use correct icon for service-specific permissions", () => {
        mountComponent(
            {},
            {
                editableServiceCount: 1,
                hasFullAccessPermission: false,
                getEditableServicesDisplay: vi.fn(() => "Internal Medicine"),
            },
        )

        const chip = wrapper.find(".q-chip")
        expect(chip.exists()).toBeTruthy()
        expect(chip.classes()).toContain("bg-positive")
    })

    it("should use correct icon for full access permissions", () => {
        mountComponent(
            {
                filteredCount: 10,
            },
            {
                hasFullAccessPermission: true,
            },
        )

        const chip = wrapper.find(".q-chip")
        expect(chip.exists()).toBeTruthy()
        expect(chip.classes()).toContain("bg-primary")
    })
})
