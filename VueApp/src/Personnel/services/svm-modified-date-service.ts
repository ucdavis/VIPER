import { useFetch } from "@/composables/ViperFetch"

const { get } = useFetch()

/**
 * Service for requesting the most recent modification date for the SVM
 * phone list.
 */
class SVMModifiedDateService {
    private baseUrl = `${import.meta.env.VITE_API_URL}phones/svm/modifiedDate`

    async getModifiedDate(): Promise<Date | null> {
        const r = await get(this.baseUrl)
        return r.result ?? null
    }
}

const svmModifiedDateService = new SVMModifiedDateService()
export { svmModifiedDateService }
