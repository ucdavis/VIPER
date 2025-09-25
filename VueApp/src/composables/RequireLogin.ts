import { useQuasar } from "quasar"
import { inject } from "vue"
import { useFetch } from "@/composables/ViperFetch"
import { useUserStore } from "@/store/UserStore"
import { useRouter, useRoute } from "vue-router"
import type { RouteLocationNormalized } from "vue-router"

// Module-level regex constants to avoid recreation on each function call
const ABSOLUTE_URL_REGEX = /^(https?:)?\/\//
const ENCODED_SLASH_REGEX = /%2f/i
const ENCODED_DOT_REGEX = /%2e/i
const ALLOWED_INTERNAL_PREFIXES = ["/", "/2/", "/vue/"]

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
    // redirects to known internal paths.
    // Ensure all valid internal routes used by your app are included in ALLOWED_INTERNAL_PREFIXES.
    // Update this array if new internal route prefixes are added to the application.
    return ALLOWED_INTERNAL_PREFIXES.some((prefix) => path.startsWith(prefix))
}

export function useRequireLogin(to: RouteLocationNormalized) {
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

        //get logged in user info
        const { get } = useFetch()
        const r = await get(baseUrl + "loggedInUser")

        //if unauth'd access allowed and no logged in user, return true
        if (allowUnAuth && (!r.success || !r.result.userId)) {
            return true
        }

        //show spinner after 250ms
        if ($q != null) {
            $q.loading.show({
                message: "Logging in",
                delay: 250, // ms
            })
        }

        //if no logged in user, redirect to cas
        if (!r.success || !r.result.userId) {
            //if user has not authed, send to VIPER 2.0 login url with this app's home page as the return url
            //application base will be "" on dev and "/2" on prod and test
            //to.fullPath will be e.g. /area/page and on test and prod we need /2/area/page as the return url
            const viperHome = import.meta.env.VITE_VIPER_HOME ?? "/"
            const applicationBase = viperHome.length == 1 ? "" : viperHome.slice(0, -1)

            // Hide loading spinner before redirect to prevent flash
            if ($q != null) {
                $q.loading.hide()
            }

            const fullReturnPath = applicationBase + to.fullPath
            // Validate return URL to prevent open redirect attacks
            if (isValidInternalPath(fullReturnPath)) {
                const returnUrl = encodeURIComponent(fullReturnPath)
                window.location.href = viperHome + "login?ReturnUrl=" + returnUrl
            } else {
                // Fallback to safe home path if validation fails
                const returnUrl = encodeURIComponent(applicationBase + "/")
                window.location.href = viperHome + "login?ReturnUrl=" + returnUrl
            }
            return false
        }
        //store the logged in user info
        userStore.loadUser(r.result)
        if (loadPermissions) {
            const permissionQueryParam = permissionPrefix !== null ? "?prefix=" + permissionPrefix : ""
            const permissionResult = await get(baseUrl + "loggedInUser/permissions" + permissionQueryParam)
            if (permissionResult.success) {
                userStore.setPermissions(permissionResult.result)
            }
        }

        if ($q != null) {
            $q.loading.hide()
        }

        if (userStore.isLoggedIn) {
            if (route.query.sendBackTo != null) {
                const redirect = route.query.sendBackTo?.toString()
                if (redirect) {
                    const redirectPath = redirect.split("?")[0] ?? ""
                    // Only redirect if it's a safe internal path
                    if (redirectPath && isValidInternalPath(redirectPath)) {
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
                }
            }
        }
        return true
    }

    return { requireLogin }
}
