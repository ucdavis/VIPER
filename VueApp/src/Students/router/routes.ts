import ViperLayout from "@/layouts/ViperLayout.vue"

const routes = [
    {
        path: "/Students/",
        meta: { layout: ViperLayout },
        component: () => import("@/Students/pages/StudentsHome.vue"),
        name: "StudentsHome",
    },
    {
        path: "/Students/Home",
        meta: { layout: ViperLayout },
        component: () => import("@/Students/pages/StudentsHome.vue"),
    },
    {
        path: "/Students/StudentClassYear",
        meta: { layout: ViperLayout },
        component: () => import("@/Students/pages/StudentClassYear.vue"),
    },
    {
        path: "/Students/StudentClassYearImport",
        meta: { layout: ViperLayout },
        component: () => import("@/Students/pages/StudentClassYearImport.vue"),
    },
    {
        path: "/Students/PhotoGallery",
        meta: { layout: ViperLayout },
        component: () => import("@/Students/pages/PhotoGallery.vue"),
        name: "PhotoGallery",
    },
]

export { routes }
