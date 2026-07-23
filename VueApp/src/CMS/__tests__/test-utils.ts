import { mount } from "@vue/test-utils"
import type { ComponentMountingOptions } from "@vue/test-utils"
import { createRouter, createWebHistory } from "vue-router"
import type { Router, RouteRecordRaw } from "vue-router"
import { Quasar, Notify, Dialog } from "quasar"
import { createPinia, setActivePinia } from "pinia"
import { nextTick } from "vue"
import { useUserStore } from "@/store/UserStore"

/**
 * Shared test utilities for CMS component/page tests.
 *
 * CMS keeps page state local, but some pages read the signed-in user's permissions from the
 * shared user store (e.g. ContentBlockEdit gates its section-path and file pickers). So the
 * helpers install Pinia and seed a full CMS admin so mounted pages behave as they would for a
 * manager (the role these tests assume).
 */

// Components build request URLs as inject("apiURL") + "cms/...". Mirror production by injecting the
// same base the app does (VITE_API_URL, e.g. "/2/api/" on TEST); assertions target the "cms/..."
// suffix, not this prefix, so they hold regardless of the configured base.
const TEST_API_URL = import.meta.env.VITE_API_URL

// The CMS area's full permission set; mounted pages default to this so existing tests see the
// same UI a manager would. Mirrors the route meta in src/CMS/router/routes.ts.
const CMS_ADMIN_PERMISSIONS = [
    "SVMSecure.CMS",
    "SVMSecure.CMS.ManageContentBlocks",
    "SVMSecure.CMS.CreateContentBlock",
    "SVMSecure.CMS.AllFiles",
    "SVMSecure.CMS.ManageNavigation",
]

// Route table mirroring the real CMS route names/paths (src/CMS/router/routes.ts) so
// router-link :to and programmatic navigation resolve exactly as they do in production.
const stub = { template: "<div />" }
const CMS_ROUTES: RouteRecordRaw[] = [
    { path: "/CMS/Home", name: "CmsHome", component: stub },
    // Root alias so tests that mount at the initial "/" location still resolve a route.
    { path: "/", redirect: "/CMS/Home" },
    { path: "/CMS/ManageContentBlocks", name: "CmsContentBlocks", component: stub },
    { path: "/CMS/ManageContentBlocks/History", name: "CmsContentBlockHistory", component: stub },
    { path: "/CMS/ManageContentBlocks/Edit/:id?", name: "CmsContentBlockEdit", component: stub },
    { path: "/CMS/ManageFiles", name: "CmsFiles", component: stub },
    { path: "/CMS/ManageFiles/Audit", name: "CmsFileAudit", component: stub },
    { path: "/CMS/ManageFiles/Import", name: "CmsFileImport", component: stub },
    { path: "/CMS/ManageFiles/BulkEncrypt", name: "CmsBulkEncrypt", component: stub },
    { path: "/CMS/ManageLeftNav", name: "CmsLeftNavMenus", component: stub },
    { path: "/CMS/ManageLeftNav/Edit/:id?", name: "CmsLeftNavEdit", component: stub },
]

function createTestRouter(): Router {
    return createRouter({
        history: createWebHistory(),
        routes: CMS_ROUTES,
    })
}

// Generic mount wrapper: registers Quasar (with Notify + Dialog so components that call
// $q.notify / $q.dialog don't blow up), Pinia (seeded as a CMS admin), a router, and the
// apiURL provide.
function mountCms<T>(component: T, options: ComponentMountingOptions<T> = {}, router?: Router) {
    const testRouter = router ?? createTestRouter()
    const pinia = createPinia()
    setActivePinia(pinia)
    useUserStore().setPermissions(CMS_ADMIN_PERMISSIONS)
    const { global: globalOptions = {}, ...rest } = options as Record<string, unknown> & {
        global?: Record<string, unknown>
    }
    return mount(
        component as never,
        {
            ...rest,
            global: {
                plugins: [[Quasar, { plugins: { Notify, Dialog } }], testRouter, pinia],
                provide: { apiURL: TEST_API_URL },
                ...globalOptions,
            },
        } as ComponentMountingOptions<T>,
    )
}

// Flush microtasks so awaited fetch mocks and the DOM both settle before assertions.
async function flushPromises(): Promise<void> {
    await Promise.resolve()
    await nextTick()
    await Promise.resolve()
}

// Router replace/push navigation resolves over several microtask turns; run enough flush
// cycles (recursively, to avoid an await-in-loop) for the URL query and any query-watcher
// reactions to settle before asserting on router.currentRoute.
async function flushRouter(cycles = 10): Promise<void> {
    if (cycles <= 0) {
        return
    }
    await flushPromises()
    await nextTick()
    await flushRouter(cycles - 1)
}

// Click the last body-level <button> whose text includes `label`. Quasar plugin dialogs/menus
// teleport to document.body (outside the mounted wrapper), and a dismissed dialog's portal can
// linger mid-transition, so the newest (last) match is always the live one.
function clickBodyButton(label: string): void {
    const btn = [...document.body.querySelectorAll("button")].filter((b) => b.textContent?.includes(label)).at(-1)
    expect(btn, `expected a "${label}" button`).toBeTruthy()
    btn!.click()
}

export { createTestRouter, mountCms, flushPromises, flushRouter, clickBodyButton }
