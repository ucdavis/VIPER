<template>
    <q-tabs
        v-model="tab"
        align="left"
        no-caps
        dense
        active-color="primary"
        indicator-color="primary"
        class="text-grey-8"
    >
        <q-tab
            v-for="section in result.sections"
            :key="section.key"
            :name="section.key"
        >
            <span class="row items-center no-wrap q-gutter-x-sm">
                <span>{{ section.title }}</span>
                <q-badge
                    :color="section.people.length > 0 ? 'primary' : 'grey-6'"
                    :label="section.people.length"
                    :aria-label="countLabel(section.people.length)"
                />
            </span>
        </q-tab>
    </q-tabs>
    <q-separator />
    <q-tab-panels
        :model-value="tab"
        animated
        keep-alive
    >
        <q-tab-panel
            v-for="section in result.sections"
            :key="section.key"
            :name="section.key"
            class="q-px-none"
        >
            <q-table
                :rows="section.people"
                :columns="columns"
                :row-key="rowKey"
                :filter="filters[section.key]"
                :rows-per-page-options="ROWS_PER_PAGE"
                :pagination="{ rowsPerPage: DEFAULT_PAGE_SIZE }"
                :title="section.title"
                no-data-label="No one matches this selection."
                no-results-label="No one matches the search."
                flat
                bordered
                dense
            >
                <template #top>
                    <q-input
                        v-model="filters[section.key]"
                        dense
                        outlined
                        clearable
                        debounce="200"
                        placeholder="Search"
                        :aria-label="`Search ${section.title}`"
                        class="col-12 col-sm-6"
                    >
                        <template #prepend>
                            <q-icon name="search" />
                        </template>
                    </q-input>
                    <q-space />
                    <q-btn
                        flat
                        dense
                        no-caps
                        icon="content_copy"
                        :label="`Copy ${countLabel(emailsOf(section).length, 'email address')}`"
                        :disable="emailsOf(section).length === 0"
                        @click="copyEmails(section)"
                    />
                </template>
            </q-table>
        </q-tab-panel>
    </q-tab-panels>
</template>

<script setup lang="ts">
import { inflect } from "inflection"
import { copyToClipboard, useQuasar } from "quasar"
import type { QTableColumn } from "quasar"
import { computed, reactive, ref, watch } from "vue"
import type {
    PersonCollectorPerson,
    PersonCollectorResult,
    PersonCollectorSection,
} from "../types/person-collector-types"

/**
 * The Person Collector lists, one tab per list with its count. Each list is a searchable, sortable
 * table with the ID columns this user may see, and can copy its email addresses, separated by
 * semicolons, for pasting into a mail client.
 */
const props = defineProps<{ result: PersonCollectorResult }>()

const $q = useQuasar()
const DEFAULT_PAGE_SIZE = 25
const LARGER_PAGE_SIZE = 50
const LARGEST_PAGE_SIZE = 100
const ALL_ROWS = 0
const ROWS_PER_PAGE = [DEFAULT_PAGE_SIZE, LARGER_PAGE_SIZE, LARGEST_PAGE_SIZE, ALL_ROWS]

type PersonField = keyof PersonCollectorPerson

function column(name: PersonField, label: string): QTableColumn<PersonCollectorPerson> {
    return { name, label, field: name, align: "left", sortable: true }
}

const NAME_COLUMNS = [column("name", "Name"), column("email", "Email")]
const LOGIN_COLUMNS = [column("loginId", "Login ID")]
const ID_COLUMNS = [
    column("employeeId", "Employee ID"),
    column("mothraId", "Mothra ID"),
    column("mailId", "Mail ID"),
    column("pidm", "PIDM"),
    column("bannerId", "Banner ID"),
]

const columns = computed(() => [
    ...NAME_COLUMNS,
    ...(props.result.showLoginIds ? LOGIN_COLUMNS : []),
    ...(props.result.showMoreIds ? ID_COLUMNS : []),
])

/** The open tab, kept when the lists reload and moved to the first list when it disappears. */
const tab = ref("")
const filters = reactive<Record<string, string>>({})

watch(
    () => props.result.sections.map((section) => section.key),
    (keys) => {
        if (!keys.includes(tab.value)) {
            tab.value = keys[0] ?? ""
        }
    },
    { immediate: true },
)

function rowKey(person: PersonCollectorPerson): string {
    return `${person.name ?? ""}|${person.email ?? ""}|${person.mothraId ?? ""}`
}

function countLabel(count: number, noun = "person"): string {
    return `${count} ${inflect(noun, count)}`
}

/** The list's distinct email addresses, in list order. */
function emailsOf(section: PersonCollectorSection): string[] {
    return [...new Set(section.people.map((person) => person.email).filter((email) => email !== null))]
}

async function copyEmails(section: PersonCollectorSection) {
    const emails = emailsOf(section)
    try {
        await copyToClipboard(emails.join("; "))
        $q.notify({ type: "positive", message: `Copied ${countLabel(emails.length, "email address")}.` })
    } catch {
        $q.notify({ type: "negative", message: "The email addresses could not be copied." })
    }
}
</script>
