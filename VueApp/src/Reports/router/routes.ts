import ViperLayout from "@/layouts/ViperLayout.vue"
import { REPORT_ADMIN_PERMISSION } from "@/Reports/admin/report-admin-permissions"

// A report's slug is a GUID. Matching only GUIDs leaves other paths under /Reports (such as
// /Reports/Admin) to their own routes or the 404 page, rather than looking them up as reports.
const SLUG = String.raw`[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}`

// The admin pages need the admin permission; the API checks it again on every request.
const adminMeta = { layout: ViperLayout, allowUnAuth: false, permissions: [REPORT_ADMIN_PERMISSION] }

const routes = [
    {
        path: "/Reports/",
        alias: "/Reports/Home",
        meta: { layout: ViperLayout, allowUnAuth: false },
        component: () => import("@/Reports/pages/ReportsHome.vue"),
        name: "ReportsHome",
    },
    {
        path: "/Reports/Admin",
        meta: adminMeta,
        component: () => import("@/Reports/pages/ReportAdminList.vue"),
        name: "ReportAdmin",
    },
    {
        path: "/Reports/Admin/New",
        meta: adminMeta,
        component: () => import("@/Reports/pages/ReportAdminEdit.vue"),
        name: "ReportAdminNew",
    },
    {
        path: "/Reports/Admin/Problems",
        meta: adminMeta,
        component: () => import("@/Reports/pages/ReportAdminProblems.vue"),
        name: "ReportAdminProblems",
    },
    {
        path: String.raw`/Reports/Admin/:id(\d+)`,
        meta: adminMeta,
        component: () => import("@/Reports/pages/ReportAdminEdit.vue"),
        name: "ReportAdminEdit",
    },
    {
        // Any admin-built report, by its slug. The API enforces each report's own permissions,
        // so the route needs none; a report the user can't view shows as not available.
        path: `/Reports/:slug(${SLUG})`,
        meta: { layout: ViperLayout, allowUnAuth: false },
        component: () => import("@/Reports/pages/ReportPage.vue"),
        name: "Report",
    },
    {
        path: "/:catchAll(.*)*",
        meta: { layout: ViperLayout },
        component: () => import("@/pages/Error404.vue"),
    },
]

export { routes }
