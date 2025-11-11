import ViperLayout from '@/layouts/ViperLayout.vue'
//import ViperLayoutSimple from '@/layouts/ViperLayoutSimple.vue'

const routes = [
    {
        path: '/Research/',
        alias: '/Research/Home',
        meta: { layout: ViperLayout, allowUnAuth: false },
        component: () => import('@/Research/pages/Home.vue'),
        name: "Home"
    },
    {
        path: '/Research/AggieEnterprise',
        meta: { layout: ViperLayout, allowUnAuth: false },
        component: () => import('@/Research/pages/AggieEnterprise.vue'),
        name: "AggieEnterprise"
    },
    {
        path: '/:catchAll(.*)*',
        meta: { layout: ViperLayout },
        component: () => import('@/pages/Error404.vue')
    }
]

export { routes as researchRoutes }
