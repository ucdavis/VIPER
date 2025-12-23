import { useFetch } from "@/composables/ViperFetch"
import type {
    TermDto,
    AvailableTermDto,
    CourseDto,
    BannerCourseDto,
    CreateCourseRequest,
    UpdateCourseRequest,
    ImportCourseRequest,
    CourseRelationshipDto,
    CourseRelationshipsResult,
    CreateCourseRelationshipRequest,
    PersonDto,
    AaudPersonDto,
    CreateInstructorRequest,
    UpdateInstructorRequest,
    ReportUnitDto,
    DepartmentDto,
    CanDeleteResult,
    InstructorEffortRecordDto,
} from "../types"

const { get, post, put, del, patch } = useFetch()

/**
 * Service for Effort API calls.
 */
class EffortService {
    private baseUrl = `${import.meta.env.VITE_API_URL}effort`

    private static extractErrorMessage(errors: unknown, fallback: string): string {
        if (typeof errors === "string") {
            return errors
        }
        if (Array.isArray(errors)) {
            return errors.join(", ")
        }
        if (errors && typeof errors === "object") {
            const values = Object.values(errors)
            if (values.length > 0) {
                return values.flat().join(", ")
            }
        }
        return fallback
    }

    /**
     * Get all terms with effort status.
     */
    async getTerms(): Promise<TermDto[]> {
        const response = await get(`${this.baseUrl}/terms`)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as TermDto[]
    }

    /**
     * Get a specific term by term code.
     */
    async getTerm(termCode: number): Promise<TermDto | null> {
        const response = await get(`${this.baseUrl}/terms/${termCode}`)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as TermDto | null
    }

    /**
     * Get the current open term.
     */
    async getCurrentTerm(): Promise<TermDto | null> {
        const response = await get(`${this.baseUrl}/terms/current`)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as TermDto | null
    }

    // Term Management Operations (require ManageTerms permission)

    /**
     * Create a new term.
     */
    async createTerm(termCode: number): Promise<TermDto> {
        const response = await post(`${this.baseUrl}/terms`, { termCode })
        return response.result as TermDto
    }

    /**
     * Delete a term. Only succeeds if no related data exists.
     */
    async deleteTerm(termCode: number): Promise<boolean> {
        const response = await del(`${this.baseUrl}/terms/${termCode}`)
        return response.success
    }

    /**
     * Open a term for effort entry.
     */
    async openTerm(termCode: number): Promise<TermDto | null> {
        const response = await post(`${this.baseUrl}/terms/${termCode}/open`, {})
        return response.result as TermDto | null
    }

    /**
     * Close a term.
     */
    async closeTerm(termCode: number): Promise<{ success: boolean; error?: string }> {
        const response = await post(`${this.baseUrl}/terms/${termCode}/close`, {})
        if (!response.success) {
            return { success: false, error: EffortService.extractErrorMessage(response.errors, "Failed to close term") }
        }
        return { success: true }
    }

    /**
     * Reopen a closed term.
     */
    async reopenTerm(termCode: number): Promise<TermDto | null> {
        const response = await post(`${this.baseUrl}/terms/${termCode}/reopen`, {})
        return response.result as TermDto | null
    }

    /**
     * Revert an open term to unopened state.
     */
    async unopenTerm(termCode: number): Promise<TermDto | null> {
        const response = await post(`${this.baseUrl}/terms/${termCode}/unopen`, {})
        return response.result as TermDto | null
    }

    /**
     * Get future terms available to add to the Effort system.
     */
    async getAvailableTerms(): Promise<AvailableTermDto[]> {
        const response = await get(`${this.baseUrl}/terms/available`)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as AvailableTermDto[]
    }

    // Course Operations

    /**
     * Get all courses for a term, optionally filtered by department.
     */
    async getCourses(termCode: number, dept?: string): Promise<CourseDto[]> {
        const params = new URLSearchParams({ termCode: termCode.toString() })
        if (dept) {
            params.append("dept", dept)
        }
        const response = await get(`${this.baseUrl}/courses?${params.toString()}`)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as CourseDto[]
    }

    /**
     * Get a single course by ID.
     */
    async getCourse(courseId: number): Promise<CourseDto | null> {
        const response = await get(`${this.baseUrl}/courses/${courseId}`)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as CourseDto
    }

