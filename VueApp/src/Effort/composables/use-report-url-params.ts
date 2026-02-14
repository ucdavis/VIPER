import { useRoute, useRouter } from "vue-router"
import type { ReportFilterParams } from "../types"

/**
 * Reads report filter params from URL query string on init,
 * and writes them back when a report is generated — enabling bookmarkable/shareable report URLs.
 */
export function useReportUrlParams() {
    const route = useRoute()
    const router = useRouter()

    function getInitialFilters(): ReportFilterParams | undefined {
        const q = route.query
        const filters: ReportFilterParams = {}

        if (q.academicYear) {
            filters.academicYear = q.academicYear as string
        } else if (q.termCode) {
            filters.termCode = Number.parseInt(q.termCode as string, 10)
        }

        if (q.department) {
            filters.department = q.department as string
        }
        if (q.personId) {
            filters.personId = Number.parseInt(q.personId as string, 10)
        }
        if (q.role) {
            filters.role = q.role as string
        }
        if (q.title) {
            filters.title = q.title as string
        }

        return Object.keys(filters).length > 0 ? filters : undefined
    }

    function updateUrlParams(params: ReportFilterParams) {
        const query: Record<string, string> = {}

        if (params.academicYear) {
            query.academicYear = params.academicYear
        } else if (params.termCode) {
            query.termCode = params.termCode.toString()
        }

        if (params.department) {
            query.department = params.department
        }
        if (params.personId) {
            query.personId = params.personId.toString()
        }
        if (params.role) {
            query.role = params.role
        }
        if (params.title) {
            query.title = params.title
        }

        router.replace({ query })
    }

    return { initialFilters: getInitialFilters(), updateUrlParams }
}
