import ViperLayout from '@/layouts/ViperLayout.vue'
import ViperLayoutSimple from '@/layouts/ViperLayoutSimple.vue'

const cmsBreadcrumbs = [{ url: "Home", name: "Return to CMS Home" }]

const routes = [
    {
        path: '/CMS/',
        meta: { layout: ViperLayout, allowUnAuth: true },
        component: () => import('@/CMS/pages/CmsHome.vue'),
        name: 'CmsAuth',
    },
    {
        path: '/CMS/Home',
        meta: { layout: ViperLayout, permissions: ["SVMSecure.CMS"] },
        component: () => import('@/CMS/pages/CmsHome.vue'),
        name: "CmsHome"
    },
    {
        path: '/CMS/LinkCollections',
        name: 'LinkCollections',
        meta: { layout: ViperLayout, permissions: ["SVMSecure.CMS.ManageContentBlocks"] },
        component: () => import('@/CMS/pages/LinkCollections.vue'),
    },
    {
        path: '/:catchAll(.*)*',
        meta: { layout: ViperLayout },
        component: () => import('@/pages/Error404.vue')
    }
]

export default routes