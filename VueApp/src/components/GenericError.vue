<script setup>
    import { computed } from vue
    import { useGenericErrorHandler } from '@/composables/ErrorHandler'
    const { errors } = useGenericErrorHandler()
    //const showViperError = computed(() => errors.value.length > 0)
</script>

<template>
    <q-dialog v-model="showViperError" v-cloak auto-close>
        <q-banner>
            <template v-slot:avatar>
                <q-icon name="error" color="red" />
            </template>
            {{viperErrorMessage}}
        </q-banner>
    </q-dialog>
</template>

<script>
    export default {
        data() {
            return {

            }
        },
        computed: {
            showViperError() {
                return errors.value.length > 0
            },
            viperErrorMessage() {
                return errors.value.join(",")
            }
        }
    }
</script>