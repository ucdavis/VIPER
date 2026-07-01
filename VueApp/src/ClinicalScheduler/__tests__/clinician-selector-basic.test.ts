import { ClinicianService } from "../services/clinician-service"
import { setupTest, createWrapper, mockErrorResponse, mockSuccessResponse } from "./clinician-selector-helpers"

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

describe("ClinicianSelector - Basic Functionality", () => {
    beforeEach(() => {
        setupTest()
    })

    describe("Component Rendering", () => {
        it("renders searchable select for full permissions", async () => {
            const wrapper = createWrapper({ isOwnScheduleOnly: false })
            await wrapper.vm.$nextTick()

            expect(wrapper.find(".q-select").exists()).toBeTruthy()
            expect(wrapper.find(".own-schedule-display").exists()).toBeFalsy()
        })

        it("renders read-only display and auto-selects if there is only 1 clinician", async () => {
            const singleClinician = {
                mothraId: "12345",
                fullName: "Smith, John",
                firstName: "John",
                lastName: "Smith",
            }
            mockSuccessResponse([singleClinician])

            const wrapper = createWrapper({
                isOwnScheduleOnly: false,
                showAffiliatesToggle: true,
                modelValue: null,
            })

            // Fetch to trigger the list loading
            await (wrapper.vm as any).fetchClinicians()
            await wrapper.vm.$nextTick()

            // It should auto-select the clinician
            const emittedUpdate = wrapper.emitted("update:modelValue")?.[0]?.[0] as any
            expect(emittedUpdate).toStrictEqual(singleClinician)

            const emittedChange = wrapper.emitted("change")?.[0]?.[0] as any
            expect(emittedChange).toStrictEqual(singleClinician)

            // It should render select without readonly class/attribute instead of own-schedule-display
            expect(wrapper.find(".own-schedule-display").exists()).toBeFalsy()
            const select = wrapper.find(".q-select")
            expect(select.exists()).toBeTruthy()
            expect(select.classes()).not.toContain("q-field--readonly")

            // The affiliates toggle should remain visible for admins
            expect(wrapper.find(".affiliates-toggle-under-field").exists()).toBeTruthy()
        })
    })

    describe("API Integration", () => {
        it.each([
            {
                name: "with viewContext",
                props: { viewContext: "rotation", year: 2024, includeAllAffiliates: false },
                expected: { year: 2024, includeAllAffiliates: false, viewContext: "rotation" },
            },
            {
                name: "without viewContext",
                props: { year: 2024, includeAllAffiliates: false },
                expected: { year: 2024, includeAllAffiliates: false, viewContext: undefined },
            },
            {
                name: "clinician viewContext",
                props: { viewContext: "clinician" },
                expected: { year: undefined, includeAllAffiliates: false, viewContext: "clinician" },
            },
        ])("fetches clinicians $name", async ({ props, expected }) => {
            const wrapper = createWrapper(props)

            // Tests without a year do not auto-fetch, so trigger the load manually.
            if (!props.year) {
                await (wrapper.vm as any).fetchClinicians()
            }

            await vi.waitFor(() => {
                expect(ClinicianService.getClinicians).toHaveBeenCalledWith(expected)
            })
        })

        it("loads and displays clinicians", async () => {
            const wrapper = createWrapper({ isOwnScheduleOnly: false })
            await (wrapper.vm as any).fetchClinicians()

            expect(ClinicianService.getClinicians).toHaveBeenCalled()
            expect((wrapper.vm as any).clinicians).toHaveLength(3)
        })

        it("emits selection changes", async () => {
            const wrapper = createWrapper({ isOwnScheduleOnly: false })
            await (wrapper.vm as any).fetchClinicians()

            ;(wrapper.vm as any).selectedClinician = "12345"
            await wrapper.vm.$nextTick()

            const emitted = wrapper.emitted("update:modelValue")?.[0]?.[0] as any
            expect(emitted?.mothraId).toBe("12345")
            expect(emitted?.fullName).toBe("Smith, John")
        })
    })

    describe("Error Handling", () => {
        it("displays and recovers from errors", async () => {
            // Test error state
            mockErrorResponse("Network error")
            const wrapper = createWrapper({ isOwnScheduleOnly: false })
            await (wrapper.vm as any).fetchClinicians()

            expect((wrapper.vm as any).error).toBe("Network error")
            expect(wrapper.find(".q-select").classes()).toContain("q-field--error")

            // Test recovery
            mockSuccessResponse([])
            await (wrapper.vm as any).fetchClinicians()
            expect((wrapper.vm as any).error).toBe(null)
        })
    })

    describe("Affiliates Toggle", () => {
        it.each([
            { show: true, ownSchedule: false, expectedExists: true },
            { show: false, ownSchedule: false, expectedExists: false },
            { show: true, ownSchedule: true, expectedExists: false },
        ])(
            "$expectedExists toggle when showAffiliatesToggle=$show and isOwnScheduleOnly=$ownSchedule",
            async ({ show, ownSchedule, expectedExists }) => {
                const wrapper = createWrapper({
                    isOwnScheduleOnly: ownSchedule,
                    showAffiliatesToggle: show,
                })

                await wrapper.vm.$nextTick()
                expect(wrapper.find(".affiliates-toggle-under-field").exists()).toBe(expectedExists)
            },
        )

        it("disables toggle for past years", async () => {
            const wrapper = createWrapper({
                isOwnScheduleOnly: false,
                showAffiliatesToggle: true,
                isPastYear: true,
            })

            await wrapper.vm.$nextTick()
            const checkbox = wrapper.find(".q-checkbox")
            expect(checkbox.exists()).toBeTruthy()
            expect(checkbox.classes()).toContain("disabled")
        })
    })
})
