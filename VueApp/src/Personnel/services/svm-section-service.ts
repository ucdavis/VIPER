import { useFetch } from "@/composables/ViperFetch"
import type { SVMSectionAPIResponse } from "../types/svm-phone-types"

const { get } = useFetch()

/**
 * Service for SVMSection API calls.
 */
class SVMSectionService {
    private baseUrl = `${import.meta.env.VITE_API_URL}phones/svm/sections`

    /**
     * Every section of the SVM list. Returns null when the request fails, which an empty array
     * cannot say: a list with no sections yet and a list that could not be loaded would otherwise
     * render identically, and only one of them should raise an error.
     */
    async getSections(): Promise<SVMSectionAPIResponse[] | null> {
        const r = await get(this.baseUrl)
        if (!r.success || !r.result) {
            return null
        }
        return r.result as SVMSectionAPIResponse[]
    }
}

const svmSectionService = new SVMSectionService()
export { svmSectionService }
