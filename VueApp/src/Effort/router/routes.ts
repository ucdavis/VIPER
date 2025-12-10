import ViperLayout from "@/layouts/ViperLayout.vue"

const routes = [
    {
        path: "/Effort/",
        meta: { layout: ViperLayout },
        component: () => import("@/Effort/pages/EffortHome.vue"),
        name: "EffortHome",
    },
    {
        path: "/Effort/Home",
        meta: { layout: ViperLayout },
        component: () => import("@/Effort/pages/EffortHome.vue"),
    },
    {
        path: "/Effort/:termCode(\\d+)",
        meta: { layout: ViperLayout },
        component: () => import("@/Effort/pages/EffortHome.vue"),
        name: "EffortHomeWithTerm",
    },
]

export { routes as effortRoutes }
