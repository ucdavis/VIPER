import { useFetch } from "@/composables/ViperFetch"
import type {
    EisAcademics,
    EisAddress,
    EisAppointmentCategories,
    EisAppointments,
    EisHistory,
    EisPersonHeader,
    EisPersonOption,
} from "../types/eis-types"

const { get, put, del } = useFetch()

type Response = Awaited<ReturnType<typeof get>>

/** The response's result, or null when the request failed or returned nothing. */
function resultOf(r: Response): unknown {
    if (!r.success || r.result === null || r.result === undefined) {
        return null
    }
    return r.result
}

async function load<T>(url: string): Promise<T | null> {
    return resultOf(await get(url)) as T | null
}

/**
 * Client for /api/personnel/eis. Every method returns null when the request fails, including
 * when the user may not view that employee, so pages can show one "not available" message
 * without revealing whether the employee exists.
 */
class EisService {
    private baseUrl = `${import.meta.env.VITE_API_URL}personnel/eis`

    /** The people this user may look up. */
    getPeople(): Promise<EisPersonOption[] | null> {
        return load<EisPersonOption[]>(`${this.baseUrl}/people`)
    }

    getHeader(employeeId: string): Promise<EisPersonHeader | null> {
        return load<EisPersonHeader>(this.personUrl(employeeId))
    }

    getAppointments(employeeId: string): Promise<EisAppointments | null> {
        return load<EisAppointments>(`${this.personUrl(employeeId)}/appointments`)
    }

    getHistory(employeeId: string): Promise<EisHistory | null> {
        return load<EisHistory>(`${this.personUrl(employeeId)}/history`)
    }

    getAddress(employeeId: string): Promise<EisAddress | null> {
        return load<EisAddress>(`${this.personUrl(employeeId)}/address`)
    }

    getAcademics(employeeId: string): Promise<EisAcademics | null> {
        return load<EisAcademics>(`${this.personUrl(employeeId)}/academics`)
    }

    getCategories(employeeId: string): Promise<EisAppointmentCategories | null> {
        return load<EisAppointmentCategories>(`${this.personUrl(employeeId)}/categories`)
    }

    /**
     * Sets or clears a manual appointment category for the current academic year. Returns the
     * updated categories, or null when the change was refused or failed.
     */
    async setFlag(employeeId: string, code: number, isSet: boolean): Promise<EisAppointmentCategories | null> {
        const url = `${this.personUrl(employeeId)}/flags/${code}`
        return resultOf(isSet ? await put(url) : await del(url)) as EisAppointmentCategories | null
    }

    /** The ID card photo, served by the API so the same access check applies. */
    photoUrl(employeeId: string): string {
        return `${this.personUrl(employeeId)}/photo`
    }

    private personUrl(employeeId: string): string {
        return `${this.baseUrl}/people/${encodeURIComponent(employeeId)}`
    }
}

const eisService = new EisService()
export { eisService }
