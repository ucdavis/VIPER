/**
 * Instructor management types for the Effort system.
 */

import type { CourseDto } from "./course-types"

type AaudPersonDto = {
    personId: number
    firstName: string
    lastName: string
    middleInitial: string | null
    fullName: string
    effortDept: string | null
    deptName: string | null
    titleCode: string | null
    title: string | null
    jobGroupId: string | null
}

type CreateInstructorRequest = {
    personId: number
    termCode: number
}

type UpdateInstructorRequest = {
    effortDept: string
    effortTitleCode: string
    jobGroupId: string | null
    reportUnits: string[] | null
    volunteerWos: boolean
}

type ReportUnitDto = {
    abbrev: string
    unit: string
}

type DepartmentDto = {
    code: string
    name: string
    group: string
}

type CanDeleteResult = {
    canDelete: boolean
    recordCount: number
}

type InstructorEffortRecordDto = {
    id: number
    courseId: number
    personId: number
    termCode: number
    effortType: string
    role: number
    roleDescription: string
    hours: number | null
    weeks: number | null
    crn: string
    modifiedDate: string | null
    effortValue: number | null
    effortLabel: string
    course: CourseDto
}

type TitleCodeDto = {
    code: string
    name: string
}

type JobGroupDto = {
    code: string
    name: string
}

type PercentAssignTypeDto = {
    id: number
    class: string
    name: string
    showOnTemplate: boolean
    isActive: boolean
    instructorCount: number
}

type InstructorByPercentAssignTypeDto = {
    personId: number
    firstName: string
    lastName: string
    fullName: string
    academicYear: string
}

type InstructorsByPercentAssignTypeResponseDto = {
    typeId: number
    typeName: string
    typeClass: string
    instructors: InstructorByPercentAssignTypeDto[]
}

export type {
    AaudPersonDto,
    CreateInstructorRequest,
    UpdateInstructorRequest,
    ReportUnitDto,
    DepartmentDto,
    CanDeleteResult,
    InstructorEffortRecordDto,
    TitleCodeDto,
    JobGroupDto,
    PercentAssignTypeDto,
    InstructorByPercentAssignTypeDto,
    InstructorsByPercentAssignTypeResponseDto,
}
