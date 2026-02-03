<template>
    <q-dialog
        :model-value="modelValue"
        persistent
        @keydown.escape="handleClose"
    >
        <q-card style="width: 100%; max-width: 600px">
            <q-card-section class="row items-center q-pb-none">
                <div class="text-h6">Add Instructor</div>
                <q-space />
                <q-btn
                    icon="close"
                    flat
                    round
                    dense
                    aria-label="Close dialog"
                    @click="handleClose"
                />
            </q-card-section>

            <q-card-section>
                <q-select
                    v-model="selectedPerson"
                    :options="searchResults"
                    :loading="isSearching"
                    label="Search for instructor"
                    use-input
                    input-debounce="300"
                    option-label="fullName"
                    option-value="personId"
                    dense
                    options-dense
                    outlined
                    clearable
                    behavior="menu"
                    @filter="onFilter"
                >
                    <template #no-option>
                        <q-item>
                            <q-item-section class="text-grey">
                                {{
                                    searchTerm.length < 2
                                        ? "Type at least 2 characters to search"
                                        : isSearching
                                          ? "Searching..."
                                          : "No results found"
                                }}
                            </q-item-section>
                        </q-item>
                    </template>
                    <template #option="scope">
                        <q-item v-bind="scope.itemProps">
                            <q-item-section>
                                <q-item-label>{{ scope.opt.fullName }}</q-item-label>
                                <q-item-label caption>
                                    {{ scope.opt.deptName || scope.opt.effortDept || "No dept" }}
                                    <span v-if="scope.opt.title || scope.opt.titleCode">
                                        &bull; {{ scope.opt.title || scope.opt.titleCode }}
                                    </span>
                                </q-item-label>
                            </q-item-section>
                        </q-item>
                    </template>
                    <template #selected-item="scope">
                        <div>
                            {{ scope.opt.fullName }}
                            <span
                                v-if="scope.opt.deptName || scope.opt.effortDept"
                                class="text-grey-7"
                            >
                                ({{ scope.opt.deptName || scope.opt.effortDept }})
                            </span>
                        </div>
                    </template>
                </q-select>

                <!-- Selected person preview -->
                <div
                    v-if="selectedPerson"
                    class="q-mt-md q-pa-sm bg-grey-2 rounded-borders"
                >
                    <div class="text-subtitle2">Selected Instructor</div>
                    <div class="q-mt-xs">
                        <strong>{{ selectedPerson.fullName }}</strong>
                    </div>
                    <div class="text-caption text-grey-7">
                        <span v-if="selectedPerson.deptName || selectedPerson.effortDept">
                            Department: {{ selectedPerson.deptName || selectedPerson.effortDept }}
                        </span>
                        <span v-else>No department assigned</span>
                    </div>
                    <div
                        v-if="selectedPerson.title || selectedPerson.titleCode"
                        class="text-caption text-grey-7"
                    >
                        Title: {{ selectedPerson.title || selectedPerson.titleCode }}
                    </div>
                </div>

                <q-banner
                    v-if="errorMessage"
                    class="bg-negative text-white q-mb-md"
                    rounded
                >
                    <template #avatar>
                        <q-icon
                            name="error"
                            color="white"
                        />
                    </template>
                    {{ errorMessage }}
                </q-banner>
            </q-card-section>

            <q-card-actions align="right">
                <q-btn
                    flat
                    label="Cancel"
                    @click="handleClose"
                />
                <q-btn
                    color="primary"
                    label="Add Instructor"
                    :loading="isSaving"
                    :disable="!selectedPerson"
                    @click="addInstructor"
                />
            </q-card-actions>
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { ref, watch } from "vue"
import { useUnsavedChanges } from "@/composables/use-unsaved-changes"
import { instructorService } from "../services/instructor-service"
import type { AaudPersonDto } from "../types"

const props = defineProps<{
    modelValue: boolean
    termCode: number | null
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    created: []
}>()

const searchTerm = ref("")
const searchResults = ref<AaudPersonDto[]>([])
const selectedPerson = ref<AaudPersonDto | null>(null)
const isSearching = ref(false)
const isSaving = ref(false)
const errorMessage = ref("")

// Form data for unsaved changes tracking (track if a person was selected)
const formData = ref({
    selectedPersonId: null as number | null,
})

// Unsaved changes tracking
const { setInitialState, confirmClose } = useUnsavedChanges(formData)

// Handle close (X button, Cancel button, or Escape key) with unsaved changes check
async function handleClose() {
    if (await confirmClose()) {
        emit("update:modelValue", false)
    }
}

// Keep formData in sync when selectedPerson changes
watch(selectedPerson, (person) => {
    formData.value.selectedPersonId = person?.personId ?? null
})

// Reset state when dialog opens
watch(
    () => props.modelValue,
    (isOpen) => {
        if (isOpen) {
            searchTerm.value = ""
            searchResults.value = []
            selectedPerson.value = null
            errorMessage.value = ""
            formData.value.selectedPersonId = null
            setInitialState()
        }
    },
)

async function onFilter(val: string, update: (fn: () => void) => void, abort: () => void) {
    searchTerm.value = val

    if (val.length < 2) {
        update(() => {
            searchResults.value = []
        })
        return
    }

    if (!props.termCode) {
        abort()
        return
    }

    isSearching.value = true
    try {
        const results = await instructorService.searchPossibleInstructors(props.termCode, val)
        update(() => {
            searchResults.value = results
        })
    } catch {
        update(() => {
            searchResults.value = []
        })
    } finally {
        isSearching.value = false
    }
}

async function addInstructor() {
    if (!selectedPerson.value || !props.termCode) return

    isSaving.value = true
    errorMessage.value = ""

    try {
        const result = await instructorService.createInstructor({
            personId: selectedPerson.value.personId,
            termCode: props.termCode,
        })

        if (result.success) {
            emit("update:modelValue", false)
            emit("created")
        } else {
            errorMessage.value = result.error || "Failed to add instructor"
        }
    } catch {
        errorMessage.value = "An unexpected error occurred"
    } finally {
        isSaving.value = false
    }
}
</script>
