<script setup lang="ts">
import { computed, ref, onMounted, watch } from "vue"
import { inflect } from "inflection"
import { useQuasar } from "quasar"
import { useTimeoutFn } from "@vueuse/core"
import StatusBanner from "@/components/StatusBanner.vue"

/**
 * Opens and closes student editing for a self-service app. The app supplies how to read and
 * toggle its access; individualGrantCount is shown only by apps that grant access per student.
 */
const props = defineProps<{
    getStatus: () => Promise<{ appOpen: boolean; individualGrantCount?: number } | null>
    toggle: () => Promise<boolean | null>
}>()

const emit = defineEmits<{
    (e: "access-status-changed"): void
}>()

const $q = useQuasar()
const loading = ref(false)
const toggling = ref(false)
const appOpen = ref(false)
const individualGrantCount = ref<number>()

const status = ref<"unknown" | "ready" | "unavailable">("unknown")

// The Delayed-Spinner Rule: a status read that resolves inside 100ms shows no spinner at all.
const showSpinner = ref(false)
const { start: startSpinnerDelay, stop: cancelSpinnerDelay } = useTimeoutFn(
    () => {
        showSpinner.value = true
    },
    100,
    { immediate: false },
)

watch(loading, (busy) => {
    if (busy) {
        startSpinnerDelay()
        return
    }
    cancelSpinnerDelay()
    showSpinner.value = false
})

const bannerType = computed(() => (status.value === "unavailable" ? "warning" : "info"))

const bannerIcon = computed(() => {
    if (status.value !== "ready") {
        return status.value === "unavailable" ? "help_outline" : "hourglass_empty"
    }
    return appOpen.value ? "edit_note" : "edit_off"
})

const toggleLabel = computed(() => (appOpen.value ? "Disable Editing" : "Enable Editing"))

async function loadAccessStatus(): Promise<boolean> {
    loading.value = true
    let result: Awaited<ReturnType<typeof props.getStatus>>
    // The callbacks are props, so a rejection cannot be ruled out; it must not leave the banner loading.
    try {
        result = await props.getStatus()
    } finally {
        loading.value = false
    }

    if (!result) {
        if (status.value === "unknown") {
            status.value = "unavailable"
        }
        return false
    }

    appOpen.value = result.appOpen
    individualGrantCount.value = result.individualGrantCount
    status.value = "ready"
    return true
}

async function handleToggleApp(): Promise<void> {
    const shouldOpen = !appOpen.value
    $q.dialog({
        title: shouldOpen ? "Enable Editing" : "Disable Editing",
        message: shouldOpen
            ? "Are you sure you want to enable editing for all DVM students?"
            : "Are you sure you want to disable editing for all DVM students?",
        cancel: { label: "Cancel", flat: true },
        ok: { label: shouldOpen ? "Enable" : "Disable", color: shouldOpen ? "positive" : "negative" },
        persistent: true,
    }).onOk(async () => {
        toggling.value = true
        try {
            // Re-read to guard against concurrent changes by another admin.
            const refreshed = await loadAccessStatus()
            if (!refreshed) {
                $q.notify({ type: "negative", message: "Unable to verify current access status. Please try again." })
                return
            }
            if (appOpen.value === shouldOpen) {
                $q.notify({
                    type: "warning",
                    message: "Editing status was already changed. Please review the current state.",
                })
                return
            }
            const result = await props.toggle()
            if (result !== null) {
                appOpen.value = result
                emit("access-status-changed")
                $q.notify({
                    type: "positive",
                    message: `Editing ${result ? "enabled" : "disabled"} for all DVM students.`,
                })
            }
        } finally {
            toggling.value = false
        }
    })
}

onMounted(loadAccessStatus)
</script>

<template>
    <div class="relative-position">
        <StatusBanner
            :type="bannerType"
            :icon="bannerIcon"
        >
            <template v-if="status === 'ready'">
                <div class="q-mb-xs">
                    Student editing is <strong>{{ appOpen ? "open" : "closed" }}</strong>
                </div>
                <div
                    v-if="individualGrantCount !== undefined"
                    class="q-mb-xs"
                >
                    {{ individualGrantCount }} {{ inflect("student", individualGrantCount) }}
                    {{ individualGrantCount === 1 ? "has" : "have" }} individually granted access
                </div>
                <q-btn
                    :label="toggleLabel"
                    color="primary"
                    :loading="toggling"
                    dense
                    no-caps
                    class="app-access-toggle-btn"
                    @click="handleToggleApp"
                >
                    <template #loading>
                        <q-spinner
                            size="1em"
                            class="q-mr-sm"
                        />
                        {{ toggleLabel }}
                    </template>
                </q-btn>
            </template>

            <div v-else-if="status === 'unavailable'">Unable to check whether student editing is open.</div>

            <div v-else>Checking whether student editing is open...</div>

            <template
                v-if="status === 'unavailable'"
                #action
            >
                <q-btn
                    flat
                    dense
                    no-caps
                    label="Try Again"
                    @click="loadAccessStatus"
                />
            </template>
        </StatusBanner>

        <q-inner-loading :showing="showSpinner" />
    </div>
</template>

<style scoped>
.app-access-toggle-btn {
    min-width: 10rem;
}
</style>
