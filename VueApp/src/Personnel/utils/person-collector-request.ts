import type { PersonCollectorRequest } from "../types/person-collector-types"

/** The degree students choose alongside the vet classes. */
const MPVM = "MPVM"

/** A request with nothing chosen. */
function emptyRequest(): PersonCollectorRequest {
    return {
        departments: [],
        fullDepartmentList: false,
        senateGroups: [],
        senateEmeriti: false,
        federationGroups: [],
        staffMsp: false,
        staffPss: false,
        staffVeterinarians: false,
        studentClasses: [],
        studentMpvm: false,
    }
}

/**
 * Whether the request asks for any list. Departments alone only filter the other lists, so they
 * count only together with "full department list", as on the legacy page.
 */
function hasChoice(request: PersonCollectorRequest): boolean {
    return (
        (request.fullDepartmentList && request.departments.length > 0) ||
        request.senateEmeriti ||
        request.senateGroups.length > 0 ||
        request.federationGroups.length > 0 ||
        request.staffMsp ||
        request.staffPss ||
        request.staffVeterinarians ||
        request.studentClasses.length > 0 ||
        request.studentMpvm
    )
}

/** The request to send: the full-list flag only counts when a department is chosen. */
function toApiRequest(request: PersonCollectorRequest): PersonCollectorRequest {
    return { ...request, fullDepartmentList: request.fullDepartmentList && request.departments.length > 0 }
}

/** The student picker's value: the chosen classes, plus MPVM when it is chosen. */
function studentKeys(request: PersonCollectorRequest): string[] {
    return request.studentMpvm ? [...request.studentClasses, MPVM] : request.studentClasses
}

/** The request with the student picker's value split back into classes and the MPVM flag. */
function withStudentKeys(request: PersonCollectorRequest, keys: string[]): PersonCollectorRequest {
    return { ...request, studentClasses: keys.filter((key) => key !== MPVM), studentMpvm: keys.includes(MPVM) }
}

export { MPVM, emptyRequest, hasChoice, studentKeys, toApiRequest, withStudentKeys }
