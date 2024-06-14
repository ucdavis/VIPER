import ViperLayout from '@/layouts/ViperLayout.vue'

const routes = [
    {
        path: '/CTS/',
        meta: { layout: ViperLayout, allowUnAuth: true },
        component: () => import('@/CTS/pages/CtsHome.vue'),
        name: "CtsHome"
    },
    {
        path: '/CTS/Home',
        meta: { layout: ViperLayout },
        component: () => import('@/CTS/pages/CtsHome.vue'),
    },
    {
        path: '/CTS/Assessments',
        meta: { layout: ViperLayout },
        component: () => import('@/CTS/pages/Assessments.vue'),
    },
    {
        path: '/CTS/ManageDomains',
        meta: { layout: ViperLayout },
        component: () => import('@/CTS/pages/ManageDomains.vue'),
    },
    {
        path: '/CTS/ManageEPAs',
        meta: { layout: ViperLayout },
        component: () => import('@/CTS/pages/ManageEpas.vue'),
    },
    {
        path: '/CTS/ManageLevels',
        meta: { layout: ViperLayout },
        component: () => import('@/CTS/pages/ManageLevels.vue'),
    },
    {
        path: '/:catchAll(.*)*',
        component: () => import('@/pages/Error404.vue')
    }
]

export default routes