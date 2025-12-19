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
            <q-toolbar
                v-cloak
                class="items-end"
            >
                <img
                    src="@/assets/UCDSVMLogo.png"
                    height="50"
                    alt="UC Davis School of Veterinary Medicine Logo"
                />

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
                    @click="leftDrawerOpen = !leftDrawerOpen"
                ></q-btn>
                <q-btn
                    flat
                    dense
                    label="Viper 2.0"
                    class="lt-md"
                    :href="viperHome"
                >
                    <span
                        v-if="environment === 'DEVELOPMENT'"
                        class="mainLayoutViperMode"
                        >Dev</span
                    >
                    <span
                        v-if="environment === 'TEST'"
                        class="mainLayoutViperMode"
                        >Test</span
                    >
                </q-btn>

                <q-btn
                    flat
                    dense
                    no-caps
                    class="gt-sm text-white q-py-none q-ml-md self-end"
                    :href="viperHome"
                >
                    <span class="mainLayoutViper">VIPER 2.0</span>
                    <span
                        v-if="environment === 'DEVELOPMENT'"
                        class="mainLayoutViperMode"
                        >Development</span
                    >
                    <span
                        v-if="environment === 'TEST'"
                        class="mainLayoutViperMode"
                        >Test</span
                    >
                </q-btn>

                <!-- Desktop: Emulation banner inline -->
                <q-banner
                    v-if="userStore.isEmulating"
                    dense
                    rounded
                    inline-actions
                    class="bg-warning text-black q-ml-lg gt-xs"
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

            <!-- Mobile: Emulation banner on its own row -->
            <q-banner
                v-if="userStore.isEmulating"
                dense
                class="bg-warning text-black lt-sm text-center"
            >
                <strong>EMULATING:</strong>
                {{ userStore.userInfo.firstName }} {{ userStore.userInfo.lastName }}
                <q-btn
                    no-caps
                    dense
                    :href="clearEmulationHref"
                    color="secondary"
                    class="text-white q-px-sm q-ml-sm"
                    >End</q-btn
                >
            </q-banner>

            <MainNav highlighted-top-nav="Effort"></MainNav>
        </q-header>

        <!-- Custom Effort Left Drawer -->
        <q-drawer
            v-model="leftDrawerOpen"
            show-if-above
            elevated
            side="left"
            :mini="!leftDrawerOpen"
            no-mini-animation
            :width="300"
            id="mainLeftDrawer"
            v-cloak
            class="no-print"
        >
            <div
                class="q-pa-sm"
                id="leftNavMenu"
            >
                <q-btn
                    dense
                    round
                    unelevated
                    color="secondary"
                    icon="close"
                    class="float-right lt-md"
                    @click="leftDrawerOpen = false"
                />
                <h2>Effort 3.0</h2>

                <!-- Current Term Display -->
                <q-list
                    dense
                    separator
                >
                    <!-- Current Term - bold header style, clickable with pencil icon -->
                    <q-item
                        v-if="currentTerm"
                        clickable
                        v-ripple
                        :to="{ name: 'TermSelection' }"
                        class="leftNavHeader"
                    >
                        <q-item-section>
                            <q-item-label lines="1">
                                {{ currentTerm.termName }}
                                <q-icon
                                    name="edit"
                                    size="xs"
                                    class="q-ml-xs"
                                />
                            </q-item-label>
                        </q-item-section>
                    </q-item>
                    <q-item
                        v-else
                        clickable
                        v-ripple
                        :to="{ name: 'TermSelection' }"
                        class="leftNavHeader"
                    >
                        <q-item-section>
                            <q-item-label lines="1">Select a Term</q-item-label>
                        </q-item-section>
                    </q-item>

                    <!-- Manage Terms - only for ManageTerms users -->
                    <q-item
                        v-if="hasManageTerms"
                        clickable
                        v-ripple
                        :to="
                            currentTerm
                                ? { name: 'TermManagement', query: { termCode: currentTerm.termCode } }
                                : { name: 'TermManagement' }
                        "
                        class="leftNavLink"
                    >
                        <q-item-section>
                            <q-item-label lines="1">Manage Terms</q-item-label>
                        </q-item-section>
                    </q-item>

                    <!-- Courses - for users with course permissions -->
                    <q-item
                        v-if="canViewCourses && currentTerm"
                        clickable
                        v-ripple
                        :to="{ name: 'CourseList', params: { termCode: currentTerm.termCode } }"
                        class="leftNavLink"
                    >
                        <q-item-section>
                            <q-item-label lines="1">Courses</q-item-label>
                        </q-item-section>
                    </q-item>

                    <!-- Audit - only for ViewAudit users (term optional) -->
                    <q-item
                        v-if="hasViewAudit"
                        clickable
                        v-ripple
                        :to="
                            currentTerm
                                ? { name: 'EffortAuditWithTerm', params: { termCode: currentTerm.termCode } }
                                : { name: 'EffortAudit' }
                        "
                        class="leftNavLink"
                    >
                        <q-item-section>
                            <q-item-label lines="1">Audit Trail</q-item-label>
                        </q-item-section>
                    </q-item>

                    <!-- Spacer -->
                    <q-item class="leftNavSpacer">
                        <q-item-section></q-item-section>
                    </q-item>

                    <q-item
                        clickable
                        v-ripple
                        href="https://ucdsvm.knowledgeowl.com/help/effort-system-overview"
                        target="_blank"
                        class="leftNavLink"
                    >
                        <q-item-section>
                            <q-item-label lines="1">
                                <q-icon
                                    name="help_outline"
                                    size="xs"
                                    class="q-mr-xs"
                                />
                                Help
                            </q-item-label>
                        </q-item-section>
                    </q-item>
                </q-list>
            </div>
        </q-drawer>

        <q-page-container id="mainLayoutBody">
            <div
                class="q-pa-md"
                v-cloak
                v-show="userStore.isLoggedIn"
            >
                <router-view></router-view>
            </div>
            <div
                v-show="!userStore.isLoggedIn"
                class="q-pa-xl flex flex-center"
                v-cloak
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

