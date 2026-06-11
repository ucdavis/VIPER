import ViperLayout from "@/layouts/ViperLayout.vue"

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
        path: "/CMS/ManageContentBlocks",
        name: "CmsContentBlocks",
        meta: { layout: ViperLayout, permissions: ["SVMSecure.CMS.ManageContentBlocks"] },
        component: () => import("@/CMS/pages/ContentBlocks.vue"),
    },
    {
        path: "/CMS/ManageContentBlocks/Edit/:id?",
        name: "CmsContentBlockEdit",
        meta: { layout: ViperLayout, permissions: ["SVMSecure.CMS.ManageContentBlocks"] },
        component: () => import("@/CMS/pages/ContentBlockEdit.vue"),
    },
    {
        path: "/:catchAll(.*)*",
        meta: { layout: ViperLayout },
        component: () => import("@/pages/Error404.vue"),
    },
]

export { routes }
