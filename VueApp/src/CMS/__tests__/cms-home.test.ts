import CmsHome from "@/CMS/pages/CmsHome.vue"
import { useUserStore } from "@/store/UserStore"
import { mountCms, flushPromises } from "./test-utils"

// The hub checks the trash for files nearing the purge cutoff (file managers only).
// Default GET: empty success. routeEditable() overrides this per test; the beforeEach below restores
// it so the override never leaks into a later test (which previously required this suite to run last).
const defaultGetImpl = (): Promise<{ success: boolean; result: unknown[] }> =>
    Promise.resolve({ success: true, result: [] })
const getMock = vi.fn<(...args: unknown[]) => Promise<{ success: boolean; result: unknown[] }>>(defaultGetImpl)
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: getMock,
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

beforeEach(() => {
    getMock.mockImplementation(defaultGetImpl)
})

function trashedFile(friendlyName: string, purgeOn: string) {
    return { fileGuid: `g-${friendlyName}`, friendlyName, purgeOn, deletedOn: "2024-01-01T00:00:00" }
}

function daysFromNow(days: number): string {
    return new Date(Date.now() + days * 24 * 60 * 60 * 1000).toISOString()
}

/**
 * CmsHome is the permission-gated hub: each tool card (and each action inside a card) only shows
 * when the user holds one of its permissions, the recent-activity rail mirrors the manage
 * permissions, and users with no CMS access get an explanatory banner instead. RecentActivity is
 * stubbed (it fetches its own data); these tests only assert which props gate its panels.
 */

const recentActivityStub = {
    name: "RecentActivity",
    props: ["showBlocks", "showFiles", "showLeftNavs"],
    template: "<div class='recent-activity-stub' />",
}

// MountCms seeds the full CMS admin permission set; narrower personas are applied afterwards
// through the same user store the page reads reactively.
async function mountHome(permissions?: string[]) {
    const wrapper = mountCms(CmsHome, { global: { stubs: { RecentActivity: recentActivityStub } } })
    if (permissions) {
        useUserStore().setPermissions(permissions)
        await flushPromises()
    }
    await flushPromises()
    return wrapper
}

function actionLabels(wrapper: Awaited<ReturnType<typeof mountHome>>): string[] {
    return wrapper.findAllComponents({ name: "QBtn" }).map((b) => b.props("label") as string)
}

describe("cmsHome.vue - permission-gated sections", () => {
    it("shows every tool card and the full activity rail for a CMS admin", async () => {
        const wrapper = await mountHome()

        // One box per left-nav group, in the nav's order (Link Collections is an action
        // inside Content Blocks, mirroring CmsNavMenu.cs).
        // h2 text includes the leading icon ligature name in happy-dom, hence stringContaining.
        expect(wrapper.findAll("h2").map((h) => h.text())).toStrictEqual([
            expect.stringContaining("Content Blocks"),
            expect.stringContaining("Files"),
            expect.stringContaining("Left Navigation"),
        ])
        expect(actionLabels(wrapper)).toContain("Manage Link Collections")
        const rail = wrapper.findComponent({ name: "RecentActivity" })
        expect(rail.props("showBlocks")).toBeTruthy()
        expect(rail.props("showFiles")).toBeTruthy()
        expect(rail.props("showLeftNavs")).toBeTruthy()
    })

    it("shows a create-only user just the Content Blocks card with only the Add action", async () => {
        const wrapper = await mountHome(["SVMSecure.CMS", "SVMSecure.CMS.CreateContentBlock"])

        expect(wrapper.text()).toContain("Content Blocks")
        expect(wrapper.text()).not.toContain("Link Collections")
        expect(wrapper.text()).not.toContain("Left Navigation")
        // Manage/History require ManageContentBlocks even inside a visible section.
        expect(actionLabels(wrapper)).toStrictEqual(["Add Content Block"])
        // No manage permission at all: the activity rail is hidden entirely.
        expect(wrapper.findComponent({ name: "RecentActivity" }).exists()).toBeFalsy()
    })

    it("shows a files-only user the Files card, including the pre-filtered Trash deep-link", async () => {
        const wrapper = await mountHome(["SVMSecure.CMS", "SVMSecure.CMS.AllFiles"])

        expect(actionLabels(wrapper)).toStrictEqual(["Manage Files", "Add File", "Audit Trail", "Trash"])
        const addFile = wrapper.findAllComponents({ name: "QBtn" }).find((b) => b.props("label") === "Add File")!
        expect(addFile.props("to")).toStrictEqual({ name: "CmsFiles", query: { upload: "1" } })
        const trash = wrapper.findAllComponents({ name: "QBtn" }).find((b) => b.props("label") === "Trash")!
        expect(trash.props("to")).toStrictEqual({ name: "CmsFiles", query: { status: "deleted" } })

        const rail = wrapper.findComponent({ name: "RecentActivity" })
        expect(rail.props("showFiles")).toBeTruthy()
        expect(rail.props("showBlocks")).toBeFalsy()
        expect(rail.props("showLeftNavs")).toBeFalsy()
    })

    it("warns file managers when trashed files purge within a week, linking to the Trash", async () => {
        getMock.mockResolvedValueOnce({
            success: true,
            result: [
                trashedFile("soon.pdf", daysFromNow(2)),
                trashedFile("soon2.pdf", daysFromNow(6)),
                trashedFile("later.pdf", daysFromNow(20)),
            ],
        })
        const wrapper = await mountHome()

        expect(wrapper.text()).toContain("2 trashed files will be permanently deleted within 7 days.")
        expect(wrapper.find("a[href*='status=deleted']").exists()).toBeTruthy()
        const trashFetch = getMock.mock.calls
            .map((c) => c[0] as unknown as string)
            .find((u) => u.includes("status=deleted"))!
        expect(trashFetch).toContain("sortBy=deletedOn")
        expect(trashFetch).toContain("descending=false")
    })

    it("shows no purge warning when nothing in the trash purges soon", async () => {
        getMock.mockResolvedValueOnce({ success: true, result: [trashedFile("later.pdf", daysFromNow(20))] })
        const wrapper = await mountHome()

        expect(wrapper.text()).not.toContain("permanently deleted within")
    })

    it("tells a user with no CMS tool permissions that they have no access", async () => {
        const wrapper = await mountHome(["SVMSecure.CMS"])

        expect(wrapper.text()).toContain("Your account does not have access to any CMS tools.")
        expect(wrapper.findAllComponents({ name: "QBtn" })).toHaveLength(0)
    })
})