<script setup lang="ts">
import { ref, watch, computed } from "vue"
import { useRoute } from "vue-router"
import { useUserStore } from "@/store/UserStore"
import { getLoginUrl } from "@/composables/RequireLogin"
import MainNav from "@/layouts/MainNav.vue"
import MiniNav from "@/layouts/MiniNav.vue"
import ProfilePic from "@/layouts/ProfilePic.vue"
import SessionTimeout from "@/components/SessionTimeout.vue"
import { effortService } from "../services/effort-service"
import { useEffortPermissions } from "../composables/use-effort-permissions"
import type { TermDto } from "../types"

const userStore = useUserStore()
const route = useRoute()
const {
    hasManageTerms,
    hasViewAudit,
    hasImportCourse,
    hasEditCourse,
    hasDeleteCourse,
    hasManageRCourseEnrollment,
    isAdmin,
} = useEffortPermissions()

// Users who can add/edit/delete courses or manage R-course enrollment should see the Courses link
const canViewCourses = computed(
    () =>
        hasImportCourse.value ||
        hasEditCourse.value ||
        hasDeleteCourse.value ||
        hasManageRCourseEnrollment.value ||
        isAdmin.value,
)

const leftDrawerOpen = ref(false)
const currentTerm = ref<TermDto | null>(null)

const clearEmulationHref = import.meta.env.VITE_VIPER_HOME + "ClearEmulation"
const environment = import.meta.env.VITE_ENVIRONMENT
const viperHome = import.meta.env.VITE_VIPER_HOME
const loginHref = getLoginUrl()

// Load term when termCode changes in route
async function loadCurrentTerm(termCode: number | null) {
    if (termCode) {
        currentTerm.value = await effortService.getTerm(termCode)
    } else {
        // No term selected - don't show any term until user picks one
        currentTerm.value = null
    }
}

// Watch both route params and query params for termCode
watch(
    () => route.params.termCode || route.query.termCode,
    (newTermCode) => {
        const termCode = newTermCode ? parseInt(newTermCode as string, 10) : null
        loadCurrentTerm(termCode)
    },
    { immediate: true },
)
</script>
