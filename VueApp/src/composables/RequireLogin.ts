import { useQuasar } from "quasar"
import { computed, inject } from "vue"
import { useFetch } from "@/composables/ViperFetch"
import { useUserStore } from "@/store/UserStore"
import { useRoute } from "vue-router"
import { applicationBase, stripApplicationBase } from "@/shared/application-base"
import type { ComputedRef } from "vue"
import type { RouteLocationNormalized } from "vue-router"

// Module-level constants to avoid recreation on each function call
const ABSOLUTE_URL_REGEX = /^(?:https?:)?\/\//u
const ENCODED_SLASH_REGEX = /%2f/iu
const ALLOWED_INTERNAL_PREFIXES = ["/", "/2/", "/vue/"]

// Browsers resolve dot-segments before issuing the request, and the URL spec counts the
// percent-encoded spellings too ("%2e" is ".", ".%2e"/"%2e."/"%2e%2e" are "..").
const DOT_SEGMENTS = new Set([".", "..", "%2e", "%2e%2e", ".%2e", "%2e."])

/**
 * Builds a login URL with a validated return path.
 * Falls back to home if the path fails validation.
 */
function buildLoginUrl(returnPath: string): string {
    const base = applicationBase()
    const welcomePath = `${base}/welcome`
    const fallbackPath = `${base}/`

    if (isValidInternalPath(returnPath)) {
        return `${welcomePath}?ReturnUrl=${encodeURIComponent(returnPath)}`
    }
    return `${welcomePath}?ReturnUrl=${encodeURIComponent(fallbackPath)}`
}

/**
 * Returns a reactive login URL that updates when the route changes.
 * Uses buildLoginUrl for consistent URL generation with validation.
 */
function getLoginUrl(): ComputedRef<string> {
    const route = useRoute()
    const base = applicationBase()
    // Reading route.fullPath makes this recompute after navigation; fullPath omits the app base the
    // router was created with, so prefix it back.
    return computed(() => buildLoginUrl(`${base}${route.fullPath}`))
}

// Helper function to validate internal redirect paths (prevent open redirect attacks)
function isValidInternalPath(path: string): boolean {
    if (!path || typeof path !== "string") {
        return false
    }

    // Some browsers treat a backslash ("/\" or "/\\") as a protocol-relative, i.e. external, redirect.
    if (ABSOLUTE_URL_REGEX.test(path) || path.includes("\\")) {
        return false
    }

    // Only the path resolves, so screen it alone: percent-encoding inside a query value is ordinary
    // ("?sendBackTo=%2Fcts%2Fepa") and must not disqualify an otherwise-valid ReturnUrl.
    const urlPath = path.split(/[?#]/u)[0] ?? ""
    if (ENCODED_SLASH_REGEX.test(urlPath)) {
        return false
    }

    // Whole-segment match, so "/Effort/.." is caught while a legitimate "/Effort/%2ename" is not.
    if (urlPath.split("/").some((segment) => DOT_SEGMENTS.has(segment.toLowerCase()))) {
        return false
    }

    // SECURITY NOTE: This approach mitigates open redirect attacks by restricting
    // Redirects to known internal paths.
    // Ensure all valid internal routes used by your app are included in ALLOWED_INTERNAL_PREFIXES.
    // Update this array if new internal route prefixes are added to the application.
    return ALLOWED_INTERNAL_PREFIXES.some((prefix) => path.startsWith(prefix))
}

function useRequireLogin(to: RouteLocationNormalized) {
    // Get Quasar instance at the composable level (in proper Vue context)
    const $q = useQuasar()

    async function requireLogin(
        loadPermissions: boolean | null = null,
        permissionPrefix: string | null = null,
    ): Promise<boolean> {
        const baseUrl = inject<string>("apiURL")!
        const userStore = useUserStore()
        const allowUnAuth = to.matched.some((record) => record.meta.allowUnAuth)

        //Get logged in user info
        const { get } = useFetch()
        const r = await get(`${baseUrl}loggedInUser`)

        //If unauth'd access allowed and no logged in user, return true
        if (allowUnAuth && (!r.success || !r.result.userId)) {
            return true
        }

        //Show spinner after 250ms
        if ($q !== null) {
            $q.loading.show({
                message: "Logging in",
                delay: 250, // Ms
            })
        }

        //If no logged in user, redirect to cas
        if (!r.success || !r.result.userId) {
            // Hide loading spinner before redirect to prevent flash
            if ($q !== null) {
                $q.loading.hide()
            }

            // Build return path with application base prefix for test/prod
            globalThis.location.href = buildLoginUrl(`${applicationBase()}${to.fullPath}`)
            return false
        }
        //Store the logged in user info
        userStore.loadUser(r.result)
        if (loadPermissions) {
            const permissionQueryParam = permissionPrefix === null ? "" : `?prefix=${permissionPrefix}`
            const permissionResult = await get(`${baseUrl}loggedInUser/permissions${permissionQueryParam}`)
            if (permissionResult.success) {
                userStore.setPermissions(permissionResult.result)
            }
        }

        if ($q !== null) {
            $q.loading.hide()
        }

        return true
    }

    return { requireLogin }
}

// Everything before the separator, and everything after it.
function splitFirst(value: string, separator: string): [string, string] {
    const at = value.indexOf(separator)
    return at === -1 ? [value, ""] : [value.slice(0, at), value.slice(at + 1)]
}

type SendBackToLocation = { path: string; query: Record<string, string>; hash: string }

/**
 * Resolves the sendBackTo deep link (set by VIPER 1) into the location the user asked for, or null
 * when there isn't a usable one. Returning the location rather than navigating lets a router guard
 * hand it back as a redirect instead of racing the navigation it is already in.
 */
function resolveSendBackTo(route: ReturnType<typeof useRoute>): SendBackToLocation | null {
    // A repeat arrives as an array, and toString() would comma-join the values into one path that
    // still looks internal.
    const target = route.query.sendBackTo
    const redirect = (Array.isArray(target) ? target[0] : target)?.toString()
    if (!redirect) {
        return null
    }

    // The fragment trails the query, so it has to come off before the query is parsed.
    const [locator, fragment] = splitFirst(redirect, "#")
    const [rawPath, search] = splitFirst(locator, "?")
    if (!rawPath || !isValidInternalPath(rawPath)) {
        return null
    }

    // Parameter names are attacker-controlled; fromEntries defines own properties, so a "__proto__"
    // key stays plain data.
    const query = Object.fromEntries(new URLSearchParams(search))
    return { path: stripApplicationBase(rawPath), query, hash: fragment ? `#${fragment}` : "" }
}

export { buildLoginUrl, getLoginUrl, isValidInternalPath, resolveSendBackTo, useRequireLogin }
