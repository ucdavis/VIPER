import ViperLayout from '@/layouts/ViperLayout.vue'

const routes = [
    {
        path: '/CAHFS/',
        meta: { layout: ViperLayout, allowUnAuth: true, showViewWhenNotLoggedIn: true },
        component: () => import('@/CAHFS/pages/CAHFSAuth.vue'),
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
        path: '/CAHFS/Section',
        name: 'CAHFSSection',
        meta: { layout: ViperLayout, permissions: ["SVMSecure.CAHFS"] },
        component: () => import('@/CAHFS/pages/CAHFSSection.vue'),
    },
    {
        path: '/:catchAll(.*)*',
        meta: { layout: ViperLayout },
        //component: () => import('@/pages/Error404.vue')
        component: () => import('@/CAHFS/pages/CAHFSHome.vue'),
    }
]

export default routes
