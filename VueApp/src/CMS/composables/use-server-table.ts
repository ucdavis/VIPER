import type { Ref } from "vue"
import { ref } from "vue"
import { useQuasar } from "quasar"
import { useFetch } from "@/composables/ViperFetch"

type ServerTablePagination = {
    sortBy: string
    descending: boolean
    page: number
    rowsPerPage: number
    rowsNumber: number
}

// Quasar's QTable @request emits pagination without a guaranteed rowsNumber.
type TableRequestPagination = {
    sortBy: string
    descending: boolean
    page: number
    rowsPerPage: number
    rowsNumber?: number
}

type TableRequest = { pagination: TableRequestPagination }

type TableQueryParams = Record<string, string | number | null | undefined>

type ServerTableOptions = {
    url: string
    // Build the query params for one page request from the table's current pagination.
    buildParams: (pagination: TableRequestPagination) => TableQueryParams
    errorMessage?: string
    pagination?: Partial<ServerTablePagination>
}

// Server-side paged QTable core shared by the CMS list pages: owns rows/loading/pagination, issues
// the GET on @request, and binds rowsNumber from the response envelope (falling back to the row
// count when the API returns none). Callers supply the URL and a params builder, keeping each
// page's filter mapping local while the fetch/pagination plumbing lives here once.
export function useServerTable<TRow>(options: ServerTableOptions) {
    const $q = useQuasar()
    const { get, createUrlSearchParams } = useFetch()
    const failureMessage = options.errorMessage ?? "Failed to load"

    const rows = ref([]) as Ref<TRow[]>
    const loading = ref(false)
    const pagination = ref<ServerTablePagination>({
        sortBy: "",
        descending: false,
        page: 1,
        rowsPerPage: 50,
        rowsNumber: 0,
        ...options.pagination,
    })

    async function onRequest(request: TableRequest) {
        const { page, rowsPerPage, sortBy, descending } = request.pagination
        loading.value = true
        const params = createUrlSearchParams(options.buildParams(request.pagination))
        const res = await get(`${options.url}?${params}`)
        if (res.success) {
            rows.value = res.result
            pagination.value.rowsNumber = res.pagination?.totalRecords ?? res.result.length
            pagination.value.page = page
            pagination.value.rowsPerPage = rowsPerPage
            pagination.value.sortBy = sortBy
            pagination.value.descending = descending
        } else {
            $q.notify({ type: "negative", message: res.errors?.[0] ?? failureMessage })
        }
        loading.value = false
    }

    function reloadFirstPage() {
        return onRequest({ pagination: { ...pagination.value, page: 1 } })
    }

    return { rows, loading, pagination, onRequest, reloadFirstPage }
}
