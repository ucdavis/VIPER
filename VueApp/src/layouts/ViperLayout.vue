<template>
    <div id="pageTop"></div>
    <a
        href="#main-content"
        class="skip-to-content"
        >Skip to main content</a
    >
    <!-- gt-sm: the drawer is hidden below QDrawer's show-if-above breakpoint,
         so the skip link would target an invisible element on small screens -->
    <a
        v-if="navarea"
        href="#leftNavMenu"
        class="skip-to-content gt-sm"
        >Skip to section menu</a
    >
    <q-layout view="hHh lpR fFf">
        <q-header
            elevated
            id="mainLayoutHeader"
            height-hint="98"
            class="no-print"
        >
            <q-toolbar class="items-end">
                <div class="viper-brand">
                    <div
                        class="viper-brand__mark"
                        aria-hidden="true"
                    >
                        <picture>
                            <source
                                :srcset="rodMarkAvif"
                                type="image/avif"
                            />
                            <img
                                :src="rodMark"
                                alt=""
                            />
                        </picture>
                    </div>
                    <picture>
                        <source
                            :srcset="schoolLockupAvif"
                            type="image/avif"
                        />
                        <img
                            class="viper-brand__name"
                            :src="schoolLockup"
                            alt="UC Davis Weill School of Veterinary Medicine"
                        />
                    </picture>
                </div>

                <!--Mini navigation for small screens-->
                <q-btn
                    flat
                    dense
                    icon="menu"
                    aria-label="Navigation menu"
                    class="q-mr-xs lt-md"
                >
                    <MiniNav v-if="userStore.isLoggedIn"></MiniNav>
                </q-btn>
                <q-btn
                    flat
                    dense
                    icon="list"
                    aria-label="Toggle sidebar"
                    class="q-mr-xs lt-md"
                    @click="mainLeftDrawer = !mainLeftDrawer"
                ></q-btn>
                <q-btn
                    flat
                    dense
                    label="Viper 2.0"
                    class="lt-md"
                    :href="viperHome"
                >
                    <q-badge
                        v-if="environment == 'DEVELOPMENT'"
                        color="negative"
                        role="presentation"
                        class="mainLayoutViperMode"
                        >Dev</q-badge
                    >
                    <q-badge
                        v-if="environment == 'TEST'"
                        color="negative"
                        role="presentation"
                        class="mainLayoutViperMode"
                        >Test</q-badge
                    >
                </q-btn>

                <!--For medium+ screens-->
                <ViperBrandButton />

                <EmulationBanner />

                <q-space></q-space>

                <!--
                @*Don't show the search until it does something*@
                @if (HttpHelper.Environment?.EnvironmentName == "Development")
                {
                <q-input rounded dense standout dark v-model="searchText" label="Search" bg-color="white" label-color="black" class="q-pa-xs">
                    <template v-slot:append>
                        <q-icon name="search" color="black"></q-icon>
                    </template>
                </q-input>
                }
                    -->
                <ProfilePic></ProfilePic>
            </q-toolbar>

            <MainNav :highlighted-top-nav="highlightedTopNav"></MainNav>
        </q-header>

        <slot
            name="left-nav"
            :drawer-open="mainLeftDrawer"
            :on-drawer-change="handleDrawerChange"
        >
            <LeftNav
                :nav="nav"
                :navarea="navarea"
                :main-left-drawer="mainLeftDrawer"
                @drawer-change="handleDrawerChange"
            ></LeftNav>
        </slot>

        <q-page-container id="mainLayoutBody">
            <main
                id="main-content"
                tabindex="-1"
            >
                <div
                    class="q-pa-md"
                    v-show="userStore.isLoggedIn || showViewWhenNotLoggedIn"
                >
                    <router-view></router-view>
                </div>
                <div
                    v-show="!userStore.isLoggedIn && !showViewWhenNotLoggedIn"
                    class="q-pa-xl flex flex-center"
                >
                    <q-card
                        class="text-center"
                        style="max-width: 400px"
                    >
                        <q-card-section>
                            <div class="text-h6">Welcome to VIPER</div>
                            <div class="text-body1 q-mt-sm text-grey-7">Please log in to access this application.</div>
                        </q-card-section>
                        <q-card-actions
                            align="center"
                            class="q-pb-md"
                        >
                            <q-btn
                                color="primary"
                                label="Log In"
                                :href="loginHref"
                                no-caps
                            />
                        </q-card-actions>
                    </q-card>
                </div>
            </main>
        </q-page-container>

        <q-footer
            elevated
            class="bg-white no-print"
        >
            <div
                class="q-py-sm q-px-md q-gutter-xs text-caption row"
                id="footerNavLinks"
            >
                <div class="col-12 col-md q-pl-md">
                    <FooterLinks />
                </div>
                <div class="col-12 col-md-auto gt-sm text-black">
                    &copy; {{ currentYear }} School of Veterinary Medicine
                </div>
            </div>
        </q-footer>
        <SessionTimeout />
    </q-layout>
</template>

<script setup lang="ts">
import { ref } from "vue"
import rodMark from "@/assets/rod-of-asclepius-white.png"
import rodMarkAvif from "@/assets/rod-of-asclepius-white.avif"
import schoolLockup from "@/assets/logo-vetmed-stacked-lockup.png"
import schoolLockupAvif from "@/assets/logo-vetmed-stacked-lockup.avif"
import { useUserStore } from "@/store/UserStore"
import { getLoginUrl } from "@/composables/RequireLogin"
import LeftNav from "@/layouts/LeftNav.vue"
import MainNav from "@/layouts/MainNav.vue"
import MiniNav from "@/layouts/MiniNav.vue"
import ProfilePic from "@/layouts/ProfilePic.vue"
import EmulationBanner from "@/layouts/EmulationBanner.vue"
import FooterLinks from "@/layouts/FooterLinks.vue"
import ViperBrandButton from "@/layouts/ViperBrandButton.vue"
import SessionTimeout from "@/components/SessionTimeout.vue"

type BreadCrumb = {
    url: string
    name: string
}

defineProps<{
    nav?: string
    navarea?: boolean
    highlightedTopNav?: string
    breadcrumbs?: BreadCrumb[]
    showViewWhenNotLoggedIn?: boolean
}>()

const userStore = useUserStore()
const mainLeftDrawer = ref(false)
const environment = import.meta.env.VITE_ENVIRONMENT
const viperHome = import.meta.env.VITE_VIPER_HOME
const loginHref = getLoginUrl()
const currentYear = new Date().getFullYear()

function handleDrawerChange(newDrawerValue: boolean): void {
    mainLeftDrawer.value = newDrawerValue
}
</script>
