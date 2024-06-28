import { defineStore } from "pinia"
import type { Ref } from 'vue'
import { ref } from 'vue'

export const useErrorStore = defineStore('error', {
    state: () => ({
        errorMessage: ref(null) as Ref<string | null>
    }),
    actions: {
        setError(message: string) {
            this.errorMessage = message
        },
        clearError() {
            this.errorMessage = null
        }
    }
})