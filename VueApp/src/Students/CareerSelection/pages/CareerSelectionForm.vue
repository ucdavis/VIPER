<script setup lang="ts">
// Template-size synthetic complexity only; the form's data logic lives in useCareerSelection,
// and the field, completeness and record-page concerns each in their own module.
// fallow-ignore-file complexity
import { ref, onMounted, computed, nextTick, type Ref } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useQuasar } from "quasar"
import type { QSelect } from "quasar"
import StatusBanner from "@/components/StatusBanner.vue"
import StudentRecordPageShell from "@/Students/components/StudentRecordPageShell.vue"
import CareerSelectionSelectWithOther from "../components/CareerSelectionSelectWithOther.vue"
import MentorSelector from "../components/MentorSelector.vue"
import { useCareerSelection } from "../composables/use-career-selection.ts"
import { careerSelectionService } from "../services/career-selection-service.ts"
import { checkHasOnePermission } from "@/composables/CheckPagePermission"
import { useConfirmLeave } from "@/composables/use-confirm-leave"
import { useSelectAriaLabel } from "@/composables/use-select-aria-label"
import { CAREER_SELECTION_PERMISSIONS } from "../constants/permissions"
import { CAREER_SELECTION_RECORD_PAGE } from "../constants/record-page"
import { missingFieldLabels } from "../utils/career-completeness"
import "@/styles/compact-form.css"
import type { CareerDropdownOption } from "../types/index.ts"

const route = useRoute()
const router = useRouter()
const $q = useQuasar()
const formRef = ref<{ validate: () => Promise<boolean> } | null>(null)
// The post-grad dropdown is named by the question above it, the same as the three
// CareerSelectionSelectWithOther fields; QSelect needs that wired onto its combobox target.
const postGradRef = ref<QSelect | null>(null)
useSelectAriaLabel(postGradRef, "career-post-grad-label")

const personId = computed(() => Number(route.params.pidm))

const { loading, saving, detail, saveErrors, studentInfo, isDirty, loadDetail, save } = useCareerSelection()

const canEdit = computed(() => detail.value?.canEdit ?? false)
const isReadOnly = computed(() => !canEdit.value)
const isAdmin = computed(() => checkHasOnePermission([CAREER_SELECTION_PERMISSIONS.ADMIN]))

const mentor = computed({
    get: () =>
        studentInfo.value.mentorId
            ? {
                  iamId: studentInfo.value.mentorIamId ?? "",
                  personId: studentInfo.value.mentorId,
                  fullName: studentInfo.value.mentorName ?? null,
              }
            : null,
    set: (value) => {
        studentInfo.value.mentorId = value?.personId ?? null
        studentInfo.value.mentorName = value?.fullName ?? ""
        studentInfo.value.mentorIamId = value?.iamId ?? null
    },
})

const careerDirectionOptions = ref([]) as Ref<CareerDropdownOption[]>
const focusOptions = ref([]) as Ref<CareerDropdownOption[]>
const postGradOptions = ref([]) as Ref<CareerDropdownOption[]>

const missingFields = computed(() => missingFieldLabels(studentInfo.value))

// loadDetail clears its own loading flag once the record arrives, but the dropdown options load
// alongside it; the form stays hidden until both have, so a choice is never shown without them.
const initializing = ref(true)

async function handleSave(): Promise<void> {
    if (!isDirty.value) {
        $q.notify({ type: "info", message: "No changes to save." })
        return
    }
    if (formRef.value) {
        const valid = await formRef.value.validate()
        if (!valid) {
            // Show the first error field and put the caret in it, so it is reached by keyboard
            // and announced, rather than only scrolled past.
            await nextTick()
            const firstError = document.querySelector<HTMLElement>(".career-selection-form .q-field--error")
            if (firstError) {
                firstError.scrollIntoView({ behavior: "smooth", block: "center" })
                // The error class sits on the field wrapper; the control inside is what takes
                // focus. preventScroll leaves the smooth scroll above to do the moving.
                firstError
                    .querySelector<HTMLElement>("input, textarea, [role='combobox']")
                    ?.focus({ preventScroll: true })
            }
            $q.notify({ type: "negative", message: "Please fix the highlighted errors before saving." })
            return
        }
    }
    const success = await save(personId.value)
    if (success) {
        saveErrors.value = []
        $q.notify({ type: "positive", message: "Career selection information saved." })
        leaveForm()
    }
}