    /**
     * Search for courses in Banner.
     */
    async searchBannerCourses(
        termCode: number,
        filters: { subjCode?: string; crseNumb?: string; seqNumb?: string; crn?: string } = {},
    ): Promise<BannerCourseDto[]> {
        const params = new URLSearchParams({ termCode: termCode.toString() })
        if (filters.subjCode) {
            params.append("subjCode", filters.subjCode)
        }
        if (filters.crseNumb) {
            params.append("crseNumb", filters.crseNumb)
        }
        if (filters.seqNumb) {
            params.append("seqNumb", filters.seqNumb)
        }
        if (filters.crn) {
            params.append("crn", filters.crn)
        }

        const response = await get(`${this.baseUrl}/courses/search?${params.toString()}`)
        if (!response.success || !Array.isArray(response.result)) {
            throw new Error(EffortService.extractErrorMessage(response.errors, "Search failed"))
        }
        return response.result as BannerCourseDto[]
    }

    /**
     * Import a course from Banner.
     */
    async importCourse(
        request: ImportCourseRequest,
    ): Promise<{ success: boolean; course?: CourseDto; error?: string }> {
        const response = await post(`${this.baseUrl}/courses/import`, request)
        if (!response.success) {
            return {
                success: false,
                error: EffortService.extractErrorMessage(response.errors, "Failed to import course"),
            }
        }
        return { success: true, course: response.result as CourseDto }
    }

    /**
     * Create a course manually.
     */
    async createCourse(
        request: CreateCourseRequest,
    ): Promise<{ success: boolean; course?: CourseDto; error?: string }> {
        const response = await post(`${this.baseUrl}/courses`, request)
        if (!response.success) {
            return {
                success: false,
                error: EffortService.extractErrorMessage(response.errors, "Failed to create course"),
            }
        }
        return { success: true, course: response.result as CourseDto }
    }

    /**
     * Update a course (full update - requires EditCourse permission).
     */
    async updateCourse(
        courseId: number,
        request: UpdateCourseRequest,
    ): Promise<{ success: boolean; course?: CourseDto; error?: string }> {
        const response = await put(`${this.baseUrl}/courses/${courseId}`, request)
        if (!response.success) {
            return {
                success: false,
                error: EffortService.extractErrorMessage(response.errors, "Failed to update course"),
            }
        }
        return { success: true, course: response.result as CourseDto }
    }

    /**
     * Update only the enrollment for an R-course (requires ManageRCourseEnrollment permission).
     */
    async updateCourseEnrollment(
        courseId: number,
        enrollment: number,
    ): Promise<{ success: boolean; course?: CourseDto; error?: string }> {
        const response = await patch(`${this.baseUrl}/courses/${courseId}/enrollment`, { enrollment })
        if (!response.success) {
            return {
                success: false,
                error: EffortService.extractErrorMessage(response.errors, "Failed to update enrollment"),
            }
        }
        return { success: true, course: response.result as CourseDto }
    }

    /**
     * Delete a course.
     */
    async deleteCourse(courseId: number): Promise<boolean> {
        const response = await del(`${this.baseUrl}/courses/${courseId}`)
        return response.success
    }

    /**
     * Check if a course can be deleted.
     */
    async canDeleteCourse(courseId: number): Promise<{ canDelete: boolean; recordCount: number }> {
        const response = await get(`${this.baseUrl}/courses/${courseId}/can-delete`)
        if (!response.success || !response.result) {
            return { canDelete: false, recordCount: 0 }
        }
        return response.result as { canDelete: boolean; recordCount: number }
    }

    /**
     * Get valid custodial department codes.
     */
    async getDepartments(): Promise<string[]> {
        const response = await get(`${this.baseUrl}/courses/departments`)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as string[]
    }

    // Course Relationship Operations

    /**
     * Get all relationships for a course (both as parent and child).
     */
    async getCourseRelationships(courseId: number): Promise<CourseRelationshipsResult> {
        const response = await get(`${this.baseUrl}/courses/${courseId}/relationships`)
        if (!response.success || !response.result) {
            return { parentRelationship: null, childRelationships: [] }
        }
        return response.result as CourseRelationshipsResult
    }

    /**
     * Get courses available to be linked as children of a parent course.
     */
    async getAvailableChildCourses(parentCourseId: number): Promise<CourseDto[]> {
        const response = await get(`${this.baseUrl}/courses/${parentCourseId}/relationships/available-children`)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as CourseDto[]
    }

