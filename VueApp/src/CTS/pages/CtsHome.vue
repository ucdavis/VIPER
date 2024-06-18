<template>
    
</template>
<script setup lang="ts">
    import { useQuasar } from 'quasar'
    import { ref, defineComponent } from 'vue'
    import { useFetch } from '@/composables/ViperFetch'
    import { useUserStore } from '@/store/UserStore'
</script>
<script lang="ts">
    export default defineComponent({
        name: "CtsHome",
        data() {
            return {
                userInfo: ref({})
            }
        }
        ,
        mounted: async function () {
            const userStore = useUserStore()
            const $q = useQuasar()
            $q.loading.show({
                message: "Logging in",
                delay: 250 // ms
            })
    
            const { result, success, get } = useFetch()
            await get(import.meta.env.VITE_API_URL + "loggedInUser")
            if (!success || !result.value.userId) {
                window.location.href = import.meta.env.VITE_VIPER_HOME + "login?ReturnUrl=" + import.meta.env.VITE_VIPER_HOME + "CTS/"
            }
            else {
                userStore.loadUser(result.value)
            }
            $q.loading.hide()

            if (userStore.isLoggedIn) {
                const redirect = this.$route.query.sendBackTo?.toString() || (import.meta.env.VITE_VIPER_HOME + 'CTS/AssessmentList')
                let paramString = redirect.split("?")[1]
                if (paramString) {
                    let queryString = new URLSearchParams(paramString)
                    let params = {} as any
                    queryString.forEach((val: string, key: string) => {
                        params[key] = val
                    })
                    this.$router.push({ path: redirect.split("?")[0], query: params })
                }
            }
        }
    })
</script>