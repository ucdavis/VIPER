const STATUS_NOTIFICATION_DISPLAY_MS = 3000
const STATUS_NOTIFICATION_FADE_MS = 350

/**
 * Transient success toast per DESIGN.md's "Banner or toast?" rule: a persistent page state uses
 * StatusBanner, a one-off confirmation like a save uses this. Mirrors the Razor pages' plain-DOM
 * `showStatusNotification()` (web/wwwroot/js/qtable.js) so the toast looks the same everywhere;
 * styled by `.viper-status-notification` in src/styles/base.css, kept in sync with
 * web/wwwroot/css/site.css.
 */
function showStatusNotification(message: string): void {
    const el = document.createElement("div")
    el.setAttribute("role", "status")
    el.setAttribute("aria-live", "polite")
    el.className = "viper-status-notification"
    el.textContent = message
    document.body.append(el)
    // Trigger reflow so the CSS transition activates
    void el.offsetHeight
    el.classList.add("viper-status-notification--visible")
    setTimeout(() => {
        el.classList.remove("viper-status-notification--visible")
        setTimeout(() => el.remove(), STATUS_NOTIFICATION_FADE_MS)
    }, STATUS_NOTIFICATION_DISPLAY_MS)
}

export { showStatusNotification }
