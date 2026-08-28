import { useFetch } from "@/composables/ViperFetch"

const { get } = useFetch()

/**
 * Service for requesting the most recent modification date for the unit
 * phone lists.
 */
class PhoneListModifiedDateService {
    private baseUrl = `${import.meta.env.VITE_API_URL}phones/phonelist`

    async getModifiedDate(code: string): Promise<Date | null> {
        const r = await get(`${this.baseUrl}/${encodeURIComponent(code)}/modifiedDate`)
        return r.result ?? null
    }
}

const phoneListModifiedDateService = new PhoneListModifiedDateService()
export { phoneListModifiedDateService }
