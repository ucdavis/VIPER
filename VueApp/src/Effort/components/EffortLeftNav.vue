<template>
    <q-drawer
        v-model="localDrawerOpen"
        show-if-above
        elevated
        side="left"
        :mini="!localDrawerOpen"
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
                @click="localDrawerOpen = false"
            />
            <h2>Effort 3.0</h2>

            <q-list
                dense
                separator
            >
                <!-- Current Term - clickable with pencil icon -->
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
                    :to="{ name: 'TermManagement' }"
                    class="leftNavLink"
                >
                    <q-item-section>
                        <q-item-label lines="1">Manage Terms</q-item-label>
                    </q-item-section>
                </q-item>

                <!-- Audit - only for ViewAudit users -->
                <q-item
                    v-if="hasViewAudit"
                    clickable
                    v-ripple
                    :to="{ name: 'EffortAudit', query: currentTerm ? { termCode: currentTerm.termCode } : {} }"
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
                    rel="noopener noreferrer"
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
</template>

<script setup lang="ts">
import { ref, watch } from "vue"
import { useRoute } from "vue-router"
import { effortService } from "../services/effort-service"
import { useEffortPermissions } from "../composables/use-effort-permissions"
import type { TermDto } from "../types"

const props = defineProps<{
    drawerOpen: boolean
    onDrawerChange: (value: boolean) => void
}>()

const route = useRoute()
const { hasManageTerms, hasViewAudit } = useEffortPermissions()

const localDrawerOpen = ref(props.drawerOpen)
const currentTerm = ref<TermDto | null>(null)

// Sync local drawer state with parent
watch(
    () => props.drawerOpen,
    (newValue) => {
        localDrawerOpen.value = newValue
    },
)

watch(localDrawerOpen, (newValue) => {
    props.onDrawerChange(newValue)
})

// Load term when termCode changes in route
async function loadCurrentTerm(termCode: number | null) {
    if (termCode) {
        currentTerm.value = await effortService.getTerm(termCode)
    } else {
        currentTerm.value = await effortService.getCurrentTerm()
    }
}

watch(
    () => route.params.termCode,
    (newTermCode) => {
        const termCode = newTermCode ? parseInt(newTermCode as string, 10) : null
        loadCurrentTerm(termCode)
    },
    { immediate: true },
)
</script>
