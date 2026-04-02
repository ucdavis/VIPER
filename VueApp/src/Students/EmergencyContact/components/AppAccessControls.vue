<script setup lang="ts">
import { ref, onMounted } from "vue"
import { inflect } from "inflection"
import { useQuasar } from "quasar"
import StatusBanner from "@/components/StatusBanner.vue"
import { emergencyContactService } from "../services/emergency-contact-service"
import type { IndividualAccess } from "../types"

const emit = defineEmits<{
    (e: "access-status-changed"): void
}>()

const $q = useQuasar()
const loading = ref(false)
const toggling = ref(false)
const appOpen = ref(false)
const individualGrants = ref<IndividualAccess[]>([])

async function loadAccessStatus(): Promise<boolean> {
    loading.value = true
    const status = await emergencyContactService.getAccessStatus()
    if (!status) {
        loading.value = false
        return false
    }
    appOpen.value = status.appOpen
    individualGrants.value = status.individualGrants
    loading.value = false
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
        // Re-read to guard against concurrent changes by another admin
        const refreshed = await loadAccessStatus()
        if (!refreshed) {
            toggling.value = false
            $q.notify({ type: "negative", message: "Unable to verify current access status. Please try again." })
            return
        }
        if (appOpen.value === shouldOpen) {
            toggling.value = false
            $q.notify({
                type: "warning",
                message: "Editing status was already changed. Please review the current state.",
            })
            return
        }
        const result = await emergencyContactService.toggleAppAccess()
        if (result !== null) {
            appOpen.value = result
            emit("access-status-changed")
            $q.notify({
                type: "positive",
                message: `Editing ${result ? "enabled" : "disabled"} for all DVM students.`,
            })
        }
        toggling.value = false
    })
}

onMounted(loadAccessStatus)
</script>

<template>
    <StatusBanner
        type="info"
        :icon="appOpen ? 'edit_note' : 'edit_off'"
    >
        <div class="q-mb-xs">
            Student editing is <strong>{{ appOpen ? "open" : "closed" }}</strong>
        </div>
        <div class="q-mb-xs">
            {{ individualGrants.length }} {{ inflect("student", individualGrants.length) }}
            {{ individualGrants.length === 1 ? "has" : "have" }} individually granted access
        </div>
        <q-btn
            :label="appOpen ? 'Disable Editing' : 'Enable Editing'"
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
                {{ appOpen ? "Disable Editing" : "Enable Editing" }}
            </template>
        </q-btn>
    </StatusBanner>

    <q-inner-loading :showing="loading" />
</template>

<style scoped>
.app-access-toggle-btn {
    min-width: 10rem;
}
</style>
