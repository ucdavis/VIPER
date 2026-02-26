//Import './assets/main.css'

import { createApp } from "vue"
import { createPinia } from "pinia"
import { router } from "./router"
import App from "./App.vue"
// Import icon libraries
import "@quasar/extras/material-icons/material-icons.css"

// Import Quasar css
import "quasar/dist/quasar.css"
import { initializeQuasar } from "@/composables/QuasarConfig"

//Import our css
import "@/styles/index.css"

const pinia = createPinia()
const app = createApp(App)
app.provide("apiURL", import.meta.env.VITE_API_URL)
app.provide("viperOneUrl", import.meta.env.VITE_VIPER_1_HOME)
app.use(pinia)
app.use(router)

// Initialize Quasar with our brand colors
initializeQuasar(app)
app.mount("#myApp")
