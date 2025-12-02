<template>
</template>


<script setup lang="ts">
    import { useQuasar } from 'quasar'
    import { ref, defineComponent, inject } from 'vue'
    import { useRouter } from 'vue-router'
    import { useFetch } from '@/composables/ViperFetch'
    import { useUserStore } from '@/store/UserStore'

</script>
<script lang="ts">
    export default defineComponent({
        name: "CAHFSAuth",
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
                window.location.href = import.meta.env.VITE_VIPER_HOME + "login?ReturnUrl=" + import.meta.env.VITE_VIPER_HOME + "CAHFS/"
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
                    this.$router.push({ name: "CAHFSHome", query: params ?? null })
                }
                else {
                    //this.loadHome = true
                    this.$router.push({ name: "CAHFSHome" })
                }

            }
        }
    })
</script>