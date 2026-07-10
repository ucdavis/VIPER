import type { Router } from "vue-router"
import { nextTick } from "vue"

/**
 * Manages focus after route navigation for screen reader accessibility (WCAG 2.4.3).
 * Without this, screen readers hear nothing on page change because scroll reset
 * alone does not move focus.
 */
export function useRouteFocus(router: Router) {
    router.afterEach((to, from) => {
        // Same-path navigations never move focus. In-page anchors (skip links): the
        // browser already focused the fragment target. Query-only updates (URL-synced
        // filters, debounced searches): the user is mid-interaction in a control, and
        // yanking focus to <main> would eat their keystrokes.
        if (to.path === from.path) {
            return
        }
        nextTick(() => {
            const main = document.querySelector("#main-content") || document.querySelector("main")
            if (main instanceof HTMLElement) {
                // Set tabindex temporarily so the element can receive focus
                // without permanently joining the tab order
                if (!main.hasAttribute("tabindex")) {
                    main.setAttribute("tabindex", "-1")
                    main.addEventListener("blur", () => main.removeAttribute("tabindex"), { once: true })
                }
                // Tag this focus as route-driven so CSS can suppress the landmark
                // focus ring: browsers count programmatic focus() as :focus-visible
                // after keyboard input or on a fresh page load, which would draw
                // the ring on every navigation. The ring belongs to skip-link jumps.
                main.dataset.routeFocus = ""
                main.addEventListener("blur", () => delete main.dataset.routeFocus, { once: true })
                main.focus({ preventScroll: true })
            }
        })
    })
}
