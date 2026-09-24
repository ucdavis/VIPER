import { useFetch } from "@/composables/ViperFetch"
import { useUserStore } from "@/store/UserStore"

// In-flight latch: dedups concurrent navigations but resets after each attempt so later
// sessions (e.g. re-auth into an SIS role) can re-fetch instead of reusing a stale resolution.
const permissionLoads = new Map<string, Promise<void>>()

async function loadPermissions(prefix: string) {
    try {
        const userStore = useUserStore()
        const { get } = useFetch()
        const apiUrl = import.meta.env.VITE_API_URL
        const perms = await get(`${apiUrl}loggedInUser/permissions?prefix=${prefix}`)
        if (perms.success && Array.isArray(perms.result)) {
            userStore.addPermissions(perms.result)
        }
    } finally {
        permissionLoads.delete(prefix)
    }
}

/**
 * Loads the permissions of one area, which requireLogin does not cover, unless the user already
 * holds some. Concurrent navigations share the one request.
 */
async function ensurePermissions(prefix: string): Promise<void> {
    const userStore = useUserStore()
    const existingPermissions = userStore.userInfo?.permissions ?? []
    if (existingPermissions.some((p: string) => p.startsWith(prefix))) {
        return
    }

    let load = permissionLoads.get(prefix)
    if (!load) {
        load = loadPermissions(prefix)
        permissionLoads.set(prefix, load)
    }
    await load
}

export { ensurePermissions }
