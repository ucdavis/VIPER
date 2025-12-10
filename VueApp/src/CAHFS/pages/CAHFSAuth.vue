<template>
    <p></p>
</template>

<script setup lang="ts">
import { useQuasar } from "quasar"
import { ref, defineComponent, inject } from "vue"
import { useFetch } from "@/composables/ViperFetch"
import { useUserStore } from "@/store/UserStore"
</script>
<script lang="ts">
export default defineComponent({
    name: "CAHFSAuth",
    data() {
        return {
            userInfo: ref({}),
            loadHome: ref(false),
        }
    },
    mounted: async function () {
        const baseUrl = inject("apiURL")
        const userStore = useUserStore()
        const $q = useQuasar()
        $q.loading.show({
            message: "Checking for log in",
            delay: 250, // ms
        })

        const { get } = useFetch()
        const r = await get(baseUrl + "loggedInUser")

        if (r.success && r.result.userId) {
            userStore.loadUser(r.result)
        }
        $q.loading.hide()

        if (userStore.isLoggedIn) {
            this.$router.push({ name: "CAHFSHome" })
        }
    },
})
</script>
