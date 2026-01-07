import EffortLayout from "@/Effort/layouts/EffortLayout.vue"

// Term code format: YYYYXX where XX is 01-10 (valid semester/quarter codes)
// Vue Router's path-to-regexp doesn't support alternation (|) in custom patterns.
// Using \d{6} for basic validation; backend validates the full format.
const TERM_CODE_PATTERN = String.raw`\d{6}`

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
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/courses`,
        meta: {
            layout: EffortLayout,
            permissions: [
                "SVMSecure.Effort.ViewAllDepartments",
                "SVMSecure.Effort.ImportCourse",
                "SVMSecure.Effort.EditCourse",
                "SVMSecure.Effort.DeleteCourse",
                "SVMSecure.Effort.ManageRCourseEnrollment",
            ],
        },
        component: () => import("@/Effort/pages/CourseList.vue"),
        name: "CourseList",
    },
    {
        path: "/Effort/audit",
        meta: { layout: EffortLayout, permissions: ["SVMSecure.Effort.ViewAudit"] },
        component: () => import("@/Effort/pages/AuditList.vue"),
        name: "EffortAudit",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/audit`,
        meta: { layout: EffortLayout, permissions: ["SVMSecure.Effort.ViewAudit"] },
        component: () => import("@/Effort/pages/AuditList.vue"),
        name: "EffortAuditWithTerm",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})`,
        meta: { layout: EffortLayout },
        component: () => import("@/Effort/pages/EffortHome.vue"),
        name: "EffortHomeWithTerm",
    },
]

export { routes as effortRoutes }
