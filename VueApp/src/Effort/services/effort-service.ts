import { useFetch } from "@/composables/ViperFetch"
import type { TermDto } from "../types"

const { get } = useFetch()

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
        return response.result as TermDto[]
    }

    /**
     * Get a specific term by term code.
     */
    async getTerm(termCode: number): Promise<TermDto | null> {
        const response = await get(`${this.baseUrl}/terms/${termCode}`)
        return response.result as TermDto | null
    }

    /**
     * Get the current open term.
     */
    async getCurrentTerm(): Promise<TermDto | null> {
        const response = await get(`${this.baseUrl}/terms/current`)
        return response.result as TermDto | null
    }
}

const effortService = new EffortService()
export { effortService }
