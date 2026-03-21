import { describe, it, expect, vi, beforeEach } from "vitest"
import { contactCompleteness, studentInfoCompleteness } from "../composables/use-emergency-contact"
import type { ContactInfo, StudentInfo } from "../types"

// Mock the service to avoid actual API calls from the composable
vi.mock("../services/emergency-contact-service", () => ({
    emergencyContactService: {
        getDetail: vi.fn(),
        updateContact: vi.fn(),
    },
}))

describe("useEmergencyContact utilities", () => {
    beforeEach(() => {
        vi.clearAllMocks()
    })

    describe("contactCompleteness", () => {
        it("should return 0 for empty contact", () => {
            const contact: ContactInfo = {
                name: null,
                relationship: null,
                workPhone: null,
                homePhone: null,
                cellPhone: null,
                email: null,
            }
            expect(contactCompleteness(contact)).toBe(0)
        })

        it("should return 6 for fully complete contact", () => {
            const contact: ContactInfo = {
                name: "John",
                relationship: "Father",
                workPhone: "5305551234",
                homePhone: "5305551235",
                cellPhone: "5305551236",
                email: "john@example.com",
            }
            expect(contactCompleteness(contact)).toBe(6)
        })

        it("should count only non-empty fields", () => {
            const contact: ContactInfo = {
                name: "John",
                relationship: "Father",
                workPhone: null,
                homePhone: null,
                cellPhone: "5305551236",
                email: null,
            }
            expect(contactCompleteness(contact)).toBe(3)
        })

        it("should treat empty string as incomplete", () => {
            const contact: ContactInfo = {
                name: "",
                relationship: "",
                workPhone: "",
                homePhone: "",
                cellPhone: "",
                email: "",
            }
            expect(contactCompleteness(contact)).toBe(0)
        })
    })

    describe("studentInfoCompleteness", () => {
        it("should return 0 for empty info", () => {
            const info: StudentInfo = {
                address: null,
                city: null,
                zip: null,
                homePhone: null,
                cellPhone: null,
            }
            expect(studentInfoCompleteness(info)).toBe(0)
        })

        it("should return 3 for fully complete info", () => {
            const info: StudentInfo = {
                address: "123 Main St",
                city: "Davis",
                zip: "95616",
                homePhone: "5305551234",
                cellPhone: "5305551235",
            }
            expect(studentInfoCompleteness(info)).toBe(3)
        })

        it("should require all three address fields for 1 point", () => {
            const info: StudentInfo = {
                address: "123 Main St",
                city: "Davis",
                zip: null,
                homePhone: "5305551234",
                cellPhone: "5305551235",
            }
            // Address group incomplete, but phones count
            expect(studentInfoCompleteness(info)).toBe(2)
        })

        it("should count address group as 0 when only some fields filled", () => {
            const info: StudentInfo = {
                address: "123 Main St",
                city: null,
                zip: null,
                homePhone: null,
                cellPhone: null,
            }
            expect(studentInfoCompleteness(info)).toBe(0)
        })

        it("should count phones independently", () => {
            const info: StudentInfo = {
                address: null,
                city: null,
                zip: null,
                homePhone: "5305551234",
                cellPhone: null,
            }
            expect(studentInfoCompleteness(info)).toBe(1)
        })
    })
})
