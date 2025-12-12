import { useFetch } from "@/composables/ViperFetch"
import type { TermDto, AvailableTermDto } from "../types"

const { get, post, del } = useFetch()

/**
 * Service for Effort API calls.
 */
class EffortService {
    private baseUrl = `${import.meta.env.VITE_API_URL}effort`

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
            let errorMessage = "Failed to close term"
            if (typeof response.errors === "string") {
                errorMessage = response.errors
            } else if (Array.isArray(response.errors)) {
                errorMessage = response.errors.join(", ")
            }
            return { success: false, error: errorMessage }
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
}

const effortService = new EffortService()
export { effortService }
