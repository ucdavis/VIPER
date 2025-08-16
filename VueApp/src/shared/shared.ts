import { createApp, ref, computed, watch, nextTick, toRaw } from 'vue'
import { Quasar } from 'quasar'

// Import Quasar CSS
import 'quasar/dist/quasar.css'

// Import Material Icons (used in existing Razor views)
import '@quasar/extras/material-icons/material-icons.css'

// Export global objects for Razor views to use
declare global {
  interface Window {
    Vue: any;
    Quasar: any;
  }
}

// Create Vue global object with the same structure as CDN version
window.Vue = {
  createApp,
  ref,
  computed,
  watch,
  nextTick,
  toRaw
}

// Export Quasar for global use
window.Quasar = Quasar

// Initialize a dummy app to ensure Quasar is properly loaded
const app = createApp({})
app.use(Quasar, {
  // Add any default Quasar configuration here
})

console.log('Shared Vue/Quasar bundle loaded successfully')