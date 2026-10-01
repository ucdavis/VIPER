import { getCurrentScope, ref, watch } from "vue"
import { isNavigationFailure, NavigationFailureType } from "vue-router"
import type { NavigationFailure, RouteLocationNormalized, RouteLocationNormalizedLoaded } from "vue-router"

/**
 * A one-time message for the page a route guard redirects to, saying why the user did not reach
 * the page they asked for. Held in memory rather than the URL, so it survives any further
 * redirects on the way and cannot come back on a reload or from a bookmark.
 */
const pendingNotice = ref<string | null>(null)

/**
 * Bumped when a notice must reach the page already on screen rather than a page about to set up.
 * See redeliverAccessNotice.
 */
const redelivery = ref(0)

/** Called by a route guard as it redirects, for the page the user lands on to show. */
function setAccessNotice(message: string): void {
    pendingNotice.value = message
}

/**
 * An afterEach hook for the router. A guard that redirects to the page the user is already on
 * ends the navigation as a duplicate, so that page does not set up again and would never take the
 * notice. Only that case is handled here: on any other navigation the old page is still mounted
 * while the guards run, so letting mounted pages take notices would hand them to the wrong page.
 */
function redeliverAccessNotice(
    _to: RouteLocationNormalized,
    _from: RouteLocationNormalizedLoaded,
    failure?: NavigationFailure | void,
): void {
    if (pendingNotice.value !== null && isNavigationFailure(failure, NavigationFailureType.duplicated)) {
        redelivery.value++
    }
}

/**
 * Takes the pending notice, if any, for a page to show; called once in a page's setup. Taking it
 * clears it, so the next page the user visits does not repeat it. While the page is mounted it
 * also takes a notice redelivered to it, and stops when the page unmounts.
 */
function useAccessNotice() {
    const notice = ref(pendingNotice.value)
    pendingNotice.value = null

    // Only inside a page, whose unmount stops the watch. Called anywhere else, nothing would ever
    // stop it, and it would go on taking notices meant for pages.
    if (getCurrentScope()) {
        watch(redelivery, () => {
            if (pendingNotice.value !== null) {
                notice.value = pendingNotice.value
                pendingNotice.value = null
            }
        })
    }

    return notice
}

export { setAccessNotice, redeliverAccessNotice, useAccessNotice }
