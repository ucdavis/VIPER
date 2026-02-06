import { useFetch } from "@/composables/ViperFetch"
import type {
    PersonDto,
    AaudPersonDto,
    CreateInstructorRequest,
    UpdateInstructorRequest,
    ReportUnitDto,
    DepartmentDto,
    CanDeleteResult,
    InstructorEffortRecordDto,
    TitleCodeDto,
    JobGroupDto,
} from "../types"

const { get, post, put, del } = useFetch()

class InstructorService {
    private baseUrl = `${import.meta.env.VITE_API_URL}effort`

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
                error: response.errors?.[0] ?? "Failed to add instructor",
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
                error: response.errors?.[0] ?? "Failed to update instructor",
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

    /**
     * Get all title codes from dictionary database for the dropdown.
     */
    async getTitleCodes(): Promise<TitleCodeDto[]> {
        const response = await get(`${this.baseUrl}/instructors/title-codes`)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as TitleCodeDto[]
    }

    /**
     * Get all job groups currently in use for the dropdown.
     */
    async getJobGroups(): Promise<JobGroupDto[]> {
        const response = await get(`${this.baseUrl}/instructors/job-groups`)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as JobGroupDto[]
    }
}

const instructorService = new InstructorService()
export { instructorService }
