/**
 * Shapes returned by /api/personnel/eis, mirroring the C# records in
 * web/Areas/Personnel/Models/Eis/EisDtos.cs. Dates are ISO strings ("2026-07-01").
 */

interface EisPersonOption {
    employeeId: string
    name: string
}

interface EisPersonHeader {
    employeeId: string
    name: string
    email: string | null
    dateOfBirth: string | null
    age: number | null
    gender: string | null
    ethnicity: string | null
    hireDate: string | null
    employmentStatus: string | null
    citizenship: string | null
    visa: string | null
    primaryAffiliation: string | null
    primaryTitle: string | null
    jobGroup: string | null
    homeDepartment: string | null
    alternateDepartment: string | null
    bargainingUnit: string | null
    laborRelationsUnit: string | null
    isStaff: boolean
    staffProgram: string | null
    staffStatus: string | null
    isFaculty: boolean
    facultyProgram: string | null
    ladderRank: string | null
    serviceCreditMonths: number
    vacationHours: number | null
    sickHours: number | null
    ptoHours: number | null
}

interface EisDistribution {
    number: string | null
    beginDate: string
    endDate: string | null
    percent: number | null
    account: string | null
    step: string
    dosCode: string | null
    amount: number
}

interface EisAppointment {
    number: string | null
    title: string
    titleCode: string
    grade: string | null
    exemptStatus: string
    department: string
    beginDate: string | null
    endDate: string | null
    distributions: EisDistribution[]
    total: number | null
}

interface EisStipend {
    effectiveDate: string
    endDate: string | null
    earningsCode: string
    annualAmount: number
}

interface EisAppointments {
    appointments: EisAppointment[]
    stipends: EisStipend[]
    totalAnnual: number
}

interface EisHistoryEntry {
    actionDate: string | null
    title: string | null
    titleCode: string | null
    department: string | null
    beginDate: string | null
    endDate: string | null
    step: string | null
    percent: number | null
    payRate: number | null
    comment: string | null
}

interface EisLeave {
    beginDate: string | null
    returnDate: string | null
    description: string | null
}

interface EisHistory {
    appointments: EisHistoryEntry[]
    ppsAppointments: EisHistoryEntry[]
    leaves: EisLeave[]
    ppsLeaves: EisLeave[]
}

interface EisPermanentAddress {
    line1: string
    line2: string | null
    city: string
    state: string
    zip: string
    releaseCampus: string
    releaseOrganization: string
}

interface EisHomePhone {
    phone: string
    releaseCampus: string | null
    releaseOrganization: string | null
}

interface EisCampusListing {
    isPrimary: boolean
    isPublic: boolean
    title: string | null
    department: string | null
    address: string | null
    phone: string | null
}

interface EisAddress {
    permanent: EisPermanentAddress | null
    homePhone: EisHomePhone | null
    campusListings: EisCampusListing[]
    campusDirectoryAvailable: boolean
}

/** One row of the Appointment Category page; flagCode is set for the manual categories. */
interface EisCategory {
    label: string
    applies: boolean
    flagCode: number | null
}

interface EisAppointmentCategories {
    categories: EisCategory[]
    academicYear: string
    canEditFlags: boolean
}

interface EisDegree {
    year: string | null
    degree: string | null
    institution: string | null
    location: string | null
    field: string | null
}

interface EisDatedItem {
    year: string | null
    text: string
}

interface EisAcademics {
    hasMivAccount: boolean
    degrees: EisDegree[]
    boards: EisDatedItem[]
    memberships: string[]
    researchFocus: string[]
    specialtyFocus: string[]
    honors: EisDatedItem[]
}

export type {
    EisAcademics,
    EisAddress,
    EisAppointment,
    EisAppointments,
    EisAppointmentCategories,
    EisCampusListing,
    EisCategory,
    EisDatedItem,
    EisDegree,
    EisDistribution,
    EisHistory,
    EisHistoryEntry,
    EisHomePhone,
    EisLeave,
    EisPermanentAddress,
    EisPersonHeader,
    EisPersonOption,
    EisStipend,
}
