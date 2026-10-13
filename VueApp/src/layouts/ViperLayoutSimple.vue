<script setup lang="ts">
import { useUserStore } from "@/store/UserStore"
import ProfilePic from "@/layouts/ProfilePic.vue"
import EmulationBanner from "@/layouts/EmulationBanner.vue"
import FooterLinks from "@/layouts/FooterLinks.vue"
import ViperBrandButton from "@/layouts/ViperBrandButton.vue"
import SessionTimeout from "@/components/SessionTimeout.vue"

type BreadCrumb = {
    url: string
    name: string
}

defineOptions({ inheritAttrs: false })

const props = defineProps<{
    breadcrumbs?: BreadCrumb[]
}>()

const userStore = useUserStore()
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
                <ViperBrandButton class-name="gt-sm self-end" />

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

                <EmulationBanner />

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
                    <FooterLinks />
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
