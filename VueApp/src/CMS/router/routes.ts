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
        path: "/:catchAll(.*)*",
        meta: { layout: ViperLayout },
        component: () => import("@/pages/Error404.vue"),
    },
]

export { routes }
