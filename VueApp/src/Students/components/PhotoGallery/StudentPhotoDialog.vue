<template>
    <q-dialog
        v-model="show"
        no-backdrop-dismiss
        @keydown="handleKeydown"
    >
        <q-card :style="cardStyle">
            <q-card-section class="q-px-none">
                <div
                    class="row items-center justify-center q-gutter-xs"
                    style="flex-wrap: nowrap"
                >
                    <q-btn
                        flat
                        dense
                        round
                        icon="chevron_left"
                        color="primary"
                        size="lg"
                        :disable="!hasPrevious"
                        @click="goToPrevious"
                        aria-label="Previous student"
                    />
                    <q-img
                        :src="currentPhotoUrl"
                        :ratio="3 / 4"
                        fit="contain"
                        :style="photoStyle"
                    >
                        <template #error>
                            <div class="absolute-full flex flex-center bg-grey-3">
                                <q-icon
                                    name="person"
                                    size="100px"
                                    color="grey-6"
                                />
                            </div>
                        </template>
                    </q-img>
                    <q-btn
                        flat
                        dense
                        round
                        icon="chevron_right"
                        color="primary"
                        size="lg"
                        :disable="!hasNext"
                        @click="goToNext"
                        aria-label="Next student"
                    />
                </div>
            </q-card-section>

            <q-card-section
                v-if="currentStudent"
                class="q-pt-sm"
            >
                <div class="row q-mb-xs">
                    <div
                        class="col-auto text-weight-bold"
                        style="min-width: 90px"
                    >
                        Name:
                    </div>
                    <div class="col">{{ currentStudent.fullName }}</div>
                </div>
                <div class="row q-mb-xs">
                    <div
                        class="col-auto text-weight-bold"
                        style="min-width: 90px"
                    >
                        Email:
                    </div>
                    <div class="col">{{ currentStudent.mailId }}@ucdavis.edu</div>
                </div>
                <div
                    v-if="currentClassYear"
                    class="row q-mb-xs"
                >
                    <div
                        class="col-auto text-weight-bold"
                        style="min-width: 90px"
                    >
                        Class Year:
                    </div>
                    <div class="col">{{ currentClassYear }}</div>
                </div>
                <div
                    v-if="currentStudent.eighthsGroup"
                    class="row q-mb-xs"
                >
                    <div
                        class="col-auto text-weight-bold"
                        style="min-width: 90px"
                    >
                        Eighths:
                    </div>
                    <div class="col">{{ currentStudent.eighthsGroup }}</div>
                </div>
                <div
                    v-if="currentStudent.twentiethsGroup"
                    class="row q-mb-xs"
                >
                    <div
                        class="col-auto text-weight-bold"
                        style="min-width: 90px"
                    >
                        Twentieths:
                    </div>
                    <div class="col">{{ currentStudent.twentiethsGroup }}</div>
                </div>
                <div
                    v-if="currentStudent.teamNumber"
                    class="row q-mb-xs"
                >
                    <div
                        class="col-auto text-weight-bold"
                        style="min-width: 90px"
                    >
                        Team:
                    </div>
                    <div class="col">{{ currentStudent.teamNumber }}</div>
                </div>
                <div
                    v-if="currentStudent.v3SpecialtyGroup"
                    class="row q-mb-xs"
                >
                    <div
                        class="col-auto text-weight-bold"
                        style="min-width: 90px"
                    >
                        Stream:
                    </div>
                    <div class="col">{{ currentStudent.v3SpecialtyGroup }}</div>
                </div>
                <div
                    v-if="currentStudent.isRossStudent"
                    class="row q-mt-sm text-center"
                >
                    <div class="col">
                        <q-badge
                            color="primary"
                            class="text-weight-bold"
                        >
                            Ross Student
                        </q-badge>
                    </div>
                </div>
            </q-card-section>

            <q-card-actions align="right">
                <q-btn
                    flat
                    label="Close"
                    color="primary"
                    v-close-popup
                />
            </q-card-actions>
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue"
import { useQuasar } from "quasar"
import type { StudentPhoto } from "../../services/photo-gallery-service"
import { getPhotoUrl } from "../../composables/use-photo-url"

const props = defineProps<{
    modelValue: boolean
    students: StudentPhoto[]
    initialIndex: number
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    "update:index": [index: number]
}>()

const $q = useQuasar()
const dialogIndex = ref(props.initialIndex)

const show = computed({
    get: () => props.modelValue,
    set: (value: boolean) => {
        emit("update:modelValue", value)
    },
})

watch(
    () => props.initialIndex,
    (newIndex) => {
        dialogIndex.value = newIndex
    },
)

const currentStudent = computed(() => {
    if (dialogIndex.value >= 0 && dialogIndex.value < props.students.length) {
        return props.students[dialogIndex.value]
    }
    return props.students[0] || undefined
})

// Calculate graduation year from class level
const calculateGradYear = (classLevel: string | null): number | null => {
    if (!classLevel || !classLevel.startsWith("V")) return null

    const dvmYear = Number.parseInt(classLevel.slice(1))
    if (Number.isNaN(dvmYear) || dvmYear < 1 || dvmYear > 4) return null

    // Get current term code (YYYYMM format)
    const now = new Date()
    const year = now.getFullYear()
    const month = now.getMonth() + 1
    let term: number
    if (month >= 1 && month <= 5) {
        term = 2 // Spring
    } else if (month >= 6 && month <= 8) {
        term = 4 // Summer
    } else {
        term = 9 // Fall
    }

    // Formula from GradYearClassLevel.GetGradYear
    // term 2 or 4 or 9: year + (5 - dvmYear) - (term == 2 ? 1 : 0)
    return year + (5 - dvmYear) - (term === 2 ? 1 : 0)
}

const currentClassYear = computed(() => {
    return currentStudent.value?.classLevel ? calculateGradYear(currentStudent.value.classLevel) : null
})

// Emit index changes to parent (for URL updates during arrow key navigation)
watch(dialogIndex, (newIndex) => {
    emit("update:index", newIndex)
})

const currentPhotoUrl = computed(() => (currentStudent.value ? getPhotoUrl(currentStudent.value) : ""))

const hasPrevious = computed(() => dialogIndex.value > 0)
const hasNext = computed(() => dialogIndex.value < props.students.length - 1)

const cardStyle = computed(() => {
    if ($q.screen.lt.sm) {
        // Mobile: < 600px
        return { minWidth: "90vw", maxWidth: "90vw", width: "90vw" }
    }
    // Desktop: >= 600px
    return { minWidth: "400px", maxWidth: "600px" }
})

const photoStyle = computed(() => {
    if ($q.screen.lt.sm) {
        // Mobile: < 600px
        return { width: "60vw", maxWidth: "250px", flexShrink: 0 }
    }
    // Desktop: >= 600px
    return { width: "350px", flexShrink: 0 }
})

const goToPrevious = () => {
    if (hasPrevious.value) {
        dialogIndex.value--
    }
}

const goToNext = () => {
    if (hasNext.value) {
        dialogIndex.value++
    }
}

const handleKeydown = (event: KeyboardEvent) => {
    if (event.key === "ArrowLeft") {
        event.preventDefault()
        goToPrevious()
    } else if (event.key === "ArrowRight") {
        event.preventDefault()
        goToNext()
    } else if (event.key === "Escape") {
        event.preventDefault()
        show.value = false
    }
}
</script>
