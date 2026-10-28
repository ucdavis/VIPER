<template>
    <div class="q-mb-md">
        <!-- Desktop: displays as a table, behaving like SVMPhoneSectionTable. -->
        <div
            v-show="loading || editRecords || hasMatches"
            class="gt-sm"
        >
            <div class="table-section-header q-mb-xs">
                <!-- tabindex allows a jump link to land focus here, as on the mobile heading. -->
                <h2
                    :id="desktopAnchorId"
                    class="table-section-heading"
                    tabindex="-1"
                >
                    Frequently Called Numbers
                </h2>
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
            </div>
            <q-table
                :rows="frequentNumbers"
                :columns="cols"
                row-key="entryId"
                dense
                :hide-pagination="true"
                v-model:pagination="pagination"
                :filter="search"
                :loading="loading"
            >
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
        </div>

        <MobileCardList
            v-model:pagination="pagination"
            :anchor-id="mobileAnchorId"
            title="Frequently Called Numbers"
            :columns="cols"
            :rows="frequentNumbers"
            :search="search"
            :loading="loading"
            :keep-when-empty="editRecords"
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
import MobileCardList from "./MobileCardList.vue"
import RecordActionButton from "./RecordActionButton.vue"
import RecordActionCell from "./RecordActionCell.vue"
import { buildFrequentNumberColumns } from "../composables/svm-phone-columns"
import { useSectionTable } from "../composables/use-section-table"
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

const cols = buildFrequentNumberColumns(props.editRecords)

const { pagination, hasMatches, desktopAnchorId, mobileAnchorId } = useSectionTable({
    columns: cols,
    rows: () => props.frequentNumbers,
    search: () => props.search,
    anchorId: () => props.anchorId,
})
</script>
