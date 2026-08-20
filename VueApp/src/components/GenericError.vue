<template>
    <q-dialog
        v-model="showError"
        @hide="hide"
        auto-close
        full-width
        backdrop-filter="brightness(50%)"
        position="top"
        aria-label="Error notification"
    >
        <q-banner class="text-center error-surface">
            <template #avatar>
                <q-icon
                    name="error"
                    color="negative"
                />
            </template>
            {{ errorMessage }}
            <span v-if="showLogin">Your session may have expired. Log in and try your action again.</span>
            <template #action>
                <LoginButton
                    v-if="showLogin"
                    dense
                    class="q-px-md"
                />
                <q-btn
                    v-close-popup
                    flat
                    round
                    dense
                    icon="close"
                    aria-label="Close"
                />
            </template>
        </q-banner>
    </q-dialog>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue"
import { useErrorStore } from "@/store/ErrorStore"
import LoginButton from "@/components/LoginButton.vue"
import { storeToRefs } from "pinia"

const errorStore = useErrorStore()
const { errorMessage, status } = storeToRefs(errorStore)
const showErrorMessage = computed(() => errorMessage.value !== null && errorMessage.value.length > 0)

const showError = ref(false)
const showLogin = ref(false)

function hide() {
    errorStore.clearError()
}

watch(showErrorMessage, () => {
    showError.value = showErrorMessage.value
    showLogin.value =
        status.value !== undefined &&
        status.value !== null &&
        (errorMessage.value === "Error: not logged in." || errorMessage.value === "Error: Access Denied.")
})
</script>
