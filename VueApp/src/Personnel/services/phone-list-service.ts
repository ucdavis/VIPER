import { useFetch } from "@/composables/ViperFetch"
import type { PhoneListInfo } from "../types/phone-list-phone-types"

const { get } = useFetch()

/**
 * Service for PhoneList metadata.
 */
class PhoneListService {
    private baseUrl = `${import.meta.env.VITE_API_URL}phones/phonelist`

    /**
     * Resolves a list by its stable code. Returns null when the code is unknown or the request
     * fails, so callers can notify and bail rather than rendering an empty list as if it were
     * a list with no entries.
     */
    async getPhoneListInfo(code: string): Promise<PhoneListInfo | null> {
        const r = await get(`${this.baseUrl}/${encodeURIComponent(code)}`)
        if (!r.success || !r.result) {
            return null
        }
        return r.result as PhoneListInfo
    }
}

const phoneListService = new PhoneListService()
export { phoneListService }
