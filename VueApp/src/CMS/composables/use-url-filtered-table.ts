import type { Ref } from "vue"
import type { LocationQuery, LocationQueryRaw } from "vue-router"
import { ref, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useServerTable } from "./use-server-table"

type UrlFilters = Record<string, string | null>

type UrlFilteredTableOptions<F extends UrlFilters> = {
    url: string
    errorMessage: string
    // The single deep-link id filter shown as a removable chip (e.g. fileGuid, contentBlockId).
    primaryKey: string
    // Default (empty) value for each synced filter; also defines the filter key set and order.
    defaultFilters: () => F
    pagination: { sortBy: string; descending: boolean }
}

// "" and null both mean "unset": collapse to null so query params and the URL omit them.
function blankToNull(value: string | null): string | null {
    return value || null
}

function readString(query: LocationQuery, key: string, fallback: string | null): string | null {
    return typeof query[key] === "string" ? (query[key] as string) : fallback
}

// A URL-synced, server-paged filter table shared by the CMS audit/history views. Filters and the
// primary id deep-link from route.query and reflect back on change (empties omitted). When in-app
// navigation reuses the view with a different query, the watcher re-syncs and re-fetches; its
// equality guard skips our own syncFiltersToUrl writes so they don't trigger a redundant fetch.
export function useUrlFilteredTable<TRow, F extends UrlFilters>(options: UrlFilteredTableOptions<F>) {
    const route = useRoute()
    const router = useRouter()

    function readFilters(query: LocationQuery): F {
        const defaults = options.defaultFilters()
        const next = { ...defaults }
        for (const key of Object.keys(defaults) as (keyof F)[]) {
            next[key] = readString(query, key as string, defaults[key]) as F[keyof F]
        }
        return next
    }

    const primary = ref(readString(route.query, options.primaryKey, null)) as Ref<string | null>
    const filters = ref(readFilters(route.query)) as Ref<F>

    const table = useServerTable<TRow>({
        url: options.url,
        errorMessage: options.errorMessage,
        pagination: options.pagination,
        buildParams: (p) => {
            const params: Record<string, string | number | null | undefined> = {
                [options.primaryKey]: primary.value,
            }
            for (const [key, value] of Object.entries(filters.value)) {
                params[key] = blankToNull(value)
            }
            params.page = p.page
            params.perPage = p.rowsPerPage
            return params
        },
    })

    function syncFiltersToUrl() {
        const query: LocationQueryRaw = { [options.primaryKey]: primary.value || undefined }
        for (const [key, value] of Object.entries(filters.value)) {
            query[key] = value || undefined
        }
        router.replace({ query })
    }

    function reload() {
        syncFiltersToUrl()
        table.reloadFirstPage()
    }

    function clearPrimaryFilter() {
        primary.value = null
        reload()
    }

    function filtersMatch(next: F): boolean {
        return Object.keys(next).every((key) => next[key as keyof F] === filters.value[key as keyof F])
    }

    watch(
        () => route.query,
        (query) => {
            const nextPrimary = readString(query, options.primaryKey, null)
            const next = readFilters(query)
            if (nextPrimary === primary.value && filtersMatch(next)) {
                return
            }
            primary.value = nextPrimary
            filters.value = next
            table.reloadFirstPage()
        },
    )

    return {
        rows: table.rows,
        loading: table.loading,
        pagination: table.pagination,
        onRequest: table.onRequest,
        filters,
        primary,
        reload,
        clearPrimaryFilter,
    }
}
