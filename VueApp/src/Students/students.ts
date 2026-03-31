import { bootstrapSpa } from "@/shared/bootstrap-spa"
import { router } from "./router"
import App from "./App.vue"

bootstrapSpa({
    areaPath: "/Students",
    appComponent: App,
    router,
    provides: {
        apiURL: import.meta.env.VITE_API_URL,
        viperOneUrl: import.meta.env.VITE_VIPER_1_HOME,
    },
})
