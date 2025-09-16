import ViperLayout from '@/layouts/ViperLayout.vue'
import ViperLayoutSimple from '@/layouts/ViperLayoutSimple.vue'

const CAHFSBreadcrumbs = [{ url: "Home", name: "Return to CAHFS Home" }]

const routes = [
    {
        path: '/CAHFS/',
        meta: { layout: ViperLayout, allowUnAuth: true },
        component: () => import('@/CAHFS/pages/CAHFSHome.vue'),
        name: 'CAHFSAuth',
    },
    {
        path: '/CAHFS/Home',
        meta: { layout: ViperLayout, permissions: ["SVMSecure.CAHFS"] },
        component: () => import('@/CAHFS/pages/CAHFSHome.vue'),
        name: "CAHFSHome"
    },
    {
        path: '/CAHFS/WebReports',
        name: 'WebReports',
        meta: { layout: ViperLayout, permissions: ["SVMSecure.CAHFS"] },
        component: () => import('@/CAHFS/pages/WebReports.vue'),
    },
    {
        path: '/:catchAll(.*)*',
        meta: { layout: ViperLayout },
        component: () => import('@/pages/Error404.vue')
    }
]

export default routes