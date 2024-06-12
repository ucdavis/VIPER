<template>
    Home!
    <q-btn label="Push Me!"></q-btn>
    <q-icon name="edit"></q-icon>
    <div class="bg-primary">
        Div
    </div>
    
</template>
<script lang="ts">
    import { useQuasar } from 'quasar'
    import { ref, defineComponent } from 'vue'
    import { useFetch } from '@/composables/ViperFetch'
    import { useUserStore } from '@/store/UserStore'

    const userStore = useUserStore()
    export default defineComponent({
        name: "CtsHome",
        data() {
            return {
                userInfo: ref({})
            }
        },
        async setup() {
            const $q = useQuasar()
            $q.loading.show({
                delay: 250 // ms
            })

            const { result, errors, vfetch } = useFetch(import.meta.env.VITE_API_URL + "loggedInUser")
            var r = await vfetch()
            if (errors.value.length || !result.value.userId) {

            }
            else {
                userStore.loadUser(result.value)
            }
            $q.loading.hide()
        },
        mounted: function() {
            if(userStore.isLoggedIn) {
                this.$router.push('Assessments')
            }
        }
    })
</script>