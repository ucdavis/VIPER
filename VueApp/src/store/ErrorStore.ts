import { defineStore } from "pinia"
import type { Ref } from 'vue'
import { ref } from 'vue'

export const useErrorStore = defineStore('error', {
    state: () => ({
        errorMessage: ref(null) as Ref<string | null>,
        status: ref(null) as Ref<number | null>
    }),
    actions: {
        setError(message: string) {
            this.errorMessage = message
        },
        setStatus(status: number) {
            this.status = status
        },
        clearError() {
            this.status = null
            this.errorMessage = null
        }
    }
})