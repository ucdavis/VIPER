import EffortLayout from "@/Effort/layouts/EffortLayout.vue"

const routes = [
    {
        path: "/Effort/",
        meta: { layout: EffortLayout },
        component: () => import("@/Effort/pages/EffortHome.vue"),
        name: "EffortHome",
    },
    {
        path: "/Effort/Home",
        meta: { layout: EffortLayout },
        component: () => import("@/Effort/pages/EffortHome.vue"),
    },
    {
        path: "/Effort/terms",
        meta: { layout: EffortLayout },
        component: () => import("@/Effort/pages/TermSelection.vue"),
        name: "TermSelection",
    },
    {
        path: "/Effort/terms/manage",
        meta: { layout: EffortLayout, permissions: ["SVMSecure.Effort.ManageTerms"] },
        component: () => import("@/Effort/pages/TermManagement.vue"),
        name: "TermManagement",
    },
    {
        path: "/Effort/:termCode(\\d+)",
        meta: { layout: EffortLayout },
        component: () => import("@/Effort/pages/EffortHome.vue"),
        name: "EffortHomeWithTerm",
    },
]

export { routes as effortRoutes }
