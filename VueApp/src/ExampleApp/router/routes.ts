//these are the two layouts we've defined for Viper
import ViperLayout from '@/layouts/ViperLayout.vue'
import ViperLayoutSimple from '@/layouts/ViperLayoutSimple.vue'

const routes = [
    {
        path: '/ExampleApp/',
        alias: '/ExampleApp/ExampleHome',
        meta: { layout: ViperLayout, allowUnAuth: true },
        component: () => import('@/ExampleApp/pages/ExampleHome.vue'),
        name: "ExampleAppHome"
    },
    //note the permission check on this route
    {
        path: '/ExampleApp/FakeUsers',
        meta: { layout: ViperLayout, allowUnAuth: false },
        component: () => import('@/ExampleApp/pages/FakeUsers.vue'),
        name: "AnotherPage"
    },
    {
        path: '/:catchAll(.*)*',
        meta: { layout: ViperLayout },
        component: () => import('@/pages/Error404.vue')
    }
]

export default routes