<script setup lang="ts">
import { computed, onMounted, ref } from "vue"
import { useQuasar } from "quasar"
import type { QTableColumn } from "quasar"
import { inflect } from "inflection"
import StatusBanner from "@/components/StatusBanner.vue"
import CareerOptionFormDialog from "./CareerOptionFormDialog.vue"
import { CAREER_OPTION_TYPES, useCareerOptionManager } from "../composables/use-career-option-manager"
import type { CareerOptionType, CareerSelectionOption } from "../types"

const { type } = defineProps<{
    type: CareerOptionType
}>()

const $q = useQuasar()
const config = computed(() => CAREER_OPTION_TYPES[type])
const headingId = computed(() => `career-options-${type}`)

const { options, loading, loadFailed, deletingId, load, save, remove } = useCareerOptionManager(type)

const dialogOpen = ref(false)
const editingOption = ref<CareerSelectionOption | null>(null)

// The table wraps its cells so a long option name cannot push the actions off a phone screen. The
// count and the buttons stay on one line, leaving the name column to take up the wrapping.
const columns: QTableColumn[] = [
    { name: "label", label: "Option", field: "label", align: "left", classes: "option-label" },
    { name: "usageCount", label: "Selected By", field: "usageCount", align: "right", classes: "text-no-wrap" },
    { name: "actions", label: "Actions", field: "id", align: "right", classes: "text-no-wrap" },
]

function openDialog(option: CareerSelectionOption | null): void {
    editingOption.value = option
    dialogOpen.value = true
}

function onSaved(label: string): void {
    $q.notify({
        type: "positive",
        message: `${editingOption.value ? "Updated" : "Added"} "${label}".`,
    })
}

function deleteLabel(option: CareerSelectionOption): string {
    if (option.usageCount === 0) {
        return `Delete ${option.label}`
    }
    return `Cannot delete ${option.label}: selected by ${option.usageCount} ${inflect("student", option.usageCount)}`
}

function confirmDelete(option: CareerSelectionOption): void {
    // The button is aria-disabled rather than disabled, so it still fires. The rule is enforced
    // here; the server refuses it too, since a selection holds a foreign key to the option.
    if (option.usageCount > 0) {
        return
    }
    $q.dialog({
        title: "Delete Option",
        message: `Delete "${option.label}"? Students will no longer be able to choose it.`,
        cancel: { label: "Cancel", flat: true },
        ok: { label: "Delete", color: "negative" },
        persistent: true,
    }).onOk(async () => {
        const result = await remove(option.id)
        if (result.success) {
            $q.notify({ type: "positive", message: `Deleted "${option.label}".` })
            return
        }
        $q.notify({
            type: "negative",
            message: result.errors.join(" ") || `Unable to delete "${option.label}". Please try again.`,
        })
    })
}

onMounted(load)
</script>

<template>
    <section
        :aria-labelledby="headingId"
        class="q-mb-lg"
    >
        <div class="row items-center q-mb-xs">
            <h2
                :id="headingId"
                class="q-ma-none"
            >
                {{ config.title }}
            </h2>
            <q-space />
            <q-btn
                label="Add Option"
                icon="add"
                color="positive"
                dense
                no-caps
                padding="xs sm"
                :aria-label="`Add ${config.singular} option`"
                :disable="loading || loadFailed"
                @click="openDialog(null)"
            />
        </div>
        <p class="q-mb-sm">{{ config.description }}</p>

        <StatusBanner
            v-if="loadFailed"
            type="error"
            class="q-mb-md"
        >
            Unable to load the {{ config.title }} options.
            <template #action>
                <q-btn
                    flat
                    dense
                    no-caps
                    label="Try Again"
                    @click="load"
                />
            </template>
        </StatusBanner>

        <template v-else>
            <q-table
                :rows="options"
                :columns="columns"
                row-key="id"
                :loading="loading"
                :pagination="{ rowsPerPage: 0 }"
                hide-pagination
                flat
                bordered
                dense
                wrap-cells
                no-data-label="No options have been added yet."
            >
                <template #body-cell-label="props">
                    <q-td :props="props">
                        {{ props.row.label }}
                    </q-td>
                </template>

                <template #body-cell-usageCount="props">
                    <q-td :props="props">
                        {{ props.row.usageCount }} {{ inflect("student", props.row.usageCount) }}
                    </q-td>
                </template>

                <template #body-cell-actions="props">
                    <q-td :props="props">
                        <template v-if="!props.row.isOther">
                            <q-btn
                                icon="edit"
                                color="primary"
                                flat
                                round
                                dense
                                size="sm"
                                :aria-label="`Edit ${props.row.label}`"
                                @click="openDialog(props.row)"
                            >
                                <q-tooltip>Edit</q-tooltip>
                            </q-btn>
                            <!-- aria-disabled rather than disable: a disabled q-btn leaves the tab
                                 order and never opens its tooltip, so the reason a delete is
                                 refused would be unreachable by keyboard. confirmDelete refuses
                                 it instead, since aria-disabled is advisory only. -->
                            <q-btn
                                icon="delete"
                                :color="props.row.usageCount > 0 ? 'grey-7' : 'negative'"
                                flat
                                round
                                dense
                                size="sm"
                                :aria-label="deleteLabel(props.row)"
                                :aria-disabled="props.row.usageCount > 0 ? 'true' : undefined"
                                :loading="deletingId === props.row.id"
                                @click="confirmDelete(props.row)"
                            >
                                <q-tooltip>{{ deleteLabel(props.row) }}</q-tooltip>
                            </q-btn>
                        </template>
                    </q-td>
                </template>
            </q-table>
        </template>

        <CareerOptionFormDialog
            v-model="dialogOpen"
            :title-id="`${headingId}-dialog-title`"
            :config="config"
            :option="editingOption"
            :existing-options="options"
            :save-option="save"
            @saved="onSaved"
        />
    </section>
</template>

<style scoped>
/* wrap-cells only breaks at spaces, so one long unbroken word still holds the table wider than a
   phone. It must be anywhere, not break-word: a table sizes its columns to their narrowest
   content, and only anywhere lets that narrowest content shrink. */
.option-label {
    overflow-wrap: anywhere;
}
</style>
