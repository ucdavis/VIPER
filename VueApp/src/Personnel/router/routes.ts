import ViperLayout from "@/layouts/ViperLayout.vue"
//Import ViperLayoutSimple from '@/layouts/ViperLayoutSimple.vue'

const routes = [
    {
        path: "/Personnel/",
        alias: "/Personnel/Home",
        meta: { layout: ViperLayout, allowUnAuth: false },
        component: () => import("@/Personnel/pages/Home.vue"),
        name: "PersonnelHome",
    },
    // Unit phone lists are addressed by their stable PhoneList.Code, so a new list is a row in
    // phones.PhoneList plus a nav entry rather than another pair of near-identical pages.
    {
        path: "/Personnel/PhoneList/:code",
        meta: { layout: ViperLayout, allowUnAuth: false },
        component: () => import("@/Personnel/pages/PhoneList.vue"),
        name: "PhoneList",
    },
    {
        // No meta.permissions: the required role is the list's own MaintainRole, which is not
        // known until the list is fetched. The page redirects if canMaintain comes back false,
        // and the API rejects writes independently.
        path: "/Personnel/PhoneList/:code/Maintain",
        meta: { layout: ViperLayout, allowUnAuth: false },
        component: () => import("@/Personnel/pages/PhoneListMaintain.vue"),
        name: "MaintainPhoneList",
    },
    // The legacy paths so existing links and bookmarks keep working.
    {
        path: "/Personnel/VMDOPhones",
        redirect: { name: "PhoneList", params: { code: "VMDO" } },
    },
    {
        path: "/Personnel/VMDOPhonesMaintain",
        redirect: { name: "MaintainPhoneList", params: { code: "VMDO" } },
    },
    {
        path: "/Personnel/SVMPhones",
        meta: { layout: ViperLayout, allowUnAuth: false },
        component: () => import("@/Personnel/pages/SVMPhones.vue"),
        name: "SchoolwidePhones",
    },
    {
        path: "/Personnel/SVMPhonesMaintain",
        meta: { layout: ViperLayout, allowUnAuth: false, permissions: ["SVMSecure.PhoneLists.SVMMaintain"] },
        component: () => import("@/Personnel/pages/SVMPhonesMaintain.vue"),
        name: "MaintainSchoolwidePhones",
    },
    // No meta.permissions: the guard only loads SVMSecure.PhoneLists.* into the browser, so a
    // route gate on SVMSecure.Personnel.PersonCollector could never pass. The API enforces it.
    {
        path: "/Personnel/PersonCollector",
        meta: { layout: ViperLayout, allowUnAuth: false },
        component: () => import("@/Personnel/pages/PersonCollector.vue"),
        name: "PersonCollector",
    },
    // Employee Information System. The API checks access to each employee (department users
    // see only the people their units pay), so these routes carry no permission meta; a page the
    // API refuses says the employee isn't available.
    {
        path: "/Personnel/EIS",
        meta: { layout: ViperLayout, allowUnAuth: false },
        component: () => import("@/Personnel/pages/EisSelectPerson.vue"),
        name: "EisSelectPerson",
    },
    {
        path: "/Personnel/EIS/:employeeId",
        meta: { layout: ViperLayout, allowUnAuth: false },
        component: () => import("@/Personnel/pages/EisPerson.vue"),
        children: [
            { path: "", name: "EisSummary", component: () => import("@/Personnel/pages/EisSummary.vue") },
            { path: "Academics", name: "EisAcademics", component: () => import("@/Personnel/pages/EisAcademics.vue") },
            {
                path: "Appointments",
                name: "EisAppointments",
                component: () => import("@/Personnel/pages/EisAppointments.vue"),
            },
            {
                path: "Category",
                name: "EisAppointmentCategory",
                component: () => import("@/Personnel/pages/EisAppointmentCategory.vue"),
            },
            { path: "History", name: "EisHistory", component: () => import("@/Personnel/pages/EisHistory.vue") },
            { path: "Address", name: "EisAddress", component: () => import("@/Personnel/pages/EisAddress.vue") },
        ],
    },
    {
        path: "/:catchAll(.*)*",
        meta: { layout: ViperLayout },
        component: () => import("@/pages/Error404.vue"),
    },
]

export { routes }
