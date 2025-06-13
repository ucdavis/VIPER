<template>
    <h2>Example App Home</h2>
</template>

<script setup lang="ts">
    import { useQuasar } from 'quasar'
    import { ref, defineComponent, inject } from 'vue'
    import { useFetch } from '@/composables/ViperFetch'
    import { useUserStore } from '@/store/UserStore'
</script>
<script lang="ts">
    export default defineComponent({
        name: "ExampleAppHome",
        data() {
            return {
                userInfo: ref({}),
                loadHome: ref(false)
            }
        }
        ,
        mounted: async function () {
            const baseUrl = inject('apiURL')
            const userStore = useUserStore()
            const $q = useQuasar()
            $q.loading.show({
                message: "Logging in",
                delay: 250 // ms
            })

            const { get } = useFetch()
            const r = await get(baseUrl + "loggedInUser")
            if (!r.success || !r.result.userId) {
                console.log(r)
                console.log(import.meta.env.VITE_VIPER_HOME + "login?ReturnUrl=" + import.meta.env.VITE_VIPER_HOME + "ExampleApp/")
                window.location.href = import.meta.env.VITE_VIPER_HOME + "login" //?ReturnUrl=" + import.meta.env.VITE_VIPER_HOME + "ExampleApp/"
            }
            else {
                userStore.loadUser(r.result)
            }
            $q.loading.hide()

            if (userStore.isLoggedIn) {
                if (this.$route.query.sendBackTo != undefined) {
                    const redirect = this.$route.query.sendBackTo?.toString()
                    let paramString = redirect.split("?")[1]
                    let params = {} as any
                    if (paramString) {
                        let queryString = new URLSearchParams(paramString)
                        queryString.forEach((val: string, key: string) => {
                            params[key] = val
                        })
                    }
                    this.$router.push({ path: redirect.split("?")[0], query: params ?? null })
                }
                else {
                    this.loadHome = true
                }

            }
        }
    })
</script>