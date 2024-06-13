<template>
    <q-btn-dropdown unelevated>
        <template v-slot:label>
            <q-avatar size="40px">
                <img :src="picSrc" height="40" id="siteProfileAvatar" />
                <q-tooltip>
                    {{ userName }}
                </q-tooltip>
            </q-avatar>
        </template>
        <q-list dense>
            <q-item v-if="isEmulating" class="bg-warning" :href="clearEmulationHref">
                <q-item-section avatar>
                    <q-avatar icon="visibility_off" color="secondary" text-color="white"></q-avatar>
                </q-item-section>
                <q-item-section>
                    End Emulation
                </q-item-section>
            </q-item>
            <q-item href="">
                <q-item-section avatar>
                    <q-avatar icon="settings" color="secondary" text-color="white"></q-avatar>
                </q-item-section>
                <q-item-section>
                    My Profile
                </q-item-section>
            </q-item>
            <q-item :href="logoutHref">
                <q-item-section avatar>
                    <q-avatar icon="logout" color="secondary" text-color="white"></q-avatar>
                </q-item-section>
                <q-item-section>
                    Logout
                </q-item-section>
            </q-item>
        </q-list>
    </q-btn-dropdown>
</template>

<script lang="ts">
    import { ref, defineComponent, computed } from 'vue'
    import { useUserStore } from '@/store/UserStore'
    export default defineComponent({
        name: 'ProfilePic',
        data() {
            return {
                clearEmulationHref: ref(import.meta.env.VITE_API_URL + "ClearEmulation"),
                logoutHref: ref(import.meta.env.VITE_API_URL + "logout")
            }
        },
        setup() {
            const userStore = useUserStore()
            const picSrc = computed(() => "https://viper.vetmed.ucdavis.edu/public/utilities/getbase64image.cfm?mailid=" + userStore.userInfo.mailId + "&altphoto=1")
            const userName = computed(() => userStore.userInfo.firstName + " " + userStore.userInfo.lastName)
            const isLoggedIn = computed(() => userStore.isLoggedIn)
            const isEmulating = computed(() => userStore.isEmulating)
            
            return {
                userStore, picSrc, userName, isLoggedIn, isEmulating
            }
        },
    })
</script>