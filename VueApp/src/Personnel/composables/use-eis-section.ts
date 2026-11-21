import { ref, watch } from "vue"
import type { Ref } from "vue"
import { useRoute } from "vue-router"

/**
 * Loads one EIS page's data for the employee in the route, reloading when the employee changes.
 * Data stays null when the load fails. A response for an employee the user has since left is
 * ignored, so a slow request can't replace the newer page's data.
 */
export function useEisSection<T>(load: (employeeId: string) => Promise<T | null>) {
    const route = useRoute()
    const data = ref(null) as Ref<T | null>
    const loading = ref(true)
    let loadId = 0

    watch(
        () => String(route.params.employeeId ?? ""),
        async (employeeId) => {
            loadId += 1
            const current = loadId
            loading.value = true
            data.value = null
            const result = await load(employeeId)
            if (current !== loadId) {
                return
            }
            data.value = result
            loading.value = false
        },
        { immediate: true },
    )

    /**
     * Replaces the data with a value already fetched elsewhere (a save's response, say), and
     * invalidates any load still in flight so a slower, stale response can't overwrite it - which
     * it otherwise could if the user navigated away and back to this employee while the save ran.
     */
    function setData(value: T) {
        loadId += 1
        data.value = value
    }

    return { data, loading, setData }
}
