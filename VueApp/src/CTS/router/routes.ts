import ViperLayout from '@/layouts/ViperLayout.vue'
import ViperLayoutSimple from '@/layouts/ViperLayoutSimple.vue'

const ctsBreadcrumbs = [{ url: "Home", name: "Return to CTS 2.0" }]

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
    /* Student pages */
    {
        path: '/CTS/MyAssessments',
        name: 'MyAssessments',
        meta: { layout: ViperLayout },
        component: () => import('@/CTS/pages/MyAssessments.vue'),
    },
    /* Assessments */
    {
        path: '/CTS/EPA',
        meta: { layout: ViperLayoutSimple, breadcrumbs: ctsBreadcrumbs },
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
        meta: { layout: ViperLayoutSimple, breadcrumbs: ctsBreadcrumbs },
        component: () => import('@/CTS/pages/AssessmentEpaEdit.vue'),
    },
    /* Course Competencies */
    {
        path: '/CTS/ManageCourseCompetencies',
        meta: { layout: ViperLayout },
        component: () => import('@/CTS/pages/ManageCourseCompetencies.vue'),
    },
    {
        path: '/CTS/ManageLegacyCompetencyMapping',
        meta: { layout: ViperLayout },
        component: () => import('@/CTS/pages/ManageLegacyCompetencyMapping.vue'),
    },
    /* Application Management */
    {
        path: '/CTS/ManageCompetencies',
        meta: { layout: ViperLayout },
        component: () => import('@/CTS/pages/ManageCompetencies.vue'),
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
        path: '/CTS/ManageMilestones',
        meta: { layout: ViperLayout },
        component: () => import('@/CTS/pages/ManageMilestones.vue')
    },
    {
        path: '/CTS/ManageLevels',
        meta: { layout: ViperLayout },
        component: () => import('@/CTS/pages/ManageLevels.vue'),
    },
    {
        path: '/CTS/ManageBundles',
        meta: { layout: ViperLayout },
        component: () => import('@/CTS/pages/ManageBundles.vue'),
    },
    {
        path: '/CTS/ManageBundleCompetencies',
        meta: { layout: ViperLayout },
        component: () => import('@/CTS/pages/ManageBundleCompetencies.vue'),
    },
    {
        path: '/CTS/ManageRoles',
        meta: { layout: ViperLayout },
        component: () => import('@/CTS/pages/ManageRoles.vue'),
    },
    {
        path: '/CTS/Audit',
        name: 'Audit Log',
        meta: { layout: ViperLayout },
        component: () => import('@/CTS/pages/AuditList.vue'),
    },
    {
        path: '/CTS/Test',
        name: 'Test Page',
        meta: { layout: ViperLayout },
        component: () => import('@/CTS/pages/TestPage.vue'),
    },
    /* Reports */
    {
        path: '/CTS/AssessmentChart',
        name: 'AssessmentCharts',
        meta: { layout: ViperLayout },
        component: () => import('@/CTS/pages/AssessmentChart.vue'),
    },
    {
        path: '/:catchAll(.*)*',
        meta: { layout: ViperLayout },
        component: () => import('@/pages/Error404.vue')
    }
]

export default routes