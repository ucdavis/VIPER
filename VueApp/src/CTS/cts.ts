//import './assets/main.css'

import { createApp } from "vue"
import { createPinia } from "pinia"
import { router } from "./router"
import App from "./App.vue"
import { Quasar } from "quasar"
// Import icon libraries
import "@quasar/extras/material-icons/material-icons.css"
import "@quasar/extras/material-symbols-outlined/material-symbols-outlined.css"
import IconSet from "quasar/icon-set/material-symbols-outlined.js"

// Import Quasar css
import "quasar/dist/quasar.css"
import { useQuasarConfig } from "@/composables/QuasarConfig"

//import our css
import "@/assets/site.css"
import "@/cts/assets/cts.css"

const pinia = createPinia()
const app = createApp(App)
Quasar.iconSet.set(IconSet)
app.provide("apiURL", import.meta.env.VITE_API_URL)

app.use(pinia)
app.use(router)
app.use(Quasar, quasarConfig)
app.mount('#myApp')
