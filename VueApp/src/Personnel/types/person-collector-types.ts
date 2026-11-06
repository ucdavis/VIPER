/** A checkbox choice on the Person Collector form. */
interface PersonCollectorChoice {
    key: string
    label: string
}

/** The form's choices and which optional columns this user will see. */
interface PersonCollectorForm {
    departments: PersonCollectorChoice[]
    senateGroups: PersonCollectorChoice[]
    federationGroups: PersonCollectorChoice[]
    studentClasses: PersonCollectorChoice[]
    showLoginIds: boolean
    showMoreIds: boolean
}

/** What to collect; lists hold choice keys. */
interface PersonCollectorRequest {
    departments: string[]
    fullDepartmentList: boolean
    senateGroups: string[]
    senateEmeriti: boolean
    federationGroups: string[]
    staffMsp: boolean
    staffPss: boolean
    staffVeterinarians: boolean
    studentClasses: string[]
    studentMpvm: boolean
}

/** One person; the ID fields are null unless the user may see them. */
interface PersonCollectorPerson {
    name: string | null
    email: string | null
    loginId: string | null
    employeeId: string | null
    mothraId: string | null
    mailId: string | null
    pidm: string | null
    bannerId: string | null
}

interface PersonCollectorSection {
    key: string
    title: string
    people: PersonCollectorPerson[]
}

interface PersonCollectorResult {
    sections: PersonCollectorSection[]
    showLoginIds: boolean
    showMoreIds: boolean
}

export type {
    PersonCollectorChoice,
    PersonCollectorForm,
    PersonCollectorPerson,
    PersonCollectorRequest,
    PersonCollectorResult,
    PersonCollectorSection,
}
