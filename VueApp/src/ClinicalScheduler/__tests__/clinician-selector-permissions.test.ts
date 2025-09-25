import { describe, it, expect, vi, beforeEach } from "vitest"
import { setupTest, createWrapper } from "./clinician-selector-helpers"

// Mock services
vi.mock("../services/clinician-service", () => ({
    ClinicianService: {
        getClinicians: vi.fn(),
    },
}))

vi.mock("../services/permission-service", () => ({
    permissionService: {
        getUserPermissions: vi.fn(),
        getPermissionSummary: vi.fn(),
        canEditService: vi.fn(),
        canEditRotation: vi.fn(),
        canEditOwnSchedule: vi.fn(),
    },
}))

describe("ClinicianSelector - Permission Scenarios", () => {
    beforeEach(() => {
        setupTest()
    })

    describe("Own Schedule Only Mode", () => {
        const readOnlyUser = {
            mothraId: "12345",
            fullName: "Smith, John",
            firstName: "John",
            lastName: "Smith",
        }

        it("renders read-only display", async () => {
            const wrapper = createWrapper({
                isOwnScheduleOnly: true,
                modelValue: readOnlyUser,
            })

            await wrapper.vm.$nextTick()
            expect(wrapper.find(".own-schedule-display").exists()).toBeTruthy()
            expect(wrapper.find(".q-select").exists()).toBeFalsy()
            expect(wrapper.text()).toContain(readOnlyUser.fullName)
        })

        it("shows loading when no clinician provided", async () => {
            const wrapper = createWrapper({
                isOwnScheduleOnly: true,
                modelValue: null,
            })

            await wrapper.vm.$nextTick()
            expect(wrapper.text()).toContain("Loading...")
        })

        it("hides affiliates toggle", async () => {
            const wrapper = createWrapper({
                isOwnScheduleOnly: true,
                showAffiliatesToggle: true,
                modelValue: readOnlyUser,
            })

            await wrapper.vm.$nextTick()
            expect(wrapper.find(".affiliates-toggle-under-field").exists()).toBeFalsy()
        })
    })

    describe("Combined Permissions", () => {
        const testUser = {
            mothraId: "12345",
            fullName: "Smith, John",
            firstName: "John",
            lastName: "Smith",
        }

        it("shows read-only in clinician view", async () => {
            const wrapper = createWrapper({
                isOwnScheduleOnly: true,
                viewContext: "clinician",
                modelValue: testUser,
            })

            await wrapper.vm.$nextTick()
            expect(wrapper.find(".own-schedule-display").exists()).toBeTruthy()
            expect(wrapper.find(".q-select").exists()).toBeFalsy()
        })

        it("shows full select in rotation view", async () => {
            const wrapper = createWrapper({
                isOwnScheduleOnly: false,
                viewContext: "rotation",
            })

            await wrapper.vm.$nextTick()
            expect(wrapper.find(".q-select").exists()).toBeTruthy()
            expect(wrapper.find(".own-schedule-display").exists()).toBeFalsy()
        })
    })
})
