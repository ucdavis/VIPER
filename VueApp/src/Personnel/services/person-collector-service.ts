import { downloadBlob, postForBlob, useFetch } from "@/composables/ViperFetch"
import type {
    PersonCollectorForm,
    PersonCollectorRequest,
    PersonCollectorResult,
} from "../types/person-collector-types"

const { get, post } = useFetch()
const baseUrl = `${import.meta.env.VITE_API_URL}personnel/person-collector`
const DEFAULT_FILENAME = "PersonCollector.xlsx"

type Response = Awaited<ReturnType<typeof get>>

/** The response's result, or null when the request failed or returned nothing. */
function resultOf(r: Response): unknown {
    if (!r.success || r.result === null || r.result === undefined) {
        return null
    }
    return r.result
}

/** The form's choices and which ID columns this user will see, or null when it can't be loaded. */
async function getForm(): Promise<PersonCollectorForm | null> {
    return resultOf(await get(`${baseUrl}/form`)) as PersonCollectorForm | null
}

/** The lists for the chosen groups, or null when the request fails. */
async function getResults(request: PersonCollectorRequest): Promise<PersonCollectorResult | null> {
    return resultOf(await post(`${baseUrl}/results`, request)) as PersonCollectorResult | null
}

/** Downloads the same lists as an Excel workbook, one sheet per list. */
async function exportExcel(request: PersonCollectorRequest): Promise<void> {
    const { blob, filename } = await postForBlob(`${baseUrl}/export`, request)
    downloadBlob(blob, filename ?? DEFAULT_FILENAME)
}

const personCollectorService = { getForm, getResults, exportExcel }
export { personCollectorService }
