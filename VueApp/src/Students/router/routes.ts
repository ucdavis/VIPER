import ViperLayout from '@/layouts/ViperLayout.vue'
import ViperLayoutSimple from '@/layouts/ViperLayoutSimple.vue'

const viperURL = import.meta.env.VITE_VIPER_HOME

const routes = [
    {
        path: '/Students/',
        meta: { layout: ViperLayout },
        component: () => import('@/Students/pages/StudentsHome.vue'),
        name: "StudentsHome"
    },
    {
        path: '/Students/Home',
        meta: { layout: ViperLayout },
        component: () => import('@/Students/pages/StudentsHome.vue'),
    },
    {
        path: '/Students/StudentClassYear',
        meta: { layout: ViperLayout },
        component: () => import('@/Students/pages/StudentClassYear.vue'),
    },
    {
        path: '/Students/StudentClassYearImport',
        meta: { layout: ViperLayout },
        component: () => import('@/Students/pages/StudentClassYearImport.vue'),
    },
]

export default routes