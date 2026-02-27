<template>
    <div class="row q-col-gutter-sm items-end q-mb-sm">
        <div class="col-12 col-sm-6">
            <q-select
                v-model="selectedTermValue"
                :options="termDropdownOptions"
                option-label="label"
                option-value="value"
                emit-value
                map-options
                label="Term"
                dense
                options-dense
                outlined
            >
                <template #option="scope">
                    <q-item v-bind="scope.itemProps">
                        <q-item-section>
                            <q-item-label :class="scope.opt.isYear ? 'text-weight-bold' : 'q-pl-md'">
                                {{ scope.opt.label }}
                            </q-item-label>
                        </q-item-section>
                    </q-item>
                </template>
            </q-select>
        </div>
        <div
            v-if="showField('department')"
            class="col-12 col-sm-6"
        >
            <q-select
                v-model="selectedDepartment"
                :options="departmentOptions"
                emit-value
                map-options
                label="Department"
                dense
                options-dense
                outlined
            />
        </div>
    </div>
    <div
        v-if="showField('faculty') || showField('role')"
        class="row q-col-gutter-sm items-end q-mb-sm"
    >
        <div
            v-if="showField('faculty')"
            :class="showField('role') ? 'col-12 col-sm-6' : 'col-12'"
        >
            <q-select
                v-model="selectedPersonId"
                :options="displayedPersonOptions"
                option-label="fullName"
                option-value="personId"
                emit-value
                map-options
                label="Faculty"
                dense
                options-dense
                outlined
                use-input
                fill-input
                hide-selected
                input-debounce="200"
                @filter="filterPersons"
                @input-value="(v: string) => (personInput = v)"
                @popup-hide="
                    () => {
                        if (!personInput) selectedPersonId = null
                    }
                "
            >
                <template #no-option>
                    <q-item>
                        <q-item-section class="text-grey"> No results </q-item-section>
                    </q-item>
                </template>
            </q-select>
        </div>
        <div
            v-if="showField('role')"
            :class="showField('faculty') ? 'col-12 col-sm-6' : 'col-12'"
        >
            <q-select
                v-model="selectedRole"
                :options="roleOptions"
                emit-value
                map-options
                label="Teaching Role"
                dense
                options-dense
                outlined
            />
        </div>
    </div>
    <div
        v-if="showField('title')"
        class="row q-col-gutter-sm items-end q-mb-md"
    >
        <div class="col-12">
            <q-select
                v-model="selectedTitle"
                :options="displayedTitleOptions"
                emit-value
                map-options
                label="Title"
                dense
                options-dense
                outlined
                use-input
                fill-input
                hide-selected
                input-debounce="200"
                @filter="filterTitles"
                @input-value="(v: string) => (titleInput = v)"
                @popup-hide="
                    () => {
                        if (!titleInput) selectedTitle = null
                    }
                "
            >
                <template #no-option>
                    <q-item>
                        <q-item-section class="text-grey"> No results </q-item-section>
                    </q-item>
                </template>
            </q-select>
        </div>
    </div>
    <div class="row items-center q-mb-md">
        <q-btn
            color="primary"
            label="Generate Report"
            icon="assessment"
            :loading="loading"
            @click="handleGenerate"
        >
            <template #loading>
                <q-spinner
                    size="1em"
                    class="q-mr-sm"
                />
                Generate Report
            </template>
        </q-btn>
        <q-space />
        <slot name="actions" />
    </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from "vue"
import { termService } from "../services/term-service"
import { instructorService } from "../services/instructor-service"
import type { ReportFilterParams, TermDto, PersonDto, JobGroupDto, TermDropdownOption } from "../types"

type FilterField = "department" | "faculty" | "role" | "title"

const props = withDefaults(
    defineProps<{
        termCode: number
        loading?: boolean
        initialFilters?: ReportFilterParams
        visibleFields?: FilterField[]
        meritOnly?: boolean
    }>(),
    {
        loading: false,
        initialFilters: undefined,
        visibleFields: () => ["department", "faculty", "role", "title"] as FilterField[],
        meritOnly: false,
    },
)

