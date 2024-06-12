//import { route } from 'quasar/wrappers'
import { createRouter, createWebHistory } from 'vue-router'
import routes from './routes'
import { useUserStore } from '@/store/UserStore'

const router = createRouter({
    scrollBehavior: () => ({ left: 0, top: 0 }),
    history: createWebHistory(),
    routes,

})

router.beforeEach(async (to, from) => {
    const userStore = useUserStore()

    if (!(to.matched.some(record => record.meta.allowUnAuth) && !userStore.isLoggedIn)) {
        return { name: "Home" }
    }
    return true
})

export default router