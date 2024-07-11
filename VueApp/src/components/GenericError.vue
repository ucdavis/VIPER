<template>
    <q-dialog v-model="showError" 
              @hide="hide" v-cloak auto-close full-width backdrop-filter="brightness(50%)"
              position="top">
        <q-banner class="text-center">
            <template v-slot:avatar>
                <q-icon name="error" color="red" />
            </template>
            {{ errorMessage }}
            <span v-if="showLogin">
                Your session may have expired. 
                <a :href="loginUrl" target="_blank">
                    Click to log in, and then try your action again.
                    <q-icon name="launch"></q-icon>
                </a>
            </span>
        </q-banner>
    </q-dialog>
</template>

<script lang="ts">
    import { computed, defineComponent, ref } from 'vue'
    import { useErrorStore } from '@/store/ErrorStore'
    import { storeToRefs } from 'pinia'

    export default defineComponent({
        name: "GenericError",
        setup() {
            const { errorMessage, status } = storeToRefs(useErrorStore())
            const showErrorMessage = computed(() => errorMessage != null
                && errorMessage.value != null
                && errorMessage.value.length > 0)
            const loginUrl = import.meta.env.VITE_VIPER_HOME + "login?ReturnUrl=" + window.location.pathname
            return { showErrorMessage, errorMessage, status, loginUrl }
        },
        data() {
            return {
                showError: ref(false),
                showLogin: ref(false)
            }
        },
        methods: {
            hide: function () {
                const errorStore = useErrorStore()
                errorStore.clearError()
            }
        },
        watch: {
            showErrorMessage: function (v) {
                this.showError = this.showErrorMessage
                this.showLogin = this.status !== undefined
            }
        }
    })
</script>