function showField(field: FilterField): boolean {
    return props.visibleFields.includes(field)
}

const emit = defineEmits<{
    generate: [params: ReportFilterParams]
}>()

// Filter state — value is either a numeric termCode or an academic year string like "2024-2025"
// When initialFilters are provided (from URL query params), use those values instead of defaults.
const selectedTermValue = ref<number | string>(
    props.initialFilters?.academicYear ?? props.initialFilters?.termCode ?? props.termCode,
)
const selectedDepartment = ref<string | null>(props.initialFilters?.department ?? null)
const selectedPersonId = ref<number | null>(props.initialFilters?.personId ?? null)
const selectedRole = ref<string | null>(props.initialFilters?.role ?? null)
const selectedTitle = ref<string | null>(props.initialFilters?.title ?? null)

// Data sources
const allTerms = ref<TermDto[]>([])
const personOptions = ref<PersonDto[]>([])
const jobGroupOptions = ref<JobGroupDto[]>([])

/**
 * Compute academic year from a term code.
 * Winter (01), Spring Sem (02), Spring Qtr (03) belong to the AY that started previous year.
 * Summer (04-08) and Fall (09-10) belong to the AY starting in the current year.
 * E.g., 202501 → "2024-2025", 202410 → "2024-2025", 202504 → "2025-2026".
 */
function getAcademicYear(termCode: number): string {
    const year = Math.floor(termCode / 100)
    const term = termCode % 100
    const startYear = term >= 1 && term <= 3 ? year - 1 : year
    return `${startYear}-${startYear + 1}`
}

/**
 * Get all term codes that belong to the currently selected academic year.
 */
function getTermCodesForYear(academicYear: string): number[] {
    return allTerms.value.filter((t) => getAcademicYear(t.termCode) === academicYear).map((t) => t.termCode)
}

/**
 * Build the grouped dropdown options: academic year headers interwoven with individual terms.
 * Terms are ordered descending (newest first), grouped by academic year.
 */
const termDropdownOptions = computed<TermDropdownOption[]>(() => {
    const options: TermDropdownOption[] = []
    const sorted = [...allTerms.value].sort((a, b) => b.termCode - a.termCode)

    let currentYear = ""
    for (const term of sorted) {
        const year = getAcademicYear(term.termCode)
        if (year !== currentYear) {
            currentYear = year
            options.push({ label: year, value: year, isYear: true })
        }
        options.push({ label: term.termName, value: term.termCode, isYear: false })
    }

    return options
})

const departmentOptions = ref<{ label: string; value: string | null }[]>([])

// Role options matching legacy (1=Course Leader, 2=Teacher)
const roleOptions = [
    { label: "All", value: null as string | null },
    { label: "Course Leader", value: "1" },
    { label: "Teacher", value: "2" },
]

// Track native input text for resetting to "All" on clear
const personInput = ref("")
const titleInput = ref("")

// Version tokens to discard stale async responses on rapid filter changes
let instructorLoadVersion = 0
let jobGroupLoadVersion = 0

// "All" sentinels prepended to option lists
const allFacultyOption = { fullName: "All Faculty", personId: null as number | null }
const allTitlesOption = { label: "All Titles", value: null as string | null }

// Person options filtered by search input
const displayedPersonOptions = ref<{ fullName: string; personId: number | null }[]>([allFacultyOption])

// Title options filtered by search input
const titleOptions = ref<{ label: string; value: string | null }[]>([allTitlesOption])
const displayedTitleOptions = ref<{ label: string; value: string | null }[]>([allTitlesOption])

async function loadTerms() {
    allTerms.value = await termService.getTerms()
}

async function loadDepartments() {
    if (!showField("department")) return
    const depts = await instructorService.getInstructorDepartments()
    const options = depts.map((d) => ({ label: d.code, value: d.code as string | null }))
    if (options.length === 1) {
        departmentOptions.value = options
        if (selectedDepartment.value === null) {
            selectedDepartment.value = options[0]!.value
        }
    } else {
        departmentOptions.value = [{ label: "All Departments", value: null }, ...options]
    }
}

