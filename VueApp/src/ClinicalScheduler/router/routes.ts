import ViperLayout from "@/layouts/ViperLayout.vue"

const routes = [
    {
        path: "/ClinicalScheduler/",
        meta: { layout: ViperLayout },
        component: () => import("@/ClinicalScheduler/pages/ClinicalSchedulerHome.vue"),
        name: "ClinicalSchedulerHome",
    },
    {
        path: "/ClinicalScheduler/Home",
        meta: { layout: ViperLayout },
        component: () => import("@/ClinicalScheduler/pages/ClinicalSchedulerHome.vue"),
    },
    {
        path: "/ClinicalScheduler/rotation",
        meta: { layout: ViperLayout },
        component: () => import("@/ClinicalScheduler/pages/RotationScheduleView.vue"),
        name: "RotationSchedule",
    },
    {
        path: "/ClinicalScheduler/rotation/:rotationId",
        meta: { layout: ViperLayout },
        component: () => import("@/ClinicalScheduler/pages/RotationScheduleView.vue"),
        name: "RotationScheduleWithId",
    },
    {
        path: "/ClinicalScheduler/clinician",
        meta: { layout: ViperLayout },
        component: () => import("@/ClinicalScheduler/pages/ClinicianScheduleView.vue"),
        name: "ClinicianSchedule",
    },
    {
        path: "/ClinicalScheduler/clinician/:mothraId",
        meta: { layout: ViperLayout },
        component: () => import("@/ClinicalScheduler/pages/ClinicianScheduleView.vue"),
        name: "ClinicianScheduleWithId",
    },
]

export { routes as clinicalSchedulerRoutes }
