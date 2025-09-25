import { Loading, QSpinnerOval, Notify, Dialog, Quasar, setCssVar } from 'quasar'
import { semanticColors } from '@/config/colors'
import type { App } from 'vue'

export function useQuasarConfig() {
    const quasarConfig = {
        plugins: {
            Loading,
            Notify,
            Dialog
        },
        brand: {
            // Use single source of truth from colors.ts
            primary: semanticColors.primary,
            secondary: semanticColors.secondary,
            accent: semanticColors.accent,

            dark: semanticColors.dark,
            'dark-page': semanticColors['dark-page'],

            positive: semanticColors.positive,
            negative: semanticColors.negative,
            info: semanticColors.info,
            warning: semanticColors.warning
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
        },
        animations: 'all',
        extras: ['material-icons', 'material-symbols-outlined'],
        framework: {
            iconSet: 'material-symbols-outlined'
        }
    }

    return { quasarConfig }
}

/**
 * Initialize Quasar with custom configuration and apply brand colors.
 * This function handles both the Quasar plugin initialization and the CSS variable overrides
 * needed because Quasar's default CSS is loaded before our configuration.
 */
export function initializeQuasar(app: App) {
    const { quasarConfig } = useQuasarConfig()
    
    // Initialize Quasar with our configuration
    app.use(Quasar, quasarConfig)
    
    // Manually set CSS variables for brand colors
    // This is needed because Quasar's CSS is loaded before our configuration
    if (quasarConfig.brand) {
        Object.entries(quasarConfig.brand).forEach(([key, value]) => {
            setCssVar(key, value as string)
        })
    }
}