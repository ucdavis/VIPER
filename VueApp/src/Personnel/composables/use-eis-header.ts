import { inject, provide, readonly } from "vue"
import type { InjectionKey, Ref } from "vue"
import type { EisPersonHeader } from "../types/eis-types"

/**
 * The person header EisPerson.vue loads once for every tab. The Summary tab reads its details
 * from here instead of fetching them again.
 */
const eisHeaderKey: InjectionKey<Readonly<Ref<EisPersonHeader | null>>> = Symbol("eisHeader")

/** Shares the loaded header, read-only, with the tab pages below. */
function provideEisHeader(header: Ref<EisPersonHeader | null>): void {
    provide(eisHeaderKey, readonly(header))
}

/** The header EisPerson.vue loaded, or null outside an EIS employee page. */
function useEisHeader(): Readonly<Ref<EisPersonHeader | null>> | null {
    return inject(eisHeaderKey, null)
}

export { provideEisHeader, useEisHeader }
