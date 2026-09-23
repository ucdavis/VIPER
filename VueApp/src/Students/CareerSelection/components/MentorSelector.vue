<template>
    <PersonSearchSelect
        :model-value="modelValue"
        :label="label"
        :search="careerSelectionService.searchMentors"
        :option-label="(person) => person.fullName"
        outlined
        @update:model-value="emitSelection"
    >
        <!-- The search is capped and matches on name. mailId distinguishes between similar names. -->
        <template #option-caption="{ opt }">{{ opt.mailId ?? opt.loginId ?? opt.iamId }}</template>
    </PersonSearchSelect>
</template>

<script setup lang="ts">
import PersonSearchSelect from "@/components/PersonSearchSelect.vue"
import { careerSelectionService } from "../services/career-selection-service"
import type { MentorOption } from "../types"

type SelectedMentor = { iamId: string; personId: number | null; fullName: string | null }

defineProps<{
    modelValue: SelectedMentor | null
    label: string
}>()

const emit = defineEmits<{ "update:modelValue": [value: SelectedMentor | null] }>()

function emitSelection(value: MentorOption | MentorOption[] | null) {
    emit("update:modelValue", (value as MentorOption | null) ?? null)
}
</script>
