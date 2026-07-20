<template>
    <div>
        <ContentBlock content-block-name="cahfs-login"></ContentBlock>

        <q-card
            flat
            bordered
            class="bg-ucdavis-blue-10 q-mt-md"
        >
            <q-card-section class="row items-start no-wrap">
                <q-icon
                    name="login"
                    color="primary"
                    size="sm"
                    class="q-mr-md"
                />
                <div>
                    <h2 class="text-subtitle1 text-weight-bold text-primary q-mt-none q-mb-xs">Members sign-in</h2>
                    <div class="text-body2">
                        Sign in with your UC Davis account to open web reports and section pages.
                    </div>
                    <q-btn
                        :href="loginHref"
                        type="a"
                        color="primary"
                        no-caps
                        label="Sign in"
                        class="q-mt-md"
                    />
                </div>
            </q-card-section>
        </q-card>
    </div>
</template>

<script setup lang="ts">
import { useQuasar } from "quasar"
import { inject, onMounted } from "vue"
import { useRouter } from "vue-router"
import { useFetch } from "@/composables/ViperFetch"
import { useUserStore } from "@/store/UserStore"
import { getLoginUrl } from "@/composables/RequireLogin"
import ContentBlock from "@/CMS/components/ContentBlock.vue"

const router = useRouter()
const baseUrl = inject<string>("apiURL")
const userStore = useUserStore()
const $q = useQuasar()
const loginHref = getLoginUrl()

async function checkAuth() {
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
}

onMounted(() => checkAuth())
</script>
