import type { VueWrapper } from "@vue/test-utils"
import type { Result } from "@/composables/ViperFetch"

/**
 * Builds a full ViperFetch Result for mocking a service call.
 *
 * The service layer resolves to the whole Result, not just the interesting fields, so a mock
 * that supplies only `success`/`result`/`errors` fails to type-check against the real return
 * type. This fills in the plumbing fields the tests never assert on.
 */
function apiResult(overrides: Partial<Result> = {}): Result {
    return {
        result: null,
        errors: [],
        success: true,
        pagination: null,
        status: 200,
        ...overrides,
    }
}

/** A failed call carrying server-supplied error messages. */
function apiError(errors: string[]): Result {
    return apiResult({ success: false, errors, status: 400 })
}

const attached: { unmount(): void }[] = []

/**
 * Registers a page mounted with attachTo: document.body, which the jump links need: their menu
 * renders through a portal, and outlives the wrapper unless it is unmounted. Call
 * unmountAttached from an afterEach to clean up.
 */
function trackAttached<W extends { unmount(): void }>(wrapper: W): W {
    attached.push(wrapper)
    return wrapper
}

/** Unmounts every page registered with trackAttached, and clears what they portalled out. */
function unmountAttached(): void {
    for (const wrapper of attached.splice(0)) {
        wrapper.unmount()
    }
    document.body.innerHTML = ""
}

/**
 * Opens a page's section jump links and returns them. The links sit behind a menu trigger, out of
 * the sticky bar's flow, so they have to be opened before they exist anywhere to query. No trigger
 * means too few sections to navigate between. The menu renders through a portal, so the page
 * must be mounted with attachTo: document.body for the links to reach the document.
 */
async function openJumpLinks(wrapper: Pick<VueWrapper, "findComponent">): Promise<HTMLAnchorElement[]> {
    const trigger = wrapper.findComponent({ name: "SectionJumpLinks" }).find("button")
    if (trigger.exists()) {
        await trigger.trigger("click")
    }
    return [...document.querySelectorAll<HTMLAnchorElement>(".q-menu a[href^='#']")]
}

/** The labels of a page's section jump links, in order. */
async function linkTargets(wrapper: Pick<VueWrapper, "findComponent">): Promise<string[]> {
    const links = await openJumpLinks(wrapper)
    return links.map((link) => link.textContent?.trim() ?? "")
}

export { apiResult, apiError, linkTargets, openJumpLinks, trackAttached, unmountAttached }
