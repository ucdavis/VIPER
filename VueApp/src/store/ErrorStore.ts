import { defineStore } from "pinia"
import { ref } from "vue"

export const useErrorStore = defineStore("error", () => {
    const errorMessage = ref<string | null>(null)
    const status = ref<number | null>(null)

    function setError(message: string) {
        errorMessage.value = message
    }

    function setStatus(newStatus: number) {
        status.value = newStatus
    }

    function clearError() {
        status.value = null
        errorMessage.value = null
    }

    return { errorMessage, status, setError, setStatus, clearError }
})
