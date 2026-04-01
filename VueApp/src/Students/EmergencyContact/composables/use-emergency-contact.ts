import { ref, computed } from "vue"
import { patterns } from "quasar"
import type { StudentContactDetail, UpdateStudentContactRequest, ContactInfo, StudentInfo } from "../types"
import { emergencyContactService } from "../services/emergency-contact-service"

function emptyContactInfo(): ContactInfo {
    return {
        name: null,
        relationship: null,
        workPhone: null,
        homePhone: null,
        cellPhone: null,
        email: null,
    }
}

function emptyStudentInfo(): StudentInfo {
    return {
        address: null,
        city: null,
        zip: null,
        homePhone: null,
        cellPhone: null,
    }
}

/**
 * Counts non-empty fields in a ContactInfo (4 checks: name, relationship, any phone, email).
 */
function contactCompleteness(contact: ContactInfo): number {
    let count = 0
    if (contact.name) {
        count += 1
    }
    if (contact.relationship) {
        count += 1
    }
    if (contact.workPhone || contact.homePhone || contact.cellPhone) {
        count += 1
    }
    if (contact.email) {
        count += 1
    }
    return count
}

/**
 * Counts student info completeness (2 checks: address+city+zip as 1, any phone).
 */
function studentInfoCompleteness(info: StudentInfo): number {
    let count = 0
    if (info.address && info.city && info.zip) {
        count += 1
    }
    if (info.homePhone || info.cellPhone) {
        count += 1
    }
    return count
}

function useEmergencyContact() {
    const loading = ref(false)
    const saving = ref(false)
    const detail = ref<StudentContactDetail | null>(null)
    const saveErrors = ref<string[]>([])

    // Form state
    const studentInfo = ref<StudentInfo>(emptyStudentInfo())
    const contactPermanent = ref(false)
    const localContact = ref<ContactInfo>(emptyContactInfo())
    const emergencyContact = ref<ContactInfo>(emptyContactInfo())
    const permanentContact = ref<ContactInfo>(emptyContactInfo())

    // Snapshot for dirty tracking
    const initialSnapshot = ref("")

    function takeSnapshot(): string {
        return JSON.stringify({
            studentInfo: studentInfo.value,
            contactPermanent: contactPermanent.value,
            localContact: localContact.value,
            emergencyContact: emergencyContact.value,
            permanentContact: permanentContact.value,
        })
    }

    const currentSnapshot = computed(() => takeSnapshot())
    const isDirty = computed(() => initialSnapshot.value !== currentSnapshot.value)

    const studentInfoComplete = computed(() => studentInfoCompleteness(studentInfo.value))
    const localContactComplete = computed(() => contactCompleteness(localContact.value))
    const emergencyContactComplete = computed(() => contactCompleteness(emergencyContact.value))
    const permanentContactComplete = computed(() => contactCompleteness(permanentContact.value))

    /** True when any contact email has an invalid format (empty emails are allowed). */
    const hasValidationErrors = computed(() => {
        const emails = [localContact.value.email, emergencyContact.value.email, permanentContact.value.email]
        return emails.some((e) => e && !patterns.testPattern.email(e))
    })

    function populateForm(data: StudentContactDetail): void {
        studentInfo.value = { ...data.studentInfo }
        contactPermanent.value = data.contactPermanent
        localContact.value = { ...data.localContact }
        emergencyContact.value = { ...data.emergencyContact }
        permanentContact.value = { ...data.permanentContact }
        initialSnapshot.value = takeSnapshot()
    }

    async function loadDetail(personId: number): Promise<void> {
        loading.value = true
        saveErrors.value = []
        const result = await emergencyContactService.getDetail(personId)
        detail.value = result
        if (result) {
            populateForm(result)
        }
        loading.value = false
    }

    function buildRequest(): UpdateStudentContactRequest {
        return {
            studentInfo: { ...studentInfo.value },
            contactPermanent: contactPermanent.value,
            localContact: { ...localContact.value },
            emergencyContact: { ...emergencyContact.value },
            permanentContact: { ...permanentContact.value },
        }
    }

    async function save(personId: number): Promise<boolean> {
        saving.value = true
        saveErrors.value = []
        const request = buildRequest()
        const response = await emergencyContactService.updateContact(personId, request)
        saving.value = false

        if (response.success && response.result) {
            detail.value = response.result
            populateForm(response.result)
            return true
        }

        saveErrors.value = response.errors
        return false
    }

    return {
        loading,
        saving,
        detail,
        saveErrors,
        studentInfo,
        contactPermanent,
        localContact,
        emergencyContact,
        permanentContact,
        isDirty,
        hasValidationErrors,
        studentInfoComplete,
        localContactComplete,
        emergencyContactComplete,
        permanentContactComplete,
        loadDetail,
        save,
        buildRequest,
        populateForm,
    }
}

export { useEmergencyContact, contactCompleteness, studentInfoCompleteness }
