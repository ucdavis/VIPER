import EffortLayout from "@/Effort/layouts/EffortLayout.vue"

// Term code format: YYYYXX where XX is 01-10 (valid semester/quarter codes)
// Vue Router's path-to-regexp doesn't support alternation (|) in custom patterns.
// Using \d{6} for basic validation; backend validates the full format.
const TERM_CODE_PATTERN = String.raw`\d{6}`

// View-level access permissions - any of these grants access to base Effort pages
const VIEW_ACCESS_PERMISSIONS = [
    "SVMSecure.Effort.ViewAllDepartments",
    "SVMSecure.Effort.ViewDept",
    "SVMSecure.Effort.VerifyEffort",
]

const routes = [
    {
        path: "/Effort/",
        meta: { layout: EffortLayout, permissions: VIEW_ACCESS_PERMISSIONS },
        component: () => import("@/Effort/pages/EffortHome.vue"),
        name: "EffortHome",
    },
    {
        path: "/Effort/Home",
        meta: { layout: EffortLayout, permissions: VIEW_ACCESS_PERMISSIONS },
        component: () => import("@/Effort/pages/EffortHome.vue"),
    },
    {
        path: "/Effort/terms",
        meta: { layout: EffortLayout, permissions: VIEW_ACCESS_PERMISSIONS },
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
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/dashboard`,
        meta: {
            layout: EffortLayout,
            permissions: ["SVMSecure.Effort.ViewAllDepartments", "SVMSecure.Effort.ViewDept"],
        },
        component: () => import("@/Effort/pages/StaffDashboard.vue"),
        name: "StaffDashboard",
    },
    {
        path: "/Effort/dashboard",
        meta: {
            layout: EffortLayout,
            permissions: ["SVMSecure.Effort.ViewAllDepartments", "SVMSecure.Effort.ViewDept"],
        },
        component: () => import("@/Effort/pages/StaffDashboard.vue"),
        name: "StaffDashboardNoTerm",
    },
    {
        path: "/Effort/percent-assign-types",
        meta: { layout: EffortLayout, permissions: ["SVMSecure.Effort.ViewAllDepartments"] },
        component: () => import("@/Effort/pages/PercentAssignTypeList.vue"),
        name: "PercentAssignTypeList",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/percent-assign-types`,
        meta: { layout: EffortLayout, permissions: ["SVMSecure.Effort.ViewAllDepartments"] },
        component: () => import("@/Effort/pages/PercentAssignTypeList.vue"),
        name: "PercentAssignTypeListWithTerm",
    },
    {
        path: `/Effort/percent-assign-types/:typeId(\\d+)/instructors`,
        meta: { layout: EffortLayout, permissions: ["SVMSecure.Effort.ViewAllDepartments"] },
        component: () => import("@/Effort/pages/PercentAssignTypeInstructors.vue"),
        name: "PercentAssignTypeInstructors",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/percent-assign-types/:typeId(\\d+)/instructors`,
        meta: { layout: EffortLayout, permissions: ["SVMSecure.Effort.ViewAllDepartments"] },
        component: () => import("@/Effort/pages/PercentAssignTypeInstructors.vue"),
        name: "PercentAssignTypeInstructorsWithTerm",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/courses`,
        meta: {
            layout: EffortLayout,
            permissions: [
                "SVMSecure.Effort.ViewAllDepartments",
                "SVMSecure.Effort.ViewDept",
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
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/courses/:courseId(\\d+)/:tab(effort|evaluation)?`,
        meta: {
            layout: EffortLayout,
            permissions: [
                "SVMSecure.Effort.ViewAllDepartments",
                "SVMSecure.Effort.ViewDept",
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
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/instructors/:personId(\\d+)/edit`,
        meta: {
            layout: EffortLayout,
            permissions: [
                "SVMSecure.Effort.ViewAllDepartments",
                "SVMSecure.Effort.ViewDept",
                "SVMSecure.Effort.EditInstructor",
            ],
        },
        component: () => import("@/Effort/pages/InstructorEdit.vue"),
        name: "InstructorEdit",
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
        path: "/Effort/effort-types",
        meta: { layout: EffortLayout, permissions: ["SVMSecure.Effort.ManageEffortTypes"] },
        component: () => import("@/Effort/pages/EffortTypeList.vue"),
        name: "EffortTypeList",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/effort-types`,
        meta: { layout: EffortLayout, permissions: ["SVMSecure.Effort.ManageEffortTypes"] },
        component: () => import("@/Effort/pages/EffortTypeList.vue"),
        name: "EffortTypeListWithTerm",
    },
    {
        path: "/Effort/audit",
        meta: { layout: EffortLayout, permissions: ["SVMSecure.Effort.ViewAudit", "SVMSecure.Effort.ViewDeptAudit"] },
        component: () => import("@/Effort/pages/AuditList.vue"),
        name: "EffortAudit",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/audit`,
        meta: { layout: EffortLayout, permissions: ["SVMSecure.Effort.ViewAudit", "SVMSecure.Effort.ViewDeptAudit"] },
        component: () => import("@/Effort/pages/AuditList.vue"),
        name: "EffortAuditWithTerm",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})/my-effort`,
        meta: { layout: EffortLayout, permissions: VIEW_ACCESS_PERMISSIONS },
        component: () => import("@/Effort/pages/MyEffort.vue"),
        name: "MyEffort",
    },
    {
        path: `/Effort/:termCode(${TERM_CODE_PATTERN})`,
        meta: { layout: EffortLayout, permissions: VIEW_ACCESS_PERMISSIONS },
        component: () => import("@/Effort/pages/EffortHome.vue"),
        name: "EffortHomeWithTerm",
    },
]

export { routes as effortRoutes }
