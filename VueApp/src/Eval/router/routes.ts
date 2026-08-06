import EvalLayout from "@/Eval/layouts/EvalLayout.vue"

const routes = [
    {
        path: "/Eval/",
        alias: "/Eval/Home",
        meta: { layout: EvalLayout, allowUnAuth: false },
        component: () => import("@/Eval/pages/EvalHome.vue"),
        name: "EvalHome",
    },
    {
        path: "/Eval/Evaluate/:studentId",
        meta: { layout: EvalLayout, permissions: ["SVMSecure.Eval"] },
        component: () => import("@/Eval/pages/Evaluate.vue"),
        name: "Evaluate",
        props: true,
    },
    {
        path: "/:catchAll(.*)*",
        meta: { layout: EvalLayout },
        component: () => import("@/pages/Error404.vue"),
    },
]

export { routes }

