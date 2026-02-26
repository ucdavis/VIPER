import type { Ref } from "vue"
import { ref } from "vue"
import { useErrorStore } from "@/store/ErrorStore"

const HTTP_STATUS_UNAUTHORIZED = 401

const errors: Ref<string[]> = ref([])
export function useGenericErrorHandler() {
    function handleError(error: string) {
        const errorStore = useErrorStore()
        errors.value = []
        errors.value.push(error)
        errorStore.setError(errors.value.join(","))
    }

    function handleAuthError(errorMessage: string | null | undefined, status: number) {
        const errorStore = useErrorStore()
        if (errorMessage) {
            errorStore.setError(errorMessage)
        } else {
            errorStore.setError(status === HTTP_STATUS_UNAUTHORIZED ? "You are not logged in." : "Access denied.")
        }
        errorStore.setStatus(status)
    }

    return { errors, handleError, handleAuthError }
}