async function loadInstructors() {
    if (!showField("faculty")) return
    const val = selectedTermValue.value
    if (!val) return

    const version = ++instructorLoadVersion
    const dept = selectedDepartment.value ?? undefined

    // Determine which term codes to load instructors for
    let termCodes: number[]
    if (typeof val === "string") {
        // Academic year selected — load from all terms in that year
        termCodes = getTermCodesForYear(val)
    } else {
        termCodes = [val]
    }

    if (termCodes.length === 0) return

    // Load instructors from all terms and deduplicate by personId
    const allPersons = await Promise.all(
        termCodes.map((tc) => instructorService.getInstructors(tc, dept, props.meritOnly || undefined)),
    )

    // Discard stale response if a newer load was triggered while awaiting
    if (version !== instructorLoadVersion) return

    const seen = new Set<number>()
    const merged: PersonDto[] = []
    for (const persons of allPersons) {
        for (const p of persons) {
            if (!seen.has(p.personId)) {
                seen.add(p.personId)
                merged.push(p)
            }
        }
    }
    merged.sort((a, b) => a.fullName.localeCompare(b.fullName))

    personOptions.value = merged
    displayedPersonOptions.value = [
        allFacultyOption,
        ...merged.map((p) => ({ fullName: p.fullName, personId: p.personId as number | null })),
    ]
}

async function loadJobGroups() {
    if (!showField("title")) return
    const version = ++jobGroupLoadVersion
    const dept = selectedDepartment.value ?? undefined
    const result = await instructorService.getJobGroups(undefined, dept)

    // Discard stale response if a newer load was triggered while awaiting
    if (version !== jobGroupLoadVersion) return

    jobGroupOptions.value = result
    const mapped = result.map((g) => ({ label: g.name || g.code, value: g.code as string | null }))
    titleOptions.value = [allTitlesOption, ...mapped]
    displayedTitleOptions.value = [allTitlesOption, ...mapped]
}

function filterTitles(val: string, update: (fn: () => void) => void) {
    update(() => {
        const source = titleOptions.value.filter((t) => t.value !== null)

        if (!val) {
            displayedTitleOptions.value = [allTitlesOption, ...source]
        } else {
            const needle = val.toLowerCase()
            displayedTitleOptions.value = source.filter((t) => t.label.toLowerCase().includes(needle))
        }
    })
}

function filterPersons(val: string, update: (fn: () => void) => void) {
    update(() => {
        const source = personOptions.value.map((p) => ({
            fullName: p.fullName,
            personId: p.personId as number | null,
        }))

        if (!val) {
            displayedPersonOptions.value = [allFacultyOption, ...source]
        } else {
            const needle = val.toLowerCase()
            displayedPersonOptions.value = source.filter((p) => p.fullName.toLowerCase().includes(needle))
        }
    })
}

function handleGenerate() {
    const val = selectedTermValue.value
    const params: ReportFilterParams = {}

    if (typeof val === "string") {
        params.academicYear = val
    } else {
        params.termCode = val
    }

    if (showField("department") && selectedDepartment.value) params.department = selectedDepartment.value
    if (showField("faculty") && selectedPersonId.value) params.personId = selectedPersonId.value
    if (showField("role") && selectedRole.value) params.role = selectedRole.value
    if (showField("title") && selectedTitle.value) params.title = selectedTitle.value

    emit("generate", params)
}

// Cascading filter reloads
watch(selectedTermValue, () => {
    selectedPersonId.value = null
    selectedTitle.value = null
    loadInstructors()
    loadJobGroups()
})

watch(selectedDepartment, () => {
    selectedPersonId.value = null
    selectedTitle.value = null
    loadInstructors()
    loadJobGroups()
})

onMounted(async () => {
    await Promise.all([loadTerms(), loadDepartments()])
    await Promise.all([loadInstructors(), loadJobGroups()])

    // Auto-generate report when URL query params provided initial filters
    if (props.initialFilters) {
        handleGenerate()
    }
})
</script>
