import { useFetch } from "@/composables/ViperFetch"
import type { SVMFrequentNumberAPIResponse, SVMFrequentNumberRecord } from "../types/svm-phone-types"

const { get, post, put, del } = useFetch()

/**
 * Service for SVM Frequent Number API calls.
 */
class SVMFrequentNumberService {
    private baseUrl = `${import.meta.env.VITE_API_URL}phones/svm/frequentnumbers`

    /**
     * The frequently called numbers. Returns null when the request fails, so a caller can tell
     * that apart from a list that genuinely has none.
     */
    async getFrequentNumbers(): Promise<SVMFrequentNumberAPIResponse[] | null> {
        const r = await get(this.baseUrl)
        if (!r.success || !r.result) {
            return null
        }
        return r.result as SVMFrequentNumberAPIResponse[]
    }

    async addFrequentNumber(formData: SVMFrequentNumberRecord) {
        return await post(this.baseUrl, formData)
    }

    async updateFrequentNumber(entryId: number, formData: SVMFrequentNumberRecord) {
        return await put(`${this.baseUrl}/${entryId}`, formData)
    }

    async deleteFrequentNumber(entryId: number) {
        return await del(`${this.baseUrl}/${entryId}`)
    }
}

const svmFrequentNumberService = new SVMFrequentNumberService()
export { svmFrequentNumberService }
