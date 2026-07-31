import { useFetch } from "@/composables/ViperFetch"
import type { CmsPersonOption } from "@/CMS/types"

/**
 * Shared CMS option-list lookups (people, permissions) so the selector components don't each
 * hand-roll the same apiURL + useFetch request against the cms/options endpoints. Returns null on a
 * failed request (vs an empty array) so callers can tell "no matches" from "the fetch failed" and
 * keep their own loading/caching/out-of-order logic.
 *
 * useFetch() is called inside each function, not at module load: the latter runs on import and would
 * trip a test's not-yet-initialized ViperFetch mock (temporal dead zone) whenever this module is
 * pulled into the import graph.
 */
const optionsUrl = `${import.meta.env.VITE_API_URL}cms/options/`

async function getPermissionOptions(): Promise<string[] | null> {
    const { get } = useFetch()
    const res = await get(`${optionsUrl}permissions`)
    return res.success ? (res.result as string[]) : null
}

async function searchPeopleOptions(search: string): Promise<CmsPersonOption[] | null> {
    const { get, createUrlSearchParams } = useFetch()
    const res = await get(`${optionsUrl}people?${createUrlSearchParams({ search })}`)
    return res.success ? (res.result as CmsPersonOption[]) : null
}

export { getPermissionOptions, searchPeopleOptions }
