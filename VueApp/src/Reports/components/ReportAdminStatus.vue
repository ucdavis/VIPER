<template>
    <div
        v-if="definition !== null"
        class="row items-center q-gutter-sm q-mb-md"
    >
        <span class="text-grey">{{ summary }}</span>
        <q-space />
        <q-btn
            v-if="definition.deletedOn === null"
            flat
            no-caps
            color="primary"
            icon="open_in_new"
            label="View report"
            :to="{ name: 'Report', params: { slug: definition.slug } }"
        />
        <q-btn
            v-if="definition.deletedOn === null"
            flat
            no-caps
            color="negative"
            icon="delete"
            label="Delete"
            @click="confirmDelete"
        />
        <q-btn
            v-else
            flat
            no-caps
            color="positive"
            icon="restore_from_trash"
            label="Restore"
            @click="emit('restore')"
        />
    </div>
    <StatusBanner
        v-if="definition?.deletedOn"
        type="warning"
    >
        This report was deleted{{ definition.deletedBy ? ` by ${definition.deletedBy}` : "" }}, so nobody can view it.
        Restore it to edit or show it again.
    </StatusBanner>
</template>

<script setup lang="ts">
import { computed } from "vue"
import StatusBanner from "@/components/StatusBanner.vue"
import { useConfirmDialog } from "@/composables/use-confirm-dialog"
import { formatCell } from "@/shared/report-builder/report-builder-format"
import type { ReportAdminDefinition } from "../admin/report-admin-types"

/** Who last changed a saved report, a link to it, and deleting or restoring it. */
const { definition } = defineProps<{ definition: ReportAdminDefinition | null }>()

const emit = defineEmits<{
    (e: "delete"): void
    (e: "restore"): void
}>()

const { confirmAction } = useConfirmDialog()

const summary = computed(() => {
    if (definition === null) {
        return ""
    }
    const when = formatCell(definition.modifiedOn, "DateTime")
    return definition.modifiedBy ? `Last changed ${when} by ${definition.modifiedBy}` : `Last changed ${when}`
})

async function confirmDelete(): Promise<void> {
    const confirmed = await confirmAction({
        title: "Delete report",
        message: `Delete "${definition?.report.title}"? Nobody will be able to view it until it's restored.`,
        okLabel: "Delete",
        okColor: "negative",
    })
    if (confirmed) {
        emit("delete")
    }
}
</script>
