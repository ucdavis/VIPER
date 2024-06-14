<template>
    Competency Tracking System 2.0
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
    
            const { result, errors, vfetch } = useFetch(import.meta.env.VITE_API_URL + "loggedInUser")
            var r = await vfetch()
            if (errors.value.length || !result.value.userId) {
                window.location.href = import.meta.env.VITE_VIPER_HOME + "login?ReturnUrl=" + import.meta.env.VITE_VIPER_HOME + "CTS/"
            }
            else {
                userStore.loadUser(result.value)
            }
            $q.loading.hide()

            if (userStore.isLoggedIn) {
                const redirect = this.$route.query.sendBackTo?.toString() || (import.meta.env.VITE_VIPER_HOME + 'CTS/Assessments')
                this.$router.push({path: redirect})
            }
        }
    })
</script>