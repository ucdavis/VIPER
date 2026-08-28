<template>
    <div class="q-mb-md">
        <!-- Desktop: table -->
        <q-table
            class="gt-sm"
            :rows="frequentNumbers"
            :columns="cols"
            row-key="entryId"
            dense
            :hide-pagination="true"
            v-model:pagination="pagination"
            :filter="search"
            :loading="loading"
        >
            <template
                v-if="editRecords"
                #top-left
            >
                <div class="row items-center q-gutter-sm">
                    <div class="q-table__title">
                        Frequently Called Numbers
                        <q-btn
                            type="button"
                            color="primary"
                            dense
                            no-caps
                            aria-label="Add Frequent Number"
                            @click="$emit('addFrequentNumber')"
                            icon="add"
                            size="xs"
                        />
                    </div>
                </div>
            </template>
            <template #body-cell-edit="cell">
                <RecordActionCell
                    action="edit"
                    :cell="cell"
                    @action="$emit('editFrequentNumber', cell.row)"
                />
            </template>
            <template #body-cell-delete="cell">
                <RecordActionCell
                    action="delete"
                    :cell="cell"
                    @action="$emit('deleteFrequentNumber', cell.row)"
                />
            </template>
        </q-table>

        <MobileCardList
            v-model:pagination="pagination"
            :anchor-id="anchorId"
            title="Frequently Called Numbers"
            :columns="cols"
            :rows="frequentNumbers"
            :search="search"
            :loading="loading"
            row-key="entryId"
            :omit-columns="['label', 'phone', 'edit', 'delete']"
            empty-message="No numbers to display."
        >
            <template #title-append>
                <q-btn
                    v-if="editRecords"
                    type="button"
                    color="primary"
                    dense
                    no-caps
                    aria-label="Add Frequent Number"
                    @click="$emit('addFrequentNumber')"
                    icon="add"
                    size="xs"
                />
            </template>
            <!-- An entry is a place and its number, so the number needs no label of its own and
                 is rendered here rather than left to the generic detail lines. -->
            <template #card-title="{ row }">{{ row.label }}</template>
            <template #card-detail="{ row }">
                <q-item-label class="text-body2">{{ row.phone }}</q-item-label>
            </template>
            <template
                v-if="editRecords"
                #card-actions="{ row }"
            >
                <RecordActionButton
                    action="edit"
                    @action="$emit('editFrequentNumber', row)"
                />
                <RecordActionButton
                    action="delete"
                    @action="$emit('deleteFrequentNumber', row)"
                />
            </template>
        </MobileCardList>
    </div>
</template>

<script setup lang="ts">
import { ref } from "vue"
import MobileCardList from "./MobileCardList.vue"
import RecordActionButton from "./RecordActionButton.vue"
import RecordActionCell from "./RecordActionCell.vue"
import { buildFrequentNumberColumns } from "../composables/svm-phone-columns"
import type { Ref } from "vue"
import type { QTableProps } from "quasar"
import type { SVMFrequentNumberRecord } from "../types/svm-phone-types"

const props = defineProps<{
    frequentNumbers: SVMFrequentNumberRecord[]
    loading: boolean
    editRecords: boolean
    search: string
    /** Set by a page offering jump links, so this heading can be one of the targets. */
    anchorId?: string
}>()
defineEmits(["addFrequentNumber", "editFrequentNumber", "deleteFrequentNumber"])
// Bound to the table, and shared with the card list's sort control.
const pagination: Ref<QTableProps["pagination"]> = ref({ rowsPerPage: 0, sortBy: null, descending: false })
const cols = buildFrequentNumberColumns(props.editRecords)
</script>
