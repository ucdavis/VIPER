import { ref, type Ref } from 'vue'
const errors: Ref<string[]> = ref([])
export function useGenericErrorHandler() {
    function handleError(error: string) {
        errors.value.push(error)
    }
    return { errors, handleError }
}