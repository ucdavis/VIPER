import { mount } from "@vue/test-utils"
import type { ComponentMountingOptions } from "@vue/test-utils"
import { createRouter, createWebHistory } from "vue-router"
import type { Router, RouteRecordRaw } from "vue-router"
import { Quasar, Notify, Dialog } from "quasar"
import { nextTick } from "vue"

/**
 * Shared test utilities for CMS component/page tests.
 *
 * CMS has no Pinia store (state is local to each component), so these helpers
 * only need a Quasar plugin set, a test router, and an "apiURL" provide value
 * that mirrors what cms.ts injects at runtime.
 */

// Components build request URLs as inject("apiURL") + "cms/...". Tests assert against
// this prefix, so keep it stable and obvious.
const TEST_API_URL = "/api/"

// Route table mirroring the real CMS route names/paths (src/CMS/router/routes.ts) so
// router-link :to and programmatic navigation resolve exactly as they do in production.
const stub = { template: "<div />" }
const CMS_ROUTES: RouteRecordRaw[] = [
    { path: "/", name: "CmsHome", component: stub },
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
// $q.notify / $q.dialog don't blow up), a router, and the apiURL provide.
function mountCms<T>(component: T, options: ComponentMountingOptions<T> = {}, router?: Router) {
    const testRouter = router ?? createTestRouter()
    const { global: globalOptions = {}, ...rest } = options as Record<string, unknown> & {
        global?: Record<string, unknown>
    }
    return mount(
        component as never,
        {
            ...rest,
            global: {
                plugins: [[Quasar, { plugins: { Notify, Dialog } }], testRouter],
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

export { createTestRouter, mountCms, flushPromises, flushRouter }
