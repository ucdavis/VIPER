import { ref, type Ref } from 'vue'
import { useErrorStore } from '@/store/ErrorStore'

const errors: Ref<string[]> = ref([])
export function useGenericErrorHandler() {
    function handleError(error: string) {
        const errorStore = useErrorStore()
        errors.value = []
        errors.value.push(error)
        errorStore.setError(errors.value.join(','))
    }

    function handleAuthError(errorMessage: string | null, status: number) {
        const errorStore = useErrorStore()
        if (errorMessage != null) {
            errorStore.setError(errorMessage)
        }
        else {
            errorStore.setError(status == 401 ? "Error: not logged in." : "Error: Access Denied.")
        }
        
        errorStore.setStatus(status)
    }

    return { errors, handleError, handleAuthError }
}