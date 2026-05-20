import PrimeVue from 'primevue/config'
import Ripple from 'primevue/ripple'
import Tooltip from 'primevue/tooltip'
import type { Plugin } from 'vue'
import Aura from '@primeuix/themes/aura'
import { definePreset } from '@primeuix/themes'
import { primevuePl } from '@/plugins/i18n/primevuePl'
import { primevueEn } from '@/plugins/i18n/primevueEn'

const MyPreset = definePreset(Aura, {
    semantic: {
        primary: {
            50: '{blue.50}',
            100: '{blue.100}',
            200: '{blue.200}',
            300: '{blue.300}',
            400: '{blue.400}',
            500: '{blue.500}',
            600: '{blue.600}',
            700: '{blue.700}',
            800: '{blue.800}',
            900: '{blue.900}',
            950: '{blue.950}',
        },
    },
})

export const primevue: Plugin = {
    install(app) {
        const currentLanguage = localStorage.getItem('language') || 'pl'
        const localeConfig = currentLanguage === 'pl' ? primevuePl : primevueEn

        app.use(PrimeVue, {
            theme: {
                preset: MyPreset,
                options: {
                    darkModeSelector: '.dark',
                },
            },
            ripple: true,
            locale: localeConfig,
        })

        app.directive('ripple', Ripple)
        app.directive('tooltip', Tooltip)
    },
}