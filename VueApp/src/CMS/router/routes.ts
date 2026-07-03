import ViperLayout from "@/layouts/ViperLayout.vue"
import { checkHasOnePermission } from "@/composables/CheckPagePermission"
import type { RouteLocationNormalized } from "vue-router"

// Granular CMS permissions that grant the Home hub. The hub adapts to whichever of these the user
// holds, and CmsNavMenu links it for any of them, so the Home route guard and the area-root
// canonicalization (router/index.ts) gate on this one shared set rather than two lists that drift.
const cmsHomePermissions = [
    "SVMSecure.CMS",
    "SVMSecure.CMS.ManageContentBlocks",
    "SVMSecure.CMS.CreateContentBlock",
    "SVMSecure.CMS.AllFiles",
    "SVMSecure.CMS.ManageNavigation",
]

const routes = [
    {
        path: "/CMS/",
        meta: { layout: ViperLayout, allowUnAuth: true },
        component: () => import("@/CMS/pages/CmsHome.vue"),
        name: "CmsAuth",
    },
    {
        path: "/CMS/Home",
        meta: {
            layout: ViperLayout,
            permissions: cmsHomePermissions,
        },
        component: () => import("@/CMS/pages/CmsHome.vue"),
        name: "CmsHome",
    },
    {
        path: "/CMS/ManageLinkCollections",
        name: "ManageLinkCollections",
        meta: { layout: ViperLayout, permissions: ["SVMSecure.CMS.ManageContentBlocks"] },
        component: () => import("@/CMS/pages/ManageLinkCollections.vue"),
    },
    {
        // Not /CMS/Files: that path is the MVC download handler (CMSController.Files),
        // which takes precedence over the SPA shell.
        path: "/CMS/ManageFiles",
        name: "CmsFiles",
        meta: { layout: ViperLayout, permissions: ["SVMSecure.CMS.AllFiles"] },
        component: () => import("@/CMS/pages/Files.vue"),
    },
    {
        path: "/CMS/ManageFiles/Audit",
        name: "CmsFileAudit",
        meta: { layout: ViperLayout, permissions: ["SVMSecure.CMS.AllFiles"] },
        component: () => import("@/CMS/pages/FileAuditLog.vue"),
    },
    {
        path: "/CMS/ManageFiles/Import",
        name: "CmsFileImport",
        meta: { layout: ViperLayout, permissions: ["SVMSecure.CMS.AllFiles"] },
        component: () => import("@/CMS/pages/ImportFiles.vue"),
    },
    {
        path: "/CMS/ManageFiles/BulkEncrypt",
        name: "CmsBulkEncrypt",
        meta: { layout: ViperLayout, permissions: ["SVMSecure.CMS.AllFiles"] },
        component: () => import("@/CMS/pages/BulkEncrypt.vue"),
    },
    {
        path: "/CMS/ManageContentBlocks",
        name: "CmsContentBlocks",
        meta: { layout: ViperLayout, permissions: ["SVMSecure.CMS.ManageContentBlocks"] },
        component: () => import("@/CMS/pages/ContentBlocks.vue"),
    },
    {
        path: "/CMS/ManageContentBlocks/History",
        name: "CmsContentBlockHistory",
        meta: { layout: ViperLayout, permissions: ["SVMSecure.CMS.ManageContentBlocks"] },
        component: () => import("@/CMS/pages/ContentBlockHistory.vue"),
    },
    {
        path: "/CMS/ManageContentBlocks/Edit/:id?",
        name: "CmsContentBlockEdit",
        // Deliberately no meta.permissions: managers, CreateContentBlock holders, and delegated
        // editors (who hold one of a block's edit permissions but NO CMS-prefixed permission) all
        // use this route, and the store only loads SVMSecure.CMS-prefixed permissions - so any
        // permission gate here would lock out delegates (a plain "SVMSecure" entry never loads).
        // requireLogin still forces an authenticated session, the server 403s the block's
        // GET/PUT/PATCH for anyone who may neither manage nor edit it, and the page adapts to the
        // user's capability (full editor vs. content-only).
        meta: {
            layout: ViperLayout,
        },
        // Edit mode (an id in the path) stays open to delegated editors — the server enforces
        // per-block access. Create mode (no id) has no per-block delegation to fall back on, so gate it
        // to users who can actually create a block; otherwise the Add form is a guaranteed-forbidden
        // save (PATCH /content/0/content → 403) for a non-creator who lands on it.
        beforeEnter: (to: RouteLocationNormalized) => {
            if (to.params.id) {
                return true
            }
            return checkHasOnePermission(["SVMSecure.CMS.CreateContentBlock", "SVMSecure.CMS.ManageContentBlocks"])
                ? true
                : { name: "CmsHome" }
        },
        component: () => import("@/CMS/pages/ContentBlockEdit.vue"),
    },
    {
        path: "/CMS/ManageLeftNav",
        name: "CmsLeftNavMenus",
        meta: { layout: ViperLayout, permissions: ["SVMSecure.CMS.ManageNavigation"] },
        component: () => import("@/CMS/pages/LeftNavMenus.vue"),
    },
    {
        path: "/CMS/ManageLeftNav/Edit/:id?",
        name: "CmsLeftNavEdit",
        meta: { layout: ViperLayout, permissions: ["SVMSecure.CMS.ManageNavigation"] },
        component: () => import("@/CMS/pages/LeftNavEdit.vue"),
    },
    {
        path: "/:catchAll(.*)*",
        meta: { layout: ViperLayout },
        component: () => import("@/pages/Error404.vue"),
    },
]

export { routes, cmsHomePermissions }