/**
 * Delegated editors (no ManageContentBlocks) get a "Blocks you can edit" card listing GET /editable
 * results, linking each to its editor. It shows only for non-managers with a non-empty list, and it
 * suppresses the no-access banner. Managers use the normal Content Blocks card, so they never fetch
 * or render this card. routeEditable() overrides the shared get() mock within these specs; the
 * beforeEach above restores it, so their ordering relative to the other specs no longer matters.
 */
describe("cmsHome.vue - delegated editable blocks card", () => {
    const EDITABLE = [
        {
            contentBlockId: 11,
            title: "Alpha",
            friendlyName: "alpha",
            viperSectionPath: "/a",
            page: null,
            modifiedOn: "2024-01-01T00:00:00",
            modifiedBy: "u",
        },
        {
            contentBlockId: 12,
            title: null,
            friendlyName: "beta",
            viperSectionPath: "/b",
            page: null,
            modifiedOn: "2024-01-02T00:00:00",
            modifiedBy: "u",
        },
    ]

    function routeEditable(items: unknown[]) {
        getMock.mockImplementation((...args: unknown[]) => {
            const url = args[0] as unknown as string
            if (url.includes("editable")) {
                return Promise.resolve({ success: true, result: items })
            }
            return Promise.resolve({ success: true, result: [] })
        })
    }

    function editLinks(wrapper: Awaited<ReturnType<typeof mountHome>>) {
        return wrapper
            .findAllComponents({ name: "QBtn" })
            .filter((b) => (b.props("to") as { name?: string } | undefined)?.name === "CmsContentBlockEdit")
    }

    it("lists editable blocks with edit links and suppresses the no-access banner for a non-manager", async () => {
        routeEditable(EDITABLE)
        const wrapper = await mountHome(["SVMSecure.CMS"])
        await flushPromises()
        await flushPromises()

        expect(wrapper.text()).toContain("Blocks you can edit")
        expect(wrapper.text()).not.toContain("does not have access to any CMS tools")

        // Falls back to the friendly name when a block has no title (block 12 -> "beta").
        expect(editLinks(wrapper).map((b) => b.props("label"))).toStrictEqual(["Alpha", "beta"])
        expect(editLinks(wrapper)[0]!.props("to")).toStrictEqual({
            name: "CmsContentBlockEdit",
            params: { id: 11 },
        })
    })

    it("does not fetch or show the editable card for a manager", async () => {
        // The shared get() mock accumulates calls across tests; clear it so this assertion only
        // sees this manager mount's requests.
        getMock.mockClear()
        routeEditable(EDITABLE)
        const wrapper = await mountHome() // Full admin
        await flushPromises()

        expect(wrapper.text()).not.toContain("Blocks you can edit")
        // Managers still get the normal tool card, and never hit the editable endpoint.
        expect(wrapper.text()).toContain("Content Blocks")
        expect(getMock.mock.calls.map((c) => c[0] as unknown as string).some((u) => u.includes("editable"))).toBeFalsy()
    })
})
