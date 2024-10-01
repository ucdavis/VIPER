import { computed } from 'vue'

export type Domain = {
    domainId: number,
    name: string,
    order: number,
    description: string | null
}

export type Competency = {
    competencyId: number | null,
    domainId: number | null,
    parentId: number | null,
    name: string,
    number: string,
    description: string | null,
    canLinkToStudent: boolean,
    domain: Domain | null,
    children: Competency[] | null,
    type: string
}

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

    serviceId: number | null
    serviceName: string | null
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

export type Role = {
    roleId: number,
    name: string,
}

export type Bundle = {
    bundleId: number | null,
    name: string,
    clinical: boolean,
    assessment: boolean,
    milestone: boolean,
    roles: Role[]
}

export type BundleRole = {
    bundleRoleId: number,
    bundleId: number,
    roleId: number,
}

export type BundleCompetency = {
    bundleCompetencyId: number,
    bundleId: number,
    roleId: number | null,
    roleName: number | null,
    levels: Level[],
    competencyId: number,
    competencyNumber: string,
    competencyName: string,
    description: string | null,
    canLinkToStudent: boolean,
    bundleCompetencyGroupId: number | null,
    order: number,
}

export type BundleCompetencyAddUpdate = {
    bundleCompetencyId: number | null,
    bundleId: number,
    competencyId: number | null,
    order: number,
    levelIds: number[],
    roleId: number | null,
    bundleCompetencyGroupId: number | null,
}

export type BundleCompetencyGroup = {
    bundleCompetencyGroupId: number | null,
    name: string,
    order: number,
}

export type Milestone = {
    milestoneId: number,
    name: string,
    competencyId: number,
    competencyName: string,
}

export type MilestoneLevel = {
    milestoneLevelId: number | null,
    milestoneId: number,
    levelId: number,
    levelName: string,
    levelOrder: number,
    description: string,
}

export type MilestoneLevelUpdate = {
    levelId: number,
    description: string,
}