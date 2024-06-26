import { createRouter, createWebHistory } from 'vue-router'
import routes from './routes'
import { useUserStore } from '@/store/UserStore'

const baseUrl = import.meta.env.VITE_VIPER_HOME
const router = createRouter({
    scrollBehavior: () => ({ left: 0, top: 0 }),
    history: createWebHistory(baseUrl),
    routes,
})

router.beforeEach(async (to) => {
    const userStore = useUserStore()
    if (!(to.matched.some(record => record.meta.allowUnAuth)) && !userStore.isLoggedIn) {
        return {
            name: "CtsHome",
            query: {
                sendBackTo: to.fullPath
            }
        }
    }
   
    return true
})

export default router