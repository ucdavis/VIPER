import ViperLayout from "@/layouts/ViperLayout.vue"
import type { RouteLocationNormalized } from "vue-router"
import { checkHasOnePermission } from "@/composables/CheckPagePermission"

const routes = [
    {
        path: "/CMS/",
        meta: { layout: ViperLayout, allowUnAuth: true },
        component: () => import("@/CMS/pages/CmsHome.vue"),
        name: "CmsAuth",
    },
    {
        path: "/CMS/Home",
        meta: { layout: ViperLayout, permissions: ["SVMSecure.CMS"] },
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
        // CreateContentBlock-only users may create via this route (no id);
        // editing an existing block requires ManageContentBlocks, matching
        // the API, which gates GET/PUT of existing blocks the same way.
        meta: {
            layout: ViperLayout,
            permissions: ["SVMSecure.CMS.ManageContentBlocks", "SVMSecure.CMS.CreateContentBlock"],
        },
        beforeEnter: (to: RouteLocationNormalized) =>
            !to.params.id || checkHasOnePermission(["SVMSecure.CMS.ManageContentBlocks"]) ? true : { name: "CmsAuth" },
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

export { routes }
