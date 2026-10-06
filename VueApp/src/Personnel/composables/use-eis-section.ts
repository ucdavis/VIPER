import { ref, watch } from "vue"
import type { Ref } from "vue"
import { useRoute } from "vue-router"

/**
 * Loads one EIS page's data for the employee in the route, reloading when the employee changes.
 * A response for an employee the user has since left is ignored, so a slow request can't
 * replace the newer page's data.
 */
export function useEisSection<T>(load: (employeeId: string) => Promise<T | null>) {
    const route = useRoute()
    const data = ref(null) as Ref<T | null>
    const loading = ref(true)
    const failed = ref(false)
    let loadId = 0

    watch(
        () => String(route.params.employeeId ?? ""),
        async (employeeId) => {
            loadId += 1
            const current = loadId
            loading.value = true
            failed.value = false
            data.value = null
            const result = await load(employeeId)
            if (current !== loadId) {
                return
            }
            data.value = result
            failed.value = result === null
            loading.value = false
        },
        { immediate: true },
    )

    return { data, loading, failed }
}
