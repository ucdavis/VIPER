import { bootstrapSpa } from "@/shared/bootstrap-spa"
import { CAHFSRouter as router } from "./router"
import App from "./App.vue"

bootstrapSpa({
    areaPath: "/CAHFS",
    appComponent: App,
    router,
    provides: { apiURL: import.meta.env.VITE_API_URL },
})
