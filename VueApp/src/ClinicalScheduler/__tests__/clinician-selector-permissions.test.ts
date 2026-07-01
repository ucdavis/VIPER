import { setupTest, createWrapper, mockSuccessResponse } from "./clinician-selector-helpers"
import { usePermissionsStore } from "../stores/permissions"

// Mock services
vi.mock("../services/clinician-service", () => ({
    ClinicianService: {
        getClinicians: vi.fn<(...args: unknown[]) => unknown>(),
    },
}))

vi.mock("../services/permission-service", () => ({
    PermissionService: {
        getUserPermissions: vi.fn<(...args: unknown[]) => unknown>(),
        getPermissionSummary: vi.fn<(...args: unknown[]) => unknown>(),
        canEditService: vi.fn<(...args: unknown[]) => unknown>(),
        canEditRotation: vi.fn<(...args: unknown[]) => unknown>(),
        canEditOwnSchedule: vi.fn<(...args: unknown[]) => unknown>(),
    },
    permissionService: {},
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

        it("locks selector to read-only when user has only own schedule permission and 1 clinician is returned", async () => {
            const singleClinician = {
                mothraId: "12345",
                fullName: "Smith, John",
                firstName: "John",
                lastName: "Smith",
            }
            mockSuccessResponse([singleClinician])

            const store = usePermissionsStore()
            vi.spyOn(store, "hasOnlyOwnSchedulePermission", "get").mockReturnValue(true)

            const wrapper = createWrapper({
                isOwnScheduleOnly: false,
                showAffiliatesToggle: true,
                modelValue: null,
            })

            await (wrapper.vm as any).fetchClinicians()
            await wrapper.vm.$nextTick()

            // The select should be read-only
            const select = wrapper.find(".q-select")
            expect(select.exists()).toBeTruthy()
            expect(select.classes()).toContain("q-field--readonly")

            // The affiliates toggle should be hidden
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
