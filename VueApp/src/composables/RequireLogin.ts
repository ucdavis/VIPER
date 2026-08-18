import { useQuasar } from "quasar"
import { computed, inject } from "vue"
import { useFetch } from "@/composables/ViperFetch"
import { useUserStore } from "@/store/UserStore"
import { useRouter, useRoute } from "vue-router"
import { applicationBase } from "@/shared/application-base"
import type { ComputedRef } from "vue"
import type { RouteLocationNormalized } from "vue-router"

// Module-level constants to avoid recreation on each function call
const ABSOLUTE_URL_REGEX = /^(?:https?:)?\/\//u
const ENCODED_SLASH_REGEX = /%2f/iu
const QUERY_OR_FRAGMENT_REGEX = /[?#]/u
const ALLOWED_INTERNAL_PREFIXES = ["/", "/2/", "/vue/"]

// Browsers resolve dot-segments before issuing the request, and the URL spec counts the
// percent-encoded spellings too ("%2e" is ".", ".%2e"/"%2e."/"%2e%2e" are ".."), all ASCII
// case-insensitive.
const DOT_SEGMENTS = new Set([".", "..", "%2e", "%2e%2e", ".%2e", "%2e."])

/**
 * Builds a login URL with a validated return path.
 * Falls back to home if the path fails validation.
 */
function buildLoginUrl(returnPath: string): string {
    // Build both paths from the normalized base so VITE_VIPER_HOME="/2" gives "/2/login" (not the
    // slash-less "/2login") and "/2///" collapses its duplicate slashes.
    const base = applicationBase()
    const loginPath = `${base}/login`
    const fallbackPath = `${base}/`

    if (isValidInternalPath(returnPath)) {
        return `${loginPath}?ReturnUrl=${encodeURIComponent(returnPath)}`
    }
    return `${loginPath}?ReturnUrl=${encodeURIComponent(fallbackPath)}`
}

/**
 * Returns the current browser path including query string and hash.
 * Safe to use directly since it comes from globalThis.location (not user input).
 */
function getCurrentPath(): string {
    return `${globalThis.location.pathname}${globalThis.location.search}${globalThis.location.hash}`
}

/**
 * Returns a reactive login URL that updates when the route changes.
 * Uses buildLoginUrl for consistent URL generation with validation.
 */
function getLoginUrl(): ComputedRef<string> {
    const route = useRoute()
    const base = applicationBase()
    return computed(() => {
        // Reading route.fullPath makes this reactive (the URL recomputes after navigation), but it
        // omits the app base the router was created with, so prefix it back (same pattern as
        // requireLogin). Fall back to the browser location outside a router context (e.g. unit tests).
        const returnPath = route ? `${base}${route.fullPath}` : getCurrentPath()
        return buildLoginUrl(returnPath)
    })
}

// Everything before the query or fragment. Only the path participates in URL resolution, so only
// the path is screened for traversal and encoded bypasses.
function pathWithoutQuery(url: string): string {
    const cut = url.search(QUERY_OR_FRAGMENT_REGEX)
    return cut >= 0 ? url.slice(0, cut) : url
}

// Helper function to validate internal redirect paths (prevent open redirect attacks)
function isValidInternalPath(path: string): boolean {
    if (!path || typeof path !== "string") {
        return false
    }

    // Rejected across the whole string: an absolute URL can only appear at the front, and a raw
    // backslash has no legitimate place in an internal ReturnUrl (some browsers treat "/\" or "/\\"
    // as protocol-relative, i.e. external, redirects).
    if (ABSOLUTE_URL_REGEX.test(path) || path.includes("\\")) {
        return false
    }

    // Traversal and encoded bypasses are screened on the path alone. Percent-encoding inside a query
    // value is ordinary ("?sendBackTo=%2Fcts%2Fepa", which the CTS landing forwards verbatim) and
    // cannot affect path resolution, so it must not disqualify an otherwise-valid ReturnUrl.
    const urlPath = pathWithoutQuery(path)
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
        const route = useRoute()
        const router = useRouter()
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

        if (userStore.isLoggedIn && route.query.sendBackTo !== null) {
            handleSendBackToRedirect(route, router)
        }
        return true
    }

    return { requireLogin }
}

/**
 * Handles the sendBackTo redirect logic after successful login.
 * Extracted to reduce nesting depth in requireLogin.
 */
function handleSendBackToRedirect(route: ReturnType<typeof useRoute>, router: ReturnType<typeof useRouter>): void {
    const redirect = route.query.sendBackTo?.toString()
    if (!redirect) {
        return
    }

    const redirectPath = redirect.split("?")[0] ?? ""
    if (!redirectPath || !isValidInternalPath(redirectPath)) {
        return
    }

    const paramString = redirect.split("?")[1] ?? ""
    const params: Record<string, string> = {}
    if (paramString) {
        const queryString = new URLSearchParams(paramString)
        for (const [key, val] of queryString.entries()) {
            params[key] = val
        }
    }
    void router.push({ path: redirectPath, query: params ?? null })
}

export { buildLoginUrl, getLoginUrl, isValidInternalPath, useRequireLogin }
