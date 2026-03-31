import { createApp } from "vue"
import type { Component } from "vue"
import { createPinia } from "pinia"
import type { Router } from "vue-router"
import { initializeQuasar } from "@/composables/QuasarConfig"

/* oxlint-disable import/no-unassigned-import -- CSS imports for side effects */
import "@quasar/extras/material-icons/material-icons.css"
import "quasar/dist/quasar.css"
import "@/styles/index.css"
/* oxlint-enable import/no-unassigned-import */

type BootstrapSpaOptions = {
    areaPath: string
    appComponent: Component
    router: Router
    provides?: Record<string, string | undefined>
}

export function bootstrapSpa({ areaPath, appComponent, router, provides }: BootstrapSpaOptions): void {
    const base = (import.meta.env.VITE_VIPER_HOME ?? "/").replace(/\/$/, "")
    if (!globalThis.location.pathname.toLowerCase().startsWith(`${base}${areaPath}`.toLowerCase())) {
        return
    }

    const pinia = createPinia()
    const app = createApp(appComponent)

    if (provides) {
        for (const [key, value] of Object.entries(provides)) {
            app.provide(key, value)
        }
    }

    app.use(pinia)
    app.use(router)
    initializeQuasar(app)
    app.mount("#myApp")
}
