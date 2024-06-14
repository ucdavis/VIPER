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
    return { errors, handleError }
}