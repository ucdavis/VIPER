<script setup lang="ts">
import { onMounted, computed, ref } from "vue"
import { useRoute, useRouter } from "vue-router"
import StudentRecordPageShell from "@/Students/components/StudentRecordPageShell.vue"
import { CAREER_SELECTION_RECORD_PAGE } from "../constants/record-page"
import { careerSelectionService } from "../services/career-selection-service.ts"
import type { CareerDropdownOption, StudentCareerDetail } from "../types/index.ts"

const route = useRoute()
const router = useRouter()

const personId = computed(() => Number(route.params.pidm))
const loading = ref(false)
const detail = ref<StudentCareerDetail | null>(null)

function handleEdit(): void {
    router.push({ name: "CareerSelectionEdit", params: { pidm: personId.value } })
}

function displayValue(value: string | null | undefined): string {
    return value || "—"
}

/** A choice reads as its own label, unless it is the catch-all, where its free text stands in. */
function choice(option: CareerDropdownOption | null, other: string | null | undefined): string {
    return option?.isOther ? displayValue(other) : displayValue(option?.label)
}

/**
 * The record as label/value pairs, in the order the form asks for them.
 * Both the labels and keys differ from the report DTO's flat structure, so we construct them manually here.
 */
const detailRows = computed<{ label: string; value: string }[]>(() => {
    const info = detail.value?.studentInfo
    if (!info) {
        return []
    }

    return [
        { label: "Career Direction", value: choice(info.direction, info.directionOther) },
        { label: "Primary Focus", value: choice(info.primaryFocus, info.primaryFocusOther) },
        { label: "Secondary Focus", value: choice(info.secondaryFocus, info.secondaryFocusOther) },
        {
            label: "Post-Graduation Plans",
            // This section has no free-text field of its own.
            value: info.postGrad?.isOther ? "Other - See short-term plans" : displayValue(info.postGrad?.label),
        },
        { label: "Mentoring Faculty", value: displayValue(info.mentorName) },
        { label: "Short Term Plans", value: displayValue(info.shortTermPlans) },
        { label: "Long Term Plans", value: displayValue(info.longTermPlans) },
    ]
})

async function load(): Promise<void> {
    loading.value = true
    detail.value = await careerSelectionService.getDetail(personId.value)
    loading.value = false
}

onMounted(() => {
    if (personId.value) {
        load()
    }
})
</script>

<template>
    <StudentRecordPageShell
        v-bind="CAREER_SELECTION_RECORD_PAGE"
        :loading="loading"
        :detail="detail"
        :can-view-list="detail?.canViewStudentList ?? false"
    >
        <template v-if="detail">
            <div class="row items-center q-mb-md">
                <h1 class="q-ma-none">Career Selection: {{ detail.fullName }}</h1>
                <q-btn
                    v-if="detail.canEdit"
                    flat
                    dense
                    no-caps
                    icon="edit"
                    label="Edit"
                    class="q-ml-sm"
                    color="primary"
                    @click="handleEdit"
                />
            </div>

            <div class="form-content">
                <q-card
                    flat
                    bordered
                    class="q-mb-md"
                >
                    <q-card-section>
                        <dl class="detail-list q-ma-none">
                            <template
                                v-for="row in detailRows"
                                :key="row.label"
                            >
                                <dt class="text-caption text-grey-7">{{ row.label }}</dt>
                                <dd class="text-body2 detail-value">{{ row.value }}</dd>
                            </template>
                        </dl>
                    </q-card-section>
                </q-card>

                <div
                    v-if="detail.lastUpdated"
                    class="text-caption text-grey q-mb-md"
                >
                    Last updated: {{ new Date(detail.lastUpdated).toLocaleString() }}
                </div>
            </div>
        </template>
    </StudentRecordPageShell>
</template>

<style scoped>
.form-content {
    max-width: 56rem;
}

/* Browsers indent dd by default. The pairs stack flush, spaced as the captioned rows they
   replaced were by q-col-gutter-sm. */
.detail-list dd {
    margin: 0 0 0.5rem;
}

.detail-list dd:last-child {
    margin-bottom: 0;
}

/* The plans are free text, often in paragraphs; keep their line breaks. Keep the value on the
   same line as its <dd>, or pre-line renders the template's own newline as a blank line. */
.detail-list .detail-value {
    white-space: pre-line;
}
</style>
