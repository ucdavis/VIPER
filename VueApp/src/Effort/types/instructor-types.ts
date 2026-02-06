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

type ChildCourseDto = {
    id: number
    subjCode: string
    crseNumb: string
    seqNumb: string
    units: number
    enrollment: number
    relationshipType: string
}

type InstructorEffortRecordDto = {
    id: number
    courseId: number
    personId: number
    termCode: number
    effortType: string
    effortTypeDescription: string
    role: number
    roleDescription: string
    hours: number | null
    weeks: number | null
    crn: string
    notes: string | null
    modifiedDate: string | null
    effortValue: number | null
    effortLabel: string
    course: CourseDto
    childCourses: ChildCourseDto[]
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

// Effort record types

type CourseOptionDto = {
    id: number
    subjCode: string
    crseNumb: string
    seqNumb: string
    units: number
    label: string
    crn: string
    isDvm: boolean
    is199299: boolean
    isRCourse: boolean
}

type AvailableCoursesDto = {
    existingCourses: CourseOptionDto[]
    allCourses: CourseOptionDto[]
}

type EffortTypeOptionDto = {
    id: string
    description: string
    usesWeeks: boolean
    allowedOnDvm: boolean
    allowedOn199299: boolean
    allowedOnRCourses: boolean
}

type RoleOptionDto = {
    id: number
    description: string
}

type CreateEffortRecordRequest = {
    personId: number
    termCode: number
    courseId: number
    effortTypeId: string
    roleId: number
    effortValue: number
    notes?: string | null
}

type UpdateEffortRecordRequest = {
    effortTypeId: string
    roleId: number
    effortValue: number
    notes?: string | null
    /** ModifiedDate from when record was loaded - for optimistic concurrency */
    originalModifiedDate?: string | null
}

type EffortRecordResult = {
    record: InstructorEffortRecordDto
    warning: string | null
}

export type {
    AaudPersonDto,
    CreateInstructorRequest,
    UpdateInstructorRequest,
    ReportUnitDto,
    DepartmentDto,
    CanDeleteResult,
    ChildCourseDto,
    InstructorEffortRecordDto,
    TitleCodeDto,
    JobGroupDto,
    PercentAssignTypeDto,
    InstructorByPercentAssignTypeDto,
    InstructorsByPercentAssignTypeResponseDto,
    CourseOptionDto,
    AvailableCoursesDto,
    EffortTypeOptionDto,
    RoleOptionDto,
    CreateEffortRecordRequest,
    UpdateEffortRecordRequest,
    EffortRecordResult,
}
