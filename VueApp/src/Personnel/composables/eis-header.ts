import type { InjectionKey, Ref } from "vue"
import type { EisPersonHeader } from "../types/eis-types"

/**
 * The person header EisPerson.vue loads once for every tab. The Summary tab reads its details
 * from here instead of fetching them again.
 */
export const eisHeaderKey: InjectionKey<Readonly<Ref<EisPersonHeader | null>>> = Symbol("eisHeader")
