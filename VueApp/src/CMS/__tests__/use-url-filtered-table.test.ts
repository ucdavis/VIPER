import { defineComponent } from "vue"
import { useUrlFilteredTable } from "@/CMS/composables/use-url-filtered-table"
import { mountCms, flushPromises, flushRouter, createTestRouter } from "./test-utils"

/**
 * useUrlFilteredTable layers URL sync onto useServerTable: filters and the primary deep-link id
 * initialize from route.query, reload() reflects them back (empties omitted) and refetches page 1,
 * clearPrimaryFilter drops the id chip, and the route.query watcher re-syncs on external
 * navigation while its equality guard skips the composable's own URL writes.
 */

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        createUrlSearchParams: (obj: Record<string, string | number | null | undefined>) => {
            const params = new URLSearchParams()
            for (const [k, v] of Object.entries(obj)) {
                if (v !== null && v !== undefined) {
                    params.append(k, v.toString())
                }
            }
            return params
        },
    }),
}))

type Row = { id: number }
type Filters = { search: string; from: string }

async function mountTable(query: Record<string, string> = {}) {
    const router = createTestRouter()
    await router.push({ path: "/CMS/ManageFiles/Audit", query })
    await router.isReady()

    let table!: ReturnType<typeof useUrlFilteredTable<Row, Filters>>
    const Host = defineComponent({
        name: "UrlFilteredTableHost",
        setup() {
            table = useUrlFilteredTable<Row, Filters>({
                url: "/api/audit",
                errorMessage: "Failed to load audit",
                primaryKey: "fileGuid",
                defaultFilters: () => ({ search: "", from: "" }),
                pagination: { sortBy: "when", descending: true },
            })
            return () => null
        },
    })
    mountCms(Host, {}, router)
    return { table, router }
}

function lastUrl(): string {
    return (mockGet.mock.calls.at(-1)?.[0] as string) ?? ""
}

beforeEach(() => {
    mockGet.mockReset()
    mockGet.mockResolvedValue({ success: true, result: [], pagination: { totalRecords: 0 } })
})

describe("useUrlFilteredTable", () => {
    it("initializes the primary id and filters from route.query and sends them, omitting empties", async () => {
        const { table } = await mountTable({ fileGuid: "g1", search: "report" })

        table.reload()
        await flushPromises()

        const url = lastUrl()
        expect(url).toContain("fileGuid=g1")
        expect(url).toContain("search=report")
        expect(url).toContain("page=1")
        expect(url).toContain("perPage=50")
        // The untouched "from" filter is blank and must not be serialized at all.
        expect(url).not.toContain("from=")
    })

    it("reload writes the active filters back to the URL, omitting empty ones", async () => {
        const { table, router } = await mountTable()

        table.filters.value.search = "hello"
        table.reload()
        await flushRouter()

        expect(router.currentRoute.value.query.search).toBe("hello")
        expect(router.currentRoute.value.query.from).toBeUndefined()
        expect(router.currentRoute.value.query.fileGuid).toBeUndefined()
    })

    it("clearPrimaryFilter drops the deep-link id from the request and the URL, then refetches", async () => {
        const { table, router } = await mountTable({ fileGuid: "g1" })
        table.reload()
        await flushRouter()
        const before = mockGet.mock.calls.length

        table.clearPrimaryFilter()
        await flushRouter()

        expect(table.primary.value).toBeNull()
        expect(mockGet.mock.calls.length).toBeGreaterThan(before)
        expect(lastUrl()).not.toContain("fileGuid=")
        expect(router.currentRoute.value.query.fileGuid).toBeUndefined()
    })

    it("re-syncs filters and refetches when external navigation changes the query", async () => {
        const { table, router } = await mountTable()
        table.reload()
        await flushRouter()
        const before = mockGet.mock.calls.length

        await router.push({ path: "/CMS/ManageFiles/Audit", query: { search: "changed", fileGuid: "g2" } })
        await flushRouter()

        expect(mockGet.mock.calls.length).toBe(before + 1)
        expect(table.filters.value.search).toBe("changed")
        expect(table.primary.value).toBe("g2")
        expect(lastUrl()).toContain("search=changed")
    })

    it("does not double-fetch when the watcher sees the composable's own URL write (equality guard)", async () => {
        const { table } = await mountTable()

        table.filters.value.search = "abc"
        table.reload()
        await flushPromises()
        const afterReload = mockGet.mock.calls.length

        // Reload's router.replace triggers the route.query watcher; state already matches.
        await flushRouter()
        expect(mockGet.mock.calls.length).toBe(afterReload)
    })
})
