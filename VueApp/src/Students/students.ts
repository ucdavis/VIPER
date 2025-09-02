//import './assets/main.css'

import { createApp } from "vue"
import { createPinia } from "pinia"
import { router } from "./router"
import App from "./App.vue"
import { Quasar } from "quasar"
// Import icon libraries
import "@quasar/extras/material-icons/material-icons.css"

// Import Quasar css
import "quasar/dist/quasar.css"
import { useQuasarConfig } from "@/composables/QuasarConfig"

//import our css
import "@/styles/index.css"

const { quasarConfig } = useQuasarConfig()
const pinia = createPinia()
const app = createApp(App)
app.provide("apiURL", import.meta.env.VITE_API_URL)
app.provide("viperOneUrl", import.meta.env.VITE_VIPER_1_HOME)
app.use(pinia)
app.use(router)
app.use(Quasar, quasarConfig)
app.mount("#myApp")
