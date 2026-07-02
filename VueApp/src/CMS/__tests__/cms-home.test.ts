import CmsHome from "@/CMS/pages/CmsHome.vue"
import { useUserStore } from "@/store/UserStore"
import { mountCms, flushPromises } from "./test-utils"

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

describe("CmsHome.vue - permission-gated sections", () => {
    it("shows every tool card and the full activity rail for a CMS admin", async () => {
        const wrapper = await mountHome()

        for (const title of ["Files", "Content Blocks", "Link Collections", "Left Navigation"]) {
            expect(wrapper.text()).toContain(title)
        }
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
        expect(actionLabels(wrapper)).toEqual(["Add Content Block"])
        // No manage permission at all: the activity rail is hidden entirely.
        expect(wrapper.findComponent({ name: "RecentActivity" }).exists()).toBeFalsy()
    })

    it("shows a files-only user the Files card, including the pre-filtered Trash deep-link", async () => {
        const wrapper = await mountHome(["SVMSecure.CMS", "SVMSecure.CMS.AllFiles"])

        expect(actionLabels(wrapper)).toEqual(["Manage Files", "Trash", "Audit Trail"])
        const trash = wrapper.findAllComponents({ name: "QBtn" }).find((b) => b.props("label") === "Trash")!
        expect(trash.props("to")).toEqual({ name: "CmsFiles", query: { status: "deleted" } })

        const rail = wrapper.findComponent({ name: "RecentActivity" })
        expect(rail.props("showFiles")).toBeTruthy()
        expect(rail.props("showBlocks")).toBeFalsy()
        expect(rail.props("showLeftNavs")).toBeFalsy()
    })

    it("tells a user with no CMS tool permissions that they have no access", async () => {
        const wrapper = await mountHome(["SVMSecure.CMS"])

        expect(wrapper.text()).toContain("Your account does not have access to any CMS tools.")
        expect(wrapper.findAllComponents({ name: "QBtn" })).toHaveLength(0)
    })
})
