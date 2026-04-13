<script setup lang="ts">
import { ref } from "vue"
import { useUserStore } from "@/store/UserStore"
import ProfilePic from "@/layouts/ProfilePic.vue"
import SessionTimeout from "@/components/SessionTimeout.vue"

export type BreadCrumb = {
    url: string
    name: string
}

const props = defineProps<{
    breadcrumbs?: BreadCrumb[]
}>()

const userStore = useUserStore()
const clearEmulationHref = ref(import.meta.env.VITE_VIPER_HOME + "ClearEmulation")
const environment = ref(import.meta.env.VITE_ENVIRONMENT)
const viperHome = import.meta.env.VITE_VIPER_HOME
const currentYear = new Date().getFullYear()
</script>

<template>
    <a
        href="#main-content"
        class="skip-to-content"
        >Skip to main content</a
    >
    <q-layout view="hHh lpr fff">
        <q-header
            elevated
            id="simplifiedLayoutHeader"
            height-hint="98"
            class="bg-white text-dark"
        >
            <div
                v-show="false"
                id="headerPlaceholder"
            >
                <a href="/"
                    ><img
                        src="https://viper.vetmed.ucdavis.edu/images/vetmed_logo.jpg"
                        alt="UC Davis Veterinary Medicine logo"
                        border="0"
                        width="134"
                        height="24"
                /></a>
                <div
                    id="topPlaceholder"
                    class="row items-center"
                >
                    <a
                        class="q-btn q-btn-item non-selectable no-outline q-btn--flat q-btn--rectangle q-btn--actionable q-focusable q-hoverable q-btn--no-uppercase q-btn--dense gt-sm text-white"
                        tabindex="0"
                        :href="viperHome"
                    >
                        <span class="q-focus-helper"></span>
                        <span class="q-btn__content text-center col items-center q-anchor--skip justify-center row">
                            <i
                                class="q-icon notranslate material-icons"
                                aria-hidden="true"
                                role="img"
                                >home</i
                            >
                            <span class="mainLayoutViper">VIPER 2.0</span>
                            <q-badge
                                v-if="environment == 'DEVELOPMENT'"
                                color="negative"
                                role="presentation"
                                class="mainLayoutViperMode"
                                >Development</q-badge
                            >
                            <q-badge
                                v-if="environment == 'TEST'"
                                color="negative"
                                role="presentation"
                                class="mainLayoutViperMode"
                                >Test</q-badge
                            >
                        </span>
                    </a>
                </div>
            </div>
            <q-toolbar>
                <q-btn
                    flat
                    dense
                    label="Viper 2.0"
                    class="lt-md"
                    :href="viperHome"
                ></q-btn>

                <a href="/"
                    ><img
                        src="https://viper.vetmed.ucdavis.edu/images/vetmed_logo.jpg"
                        alt="UC Davis Veterinary Medicine logo"
                        border="0"
                        width="201"
                        height="36"
                /></a>
                <q-btn
                    flat
                    dense
                    no-caps
                    class="gt-sm self-end"
                    :href="viperHome"
                >
                    <span class="mainLayoutViper">VIPER 2.0</span>
                    <q-badge
                        v-if="environment == 'DEVELOPMENT'"
                        color="negative"
                        role="presentation"
                        class="mainLayoutViperMode"
                        >Development</q-badge
                    >
                    <q-badge
                        v-if="environment == 'TEST'"
                        color="negative"
                        role="presentation"
                        class="mainLayoutViperMode"
                        >Test</q-badge
                    >
                </q-btn>

                <div class="breadcrumbs self-end q-ml-lg q-pb-xs row">
                    <span
                        v-for="(breadcrumb, index) in props?.breadcrumbs"
                        :key="breadcrumb.url"
                    >
                        <RouterLink
                            v-if="breadcrumb?.url?.length"
                            :to="breadcrumb.url"
                            >{{ breadcrumb.name }}</RouterLink
                        >
                        <span v-else>{{ breadcrumb.name }}</span>
                        <span
                            v-if="props?.breadcrumbs != undefined && index < props?.breadcrumbs.length - 1"
                            class="q-px-sm"
                            >&gt;</span
                        >
                    </span>
                </div>

                <q-banner
                    v-if="userStore.isEmulating"
                    dense
                    rounded
                    inline-actions
                    class="bg-warning text-black q-ml-lg"
                >
                    <strong>EMULATING:</strong>
                    {{ userStore.userInfo.firstName }} {{ userStore.userInfo.lastName }}
                    <q-btn
                        no-caps
                        dense
                        :href="clearEmulationHref"
                        color="secondary"
                        class="text-white q-px-sm q-ml-md"
                        >End Emulation</q-btn
                    >
                </q-banner>

                <q-space></q-space>
                <ProfilePic></ProfilePic>
            </q-toolbar>
        </q-header>

        <q-page-container id="mainLayoutBody">
            <main
                id="main-content"
                tabindex="-1"
            >
                <div
                    class="q-pa-md"
                    v-show="userStore.isLoggedIn"
                >
                    <router-view></router-view>
                </div>
            </main>
        </q-page-container>

        <q-footer
            elevated
            class="bg-white"
        >
            <div
                class="q-pa-y-sm q-pa-x-md q-gutter-xs text-caption row justify-between items-center"
                id="footerNavLinks"
            >
                <div class="q-pl-md">
                    <a
                        href="https://svmithelp.vetmed.ucdavis.edu/"
                        target="_blank"
                        rel="noopener"
                        class="text-primary"
                    >
                        <q-icon
                            color="primary"
                            name="help_center"
                            size="xs"
                            aria-hidden="true"
                        ></q-icon>
                        SVM-IT ServiceDesk
                        <span class="sr-only">(opens in new window)</span>
                    </a>
                    <span class="text-primary q-px-sm">|</span>
                    <a
                        href="https://www.vetmed.ucdavis.edu/"
                        target="_blank"
                        rel="noopener"
                        class="text-primary"
                    >
                        <q-icon
                            color="primary"
                            name="navigation"
                            size="xs"
                            aria-hidden="true"
                        ></q-icon>
                        SVM Home
                        <span class="sr-only">(opens in new window)</span>
                    </a>
                    <span class="text-primary q-px-sm">|</span>
                    <a
                        href="https://www.ucdavis.edu/"
                        target="_blank"
                        rel="noopener"
                        class="text-primary"
                    >
                        <q-icon
                            color="primary"
                            name="school"
                            size="xs"
                            aria-hidden="true"
                        ></q-icon>
                        UC Davis
                        <span class="sr-only">(opens in new window)</span>
                    </a>
                </div>
                <div class="">
                    <div class="text-black">&copy; {{ currentYear }} School of Veterinary Medicine</div>
                </div>
                <div class="q-pr-md">
                    <div
                        id="ucdavislogo"
                        class="q-mt-sm"
                    >
                        <a href="/"
                            ><img
                                src="https://viper.vetmed.ucdavis.edu/images/vetmed_logo.jpg"
                                alt="UC Davis Veterinary Medicine logo"
                                border="0"
                                width="134"
                                height="24"
                        /></a>
                    </div>
                </div>
            </div>
        </q-footer>

        <SessionTimeout />
    </q-layout>
</template>
