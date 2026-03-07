import EffortLayout from "@/Effort/layouts/EffortLayout.vue"

const TERM_CODE_PATTERN = String.raw`\d{6}`
const OPT_TERM = `:termCode(${TERM_CODE_PATTERN})?`

const REPORT_PERMISSIONS = [
    "SVMSecure.Effort.ViewAllDepartments",
    "SVMSecure.Effort.ViewDept",
    "SVMSecure.Effort.Reports",
]

const reportRoutes = [
    {
        path: `/Effort/${OPT_TERM}/reports/teaching/grouped`,
        meta: { layout: EffortLayout, permissions: REPORT_PERMISSIONS },
        component: () => import("@/Effort/pages/TeachingActivityGrouped.vue"),
        name: "TeachingActivityGrouped",
    },
    {
        path: `/Effort/${OPT_TERM}/reports/teaching/individual`,
        meta: { layout: EffortLayout, permissions: REPORT_PERMISSIONS },
        component: () => import("@/Effort/pages/TeachingActivityIndividual.vue"),
        name: "TeachingActivityIndividual",
    },
    {
        path: `/Effort/${OPT_TERM}/reports/teaching/dept-summary`,
        meta: { layout: EffortLayout, permissions: REPORT_PERMISSIONS },
        component: () => import("@/Effort/pages/DeptSummary.vue"),
        name: "DeptSummary",
    },
    {
        path: `/Effort/${OPT_TERM}/reports/teaching/school-summary`,
        meta: {
            layout: EffortLayout,
            permissions: ["SVMSecure.Effort.ViewAllDepartments", "SVMSecure.Effort.SchoolSummary"],
        },
        component: () => import("@/Effort/pages/SchoolSummary.vue"),
        name: "SchoolSummary",
    },
    {
        path: `/Effort/${OPT_TERM}/reports/merit/detail`,
        meta: { layout: EffortLayout, permissions: REPORT_PERMISSIONS },
        component: () => import("@/Effort/pages/MeritDetail.vue"),
        name: "MeritDetail",
    },
    {
        path: `/Effort/${OPT_TERM}/reports/merit/average`,
        meta: { layout: EffortLayout, permissions: REPORT_PERMISSIONS },
        component: () => import("@/Effort/pages/MeritAverage.vue"),
        name: "MeritAverage",
    },
    {
        path: `/Effort/${OPT_TERM}/reports/merit/summary`,
        meta: { layout: EffortLayout, permissions: REPORT_PERMISSIONS },
        component: () => import("@/Effort/pages/MeritSummary.vue"),
        name: "MeritSummary",
    },
    {
        path: `/Effort/${OPT_TERM}/reports/merit/clinical`,
        meta: { layout: EffortLayout, permissions: REPORT_PERMISSIONS },
        component: () => import("@/Effort/pages/ClinicalEffort.vue"),
        name: "ClinicalEffort",
    },
    {
        path: `/Effort/${OPT_TERM}/reports/merit/multiyear`,
        meta: { layout: EffortLayout, permissions: REPORT_PERMISSIONS },
        component: () => import("@/Effort/pages/MultiYearReport.vue"),
        name: "MultiYearReport",
    },
    {
        path: `/Effort/${OPT_TERM}/reports/clinical-schedule`,
        meta: {
            layout: EffortLayout,
            permissions: ["SVMSecure.Effort.ViewAllDepartments"],
        },
        component: () => import("@/Effort/pages/ScheduledCliWeeks.vue"),
        name: "ScheduledCliWeeks",
    },
    {
        path: `/Effort/${OPT_TERM}/reports/eval/summary`,
        meta: { layout: EffortLayout, permissions: REPORT_PERMISSIONS },
        component: () => import("@/Effort/pages/EvalSummary.vue"),
        name: "EvalSummary",
    },
    {
        path: `/Effort/${OPT_TERM}/reports/eval/detail`,
        meta: { layout: EffortLayout, permissions: REPORT_PERMISSIONS },
        component: () => import("@/Effort/pages/EvalDetail.vue"),
        name: "EvalDetail",
    },
]

export { reportRoutes }
