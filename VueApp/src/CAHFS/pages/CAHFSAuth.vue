<template>
    <ContentBlock content-block-name="cahfs-login"></ContentBlock>
</template>

<script setup lang="ts">
import { useQuasar } from "quasar"
import { inject, onMounted } from "vue"
import { useRouter } from "vue-router"
import { useFetch } from "@/composables/ViperFetch"
import { useUserStore } from "@/store/UserStore"
import ContentBlock from "@/CMS/components/ContentBlock.vue"

const router = useRouter()
const baseUrl = inject<string>("apiURL")
const userStore = useUserStore()
const $q = useQuasar()

onMounted(async () => {
    $q.loading.show({
        message: "Checking for log in",
        delay: 250,
    })

    const { get } = useFetch()
    const r = await get(baseUrl + "loggedInUser")

    if (r.success && r.result.userId) {
        userStore.loadUser(r.result)
    }
    $q.loading.hide()

    if (userStore.isLoggedIn) {
        router.push({ name: "CAHFSHome" })
    }
})
</script>
