import { useQuasar } from 'quasar'
import { inject } from 'vue'
import { useFetch } from '@/composables/ViperFetch'
import { useUserStore } from '@/store/UserStore'
import { useRouter, useRoute } from 'vue-router'
import type { RouteLocationNormalized } from 'vue-router'

export default function useRequireLogin(to: RouteLocationNormalized) {
    async function requireLogin(loadPermissions: boolean | null = null, permissionPrefix: string | null = null) {
        const baseUrl = inject('apiURL')
        const userStore = useUserStore()
        const route = useRoute()
        const router = useRouter()
        const allowUnAuth = to.matched.some(record => record.meta.allowUnAuth)

        //note that this only checks authentication, not authorization. server side functions should validate authorization before returning data.
        if (allowUnAuth || userStore.isLoggedIn) {
            return true;
        }

        //show spinner after 250ms
        const $q = useQuasar()
        $q.loading.show({
            message: "Logging in",
            delay: 250 // ms
        })

        //get logged in user info
        const { get } = useFetch()
        const r = await get(baseUrl + "loggedInUser")
        if (!r.success || !r.result.userId) {
            //if user has not authed, send to VIPER 2.0 login url with this app's home page as the return url
            //application base will be "" on dev and "/2" on prod and test
            //to.fullPath will be e.g. /area/page and on test and prod we need /2/area/page as the return url
            const applicationBase = (import.meta.env.VITE_VIPER_HOME.length == 1
                ? ""
                : import.meta.env.VITE_VIPER_HOME.substring(0, import.meta.env.VITE_VIPER_HOME.length - 1))
            window.location.href = import.meta.env.VITE_VIPER_HOME + "login?ReturnUrl=" + applicationBase + to.fullPath
        }
        else {
            //store the logged in user info
            userStore.loadUser(r.result)
            if (loadPermissions) {
                const permissionQueryParam = permissionPrefix != null ? ("?prefix=" + permissionPrefix) : ""
                const permissionResult = await get(baseUrl + "loggedInUser/permissions" + permissionQueryParam)
                if (permissionResult.success) {
                    userStore.setPermissions(permissionResult.result)
                }
            }

        }
        $q.loading.hide()

        if (userStore.isLoggedIn) {
            if (route.query.sendBackTo != undefined) {
                const redirect = route.query.sendBackTo?.toString()
                const paramString = redirect.split("?")[1]
                const params = {} as any
                if (paramString) {
                    const queryString = new URLSearchParams(paramString)
                    queryString.forEach((val: string, key: string) => {
                        params[key] = val
                    })
                }
                router.push({ path: redirect.split("?")[0], query: params ?? null })
            }
        }
    }

    return { requireLogin }
}