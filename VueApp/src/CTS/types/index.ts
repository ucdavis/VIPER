import { computed } from 'vue'
export type Epa = {
    epaId: number | null
    order: number | null
    name: string
    description: string,
    active: boolean,
    services: Service[]
}

export type Assessment = {
    AssessmentType: string
    levelId: number
    levelName: string
    levelValue: number
    comment: string | null

    encounterId: number
    encounterType: string
    encounterDate: Date
    enteredOn: Date
    editComment: string | null

    studentUserId: number
    studentName: string
    studentMailId: string
    enteredBy: number
    enteredByName: string
    editable: boolean | null

    epaId: number | null
    epaName: string | null
}

export type StudentEpaFormData = {
    epaId: number,
    levelId: number,
    comment: string,
    studentId: number,
    serviceId: number,
    encounterDate: Date | null
}

export type Level = {
    levelId: number,
    levelName: string,
    order: number,
    description: string,
    active: boolean,
    epa: boolean,
    course: boolean,
    clinical: boolean,
    milestone: boolean,
    dops: boolean
}

export type ServiceSelect = {
    serviceId: number,
    serviceName: string,
    thisWeek: boolean,
    lastWeek: boolean,
    scheduled: boolean
}

export type Service = {
    serviceId: number,
    serviceName: string
}

export type Student = {
    personId: number,
    firstName: string,
    lastName: string,
    mothraId: string,
    iamId: string,
    mailId: string,
    classLevel: string | null,
    classYear: number | null,
    active: boolean,
    currentClassYear: boolean,
}

export type Person = {
    personId: number,
    firstName: string,
    lastName: string,
    fullName: string,
    fullNameLastFirst: string,
}