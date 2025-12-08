<script setup lang="ts">
import { ref } from "vue"
import { useUserStore } from "@/store/UserStore"
import { getLoginUrl } from "@/composables/RequireLogin"

const userStore = useUserStore()
//https://" + HttpHelper.HttpContext?.Request.Host.Value
const onDev = import.meta.env.VITE_ENVIRONMENT === "DEVELOPMENT"
const viperHome = import.meta.env.VITE_VIPER_HOME
const loginHref = getLoginUrl()
const sessionRefreshUrl =
    (onDev ? "http://localhost/" : "/") +
    "public/timeout/seconds_until_timeout_v2.cfm?id=" +
    userStore.userInfo.loginId +
    "&service=" +
    (onDev ? "Viper2-dev" : "Viper2")
const showSessionTimeoutWarning = ref(false)
const sessionExpireTime = ref("")
const sessionExpired = ref(false)
let sessionTimeoutCheckEventId = 0
const sessionReloaded = ref(false)

async function checkSessionTimeout() {
    //try to get the session timeout from an external application
    try {
        fetch(sessionRefreshUrl)
            .then((r) => (r.status === 200 ? r.json() : r))
            .then((r) => {
                let nextCheck = 300
                //show timeout warning if the session will time out in 5 minutes or less
                if (r.secondsUntilTimeout !== undefined && r.secondsUntilTimeout < 300) {
                    showSessionTimeoutWarning.value = true
                    sessionExpired.value = r.secondsUntilTimeout < 15 //consider session timing out in 15 seconds to be timed out already
                    var d = new Date(r.sessionTimeoutDateTime)
                    sessionExpireTime.value =
                        (d.getHours() > 12 ? d.getHours() - 12 : d.getHours()) +
                        ":" +
                        ("0" + d.getMinutes()).slice(-2) +
                        (d.getHours() >= 12 ? " PM" : " AM")
                    nextCheck = sessionExpired.value ? 0 : Math.max(r.secondsUntilTimeout - 15, 5)
                }
                if (nextCheck > 0) {
                    sessionTimeoutCheckEventId = window.setTimeout(checkSessionTimeout, nextCheck * 1000)
                }
            })
    } catch (e) {
        void e
    }
}
async function extendSession() {
    fetch(viperHome + "RefreshSession")
        .then((r) => (r.status === 200 ? r.json() : r))
        .then((r) => {
            try {
                clearTimeout(sessionTimeoutCheckEventId)
            } catch (e) {
                void e
            }

            var d = new Date(r.sessionTimeoutDateTime)
            sessionExpireTime.value =
                (d.getHours() > 12 ? d.getHours() - 12 : d.getHours()) +
                ":" +
                ("0" + d.getMinutes()).slice(-2) +
                (d.getHours() >= 12 ? " PM" : " AM")
            sessionReloaded.value = true
            sessionTimeoutCheckEventId = window.setTimeout(checkSessionTimeout, 5000)

            window.setTimeout(hideSessionTimeoutWarning, 1000)
        })
}
function hideSessionTimeoutWarning() {
    showSessionTimeoutWarning.value = false
    sessionReloaded.value = false
}

sessionTimeoutCheckEventId = window.setTimeout(checkSessionTimeout, 60000)
</script>

<template>
    <q-dialog
        position="top"
        full-width
        v-model="showSessionTimeoutWarning"
        seamless
        v-cloak
    >
        <q-card :class="'q-mx-lg ' + (sessionExpired ? 'bg-red-1' : 'bg-grey-2')">
            <q-card-section class="row items-center no-wrap">
                <q-icon
                    size="md"
                    :name="sessionExpired ? 'error' : 'warning'"
                    :color="sessionExpired ? 'red' : 'orange'"
                ></q-icon>
                <q-space></q-space>
                <div v-if="sessionExpired">Your session has expired. Please log in again.</div>
                <div v-else-if="!sessionExpired && !sessionReloaded">
                    Your session will expire at {{ sessionExpireTime }}. Click Refresh Session to continue working.
                </div>
                <div v-else-if="!sessionExpired">Your session has been extended to {{ sessionExpireTime }}.</div>
                <q-space></q-space>
                <q-btn
                    dense
                    color="secondary"
                    class="q-px-md"
                    label="Log in"
                    v-if="sessionExpired"
                    :href="loginHref"
                ></q-btn>
                <q-btn
                    dense
                    color="secondary"
                    class="q-px-md"
                    label="Refresh Session"
                    v-if="!sessionExpired && !sessionReloaded"
                    @click="extendSession"
                ></q-btn>
            </q-card-section>
        </q-card>
    </q-dialog>
</template>
