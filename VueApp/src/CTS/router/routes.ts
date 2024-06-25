import ViperLayout from '@/layouts/ViperLayout.vue'
import ViperLayoutSimple from '@/layouts/ViperLayoutSimple.vue'

const routes = [
    {
        path: '/CTS/',
        meta: { layout: ViperLayoutSimple, allowUnAuth: true },
        component: () => import('@/CTS/pages/CtsHome.vue'),
        name: "CtsHome"
    },
    {
        path: '/CTS/Home',
        meta: { layout: ViperLayoutSimple },
        component: () => import('@/CTS/pages/CtsHome.vue'),
    },
    /* Assessments */
    {
        path: '/CTS/EPA',
        meta: { layout: ViperLayoutSimple },
        component: () => import('@/CTS/pages/AssessmentEpa.vue'),
    },
    {
        path: '/CTS/AssessmentList',
        name: 'AssessmentList',
        meta: { layout: ViperLayout },
        component: () => import('@/CTS/pages/AssessmentList.vue'),
    },
    {
        path: '/CTS/AssessmentEpaEdit',
        name: 'AssessmentEpaEdit',
        meta: { layout: ViperLayoutSimple },
        component: () => import('@/CTS/pages/AssessmentEpaEdit.vue'),
    },
    /* Application Management */
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
        path: '/CTS/Audit',
        name: 'Audit Log',
        meta: { layout: ViperLayout },
        component: () => import('@/CTS/pages/AuditList.vue'),
    },
    {
        path: '/:catchAll(.*)*',
        component: () => import('@/pages/Error404.vue')
    }
]

export default routes