import type { InjectionKey } from "vue"

/** Shared injection key for tracking phone validation errors across PhoneInput instances. */
const phoneErrorsKey: InjectionKey<Set<symbol>> = Symbol("phoneErrors")

export { phoneErrorsKey }
