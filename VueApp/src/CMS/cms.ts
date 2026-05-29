import { bootstrapSpa } from "@/shared/bootstrap-spa"
import { cmsRouter as router } from "./router"
import App from "./App.vue"

bootstrapSpa({
    areaPath: "/CMS",
    appComponent: App,
    router,
    provides: { apiURL: import.meta.env.VITE_API_URL },
})
