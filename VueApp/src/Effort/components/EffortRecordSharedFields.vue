<script setup lang="ts">
import StatusBanner from "@/components/StatusBanner.vue"
import type { RoleOptionDto } from "../types"
import { effortValueRules, requiredRule } from "../validation"

defineProps<{
    roles: RoleOptionDto[]
    isLoadingOptions: boolean
    effortLabel: string
    showNotes: boolean
    notesHint: string
    warningMessage?: string
    errorMessage?: string
}>()

const selectedRole = defineModel<number | null>("selectedRole", { required: true })
const effortValue = defineModel<number | null>("effortValue", { required: true })
const notes = defineModel<string | null>("notes", { required: true })
</script>

<template>
    <!-- Role Selection -->
    <q-select
        v-model="selectedRole"
        :options="roles"
        label="Role *"
        dense
        options-dense
        outlined
        option-value="id"
        option-label="description"
        emit-value
        map-options
        :loading="isLoadingOptions"
        :rules="[requiredRule('Role')]"
        lazy-rules="ondemand"
    />

    <!-- Effort Value -->
    <q-input
        v-model.number="effortValue"
        :label="effortLabel"
        type="number"
        dense
        outlined
        min="0"
        :rules="effortValueRules"
        lazy-rules="ondemand"
    />

    <!-- Notes (generic R-Course only) -->
    <q-input
        v-if="showNotes"
        v-model="notes"
        label="Notes"
        type="textarea"
        dense
        outlined
        maxlength="500"
        counter
        autogrow
        :hint="notesHint"
    />

    <!-- Warning Message -->
    <StatusBanner
        v-if="warningMessage"
        type="warning"
    >
        {{ warningMessage }}
    </StatusBanner>

    <!-- Error Message -->
    <StatusBanner
        v-if="errorMessage"
        type="error"
    >
        {{ errorMessage }}
    </StatusBanner>
</template>
