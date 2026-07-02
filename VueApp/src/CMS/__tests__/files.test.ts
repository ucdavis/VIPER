import Files from "@/CMS/pages/Files.vue"
import { mountCms, flushPromises, createTestRouter } from "./test-utils"

/**
 * Files is a server-paged list. Its filters initialize from the URL (deep-link), onRequest builds
 * the page/sort/filter query and binds rowsNumber from the response pagination. Mock ViperFetch
 * and route requests by URL (folders / folder-counts / the paged list).
 */

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        del: vi.fn<(...args: unknown[]) => unknown>(),
        post: vi.fn<(...args: unknown[]) => unknown>(),
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

const FILE_ROW = {
    fileGuid: "g1",
    fileName: "a.pdf",
    folder: "Apps",
    friendlyName: "a.pdf",
    encrypted: false,
    description: "",
    allowPublicAccess: false,
    oldUrl: null,
    modifiedOn: "2024-01-01T00:00:00",
    modifiedBy: "u",
    deletedOn: null,
    purgeOn: null,
    permissions: [],
    people: [],
    url: "/files/g1",
    friendlyUrl: "/Apps/a.pdf",
}

// Returns the list endpoint URL (the bare "cms/files/?..." call, not folders/folder-counts).
function lastListUrl(): string {
    const listCalls = mockGet.mock.calls.map((c) => c[0] as string).filter((u) => u.includes("cms/files/?"))
    return listCalls.at(-1) ?? ""
}

function routeGet(opts: { rows?: unknown[]; total?: number } = {}) {
    const rows = opts.rows ?? [FILE_ROW]
    mockGet.mockReset()
    mockGet.mockImplementation((...args: unknown[]) => {
        const url = args[0] as string
        if (url.includes("folder-counts")) {
            return Promise.resolve({ success: true, result: [{ folder: "Apps", count: 3 }] })
        }
        if (url.includes("folders")) {
            return Promise.resolve({ success: true, result: ["Apps", "Reports"] })
        }
        // The paged list request.
        return Promise.resolve({
            success: true,
            result: rows,
            pagination: opts.total === undefined ? undefined : { totalRecords: opts.total },
        })
    })
}

async function mountPage(query: Record<string, string> = {}) {
    const router = createTestRouter()
    await router.push({ path: "/CMS/ManageFiles", query })
    await router.isReady()
    const wrapper = mountCms(Files, {}, router)
    await flushPromises()
    await flushPromises()
    return wrapper
}

describe("Files.vue - query param construction", () => {
    beforeEach(() => routeGet({ total: 42 }))

    it("requests page 1 with default sort and status on mount, binding rowsNumber from pagination", async () => {
        const wrapper = await mountPage()
        const url = lastListUrl()
        expect(url).toContain("page=1")
        expect(url).toContain("perPage=50")
        expect(url).toContain("sortBy=friendlyName")
        expect(url).toContain("status=active")
        expect(wrapper.findComponent({ name: "QTable" }).props("rows")).toHaveLength(1)
        // RowsNumber is read off the QTable's bound pagination prop.
        const pag = wrapper.findComponent({ name: "QTable" }).props("pagination") as { rowsNumber: number }
        expect(pag.rowsNumber).toBe(42)
    })

    it("initializes filters from the URL query (deep-link) and reflects them in the request", async () => {
        await mountPage({ search: "report", status: "deleted", encrypted: "1", public: "1", folder: "Reports" })
        const url = lastListUrl()
        expect(url).toContain("search=report")
        expect(url).toContain("status=deleted")
        expect(url).toContain("encrypted=true")
        expect(url).toContain("isPublic=true")
        expect(url).toContain("folder=Reports")
    })

    it("omits the folder param when folder is the All sentinel", async () => {
        await mountPage()
        expect(lastListUrl()).not.toContain("folder=")
    })

    it("falls back to result length for rowsNumber when no pagination is returned", async () => {
        routeGet({ rows: [FILE_ROW, { ...FILE_ROW, fileGuid: "g2" }] })
        const wrapper = await mountPage()
        const pag = wrapper.findComponent({ name: "QTable" }).props("pagination") as { rowsNumber: number }
        expect(pag.rowsNumber).toBe(2)
    })
})

describe("Files.vue - onRequest pagination passthrough", () => {
    beforeEach(() => routeGet({ total: 200 }))

    it("uses the sort/descending/page from a table request", async () => {
        const wrapper = await mountPage()
        await wrapper.findComponent({ name: "QTable" }).vm.$emit("request", {
            pagination: { page: 3, rowsPerPage: 25, sortBy: "modifiedOn", descending: true },
        })
        await flushPromises()
        const url = lastListUrl()
        expect(url).toContain("page=3")
        expect(url).toContain("perPage=25")
        expect(url).toContain("sortBy=modifiedOn")
        expect(url).toContain("descending=true")
    })
})
