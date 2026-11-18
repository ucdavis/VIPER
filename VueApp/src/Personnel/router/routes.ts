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
    {
        path: "/:catchAll(.*)*",
        meta: { layout: ViperLayout },
        component: () => import("@/pages/Error404.vue"),
    },
]

export { routes }
