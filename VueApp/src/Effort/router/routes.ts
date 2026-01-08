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
        path: "/Effort/types",
        meta: { layout: EffortLayout, permissions: ["SVMSecure.Effort.ViewAllDepartments"] },
        component: () => import("@/Effort/pages/EffortTypeList.vue"),
        name: "EffortTypeList",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/types`,
        meta: { layout: EffortLayout, permissions: ["SVMSecure.Effort.ViewAllDepartments"] },
        component: () => import("@/Effort/pages/EffortTypeList.vue"),
        name: "EffortTypeListWithTerm",
    },
    {
        path: `/Effort/types/:typeId(\\d+)/instructors`,
        meta: { layout: EffortLayout, permissions: ["SVMSecure.Effort.ViewAllDepartments"] },
        component: () => import("@/Effort/pages/EffortTypeInstructors.vue"),
        name: "EffortTypeInstructors",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/types/:typeId(\\d+)/instructors`,
        meta: { layout: EffortLayout, permissions: ["SVMSecure.Effort.ViewAllDepartments"] },
        component: () => import("@/Effort/pages/EffortTypeInstructors.vue"),
        name: "EffortTypeInstructorsWithTerm",
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
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/courses/:courseId(\\d+)`,
        meta: {
            layout: EffortLayout,
            permissions: [
                "SVMSecure.Effort.ViewAllDepartments",
                "SVMSecure.Effort.ImportCourse",
                "SVMSecure.Effort.EditCourse",
                "SVMSecure.Effort.DeleteCourse",
                "SVMSecure.Effort.ManageRCourseEnrollment",
                "SVMSecure.Effort.LinkCourses",
            ],
        },
        component: () => import("@/Effort/pages/CourseDetail.vue"),
        name: "CourseDetail",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/instructors`,
        meta: {
            layout: EffortLayout,
            permissions: [
                "SVMSecure.Effort.ViewAllDepartments",
                "SVMSecure.Effort.ViewDept",
                "SVMSecure.Effort.ImportInstructor",
                "SVMSecure.Effort.EditInstructor",
                "SVMSecure.Effort.DeleteInstructor",
            ],
        },
        component: () => import("@/Effort/pages/InstructorList.vue"),
        name: "InstructorList",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/instructors/:personId(\\d+)`,
        meta: {
            layout: EffortLayout,
            permissions: [
                "SVMSecure.Effort.ViewAllDepartments",
                "SVMSecure.Effort.ViewDept",
                "SVMSecure.Effort.ImportInstructor",
                "SVMSecure.Effort.EditInstructor",
                "SVMSecure.Effort.DeleteInstructor",
            ],
        },
        component: () => import("@/Effort/pages/InstructorDetail.vue"),
        name: "InstructorDetail",
    },
    {
        path: "/Effort/units",
        meta: { layout: EffortLayout, permissions: ["SVMSecure.Effort.ManageUnits"] },
        component: () => import("@/Effort/pages/UnitList.vue"),
        name: "UnitList",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/units`,
        meta: { layout: EffortLayout, permissions: ["SVMSecure.Effort.ManageUnits"] },
        component: () => import("@/Effort/pages/UnitList.vue"),
        name: "UnitListWithTerm",
    },
    {
        path: "/Effort/session-types",
        meta: { layout: EffortLayout, permissions: ["SVMSecure.Effort.ManageSessionTypes"] },
        component: () => import("@/Effort/pages/SessionTypeList.vue"),
        name: "SessionTypeList",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/session-types`,
        meta: { layout: EffortLayout, permissions: ["SVMSecure.Effort.ManageSessionTypes"] },
        component: () => import("@/Effort/pages/SessionTypeList.vue"),
        name: "SessionTypeListWithTerm",
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
