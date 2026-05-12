<script setup lang="ts">
import { useDateFunctions } from "@/composables/DateFunctions"
import type { TermDto } from "../types"
import type { QTableColumn } from "quasar"

defineProps<{
    title: string
    rows: TermDto[]
    columns: QTableColumn[]
    emptyMessage?: string
}>()

const { formatDate } = useDateFunctions()
</script>

<template>
    <div class="q-mb-lg">
        <h2 class="text-h6 q-mb-sm q-mt-none">{{ title }}</h2>
        <!-- Desktop: Table -->
        <q-table
            :rows="rows"
            :columns="columns"
            row-key="termCode"
            dense
            flat
            bordered
            :pagination="{ rowsPerPage: 0 }"
            hide-pagination
            class="gt-xs"
        >
            <template #body-cell-termName="props">
                <q-td :props="props">
                    <router-link
                        :to="`/Effort/${props.row.termCode}`"
                        class="text-primary"
                    >
                        {{ props.row.termName }}
                    </router-link>
                </q-td>
            </template>
            <template #body-cell-harvestedDate="props">
                <q-td :props="props">{{ formatDate(props.row.harvestedDate) }}</q-td>
            </template>
            <template #body-cell-openedDate="props">
                <q-td :props="props">{{ formatDate(props.row.openedDate) }}</q-td>
            </template>
            <template #body-cell-expectedCloseDate="props">
                <q-td :props="props">{{ formatDate(props.row.expectedCloseDate) }}</q-td>
            </template>
            <template #body-cell-closedDate="props">
                <q-td :props="props">{{ formatDate(props.row.closedDate) }}</q-td>
            </template>
            <template
                v-if="emptyMessage"
                #no-data
            >
                <div class="text-grey q-pa-sm">{{ emptyMessage }}</div>
            </template>
        </q-table>

        <!-- Mobile: List -->
        <div class="lt-sm">
            <div class="text-subtitle2 q-mb-xs">{{ title }}</div>
            <q-list
                bordered
                separator
                dense
            >
                <q-item
                    v-for="term in rows"
                    :key="term.termCode"
                    v-ripple
                    clickable
                    :to="`/Effort/${term.termCode}`"
                >
                    <q-item-section>
                        <q-item-label>{{ term.termName }}</q-item-label>
                        <q-item-label caption>
                            <slot
                                name="caption"
                                :term="term"
                            />
                        </q-item-label>
                    </q-item-section>
                    <q-item-section side>
                        <q-icon name="chevron_right" />
                    </q-item-section>
                </q-item>
                <q-item v-if="rows.length === 0 && emptyMessage">
                    <q-item-section class="text-grey">{{ emptyMessage }}</q-item-section>
                </q-item>
            </q-list>
        </div>
    </div>
</template>
