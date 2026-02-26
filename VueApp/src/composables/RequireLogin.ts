import { useQuasar } from "quasar"
import { computed, inject } from "vue"
import { useFetch } from "@/composables/ViperFetch"
import { useUserStore } from "@/store/UserStore"
import { useRouter, useRoute } from "vue-router"
import type { ComputedRef } from "vue"
import type { RouteLocationNormalized } from "vue-router"

// Module-level regex constants to avoid recreation on each function call
const ABSOLUTE_URL_REGEX = /^(https?:)?\/\//
const ENCODED_SLASH_REGEX = /%2f/i
const ENCODED_DOT_REGEX = /%2e/i
const ALLOWED_INTERNAL_PREFIXES = ["/", "/2/", "/vue/"]

/**
 * Builds a login URL with a validated return path.
 * Falls back to home if the path fails validation.
 */
function buildLoginUrl(returnPath: string): string {
    const viperHome = import.meta.env.VITE_VIPER_HOME ?? "/"
    const applicationBase = viperHome.length === 1 ? "" : viperHome.slice(0, -1)
    const fallbackPath = `${applicationBase}/`

    if (isValidInternalPath(returnPath)) {
        return `${viperHome}login?ReturnUrl=${encodeURIComponent(returnPath)}`
    }
    return `${viperHome}login?ReturnUrl=${encodeURIComponent(fallbackPath)}`
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
    return computed(() => {
        // Use route.fullPath length check to create reactive dependency while using the value
        if (route.fullPath.length >= 0) {
            return buildLoginUrl(getCurrentPath())
        }
        return buildLoginUrl(getCurrentPath())
    })
}

// Helper function to validate internal redirect paths (prevent open redirect attacks)
function isValidInternalPath(path: string): boolean {
    if (!path || typeof path !== "string") {
        return false
    }

    // Reject absolute URLs, path traversal, and encoded bypasses
    if (
        ABSOLUTE_URL_REGEX.test(path) ||
        path.includes("../") ||
        ENCODED_SLASH_REGEX.test(path) ||
        ENCODED_DOT_REGEX.test(path)
    ) {
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
        const baseUrl = inject("apiURL") as string
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
            const viperHome = import.meta.env.VITE_VIPER_HOME ?? "/"
            const applicationBase = viperHome.length === 1 ? "" : viperHome.slice(0, -1)
            const fullReturnPath = `${applicationBase}${to.fullPath}`
            globalThis.location.href = buildLoginUrl(fullReturnPath)
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
    router.push({ path: redirectPath, query: params ?? null })
}

export { getLoginUrl, useRequireLogin }
