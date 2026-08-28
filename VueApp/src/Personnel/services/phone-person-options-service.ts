import { useFetch } from "@/composables/ViperFetch"
import type { AugmentedViperPerson } from "../types/phone-types"

/**
 * Phone option-list lookups for people. Returns null on a
 * failed request (vs an empty array) so callers can tell "no matches" from "the fetch failed".
 */
const { get, createUrlSearchParams } = useFetch()
const optionsUrl = `${import.meta.env.VITE_API_URL}phones/people`

async function searchPeopleOptions(search: string, listCode: string = ""): Promise<AugmentedViperPerson[] | null> {
    const res = await get(`${optionsUrl}?${createUrlSearchParams({ search, listCode })}`)
    return res.success ? (res.result as AugmentedViperPerson[]) : null
}

export { searchPeopleOptions }
