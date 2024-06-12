<template>
    <q-btn-dropdown unelevated>
        <template v-slot:label>
            <q-avatar size="40px">
                <img :src="picSrc" height="40" id="siteProfileAvatar" />
                <q-tooltip :target="siteProfileAvatar">
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

<script>
    import { ref, defineComponent } from 'vue'
    /*
    import { useUserStore } from '@/store/UserStore'
    try {
        const userStore = useUserStore()
    }
    catch (e) {
        console.log(e)
    }
    */
    export default defineComponent({
        name: 'ProfilePic',
        data() {
            return {
                picSrc: ref(""),
                userName: ref(""),
                isLoggedIn: ref(false),
                isEmulating: ref(false),
                clearEmulationHref: ref(""),
                logoutHref: ref("")
            }
        },
        methods: {
            
        },
        /*
        mounted: async function () {
            try {
                if (userStore) {
                    this.isLoggedIn = userStore.isLoggedIn
                    if (userStore.isLoggedIn) {
                        this.picSrc = "https://viper.vetmed.ucdavis.edu/public/utilities/getbase64image.cfm?mailid=" + userStore.userInfo.mailId + "&altphoto=1"
                        this.userName = userStore.userInfo.firstName + " " + userStore.userInfo.lastName
                        this.logoutHref = import.meta.env.VITE_API_URL + "logout"
                        this.clearEmulationHref = import.meta.env.VITE_API_URL + "ClearEmulation"
                        this.isEmulating = userStore.isEmulating

                    }
                }
            } catch (e) {
                console.log(e)
            }
        }
        */
    })
</script>