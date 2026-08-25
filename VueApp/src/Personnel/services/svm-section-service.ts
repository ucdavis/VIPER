import { useFetch } from "@/composables/ViperFetch"
import type { SVMSectionAPIResponse } from "../types/svm-phone-types"

const { get } = useFetch()

/**
 * Service for SVMSection API calls.
 */
class SVMSectionService {
    private baseUrl = `${import.meta.env.VITE_API_URL}phones/svm/sections`

    async getSections(): Promise<SVMSectionAPIResponse[]> {
        const r = await get(this.baseUrl)

        const results = r.result
        if (!results || results.length === 0) {
            return []
        }
        return results as SVMSectionAPIResponse[]
    }
}

const svmSectionService = new SVMSectionService()
export { svmSectionService }
