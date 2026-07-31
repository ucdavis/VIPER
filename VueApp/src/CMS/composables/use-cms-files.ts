import { computed, toValue } from "vue"
import type { MaybeRefOrGetter } from "vue"
import { useFetch } from "@/composables/ViperFetch"

// Service layer for the CMS file operations the inline uploader needs. Every endpoint is built from
// the app API base (VITE_API_URL, injected as `apiURL`) and every call goes through `useFetch()`, so
// the component never constructs URLs or touches `fetch` itself.
//
// Scoped mode (a content-block id given) targets the block-scoped files API a delegated editor can
// reach: `CMS/content/{id}/files/...`, where the folder + view permissions are derived from the block
// server-side. Global mode (no id) targets the folder-wide `cms/files/...` API. The block-scoped API
// exposes only check-name / upload / delete, so per-file overwrite-in-place and use-existing (GET/PUT
// on a specific file) are global-only; callers gate those on `isScoped`.
export function useCmsFiles(apiRoot: string, contentBlockId: MaybeRefOrGetter<number | null | undefined>) {
    const { get, postForm, putForm, del, createUrlSearchParams } = useFetch()

    const isScoped = computed(() => typeof toValue(contentBlockId) === "number")
    const base = computed(() =>
        isScoped.value ? `${apiRoot}CMS/content/${toValue(contentBlockId)}/files/` : `${apiRoot}cms/files/`,
    )

    return {
        isScoped,
        // Block-scoped check-name derives the folder from the block; the global one takes it as a param.
        checkName(fileName: string, folder: string) {
            const params = isScoped.value
                ? createUrlSearchParams({ fileName })
                : createUrlSearchParams({ folder, fileName })
            return get(`${base.value}check-name?${params}`)
        },
        getFile(fileGuid: string) {
            return get(base.value + fileGuid)
        },
        upload(data: FormData) {
            return postForm(base.value, data)
        },
        overwriteInPlace(existingFileGuid: string, data: FormData) {
            return putForm(base.value + existingFileGuid, data)
        },
        remove(fileGuid: string) {
            return del(base.value + fileGuid)
        },
    }
}
