import { createApp } from "vue"
import { createPinia } from "pinia"
import { effortRouter as router } from "./router"
import App from "./App.vue"
import { initializeQuasar } from "@/composables/QuasarConfig"

/* oxlint-disable import/no-unassigned-import -- CSS imports for side effects */
import "@quasar/extras/material-icons/material-icons.css"
import "quasar/dist/quasar.css"
import "@/styles/index.css"
/* oxlint-enable import/no-unassigned-import */

const pinia = createPinia()
const app = createApp(App)
app.provide("apiURL", import.meta.env.VITE_API_URL)
app.provide("viperOneUrl", import.meta.env.VITE_VIPER_1_HOME)
app.use(pinia)
app.use(router)

initializeQuasar(app)

app.mount("#myApp")
