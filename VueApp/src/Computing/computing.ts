//load createApp function from vue
import { createApp } from 'vue'
//load pinia storage plugin
import { createPinia } from 'pinia';
//load the app router and application
import router from './router'
import App from './App.vue'
//import quasar, icon libraries, css, and our default quasar config
import { Quasar } from 'quasar'
import '@quasar/extras/material-icons/material-icons.css'
import 'quasar/dist/quasar.css'
import { useQuasarConfig } from '@/composables/QuasarConfig'
//import our css
import '@/assets/site.css'

const { quasarConfig } = useQuasarConfig()
const pinia = createPinia()
const app = createApp(App)

//make the API url available to any page or component in this app
app.provide("apiURL", import.meta.env.VITE_API_URL)

//load plugins
app.use(pinia)
app.use(router)
app.use(Quasar, quasarConfig)
app.mount('#myApp')