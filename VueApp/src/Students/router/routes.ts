import type { RouteLocationNormalized } from "vue-router"
import ViperLayout from "@/layouts/ViperLayout.vue"
import { checkHasOnePermission } from "@/composables/CheckPagePermission"
import { useUserStore } from "@/store/UserStore"

const adminPermissions = ["SVMSecure.Students.EmergencyContactAdmin", "SVMSecure.SIS.AllStudents"]

function requireOwnStudentRecord(pidm: string | number) {
    if (checkHasOnePermission(adminPermissions)) {
        return true
    }

    const userStore = useUserStore()
    const currentUserId = userStore.userInfo.userId
    if (!currentUserId) {
        return { name: "StudentsHome" }
    }

    return Number(pidm) === Number(currentUserId)
        ? true
        : { name: "EmergencyContactView", params: { pidm: currentUserId } }
}

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
    {
        path: "/Students/EmergencyContact/",
        meta: {
            layout: ViperLayout,
            permissions: [
                "SVMSecure.Students.EmergencyContactAdmin",
                "SVMSecure.Students.EmergencyContactStudent",
                "SVMSecure.SIS.AllStudents",
            ],
        },
        children: [
            {
                path: "",
                name: "EmergencyContactList",
                meta: { layout: ViperLayout },
                beforeEnter: () => {
                    if (!checkHasOnePermission(adminPermissions)) {
                        const userStore = useUserStore()
                        if (userStore.userInfo.userId) {
                            return { name: "EmergencyContactEdit", params: { pidm: userStore.userInfo.userId } }
                        }
                    }
                },
                component: () => import("@/Students/EmergencyContact/pages/EmergencyContactList.vue"),
            },
            {
                path: "edit/:pidm",
                name: "EmergencyContactEdit",
                beforeEnter: (to: RouteLocationNormalized) => requireOwnStudentRecord(to.params.pidm as string),
                meta: {
                    layout: ViperLayout,
                    permissions: [
                        "SVMSecure.Students.EmergencyContactAdmin",
                        "SVMSecure.Students.EmergencyContactStudent",
                    ],
                },
                component: () => import("@/Students/EmergencyContact/pages/EmergencyContactForm.vue"),
            },
            {
                path: "view/:pidm",
                name: "EmergencyContactView",
                beforeEnter: (to: RouteLocationNormalized) => requireOwnStudentRecord(to.params.pidm as string),
                meta: { layout: ViperLayout },
                component: () => import("@/Students/EmergencyContact/pages/EmergencyContactView.vue"),
            },
            {
                path: "report",
                name: "EmergencyContactReport",
                meta: {
                    layout: ViperLayout,
                    permissions: ["SVMSecure.Students.EmergencyContactAdmin", "SVMSecure.SIS.AllStudents"],
                },
                component: () => import("@/Students/EmergencyContact/pages/EmergencyContactReport.vue"),
            },
        ],
    },
]

export { routes }
