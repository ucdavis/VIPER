import { ref } from "vue"

/**
 * A one-time message for the page a route guard redirects to, saying why the user did not reach
 * the page they asked for. Held in memory rather than the URL, so it survives any further
 * redirects on the way and cannot come back on a reload or from a bookmark.
 */
const pendingNotice = ref<string | null>(null)

/** Called by a route guard as it redirects, for the page the user lands on to show. */
function setAccessNotice(message: string): void {
    pendingNotice.value = message
}

/**
 * Takes the pending notice, if any, for a page to show; called once in a page's setup. Taking it
 * clears it, so the next page the user visits does not repeat it.
 */
function useAccessNotice() {
    const notice = ref(pendingNotice.value)
    pendingNotice.value = null
    return notice
}

export { setAccessNotice, useAccessNotice }
