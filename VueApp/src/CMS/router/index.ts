import { createRouter, createWebHistory } from 'vue-router'
import routes from './routes'
import { useRequireLogin } from '@/composables/RequireLogin'
import checkHasOnePermission from '@/composables/CheckPagePermission'

const baseUrl = import.meta.env.VITE_VIPER_HOME
const router = createRouter({
    scrollBehavior: () => ({ left: 0, top: 0 }),
    history: createWebHistory(baseUrl),
    routes,
})

router.beforeEach(async (to) => {
    const { requireLogin } = useRequireLogin(to)
    const loginResult = await requireLogin(true, "SVMSecure.CMS")
    if (loginResult !== undefined && !loginResult) {
        return false
    }
    if (to.meta.permissions != undefined) {
        const hasPerm = checkHasOnePermission(to.meta.permissions as string[])
        if (!hasPerm) {
            return { name: "CmsAuth" }
        }
    }
})

export default router
