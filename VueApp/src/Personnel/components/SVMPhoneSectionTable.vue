<template>
    <div class="q-mb-md">
        <!-- Desktop: table -->
        <q-table
            class="gt-sm"
            :rows="section.rows"
            :columns="section.cols"
            row-key="entryId"
            dense
            :hide-pagination="true"
            v-model:pagination="pagination"
            :filter="search"
            :loading="loading"
        >
            <template
                #top-left
                v-if="isModify"
            >
                <div class="row items-center q-gutter-sm">
                    <div class="q-table__title">
                        {{ section.title }}
                        <q-btn
                            type="button"
                            color="primary"
                            dense
                            no-caps
                            :aria-label="`Add to ${section.title}`"
                            @click="$emit('addRecord', section.title, section.id)"
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
                    @action="$emit('editRecord', cell.row)"
                />
            </template>
            <template #body-cell-delete="cell">
                <RecordActionCell
                    action="delete"
                    :cell="cell"
                    @action="$emit('deleteRecord', cell.row)"
                />
            </template>
        </q-table>

        <MobileCardList
            v-model:pagination="pagination"
            :anchor-id="anchorId"
            :title="section.title"
            :columns="section.cols"
            :rows="section.rows"
            :search="search"
            :loading="loading"
            row-key="entryId"
            :omit-columns="['unitName', 'abbreviation', 'edit', 'delete']"
            empty-message="No records to display."
        >
            <template #title-append>
                <q-btn
                    v-if="isModify"
                    type="button"
                    color="primary"
                    dense
                    no-caps
                    :aria-label="`Add to ${section.title}`"
                    @click="$emit('addRecord', section.title, section.id)"
                    icon="add"
                    size="xs"
                />
            </template>
            <template #card-title="{ row }">{{ unitHeading(row) }}</template>
            <template
                v-if="isModify"
                #card-actions="{ row }"
            >
                <RecordActionButton
                    action="edit"
                    @action="$emit('editRecord', row)"
                />
                <RecordActionButton
                    action="delete"
                    @action="$emit('deleteRecord', row)"
                />
            </template>
        </MobileCardList>
    </div>
</template>

<script setup lang="ts">
import { computed, ref } from "vue"
import MobileCardList from "./MobileCardList.vue"
import RecordActionButton from "./RecordActionButton.vue"
import RecordActionCell from "./RecordActionCell.vue"
import { columnText } from "../composables/use-mobile-table-rows"
import type { Ref } from "vue"
import type { QTableProps } from "quasar"
import type { SVMPhoneDisplayRecord, SVMPhoneSection } from "../types/svm-phone-types"

const props = defineProps<{
    section: SVMPhoneSection
    loading: boolean
    isModify: boolean
    search: string
    /** Set by a page offering jump links, so this heading can be one of the targets. */
    anchorId?: string
}>()
defineEmits(["addRecord", "editRecord", "deleteRecord"])
// Bound to the table, and shared with the card list's sort control.
const pagination: Ref<QTableProps["pagination"]> = ref({ rowsPerPage: 0, sortBy: null, descending: false })

// Gated on the column rather than the row's own field: a section that does not include the
// abbreviation column does not show one on desktop either.
const abbreviationColumn = computed(() => (props.section.cols ?? []).find((col) => col.name === "abbreviation"))

/** The unit name, carrying its abbreviation in parentheses where the section uses one. */
function unitHeading(row: SVMPhoneDisplayRecord): string {
    const name = row.unitName ?? ""
    const abbreviation = abbreviationColumn.value !== undefined ? columnText(abbreviationColumn.value, row) : ""
    return abbreviation === "" ? name : `${name} (${abbreviation})`
}
</script>
