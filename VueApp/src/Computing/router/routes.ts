import ViperLayout from '@/layouts/ViperLayout.vue'
//import ViperLayoutSimple from '@/layouts/ViperLayoutSimple.vue'

const routes = [
    {
        path: '/Computing/',
        alias: '/Computing/Home',
        meta: { layout: ViperLayout, allowUnAuth: false },
        component: () => import('@/Computing/pages/Home.vue'),
        name: "ExampleAppHome"
    },
    {
        path: '/Computing/BiorenderStudents',
        meta: { layout: ViperLayout, allowUnAuth: false },
        component: () => import('@/Computing/pages/BiorenderStudents.vue'),
        name: "AnotherPage"
    },
    {
        path: '/:catchAll(.*)*',
        meta: { layout: ViperLayout },
        component: () => import('@/pages/Error404.vue')
    }
]

export default routes