<template>
    <div class="q-mb-md">
        <!-- Desktop: displays as a table, whose header does not use QTable's title.
             In read-only mode, hidden when empty.
             In maintain mode, displays when empty with the add button. -->
        <div
            v-show="showDesktopTable"
            class="gt-sm"
        >
            <div class="table-section-header q-mb-xs">
                <!-- tabindex allows a jump link to land focus here, as on the mobile heading. -->
                <h2
                    :id="desktopAnchorId"
                    class="table-section-heading"
                    tabindex="-1"
                >
                    {{ unit.name }}
                </h2>
                <q-btn
                    v-if="isMaintain"
                    type="button"
                    color="primary"
                    dense
                    no-caps
                    :aria-label="`Add to ${unit.name}`"
                    @click="$emit('addRecord', unit)"
                    icon="add"
                    size="xs"
                />
            </div>
            <q-table
                :rows="unit.rows"
                :columns="unit.cols"
                row-key="unitPersonId"
                dense
                :hide-pagination="true"
                v-model:pagination="pagination"
                :filter="search"
                :loading="loading"
            >
                <template
                    #body-cell-name="nameProps"
                    v-if="!isMaintain"
                >
                    <q-td
                        :props="nameProps"
                        v-if="canMail(nameProps.row)"
                    >
                        <a :href="mailtoHref(nameProps.row)">{{ nameProps.row.name }}</a>
                    </q-td>
                    <q-td
                        :props="nameProps"
                        v-else
                    >
                        {{ nameProps.row.name }}
                    </q-td>
                </template>
                <template #body-cell-listFirst="listFirstProps">
                    <q-td :props="listFirstProps">
                        <q-icon
                            v-if="listFirstProps.row.listFirst"
                            name="check"
                        ></q-icon>
                    </q-td>
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
        </div>

        <!-- The v-show for filters is handled within the component itself -->
        <MobileCardList
            v-model:pagination="pagination"
            :title="unit.name"
            :columns="unit.cols"
            :rows="unit.rows"
            :search="search"
            :loading="loading"
            :keep-when-empty="isMaintain"
            :anchor-id="mobileAnchorId"
            row-key="unitPersonId"
            :omit-columns="['name', 'edit', 'delete']"
            empty-message="No records to display."
        >
            <template #title-append>
                <q-btn
                    v-if="isMaintain"
                    type="button"
                    color="primary"
                    dense
                    no-caps
                    :aria-label="`Add to ${unit.name}`"
                    @click="$emit('addRecord', unit)"
                    icon="add"
                    size="xs"
                />
            </template>
            <!-- Mailed from the read-only list only, as on desktop: the maintain view is for
                 editing the record, not for contacting the person. -->
            <template #card-title="{ row }">
                <a
                    v-if="canMail(row)"
                    :href="mailtoHref(row)"
                    >{{ row.name }}</a
                >
                <template v-else>{{ row.name }}</template>
            </template>
            <template
                v-if="isMaintain"
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
import { computed } from "vue"
import MobileCardList from "./MobileCardList.vue"
import RecordActionButton from "./RecordActionButton.vue"
import RecordActionCell from "./RecordActionCell.vue"
import { useSectionTable } from "../composables/use-section-table"
import type { PhoneListDisplayRecord, PhoneListUnit } from "../types/phone-list-phone-types"

const props = defineProps<{
    unit: PhoneListUnit
    loading: boolean
    isMaintain: boolean
    search: string
    /** Set by a page offering jump links, so this heading can be one of the targets. */
    anchorId?: string
}>()
defineEmits(["addRecord", "editRecord", "deleteRecord"])

const { pagination, hasMatches, desktopAnchorId, mobileAnchorId } = useSectionTable({
    columns: () => props.unit.cols,
    rows: () => props.unit.rows,
    search: () => props.search,
    anchorId: () => props.anchorId,
})

// Loading keeps the table up for its loading bar; maintain keeps an emptied table for its add button.
const showDesktopTable = computed(() => props.loading || props.isMaintain || hasMatches.value)

/** Names are mailed from the read-only list only: the maintain view is for editing the record. */
function canMail(row: PhoneListDisplayRecord): boolean {
    return !props.isMaintain && row.employeeMailId !== ""
}

function mailtoHref(row: PhoneListDisplayRecord): string {
    return `mailto:${row.employeeMailId}@ucdavis.edu`
}
</script>
