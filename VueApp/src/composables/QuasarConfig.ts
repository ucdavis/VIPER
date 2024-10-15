import { Loading, QSpinnerOval } from 'quasar'
export function useQuasarConfig() {
    const quasarConfig = {
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
            },
            animations: 'all',
            extras: ['material-icons', 'material-symbols-outlined'],
            framework: {
                iconSet: 'material-symbols-outlined'
            },
        },
    }

    return { quasarConfig }
}