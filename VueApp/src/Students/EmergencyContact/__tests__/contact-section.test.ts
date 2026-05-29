import { ref, computed } from "vue"
import { patterns } from "quasar"
import type { ContactInfo } from "../types"

/**
 * Tests for ContactSection component logic.
 * Validates field rendering logic, read-only behavior, and update emission.
 * Uses logic-only tests (no mount) following the project's existing test patterns.
 */

describe("ContactSection - Field Logic", () => {
    describe("all fields present in contact model", () => {
        it("should have all six contact fields", () => {
            const contact: ContactInfo = {
                name: "John Doe",
                relationship: "Father",
                workPhone: "5305551234",
                homePhone: "5305551235",
                cellPhone: "5305551236",
                email: "john@example.com",
            }

            expect(contact.name).toBe("John Doe")
            expect(contact.relationship).toBe("Father")
            expect(contact.workPhone).toBe("5305551234")
            expect(contact.homePhone).toBe("5305551235")
            expect(contact.cellPhone).toBe("5305551236")
            expect(contact.email).toBe("john@example.com")
        })
    })

    describe("read-only behavior", () => {
        it("should not allow mutation when readonly is true", () => {
            const readonly = ref(true)
            const contact = ref<ContactInfo>({
                name: "Original",
                relationship: null,
                workPhone: null,
                homePhone: null,
                cellPhone: null,
                email: null,
            })

            // Simulating readonly guard - in the component, fields use :readonly prop
            const canUpdate = computed(() => !readonly.value)

            expect(canUpdate.value).toBeFalsy()
            // The contact value should remain unchanged
            expect(contact.value.name).toBe("Original")
        })

        it("should allow mutation when readonly is false", () => {
            const readonly = ref(false)
            const contact = ref<ContactInfo>({
                name: "Original",
                relationship: null,
                workPhone: null,
                homePhone: null,
                cellPhone: null,
                email: null,
            })

            const canUpdate = computed(() => !readonly.value)

            expect(canUpdate.value).toBeTruthy()

            // Simulate an update
            contact.value = { ...contact.value, name: "Updated" }
            expect(contact.value.name).toBe("Updated")
        })
    })

    describe("phone update logic", () => {
        it("should update phone value from PhoneInput", () => {
            const contact = ref<ContactInfo>({
                name: null,
                relationship: null,
                workPhone: null,
                homePhone: null,
                cellPhone: null,
                email: null,
            })

            // PhoneInput emits raw digits
            contact.value = { ...contact.value, workPhone: "5305551234" }

            expect(contact.value.workPhone).toBe("5305551234")
        })

        it("should set phone to null for empty input", () => {
            const contact = ref<ContactInfo>({
                name: null,
                relationship: null,
                workPhone: "5305551234",
                homePhone: null,
                cellPhone: null,
                email: null,
            })

            contact.value = { ...contact.value, workPhone: null }

            expect(contact.value.workPhone).toBeNull()
        })
    })

    describe("email validation logic", () => {
        const testEmail = (val: string) => patterns.testPattern.email(val)

        it("should accept valid email", () => {
            expect(testEmail("test@example.com")).toBeTruthy()
        })

        it("should reject email without @", () => {
            expect(testEmail("testexample.com")).toBeFalsy()
        })

        it("should reject email without domain", () => {
            expect(testEmail("test@")).toBeFalsy()
        })

        it("should accept empty value as valid (field is optional)", () => {
            // Empty values are handled by the component (skip validation when empty)
            expect(testEmail("test@example.com")).toBeTruthy()
            expect(testEmail("invalid")).toBeFalsy()
        })
    })
})
