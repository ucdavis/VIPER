<template>
    <q-btn-dropdown
        v-if="isLoggedIn"
        unelevated
    >
        <template #label>
            <q-avatar size="40px">
                <img
                    :src="picSrc"
                    height="40"
                    id="siteProfileAvatar"
                    :alt="userName"
                />
                <q-tooltip>
                    {{ userName }}
                </q-tooltip>
            </q-avatar>
        </template>
        <q-list dense>
            <q-item
                v-if="isEmulating"
                class="bg-warning"
                :href="clearEmulationHref"
            >
                <q-item-section avatar>
                    <q-avatar
                        icon="visibility_off"
                        color="secondary"
                        text-color="white"
                    ></q-avatar>
                </q-item-section>
                <q-item-section> End Emulation </q-item-section>
            </q-item>
            <q-item href="">
                <q-item-section avatar>
                    <q-avatar
                        icon="settings"
                        color="secondary"
                        text-color="white"
                    ></q-avatar>
                </q-item-section>
                <q-item-section> My Profile </q-item-section>
            </q-item>
            <q-item :href="logoutHref">
                <q-item-section avatar>
                    <q-avatar
                        icon="logout"
                        color="secondary"
                        text-color="white"
                    ></q-avatar>
                </q-item-section>
                <q-item-section> Logout </q-item-section>
            </q-item>
        </q-list>
    </q-btn-dropdown>
    <q-btn
        v-else
        round
        flat
        icon="person"
        :href="loginHref"
    >
        <q-tooltip>Login</q-tooltip>
    </q-btn>
</template>

<script lang="ts">
import { defineComponent, computed } from "vue"
import { useUserStore } from "@/store/UserStore"
import { getLoginUrl } from "@/composables/RequireLogin"
export default defineComponent({
    name: "ProfilePic",
    setup() {
        const userStore = useUserStore()
        const clearEmulationHref = import.meta.env.VITE_VIPER_HOME + "ClearEmulation"
        const logoutHref = import.meta.env.VITE_VIPER_HOME + "logout"
        const loginHref = getLoginUrl()
        const picSrc = computed(
            () =>
                "https://viper.vetmed.ucdavis.edu/public/utilities/getbase64image.cfm?mailid=" +
                userStore.userInfo.mailId +
                "&altphoto=1",
        )
        const userName = computed(() => userStore.userInfo.firstName + " " + userStore.userInfo.lastName)
        const isLoggedIn = computed(() => userStore.isLoggedIn)
        const isEmulating = computed(() => userStore.isEmulating)

        return {
            userStore,
            picSrc,
            userName,
            isLoggedIn,
            isEmulating,
            clearEmulationHref,
            logoutHref,
            loginHref,
        }
    },
})
</script>
