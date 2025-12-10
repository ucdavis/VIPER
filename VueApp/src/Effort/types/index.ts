/**
 * TypeScript types for the Effort system.
 */

type TermDto = {
    termCode: number
    termName: string
    status: string
    harvestedDate: string | null
    openedDate: string | null
    closedDate: string | null
    isOpen: boolean
    canEdit: boolean
}

type PersonDto = {
    personId: number
    termCode: number
    firstName: string
    lastName: string
    middleInitial: string | null
    fullName: string
    effortTitleCode: string
    effortDept: string
    percentAdmin: number
    title: string | null
    adminUnit: string | null
    effortVerified: string | null
    reportUnit: string | null
    percentClinical: number | null
    isVerified: boolean
}

type CourseDto = {
    id: number
    crn: string
    termCode: number
    subjCode: string
    crseNumb: string
    seqNumb: string
    courseCode: string
    enrollment: number
    units: number
    custDept: string
}

type RecordDto = {
    id: number
    courseId: number
    personId: number
    termCode: number
    sessionType: string
    role: number
    roleDescription: string
    hours: number | null
    weeks: number | null
    crn: string
    modifiedDate: string | null
    effortValue: number | null
    effortLabel: string
}

export type { TermDto, PersonDto, CourseDto, RecordDto }
