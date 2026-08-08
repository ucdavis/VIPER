<script setup lang="ts">
import { ref } from "vue"
import { useUserStore } from "@/store/UserStore"
import { getLoginUrl } from "@/composables/RequireLogin"
import StatusBanner from "@/components/StatusBanner.vue"

const userStore = useUserStore()
const viperHome = import.meta.env.VITE_VIPER_HOME
const sessionTimeoutUrl = `${import.meta.env.VITE_API_URL}sessionTimeout`
const loginHref = getLoginUrl()
const showSessionTimeoutWarning = ref(false)
const sessionExpireTime = ref("")
const sessionExpired = ref(false)
let sessionTimeoutCheckEventId = 0
const sessionReloaded = ref(false)
const sessionExtendFailed = ref(false)

// Hour 0 is 12 AM, not 0 AM.
function formatExpireTime(sessionTimeoutDateTime: string) {
    const d = new Date(sessionTimeoutDateTime)
    return (d.getHours() % 12 || 12) + ":" + ("0" + d.getMinutes()).slice(-2) + (d.getHours() >= 12 ? " PM" : " AM")
}

// Plain fetch rather than useFetch, deliberately: these are session lifecycle calls. useFetch
// pushes every failure into the global error store, which would raise a banner on each blip of a
// silent five minute poll, and would fire the auth handler at exactly the moment our own dialog
// is offering the user a log in.
async function checkSessionTimeout() {
    if (!userStore.userInfo.loginId) {
        return //don't check the session if the user is not logged in
    }
    // Timeout so a request that hangs rather than fails still reaches the catch below.
    fetch(sessionTimeoutUrl, { signal: AbortSignal.timeout(10000) })
        .then((r) => (r.ok ? r.json() : Promise.reject(new Error("Session check returned " + r.status))))
        .then((r) => {
            let nextCheck = 300
            //show timeout warning if the session will time out in 5 minutes or less
            // "<=" so an exact 300 still warns: otherwise the next poll lands at expiry.
            if (r.secondsUntilTimeout !== undefined && r.secondsUntilTimeout <= 300) {
                showSessionTimeoutWarning.value = true
                sessionExpired.value = r.secondsUntilTimeout < 15 //consider session timing out in 15 seconds to be timed out already
                sessionExpireTime.value = formatExpireTime(r.sessionTimeoutDateTime)
                nextCheck = sessionExpired.value ? 0 : Math.max(r.secondsUntilTimeout - 15, 5)
            } else if (r.secondsUntilTimeout !== undefined) {
                // Extended elsewhere, in another tab or by an API call, so stand the warning down.
                hideSessionTimeoutWarning()
            }
            if (nextCheck > 0) {
                sessionTimeoutCheckEventId = window.setTimeout(checkSessionTimeout, nextCheck * 1000)
            }
        })
        // Silent, but reschedule: one failed poll must not stop the checks for the life of the page.
        // Retry quickly once the warning is up, since expiry is minutes away.
        .catch(() => {
            const retry = showSessionTimeoutWarning.value ? 15000 : 300000
            sessionTimeoutCheckEventId = window.setTimeout(checkSessionTimeout, retry)
        })
}

async function extendSession() {
    sessionExtendFailed.value = false
    fetch(viperHome + "RefreshSession", { signal: AbortSignal.timeout(10000) })
        .then((r) => (r.ok ? r.json() : Promise.reject(new Error("RefreshSession returned " + r.status))))
        .then((r) => {
            clearTimeout(sessionTimeoutCheckEventId)
            sessionExpireTime.value = formatExpireTime(r.sessionTimeoutDateTime)
            sessionReloaded.value = true
            sessionTimeoutCheckEventId = window.setTimeout(checkSessionTimeout, 5000)

            window.setTimeout(hideSessionTimeoutWarning, 1000)
        })
        // Leave the dialog up so the user can retry or log in, and say so: the session was not extended.
        .catch(() => {
            sessionExtendFailed.value = true
        })
}

function hideSessionTimeoutWarning() {
    showSessionTimeoutWarning.value = false
    sessionExpired.value = false
    sessionReloaded.value = false
    sessionExtendFailed.value = false
}

sessionTimeoutCheckEventId = window.setTimeout(checkSessionTimeout, 60000)
</script>

<template>
    <q-dialog
        position="top"
        full-width
        v-model="showSessionTimeoutWarning"
        seamless
        aria-label="Session timeout warning"
    >
        <q-card :class="'q-mx-lg ' + (sessionExpired ? 'error-surface' : 'bg-grey-2')">
            <q-card-section class="row items-center no-wrap">
                <q-icon
                    size="md"
                    :name="sessionExpired ? 'error' : 'warning'"
                    :color="sessionExpired ? 'negative' : 'warning'"
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
                    v-if="sessionExpired || sessionExtendFailed"
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
            <q-card-section
                v-if="sessionExtendFailed && !sessionExpired"
                class="q-pt-none"
            >
                <StatusBanner type="error"> Could not extend your session. Please try again. </StatusBanner>
            </q-card-section>
        </q-card>
    </q-dialog>
</template>
