import { useFetch } from "@/composables/ViperFetch"
import type {
    CourseDto,
    BannerCourseDto,
    CreateCourseRequest,
    UpdateCourseRequest,
    ImportCourseRequest,
    CourseRelationshipDto,
    CourseRelationshipsResult,
    CreateCourseRelationshipRequest,
    CourseEffortResponseDto,
    CourseInstructorOptionsDto,
    CourseEvaluationStatusDto,
    CreateAdHocEvalRequest,
    UpdateAdHocEvalRequest,
    AdHocEvalResultDto,
} from "../types"

const { get, post, put, del, patch } = useFetch()

class CourseService {
    private baseUrl = `${import.meta.env.VITE_API_URL}effort`

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
            throw new Error(response.errors?.[0] ?? "Search failed")
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
                error: response.errors?.[0] ?? "Failed to import course",
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
                error: response.errors?.[0] ?? "Failed to create course",
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
                error: response.errors?.[0] ?? "Failed to update course",
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
                error: response.errors?.[0] ?? "Failed to update enrollment",
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
                error: response.errors?.[0] ?? "Failed to create relationship",
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

    // Course Effort Operations

    /**
     * Get all effort records for a course, including permission metadata.
     */
    async getCourseEffort(courseId: number): Promise<CourseEffortResponseDto> {
        const response = await get(`${this.baseUrl}/courses/${courseId}/effort`)
        if (!response.success || !response.result) {
            return { courseId, termCode: 0, canAddEffort: false, isChildCourse: false, records: [] }
        }
        return response.result as CourseEffortResponseDto
    }

    /**
     * Get possible instructors for adding effort to a course.
     * Returns instructors grouped by those already on the course and all available.
     */
    async getPossibleInstructors(courseId: number): Promise<CourseInstructorOptionsDto> {
        const response = await get(`${this.baseUrl}/courses/${courseId}/possible-instructors`)
        if (!response.success || !response.result) {
            return { existingInstructors: [], otherInstructors: [] }
        }
        return response.result as CourseInstructorOptionsDto
    }

    // Self-Service Course Import Operations

    /**
     * Search for Banner courses that the current user is an instructor for.
     * Only returns courses where the user is listed as instructor in Banner.
     * Excludes DVM/VET courses.
     */
    async searchBannerCoursesForSelf(
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

        const response = await get(`${this.baseUrl}/courses/self-service/search?${params.toString()}`)
        if (!response.success || !Array.isArray(response.result)) {
            throw new Error(response.errors?.[0] ?? "Search failed")
        }
        return response.result as BannerCourseDto[]
    }

    /**
     * Import a course from Banner for self-service (instructor importing their own course).
     * This is idempotent - returns the existing course if already imported.
     */
    async importCourseForSelf(
        request: ImportCourseRequest,
    ): Promise<{ success: boolean; course?: CourseDto; wasExisting?: boolean; message?: string; error?: string }> {
        const response = await post(`${this.baseUrl}/courses/self-service/import`, request)
        if (!response.success) {
            return {
                success: false,
                error: response.errors?.[0] ?? "Failed to import course",
            }
        }
        const result = response.result as {
            success: boolean
            course?: CourseDto
            wasExisting?: boolean
            message?: string
            error?: string
        }
        return result
    }

    // Evaluation Operations

    /**
     * Get evaluation status for all instructors on a course (and its children).
     */
    async getCourseEvaluations(courseId: number): Promise<CourseEvaluationStatusDto> {
        const response = await get(`${this.baseUrl}/courses/${courseId}/evaluations`)
        if (!response.success || !response.result) {
            return { canEditAdHoc: false, maxRatingCount: 500, instructors: [], courses: [] }
        }
        return response.result as CourseEvaluationStatusDto
    }

    /**
     * Create an ad-hoc evaluation record.
     */
    async createEvaluation(courseId: number, request: CreateAdHocEvalRequest): Promise<AdHocEvalResultDto> {
        const response = await post(`${this.baseUrl}/courses/${courseId}/evaluations`, request)
        if (!response.success) {
            return { success: false, error: response.errors?.[0] ?? "Failed to create evaluation" }
        }
        return response.result as AdHocEvalResultDto
    }

    /**
     * Update an ad-hoc evaluation record.
     */
    async updateEvaluation(
        courseId: number,
        quantId: number,
        request: UpdateAdHocEvalRequest,
    ): Promise<AdHocEvalResultDto> {
        const response = await put(`${this.baseUrl}/courses/${courseId}/evaluations/${quantId}`, request)
        if (!response.success) {
            return { success: false, error: response.errors?.[0] ?? "Failed to update evaluation" }
        }
        return response.result as AdHocEvalResultDto
    }

    /**
     * Delete an ad-hoc evaluation record.
     */
    async deleteEvaluation(courseId: number, quantId: number): Promise<boolean> {
        const response = await del(`${this.baseUrl}/courses/${courseId}/evaluations/${quantId}`)
        return response.success
    }
}

const courseService = new CourseService()
export { courseService }
