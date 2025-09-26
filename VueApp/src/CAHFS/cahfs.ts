import { createApp } from "vue"
import { createPinia } from "pinia"
import router from "./router"
import App from "./App.vue"
import { initializeQuasar } from "@/composables/QuasarConfig"
// Import icon libraries
import "@quasar/extras/material-icons/material-icons.css"
import "@quasar/extras/material-symbols-outlined/material-symbols-outlined.css"

// Import Quasar css
import "quasar/dist/quasar.css"

//import our css
import "@/assets/site.css"

const pinia = createPinia()
const app = createApp(App)
app.provide("apiURL", import.meta.env.VITE_API_URL)

app.use(pinia)
app.use(router)

// Initialize Quasar with our brand colors
initializeQuasar(app)

app.mount("#myApp")
