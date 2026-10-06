<template>
    <q-card
        flat
        bordered
    >
        <q-card-section class="row items-center q-pb-none">
            <h2 class="text-h6 q-my-none col">Choose people</h2>
            <q-btn
                flat
                dense
                no-caps
                icon="restart_alt"
                label="Clear"
                :disable="!hasChoice(model) && model.departments.length === 0"
                @click="model = emptyRequest()"
            />
        </q-card-section>

        <q-card-section class="q-gutter-y-sm">
            <h3 class="text-subtitle2 q-my-none">Staff and faculty department filter</h3>
            <PersonCollectorChoices
                v-model="departments"
                label="Departments"
                :choices="form.departments"
                all-label="All listed departments"
            />
            <p class="text-caption text-grey-8 q-mb-none">Leave empty to include every SVM department.</p>
            <q-toggle
                v-model="fullDepartmentList"
                label="Everyone in these departments"
                :disable="model.departments.length === 0"
                dense
            />
        </q-card-section>

        <q-separator inset />

        <q-card-section class="q-gutter-y-sm">
            <h3 class="text-subtitle2 q-my-none">Faculty</h3>
            <PersonCollectorChoices
                v-model="senateGroups"
                label="Senate titles"
                :choices="form.senateGroups"
            />
            <q-toggle
                v-model="senateEmeriti"
                label="Senate emeriti"
                dense
            />
            <PersonCollectorChoices
                v-model="federationGroups"
                label="Federation titles"
                :choices="form.federationGroups"
            />
        </q-card-section>

        <q-separator inset />

        <q-card-section class="column q-gutter-y-xs">
            <h3 class="text-subtitle2 q-my-none">Staff</h3>
            <q-toggle
                v-model="staffMsp"
                label="MSP"
                dense
            />
            <q-toggle
                v-model="staffPss"
                label="PSS"
                dense
            />
            <q-toggle
                v-model="staffVeterinarians"
                label="Staff veterinarians"
                dense
            />
        </q-card-section>

        <q-separator inset />

        <q-card-section>
            <h3 class="text-subtitle2 q-mt-none q-mb-sm">Students</h3>
            <PersonCollectorChoices
                v-model="students"
                label="Classes and programs"
                :choices="[...form.studentClasses, { key: MPVM, label: MPVM }]"
                all-label="All students"
            />
        </q-card-section>
    </q-card>
</template>

<script setup lang="ts">
import { computed } from "vue"
import type { WritableComputedRef } from "vue"
import PersonCollectorChoices from "./PersonCollectorChoices.vue"
import { MPVM, emptyRequest, hasChoice, studentKeys, withStudentKeys } from "../services/person-collector-request"
import type { PersonCollectorForm, PersonCollectorRequest } from "../types/person-collector-types"

/**
 * The Person Collector's choices. Every change replaces the whole request, so the page can watch
 * one value and reload the lists when it changes.
 */
defineProps<{ form: PersonCollectorForm }>()

const model = defineModel<PersonCollectorRequest>({ required: true })

/** A two-way binding for one request field that writes back a new request object. */
function field<K extends keyof PersonCollectorRequest>(key: K): WritableComputedRef<PersonCollectorRequest[K]> {
    return computed({
        get: () => model.value[key],
        set: (value) => {
            model.value = { ...model.value, [key]: value }
        },
    })
}

const departments = field("departments")
const fullDepartmentList = field("fullDepartmentList")
const senateGroups = field("senateGroups")
const senateEmeriti = field("senateEmeriti")
const federationGroups = field("federationGroups")
const staffMsp = field("staffMsp")
const staffPss = field("staffPss")
const staffVeterinarians = field("staffVeterinarians")

const students = computed({
    get: () => studentKeys(model.value),
    set: (keys: string[]) => {
        model.value = withStudentKeys(model.value, keys)
    },
})
</script>
