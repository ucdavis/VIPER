//Load createApp function from vue
import { createApp } from "vue"
//Load pinia storage plugin
import { createPinia } from "pinia"
//Load the app router and application
import { router } from "./router"
import App from "./App.vue"
//Import quasar, icon libraries, css, and our default quasar config
import "@quasar/extras/material-icons/material-icons.css"
import "quasar/dist/quasar.css"
import { initializeQuasar } from "@/composables/QuasarConfig"
//Import our css
import "@/styles/index.css"

const pinia = createPinia()
const app = createApp(App)

//Make the API url available to any page or component in this app
app.provide("apiURL", import.meta.env.VITE_API_URL)

//Load plugins
app.use(pinia)
app.use(router)

// Initialize Quasar with our brand colors
initializeQuasar(app)
app.mount("#myApp")
