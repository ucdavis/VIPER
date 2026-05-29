import { useEmergencyContact, contactCompleteness, studentInfoCompleteness } from "../composables/use-emergency-contact"
import { emergencyContactService } from "../services/emergency-contact-service"
import type { ContactInfo, StudentInfo, StudentContactDetail } from "../types"

// Mock the service to avoid actual API calls from the composable
vi.mock("../services/emergency-contact-service", () => ({
    emergencyContactService: {
        getDetail: vi.fn<typeof emergencyContactService.getDetail>(),
        updateContact: vi.fn<typeof emergencyContactService.updateContact>(),
    },
}))

function makeDetail(overrides: Partial<StudentContactDetail> = {}): StudentContactDetail {
    return {
        personId: 100,
        fullName: "Test Student",
        classLevel: "V1",
        studentInfo: { address: "123 Main", city: "Davis", zip: "95616", homePhone: null, cellPhone: "5305551234" },
        contactPermanent: false,
        localContact: {
            name: "Local",
            relationship: "Friend",
            workPhone: null,
            homePhone: null,
            cellPhone: "5305552222",
            email: "local@example.com",
        },
        emergencyContact: {
            name: "Emergency",
            relationship: "Parent",
            workPhone: null,
            homePhone: "5305553333",
            cellPhone: null,
            email: "parent@example.com",
        },
        permanentContact: {
            name: null,
            relationship: null,
            workPhone: null,
            homePhone: null,
            cellPhone: null,
            email: null,
        },
        canEdit: true,
        isAdmin: false,
        lastUpdated: "2026-04-17T10:00:00",
        updatedBy: null,
        ...overrides,
    }
}

describe("useEmergencyContact utilities", () => {
    beforeEach(() => {
        vi.clearAllMocks()
    })

    describe(contactCompleteness, () => {
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

        it("should return 4 for fully complete contact", () => {
            const contact: ContactInfo = {
                name: "John Doe",
                relationship: "Father",
                workPhone: "5305551234",
                homePhone: "5305551235",
                cellPhone: "5305551236",
                email: "jdoe@example.com",
            }
            expect(contactCompleteness(contact)).toBe(4)
        })

        it("should count only non-empty fields", () => {
            const contact: ContactInfo = {
                name: "John Doe",
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

    describe(studentInfoCompleteness, () => {
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

        it("should return 2 for fully complete info", () => {
            const info: StudentInfo = {
                address: "One Shields Avenue",
                city: "Davis",
                zip: "95616",
                homePhone: "5305551234",
                cellPhone: "5305551235",
            }
            expect(studentInfoCompleteness(info)).toBe(2)
        })

        it("should require all three address fields for 1 point", () => {
            const info: StudentInfo = {
                address: "One Shields Avenue",
                city: "Davis",
                zip: null,
                homePhone: "5305551234",
                cellPhone: "5305551235",
            }
            // Address group incomplete, but any-phone counts
            expect(studentInfoCompleteness(info)).toBe(1)
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

describe("useEmergencyContact — stateful behavior", () => {
    beforeEach(() => {
        vi.clearAllMocks()
    })

    describe("loadDetail", () => {
        it("populates form and clears loading on success", async () => {
            const detail = makeDetail()
            vi.mocked(emergencyContactService.getDetail).mockResolvedValue(detail)

            const c = useEmergencyContact()
            expect(c.loading.value).toBeFalsy()
            const promise = c.loadDetail(100)
            expect(c.loading.value).toBeTruthy()
            await promise

            expect(c.loading.value).toBeFalsy()
            expect(c.detail.value).toEqual(detail)
            expect(c.studentInfo.value).toEqual(detail.studentInfo)
            expect(c.localContact.value).toEqual(detail.localContact)
            expect(c.isDirty.value).toBeFalsy()
        })

        it("clears loading and leaves detail null when service returns null", async () => {
            vi.mocked(emergencyContactService.getDetail).mockResolvedValue(null)

            const c = useEmergencyContact()
            await c.loadDetail(9999)

            expect(c.loading.value).toBeFalsy()
            expect(c.detail.value).toBeNull()
        })

        it("resets prior saveErrors on re-load", async () => {
            const c = useEmergencyContact()
            c.saveErrors.value = ["stale error"]
            vi.mocked(emergencyContactService.getDetail).mockResolvedValue(makeDetail())

            await c.loadDetail(100)

            expect(c.saveErrors.value).toEqual([])
        })
    })

    describe("isDirty", () => {
        it("is false immediately after populateForm", () => {
            const c = useEmergencyContact()
            c.populateForm(makeDetail())
            expect(c.isDirty.value).toBeFalsy()
        })

        it("flips to true when a field changes", () => {
            const c = useEmergencyContact()
            c.populateForm(makeDetail())
            c.studentInfo.value = { ...c.studentInfo.value, city: "Woodland" }
            expect(c.isDirty.value).toBeTruthy()
        })

        it("resets to false after a successful save", async () => {
            const c = useEmergencyContact()
            c.populateForm(makeDetail())
            c.localContact.value = { ...c.localContact.value, name: "Changed" }
            expect(c.isDirty.value).toBeTruthy()

            vi.mocked(emergencyContactService.updateContact).mockResolvedValue({
                success: true,
                result: makeDetail({ localContact: { ...makeDetail().localContact, name: "Changed" } }),
                errors: [],
            })

            const ok = await c.save(100)

            expect(ok).toBeTruthy()
            expect(c.isDirty.value).toBeFalsy()
        })
    })

    describe("save", () => {
        it("populates saveErrors on failure and returns false", async () => {
            vi.mocked(emergencyContactService.updateContact).mockResolvedValue({
                success: false,
                result: null,
                errors: ["Invalid phone number"],
            })

            const c = useEmergencyContact()
            c.populateForm(makeDetail())
            const ok = await c.save(100)

            expect(ok).toBeFalsy()
            expect(c.saveErrors.value).toEqual(["Invalid phone number"])
            expect(c.saving.value).toBeFalsy()
        })

        it("clears prior saveErrors at the start of a new save", async () => {
            const c = useEmergencyContact()
            c.saveErrors.value = ["previous failure"]
            c.populateForm(makeDetail())

            vi.mocked(emergencyContactService.updateContact).mockResolvedValue({
                success: true,
                result: makeDetail(),
                errors: [],
            })

            await c.save(100)

            expect(c.saveErrors.value).toEqual([])
        })

        it("updates detail and refreshes snapshot on success", async () => {
            const updated = makeDetail({
                lastUpdated: "2026-04-18T12:00:00",
                updatedBy: "admin",
            })
            vi.mocked(emergencyContactService.updateContact).mockResolvedValue({
                success: true,
                result: updated,
                errors: [],
            })

            const c = useEmergencyContact()
            c.populateForm(makeDetail())

            await c.save(100)

            expect(c.detail.value?.lastUpdated).toBe("2026-04-18T12:00:00")
            expect(c.detail.value?.updatedBy).toBe("admin")
            expect(c.isDirty.value).toBeFalsy()
        })
    })

    describe("hasValidationErrors", () => {
        it("returns true when any contact email is malformed", () => {
            const c = useEmergencyContact()
            c.populateForm(makeDetail())
            c.localContact.value = { ...c.localContact.value, email: "not-an-email" }
            expect(c.hasValidationErrors.value).toBeTruthy()
        })

        it("returns false when all emails are valid or empty", () => {
            const c = useEmergencyContact()
            c.populateForm(makeDetail())
            c.permanentContact.value = { ...c.permanentContact.value, email: null }
            expect(c.hasValidationErrors.value).toBeFalsy()
        })
    })
})
