import { ref } from "vue"

/**
 * Debounced, out-of-order-safe server search for a QSelect's @filter handler. Shared by every
 * PersonSelector variant (CMS, Personnel): the search/race-guard logic is identical across them,
 * only the search function and result type differ per caller.
 */
function usePersonSearch<T>(search: (value: string) => Promise<T[] | null>) {
    const options = ref<T[]>([])
    const loading = ref(false)
    // Guards against out-of-order responses: only the latest search may update options
    let searchSeq = 0

    async function searchPeople(val: string, update: (fn: () => void) => void) {
        if (val.trim().length < 2) {
            // Invalidate any in-flight search too, or its late response would repopulate
            // the options we just cleared.
            searchSeq += 1
            loading.value = false
            update(() => {
                options.value = []
            })
            return
        }
        searchSeq += 1
        const seq = searchSeq
        loading.value = true
        const result = await search(val.trim())
        if (seq !== searchSeq) {
            return
        }
        loading.value = false
        update(() => {
            options.value = result ?? []
        })
    }

    return { options, loading, searchPeople }
}

export { usePersonSearch }
