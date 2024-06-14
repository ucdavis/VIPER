<template>
    <q-dialog v-model="showError" 
              @hide="hide" v-cloak auto-close
              position="top">
        <q-banner>
            <template v-slot:avatar>
                <q-icon name="error" color="red" />
            </template>
            {{ errorMessage }}
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
            const { errorMessage } = storeToRefs(useErrorStore())
            const showErrorMessage = computed(() => errorMessage != null
                && errorMessage.value != null
                && errorMessage.value.length > 0)
            return { showErrorMessage, errorMessage }
        },
        data() {
            return {
                showError: ref(false)
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
            }
        }
    })
</script>