// Returns to the roster for those who can see it, otherwise to the student's read-only record.
function leaveForm(): void {
    if (detail.value?.canViewStudentList) {
        router.push({ name: "CareerSelectionList" })
    } else {
        router.push({ name: "CareerSelectionView", params: { pidm: personId.value } })
    }
}

async function initForm(): Promise<void> {
    try {
        if (personId.value) {
            const [careerOptions, speciesOpts, postGradOpts] = await Promise.all([
                careerSelectionService.getDropdownOptions("career"),
                careerSelectionService.getDropdownOptions("species"),
                careerSelectionService.getDropdownOptions("postGrad"),
                loadDetail(personId.value),
            ])
            careerDirectionOptions.value = careerOptions
            focusOptions.value = speciesOpts
            postGradOptions.value = postGradOpts
            // Students without edit access should see the read-only view page instead.
            if (detail.value && !detail.value.canEdit && !detail.value.canViewStudentList) {
                router.replace({ name: "CareerSelectionView", params: { pidm: personId.value } })
                return
            }
        }
    } finally {
        initializing.value = false
    }
}

onMounted(() => {
    initForm()
})

useConfirmLeave(isDirty)
</script>

<template>
    <StudentRecordPageShell
        v-bind="CAREER_SELECTION_RECORD_PAGE"
        :loading="loading || initializing"
        :detail="detail"
        :can-view-list="detail?.canViewStudentList ?? false"
    >
        <template v-if="detail">
            <h1 class="q-ma-none q-mb-md">Career Selection: {{ detail.fullName }}</h1>

            <StatusBanner
                v-if="missingFields.length > 0"
                type="warning"
            >
                <div class="text-weight-bold q-mb-xs">Missing or incomplete information:</div>
                <ul class="q-ma-none q-pl-md">
                    <li
                        v-for="field in missingFields"
                        :key="field"
                    >
                        {{ field }}
                    </li>
                </ul>
            </StatusBanner>

            <div class="form-content text-body2 text-grey-8 q-mb-md">
                One role of the Clinical Education Committee (CEC) is to help provide you with the best guidance
                possible when you plan selections for your junior year emphasis, senior year clinical rotations, and
                externships. To help us do this the CEC needs information about your current career interests so that we
                can pair you with the most appropriate CEC advisor. Please answer the following questions based on your
                current plans. We understand that your plans may change. If they do you will have another opportunity to
                update this information during fall semester of junior year. Responses from this survey will become part
                of the information that is provided to the faculty on CEC when they review student clinical schedule
                selections in fall of Year 3.
            </div>

            <q-form
                ref="formRef"
                class="career-selection-form compact-form form-content"
            >
                <!-- Student Career Information -->
                <q-card
                    flat
                    bordered
                    class="q-mb-md"
                >
                    <CareerSelectionSelectWithOther
                        label="What is your current career direction?"
                        :options="careerDirectionOptions"
                        :read-only="isReadOnly"
                        v-model:select-model="studentInfo.direction"
                        v-model:other-model="studentInfo.directionOther"
                        clearable
                        empty-label="None Selected"
                    />

                    <CareerSelectionSelectWithOther
                        label="What is your current primary species/career focus?"
                        :options="focusOptions"
                        :read-only="isReadOnly"
                        v-model:select-model="studentInfo.primaryFocus"
                        v-model:other-model="studentInfo.primaryFocusOther"
                        clearable
                        empty-label="None Selected"
                    />

                    <CareerSelectionSelectWithOther
                        label="What is your current secondary species/career focus?"
                        :options="focusOptions"
                        :read-only="isReadOnly"
                        v-model:select-model="studentInfo.secondaryFocus"
                        v-model:other-model="studentInfo.secondaryFocusOther"
                        clearable
                        empty-label="None Selected"
                    />

                    <q-card-section>
                        <div
                            id="career-post-grad-label"
                            class="q-mb-xs"
                        >
                            What are your current postgraduate plans (first 1-5 years post-graduation)?
                        </div>
                        <div class="row q-col-gutter-sm">
                            <div class="col-12 col-sm-6">
                                <q-select
                                    ref="postGradRef"
                                    v-model="studentInfo.postGrad"
                                    :options="postGradOptions"
                                    dense
                                    options-dense
                                    outlined
                                    :readonly="isReadOnly"
                                    clearable
                                    :display-value="studentInfo.postGrad?.label ?? 'None Selected'"
                                />
                            </div>
                            <!-- Mounted whether or not the catch-all is selected: a live region
                                 inserted at the same moment as its text is not reliably
                                 announced, so only the text inside it toggles. -->
                            <div
                                class="col-12 col-sm-6 q-mt-sm"
                                role="status"
                            >
                                <span v-if="studentInfo.postGrad?.isOther"
                                    >Please explain in the short-term career section below</span
                                >
                            </div>
                        </div>
                    </q-card-section>

                    <q-card-section v-if="isAdmin">
                        <div class="row q-col-gutter-sm">
                            <div class="col-12 col-sm-6">
                                <MentorSelector
                                    v-model="mentor"
                                    label="Mentoring Faculty"
                                />
                            </div>
                        </div>
                    </q-card-section>
                    <q-card-section v-else-if="studentInfo.mentorName">
                        Mentoring Faculty: {{ studentInfo.mentorName }}
                    </q-card-section>

                    <q-card-section>
                        <div
                            id="career-short-term-label"
                            class="q-mb-xs"
                        >
                            Please describe your current short-term (next 5 years) career plans.
                        </div>
                        <div class="row q-col-gutter-sm">
                            <div class="col-12 col-sm-12">
                                <q-input
                                    v-model="studentInfo.shortTermPlans"
                                    aria-labelledby="career-short-term-label"
                                    outlined
                                    dense
                                    autogrow
                                    type="textarea"
                                    :readonly="isReadOnly"
                                    counter
                                    maxlength="5000"
                                    :rules="[
                                        (val) =>
                                            (val ?? '').length <= 5000 ||
                                            'Short Term Statement must be 5000 characters or fewer.',
                                    ]"
                                ></q-input>
                            </div>
                        </div>
                    </q-card-section>

                    <q-card-section>
                        <div
                            id="career-long-term-label"
                            class="q-mb-xs"
                        >
                            Please describe your current long-term (next 15-20 years) career plans.
                        </div>
                        <div class="row q-col-gutter-sm">
                            <div class="col-12 col-sm-12">
                                <q-input
                                    v-model="studentInfo.longTermPlans"
                                    aria-labelledby="career-long-term-label"
                                    outlined
                                    dense
                                    autogrow
                                    type="textarea"
                                    :readonly="isReadOnly"
                                    counter
                                    maxlength="5000"
                                    :rules="[
                                        (val) =>
                                            (val ?? '').length <= 5000 ||
                                            'Long Term Statement must be 5000 characters or fewer.',
                                    ]"
                                ></q-input>
                            </div>
                        </div>
                    </q-card-section>
                </q-card>

                <div
                    v-if="detail.lastUpdated"
                    class="text-caption text-grey q-mb-md"
                >
                    Last updated: {{ new Date(detail.lastUpdated).toLocaleString() }}
                </div>

                <StatusBanner
                    v-if="saveErrors.length > 0"
                    type="error"
                >
                    <div
                        v-for="(error, idx) in saveErrors"
                        :key="idx"
                    >
                        {{ error }}
                    </div>
                </StatusBanner>

                <div
                    v-if="canEdit"
                    class="row q-gutter-sm q-mt-md"
                >
                    <q-btn
                        color="primary"
                        label="Save"
                        no-caps
                        :loading="saving"
                        @click="handleSave"
                    >
                        <template #loading>
                            <q-spinner
                                size="1em"
                                class="q-mr-sm"
                            />
                            Save
                        </template>
                    </q-btn>
                    <q-btn
                        flat
                        no-caps
                        label="Cancel"
                        @click="leaveForm"
                    />
                </div>
            </q-form>
        </template>
    </StudentRecordPageShell>
</template>

<style scoped>
.form-content {
    max-width: 56rem;
}
</style>
