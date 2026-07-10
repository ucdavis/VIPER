import { createMemoryHistory, createRouter } from "vue-router"
import type { RouteLocationNormalized } from "vue-router"
import { nextTick } from "vue"
import { createSpaRouter } from "../create-spa-router"
import { useRouteFocus } from "@/composables/use-route-focus"

const DummyPage = { template: "<div>page</div>" }

function route(path: string, hash = ""): RouteLocationNormalized {
    return { path, hash } as RouteLocationNormalized
}

describe("createSpaRouter scroll behavior", () => {
    const router = createSpaRouter([{ path: "/", component: DummyPage }])
    const scrollBehavior = router.options.scrollBehavior!

    it("scrolls to top when navigating to a different page", () => {
        expect(scrollBehavior(route("/b"), route("/a"), null)).toStrictEqual({ left: 0, top: 0 })
    })

    it("restores the saved position (popstate back/forward) as-is, before any path/hash logic", () => {
        const savedPosition = { left: 0, top: 420 }
        // A different-page or hash navigation would otherwise scroll to top / the fragment;
        // savedPosition wins because it means a browser back/forward.
        expect(scrollBehavior(route("/b", "#section"), route("/a"), savedPosition)).toBe(savedPosition)
    })

    it("keeps scroll position for query-only changes on the same page", () => {
        expect(scrollBehavior(route("/a"), route("/a"), null)).toBeFalsy()
    })

    it("scrolls to the fragment target when navigating to a hash on another page", () => {
        expect(scrollBehavior(route("/b", "#section"), route("/a"), null)).toStrictEqual({ el: "#section" })
    })

    it("scrolls to the fragment target when the hash changes on the same page", () => {
        expect(scrollBehavior(route("/a", "#section"), route("/a"), null)).toStrictEqual({ el: "#section" })
    })

    it("keeps scroll position for query-only changes when the hash is unchanged", () => {
        expect(scrollBehavior(route("/a", "#section"), route("/a", "#section"), null)).toBeFalsy()
    })
})

describe("route-change focus management", () => {
    function buildRouter() {
        const router = createRouter({
            history: createMemoryHistory(),
            routes: [
                { path: "/", component: DummyPage },
                { path: "/other", component: DummyPage },
            ],
        })
        useRouteFocus(router)
        return router
    }

    beforeEach(() => {
        document.body.innerHTML = [
            '<div id="leftNavMenu" tabindex="-1"></div>',
            '<main id="main-content"></main>',
        ].join("")
    })

    it("moves focus to the main landmark after page navigation", async () => {
        const router = buildRouter()
        await router.push("/")
        await router.isReady()

        await router.push("/other")
        await nextTick()

        expect(document.activeElement?.id).toBe("main-content")
    })

    it("tags route-driven focus so CSS suppresses the landmark focus ring", async () => {
        const router = buildRouter()
        await router.push("/")
        await router.isReady()

        await router.push("/other")
        await nextTick()

        expect(document.querySelector<HTMLElement>("#main-content")!.dataset.routeFocus).toBeDefined()
    })

    it("clears the route-focus tag on blur so a later skip-link jump keeps its ring", async () => {
        const router = buildRouter()
        await router.push("/")
        await router.isReady()

        await router.push("/other")
        await nextTick()

        document.querySelector<HTMLElement>("#leftNavMenu")!.focus()

        expect(document.querySelector<HTMLElement>("#main-content")!.dataset.routeFocus).toBeUndefined()
    })

    it("does not steal focus on in-page anchor navigation (skip links)", async () => {
        const router = buildRouter()
        await router.push("/other")
        await router.isReady()

        const skipTarget = document.querySelector<HTMLElement>("#leftNavMenu")!
        skipTarget.focus()
        await router.push({ path: "/other", hash: "#leftNavMenu" })
        await nextTick()

        expect(document.activeElement?.id).toBe("leftNavMenu")
    })

    it("does not steal focus on query-only navigation (URL-synced filters)", async () => {
        // Filter changes replace the route with new query params while the user is
        // typing/selecting in a control; focus must stay in that control.
        const router = buildRouter()
        await router.push("/other")
        await router.isReady()

        const filterControl = document.querySelector<HTMLElement>("#leftNavMenu")!
        filterControl.focus()
        await router.replace({ path: "/other", query: { search: "abc" } })
        await nextTick()

        expect(document.activeElement?.id).toBe("leftNavMenu")
    })
})
