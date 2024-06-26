
<script setup lang="ts">
    import { ref } from 'vue'
    import { useUserStore } from '@/store/UserStore'
    import ProfilePic from '@/layouts/ProfilePic.vue'

    const userStore = useUserStore()
    const clearEmulationHref = ref(import.meta.env.VITE_VIPER_HOME + "ClearEmulation")
    const environment = ref(import.meta.env.VITE_ENVIRONMENT)
    const viperHome = import.meta.env.VITE_VIPER_HOME
</script>

<template>
    <q-layout view="hHh lpr fff">
        <q-header elevated id="simplifiedLayoutHeader" height-hint="98" class="bg-white text-dark">
            <div v-show="false" id="headerPlaceholder">
                <a href="/"><img src="https://viper.vetmed.ucdavis.edu/images/vetmed_logo.jpg" alt="UC Davis Veterinary Medicine logo" border="0" width="134" height="24"></a>
                <div id="topPlaceholder" class="row items-center">
                    <a class="q-btn q-btn-item non-selectable no-outline q-btn--flat q-btn--rectangle q-btn--actionable q-focusable q-hoverable q-btn--no-uppercase q-btn--dense gt-sm text-white"
                       tabindex="0" :href="viperHome">
                        <span class="q-focus-helper"></span>
                        <span class="q-btn__content text-center col items-center q-anchor--skip justify-center row">
                            <i class="q-icon notranslate material-icons" aria-hidden="true" role="img">home</i>
                            <span class="mainLayoutViper">VIPER 2.0</span>
                            <span v-if="environment == 'DEVELOPMENT'" class="mainLayoutViperMode">Development</span>
                            <span v-if="environment == 'TEST'" class="mainLayoutViperMode">Test</span>
                        </span>
                    </a>
                </div>
            </div>
            <q-toolbar v-cloak>
                <q-btn flat dense label="Viper 2.0" class="lt-md" :href="viperHome"></q-btn>

                <q-btn flat dense no-caps class="gt-sm" :href="viperHome">
                    <a href="/"><img src="https://viper.vetmed.ucdavis.edu/images/vetmed_logo.jpg" alt="UC Davis Veterinary Medicine logo" border="0" width="201" height="36"></a>
                    <span class="mainLayoutViper">VIPER 2.0</span>
                    <span v-if="environment == 'DEVELOPMENT'" class="mainLayoutViperMode">Development</span>
                    <span v-if="environment == 'TEST'" class="mainLayoutViperMode">Test</span>
                </q-btn>

                <q-banner v-if="userStore.isEmulating" dense rounded inline-actions class="bg-warning text-black q-ml-lg">
                    <strong>EMULATING:</strong>
                    {{ userStore.userInfo.firstName }} {{ userStore.userInfo.lastName }}
                    <q-btn no-caps dense :href="clearEmulationHref" color="secondary" class="text-white q-px-sm q-ml-md">End Emulation</q-btn>
                </q-banner>

                <q-space></q-space>
                <ProfilePic></ProfilePic>
            </q-toolbar>
        </q-header>

        <q-page-container id="mainLayoutBody">
            <div class="q-pa-md" v-cloak>
                <router-view></router-view>
            </div>
        </q-page-container>

        <q-footer elevated class="bg-white" v-cloak>
            <div class="q-pa-y-sm q-pa-x-md q-gutter-xs text-caption row justify-between items-center" id="footerNavLinks">
                <div class="q-pl-md">
                    <a href="https://svmithelp.vetmed.ucdavis.edu/" target="_blank" rel="noopener" class="text-primary">
                        <q-icon color="primary" name="help_center" size="xs"></q-icon>
                        SVM-IT ServiceDesk
                    </a>
                    <span class="text-primary q-px-sm">|</span>
                    <a href="http://www.vetmed.ucdavis.edu/" target="_blank" rel="noopener" class="text-primary">
                        <q-icon color="primary" name="navigation" size="xs"></q-icon>
                        SVM Home
                    </a>
                    <span class="text-primary q-px-sm">|</span>
                    <a href="http://www.ucdavis.edu/" target="_blank" rel="noopener" class="text-primary">
                        <q-icon color="primary" name="school" size="xs"></q-icon>
                        UC Davis
                    </a>
                </div>
                <div class="">
                    <div class="text-black">
                        &copy; 2023 School of Veterinary Medicine
                    </div>
                </div>
                <div class="q-pr-md">
                    <div id="ucdavislogo" class="q-mt-sm">
                        <a href="/"><img src="https://viper.vetmed.ucdavis.edu/images/vetmed_logo.jpg" alt="UC Davis Veterinary Medicine logo" border="0" width="134" height="24"></a>
                    </div>
                </div>
            </div>
        </q-footer>

    </q-layout>
</template>