import { bootstrapSpa } from "@/shared/bootstrap-spa"
import { router } from "./router"
import App from "./App.vue"

/* oxlint-disable import/no-unassigned-import -- CSS imports for side effects */
import "@/CTS/assets/cts.css"
/* oxlint-enable import/no-unassigned-import */

bootstrapSpa({
    areaPath: "/CTS",
    appComponent: App,
    router,
    provides: { apiURL: import.meta.env.VITE_API_URL },
})
