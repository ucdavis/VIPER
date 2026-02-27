import EffortLayout from "@/Effort/layouts/EffortLayout.vue"

const TERM_CODE_PATTERN = String.raw`\d{6}`

const REPORT_PERMISSIONS = [
    "SVMSecure.Effort.ViewAllDepartments",
    "SVMSecure.Effort.ViewDept",
    "SVMSecure.Effort.Reports",
]

const reportRoutes = [
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/reports/teaching/grouped`,
        meta: { layout: EffortLayout, permissions: REPORT_PERMISSIONS },
        component: () => import("@/Effort/pages/TeachingActivityGrouped.vue"),
        name: "TeachingActivityGrouped",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/reports/teaching/individual`,
        meta: { layout: EffortLayout, permissions: REPORT_PERMISSIONS },
        component: () => import("@/Effort/pages/TeachingActivityIndividual.vue"),
        name: "TeachingActivityIndividual",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/reports/teaching/dept-summary`,
        meta: { layout: EffortLayout, permissions: REPORT_PERMISSIONS },
        component: () => import("@/Effort/pages/DeptSummary.vue"),
        name: "DeptSummary",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/reports/teaching/school-summary`,
        meta: {
            layout: EffortLayout,
            permissions: ["SVMSecure.Effort.ViewAllDepartments", "SVMSecure.Effort.SchoolSummary"],
        },
        component: () => import("@/Effort/pages/SchoolSummary.vue"),
        name: "SchoolSummary",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/reports/merit/detail`,
        meta: { layout: EffortLayout, permissions: REPORT_PERMISSIONS },
        component: () => import("@/Effort/pages/MeritDetail.vue"),
        name: "MeritDetail",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/reports/merit/average`,
        meta: { layout: EffortLayout, permissions: REPORT_PERMISSIONS },
        component: () => import("@/Effort/pages/MeritAverage.vue"),
        name: "MeritAverage",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/reports/merit/summary`,
        meta: { layout: EffortLayout, permissions: REPORT_PERMISSIONS },
        component: () => import("@/Effort/pages/MeritSummary.vue"),
        name: "MeritSummary",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/reports/merit/clinical`,
        meta: { layout: EffortLayout, permissions: REPORT_PERMISSIONS },
        component: () => import("@/Effort/pages/ClinicalEffort.vue"),
        name: "ClinicalEffort",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/reports/merit/multiyear`,
        meta: { layout: EffortLayout, permissions: REPORT_PERMISSIONS },
        component: () => import("@/Effort/pages/MultiYearReport.vue"),
        name: "MultiYearReport",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/reports/clinical-schedule`,
        meta: {
            layout: EffortLayout,
            permissions: ["SVMSecure.Effort.ViewAllDepartments"],
        },
        component: () => import("@/Effort/pages/ScheduledCliWeeks.vue"),
        name: "ScheduledCliWeeks",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/reports/eval/summary`,
        meta: { layout: EffortLayout, permissions: REPORT_PERMISSIONS },
        component: () => import("@/Effort/pages/EvalSummary.vue"),
        name: "EvalSummary",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/reports/eval/detail`,
        meta: { layout: EffortLayout, permissions: REPORT_PERMISSIONS },
        component: () => import("@/Effort/pages/EvalDetail.vue"),
        name: "EvalDetail",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/reports/year-stats`,
        meta: {
            layout: EffortLayout,
            permissions: ["SVMSecure.Effort.ViewAllDepartments"],
        },
        component: () => import("@/Effort/pages/YearStatistics.vue"),
        name: "YearStatistics",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/reports/zero-effort`,
        meta: { layout: EffortLayout, permissions: REPORT_PERMISSIONS },
        component: () => import("@/Effort/pages/ZeroEffort.vue"),
        name: "ZeroEffort",
    },
]

export { reportRoutes }
