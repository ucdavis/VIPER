<template>
    <div id="pageTop"></div>
    <q-layout view="hHh lpR fFf">
        <q-header
            elevated
            id="mainLayoutHeader"
            height-hint="98"
            v-cloak
            class="no-print"
        >
            <div
                v-show="false"
                id="headerPlaceholder"
            >
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
                            <span
                                v-if="environment == 'DEVELOPMENT'"
                                class="mainLayoutViperMode"
                                >Development</span
                            >
                            <span
                                v-if="environment == 'TEST'"
                                class="mainLayoutViperMode"
                                >Test</span
                            >
                        </span>
                    </a>
                </div>
            </div>
            <q-toolbar
                v-cloak
                class="items-end"
            >
                <img
                    src="@/assets/UCDSVMLogo.png"
                    height="50"
                    alt="UC Davis School of Veterinary Medicine Logo"
                />

                <!--Mini navigation for small screens-->
                <q-btn
                    flat
                    dense
                    icon="menu"
                    class="q-mr-xs lt-md"
                >
                    <MiniNav v-if="userStore.isLoggedIn"></MiniNav>
                </q-btn>
                <q-btn
                    flat
                    dense
                    icon="list"
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
                    <span
                        v-if="environment == 'DEVELOPMENT'"
                        class="mainLayoutViperMode"
                        >Dev</span
                    >
                    <span
                        v-if="environment == 'TEST'"
                        class="mainLayoutViperMode"
                        >Test</span
                    >
                </q-btn>

                <!--For medium+ screens-->
                <q-btn
                    flat
                    dense
                    no-caps
                    class="gt-sm text-white q-py-none q-ml-md self-end"
                    :href="viperHome"
                >
                    <span class="mainLayoutViper">VIPER 2.0</span>
                    <span
                        v-if="environment == 'DEVELOPMENT'"
                        class="mainLayoutViperMode"
                        >Development</span
                    >
                    <span
                        v-if="environment == 'TEST'"
                        class="mainLayoutViperMode"
                        >Test</span
                    >
                </q-btn>

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

            <MainNav
                v-if="userStore.isLoggedIn"
                :highlighted-top-nav="highlightedTopNav"
            ></MainNav>
        </q-header>

        <LeftNav
            v-if="userStore.isLoggedIn"
            :nav="nav"
            :navarea="navarea"
            :main-left-drawer="mainLeftDrawer"
            @drawer-change="handleDrawerChange"
        ></LeftNav>

        <q-page-container id="mainLayoutBody">
            <div
                class="q-pa-md"
                v-cloak
                v-show="userStore.isLoggedIn"
            >
                <router-view></router-view>
            </div>
        </q-page-container>

        <q-footer
            elevated
            class="bg-white no-print"
            v-cloak
        >
            <div
                class="q-py-sm q-px-md q-gutter-xs text-caption row"
                id="footerNavLinks"
            >
                <div class="col-12 col-md q-pl-md">
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
                        ></q-icon>
                        SVM-IT ServiceDesk
                    </a>
                    <span class="text-primary q-px-sm">|</span>
                    <a
                        href="http://www.vetmed.ucdavis.edu/"
                        target="_blank"
                        rel="noopener"
                        class="text-primary"
                    >
                        <q-icon
                            color="primary"
                            name="navigation"
                            size="xs"
                        ></q-icon>
                        SVM Home
                    </a>
                    <span class="text-primary q-px-sm">|</span>
                    <a
                        href="http://www.ucdavis.edu/"
                        target="_blank"
                        rel="noopener"
                        class="text-primary"
                    >
                        <q-icon
                            color="primary"
                            name="school"
                            size="xs"
                        ></q-icon>
                        UC Davis
                    </a>
                </div>
                <div class="col-12 col-md-auto gt-sm text-black">&copy; 2023 School of Veterinary Medicine</div>
            </div>
        </q-footer>
        <SessionTimeout />
    </q-layout>
</template>
<script lang="ts">
import { ref } from "vue"
import type { PropType } from "vue"
import { useUserStore } from "@/store/UserStore"
import LeftNav from "@/layouts/LeftNav.vue"
import MainNav from "@/layouts/MainNav.vue"
import MiniNav from "@/layouts/MiniNav.vue"
import ProfilePic from "@/layouts/ProfilePic.vue"
import SessionTimeout from "@/components/SessionTimeout.vue"

type BreadCrumb = {
    url: string
    name: string
}

export default {
    name: "ViperLayout",
    setup() {
        const userStore = useUserStore()
        const mainLeftDrawer = ref(false)
        return { userStore, mainLeftDrawer }
    },
    props: {
        nav: String,
        navarea: Boolean,
        highlightedTopNav: {
            type: String,
            default: "",
        },
        breadcrumbs: {
            type: Array as PropType<BreadCrumb[]>,
            default: () => [],
        },
    },
    components: {
        LeftNav,
        MainNav,
        MiniNav,
        ProfilePic,
        SessionTimeout,
    },
    data() {
        return {
            topNav: [],
            leftNav: [],
            clearEmulationHref: ref(import.meta.env.VITE_VIPER_HOME + "ClearEmulation"),
            environment: ref(import.meta.env.VITE_ENVIRONMENT),
            viperHome: ref(import.meta.env.VITE_VIPER_HOME),
        }
    },
    methods: {
        handleDrawerChange(newDrawerValue: boolean): void {
            this.mainLeftDrawer = newDrawerValue
        },
    },
}
</script>
