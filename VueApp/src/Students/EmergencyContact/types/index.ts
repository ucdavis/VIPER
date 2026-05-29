type ContactInfo = {
    name: string | null
    relationship: string | null
    workPhone: string | null
    homePhone: string | null
    cellPhone: string | null
    email: string | null
}

type StudentInfo = {
    address: string | null
    city: string | null
    zip: string | null
    homePhone: string | null
    cellPhone: string | null
}

type StudentContactListItem = {
    personId: number
    rowKey: string
    hasDetailRoute: boolean
    fullName: string
    classLevel: string
    email: string
    cellPhone: string | null
    studentInfoComplete: number
    studentInfoTotal: number
    studentInfoMissing: string[]
    localContactComplete: number
    localContactTotal: number
    localContactMissing: string[]
    emergencyContactComplete: number
    emergencyContactTotal: number
    emergencyContactMissing: string[]
    permanentContactComplete: number
    permanentContactTotal: number
    permanentContactMissing: string[]
    lastUpdated: string | null
    completenessStatus: string
}

type StudentContactDetail = {
    personId: number
    fullName: string
    classLevel: string
    studentInfo: StudentInfo
    contactPermanent: boolean
    localContact: ContactInfo
    emergencyContact: ContactInfo
    permanentContact: ContactInfo
    canEdit: boolean
    isAdmin: boolean
    lastUpdated: string | null
    updatedBy: string | null
}

type UpdateStudentContactRequest = {
    studentInfo: StudentInfo
    contactPermanent: boolean
    localContact: ContactInfo
    emergencyContact: ContactInfo
    permanentContact: ContactInfo
}

type StudentContactReport = {
    personId: number
    rowKey: string
    fullName: string
    classLevel: string
    address: string | null
    city: string | null
    zip: string | null
    homePhone: string | null
    cellPhone: string | null
    localContact: ContactInfo
    emergencyContact: ContactInfo
    permanentContact: ContactInfo
    contactPermanent: boolean
}

type IndividualAccess = {
    personId: number
    fullName: string
}

type AppAccessStatus = {
    appOpen: boolean
    individualGrants: IndividualAccess[]
}

export type {
    ContactInfo,
    StudentInfo,
    StudentContactListItem,
    StudentContactDetail,
    UpdateStudentContactRequest,
    StudentContactReport,
    IndividualAccess,
    AppAccessStatus,
}