    /**
     * Create a course relationship.
     */
    async createCourseRelationship(
        parentCourseId: number,
        request: CreateCourseRelationshipRequest,
    ): Promise<{ success: boolean; relationship?: CourseRelationshipDto; error?: string }> {
        const response = await post(`${this.baseUrl}/courses/${parentCourseId}/relationships`, request)
        if (!response.success) {
            return {
                success: false,
                error: EffortService.extractErrorMessage(response.errors, "Failed to create relationship"),
            }
        }
        return { success: true, relationship: response.result as CourseRelationshipDto }
    }

    /**
     * Delete a course relationship.
     */
    async deleteCourseRelationship(parentCourseId: number, relationshipId: number): Promise<boolean> {
        const response = await del(`${this.baseUrl}/courses/${parentCourseId}/relationships/${relationshipId}`)
        return response.success
    }

    // Instructor Operations

    /**
     * Get all instructors for a term, optionally filtered by department.
     */
    async getInstructors(termCode: number, dept?: string): Promise<PersonDto[]> {
        const params = new URLSearchParams({ termCode: termCode.toString() })
        if (dept) {
            params.append("dept", dept)
        }
        const response = await get(`${this.baseUrl}/instructors?${params.toString()}`)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as PersonDto[]
    }

    /**
     * Get a single instructor by person ID and term code.
     */
    async getInstructor(personId: number, termCode: number): Promise<PersonDto | null> {
        const response = await get(`${this.baseUrl}/instructors/${personId}?termCode=${termCode}`)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as PersonDto
    }

    /**
     * Search for possible instructors in AAUD who can be added to the term.
     */
    async searchPossibleInstructors(termCode: number, searchTerm?: string): Promise<AaudPersonDto[]> {
        const params = new URLSearchParams({ termCode: termCode.toString() })
        if (searchTerm) {
            params.append("q", searchTerm)
        }
        const response = await get(`${this.baseUrl}/instructors/search?${params.toString()}`)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as AaudPersonDto[]
    }

    /**
     * Create an instructor (add to term).
     */
    async createInstructor(
        request: CreateInstructorRequest,
    ): Promise<{ success: boolean; instructor?: PersonDto; error?: string }> {
        const response = await post(`${this.baseUrl}/instructors`, request)
        if (!response.success) {
            return {
                success: false,
                error: EffortService.extractErrorMessage(response.errors, "Failed to add instructor"),
            }
        }
        return { success: true, instructor: response.result as PersonDto }
    }

    /**
     * Update an instructor's details.
     */
    async updateInstructor(
        personId: number,
        termCode: number,
        request: UpdateInstructorRequest,
    ): Promise<{ success: boolean; instructor?: PersonDto; error?: string }> {
        const response = await put(`${this.baseUrl}/instructors/${personId}?termCode=${termCode}`, request)
        if (!response.success) {
            return {
                success: false,
                error: EffortService.extractErrorMessage(response.errors, "Failed to update instructor"),
            }
        }
        return { success: true, instructor: response.result as PersonDto }
    }

    /**
     * Delete an instructor and their effort records for the term.
     */
    async deleteInstructor(personId: number, termCode: number): Promise<boolean> {
        const response = await del(`${this.baseUrl}/instructors/${personId}?termCode=${termCode}`)
        return response.success
    }

    /**
     * Check if an instructor can be deleted and get count of effort records.
     */
    async canDeleteInstructor(personId: number, termCode: number): Promise<CanDeleteResult> {
        const response = await get(`${this.baseUrl}/instructors/${personId}/can-delete?termCode=${termCode}`)
        if (!response.success || !response.result) {
            return { canDelete: false, recordCount: 0 }
        }
        return response.result as CanDeleteResult
    }

    /**
     * Get valid departments with grouping info.
     */
    async getInstructorDepartments(): Promise<DepartmentDto[]> {
        const response = await get(`${this.baseUrl}/instructors/departments`)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as DepartmentDto[]
    }

    /**
     * Get all report units for the multi-select dropdown.
     */
    async getReportUnits(): Promise<ReportUnitDto[]> {
        const response = await get(`${this.baseUrl}/instructors/report-units`)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as ReportUnitDto[]
    }

    /**
     * Get effort records for an instructor in a specific term.
     */
    async getInstructorEffortRecords(personId: number, termCode: number): Promise<InstructorEffortRecordDto[]> {
        const response = await get(`${this.baseUrl}/instructors/${personId}/effort?termCode=${termCode}`)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as InstructorEffortRecordDto[]
    }
}

const effortService = new EffortService()
export { effortService }
