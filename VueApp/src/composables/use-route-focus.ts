import type { Router } from "vue-router"
import { nextTick } from "vue"

/**
 * Manages focus after route navigation for screen reader accessibility (WCAG 2.4.3).
 * Without this, screen readers hear nothing on page change because scroll reset
 * alone does not move focus.
 */
export function useRouteFocus(router: Router) {
    router.afterEach((to, from) => {
        // In-page anchor navigation (skip links): the browser already moved
        // focus to the fragment target; don't steal it back to <main>.
        if (to.path === from.path && to.hash) {
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
                main.focus({ preventScroll: true })
            }
        })
    })
}
