//Import './assets/main.css'

import { createApp } from "vue"
import { createPinia } from "pinia"
import { cmsRouter as router } from "./router"
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

app.use(pinia)
app.use(router)

// Initialize Quasar with our brand colors
initializeQuasar(app)
app.mount("#myApp")
