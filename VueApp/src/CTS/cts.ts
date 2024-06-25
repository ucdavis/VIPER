//import './assets/main.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia';
import router from './router'
import App from './App.vue'
import { Quasar, Loading, QSpinnerOval } from 'quasar'
// Import icon libraries
import '@quasar/extras/material-icons/material-icons.css'

// Import Quasar css
import 'quasar/dist/quasar.css'

import { useErrorStore } from '@/store/ErrorStore'

const pinia = createPinia()
const app = createApp(App)

app.config.errorHandler = (err: any, instance: any, info: any) => {
    const errorStore = useErrorStore();
    errorStore.setError(err.message);
}
app.provide("apiURL", import.meta.env.VITE_API_URL)

app.use(pinia)
app.use(router)
app.use(Quasar, {
    plugins: {
        Loading
    },
    config: {
        brand: {
            primary: '#022851',
            secondary: '#295687',
            accent: '#ffc519',

            dark: '#1d1d1d',
            'dark-page': '#121212',

            positive: '#226e34',
            negative: '#6e2222',
            info: '#289094',
            warning: '#ffc519'
        },
        loading: {
            delay: 100,
            message: "Loading",
            group: "group",
            spinner: QSpinnerOval,
            spinnerColor: "light",
            backgroundColor: "dark",
            messageColor: "light",
            boxClass: "bg-grey-2 text-grey-9"
        }
    }
})
app.mount('#myApp')