import { defineComponent } from "vue"
import { useServerTable } from "@/CMS/composables/use-server-table"
import { mountCms, flushPromises } from "./test-utils"

/**
 * useServerTable is the server-paged QTable core shared by the CMS list pages: onRequest issues
 * the GET built from buildParams, binds rows and rowsNumber (falling back to the row count when
 * the API returns no pagination envelope), copies the request pagination into state, and notifies
 * on failure. Direct spec so regressions surface here rather than in every list page.
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

// The composable needs a component context for useQuasar/useFetch; host it in a renderless
// component and drive it through its returned API (the same surface the pages use).
function mountTable(options: { errorMessage?: string } = {}) {
    let table!: ReturnType<typeof useServerTable<Row>>
    const Host = defineComponent({
        name: "ServerTableHost",
        setup() {
            table = useServerTable<Row>({
                url: "/api/things",
                errorMessage: options.errorMessage,
                pagination: { sortBy: "name", descending: false, rowsPerPage: 25 },
                buildParams: (p) => ({
                    page: p.page,
                    perPage: p.rowsPerPage,
                    sortBy: p.sortBy,
                    descending: p.descending ? "true" : "false",
                }),
            })
            return () => null
        },
    })
    mountCms(Host)
    return table
}

const request = (page: number, overrides: Partial<{ sortBy: string; descending: boolean }> = {}) => ({
    pagination: { page, rowsPerPage: 25, sortBy: "name", descending: false, ...overrides },
})

beforeEach(() => {
    mockGet.mockReset()
})

describe("useServerTable", () => {
    it("binds rows and takes rowsNumber from the response pagination envelope", async () => {
        mockGet.mockResolvedValue({ success: true, result: [{ id: 1 }], pagination: { totalRecords: 91 } })
        const table = mountTable()

        await table.onRequest(request(2, { sortBy: "modifiedOn", descending: true }))

        expect(mockGet.mock.calls[0]![0]).toBe("/api/things?page=2&perPage=25&sortBy=modifiedOn&descending=true")
        expect(table.rows.value).toEqual([{ id: 1 }])
        expect(table.pagination.value).toMatchObject({
            rowsNumber: 91,
            page: 2,
            sortBy: "modifiedOn",
            descending: true,
        })
        expect(table.loading.value).toBeFalsy()
    })

    it("falls back to the returned row count for rowsNumber when no envelope is present", async () => {
        mockGet.mockResolvedValue({ success: true, result: [{ id: 1 }, { id: 2 }, { id: 3 }] })
        const table = mountTable()

        await table.onRequest(request(1))

        expect(table.pagination.value.rowsNumber).toBe(3)
    })

    it("notifies with the server error and keeps the previous rows when the request fails", async () => {
        mockGet.mockResolvedValueOnce({ success: true, result: [{ id: 1 }] })
        const table = mountTable()
        await table.onRequest(request(1))

        mockGet.mockResolvedValueOnce({ success: false, errors: ["backend exploded"] })
        await table.onRequest(request(2))
        await flushPromises()

        // The notification teleports to document.body.
        expect(document.body.textContent).toContain("backend exploded")
        expect(table.rows.value).toEqual([{ id: 1 }])
        expect(table.loading.value).toBeFalsy()
    })

    it("falls back to the caller's errorMessage when a failure carries no errors", async () => {
        mockGet.mockResolvedValue({ success: false, errors: null })
        const table = mountTable({ errorMessage: "Failed to load things" })

        await table.onRequest(request(1))
        await flushPromises()

        expect(document.body.textContent).toContain("Failed to load things")
    })

    it("reloadFirstPage re-requests page 1 while keeping the current sort and page size", async () => {
        mockGet.mockResolvedValue({ success: true, result: [], pagination: { totalRecords: 0 } })
        const table = mountTable()
        await table.onRequest(request(3, { sortBy: "modifiedOn", descending: true }))

        await table.reloadFirstPage()
        await flushPromises()

        const lastUrl = mockGet.mock.calls.at(-1)![0] as string
        expect(lastUrl).toContain("page=1")
        expect(lastUrl).toContain("sortBy=modifiedOn")
        expect(lastUrl).toContain("descending=true")
    })
})
