<template>
    <ul class="eis-categories">
        <li
            v-for="category in categories"
            :key="category.label"
            class="eis-category"
        >
            <q-checkbox
                v-if="isManual(category)"
                :model-value="category.applies"
                :label="category.label"
                :disable="!canEdit"
                dense
                @update:model-value="(value: boolean) => emit('change', category.flagCode, value)"
            />
            <template v-else>
                <q-icon
                    :name="category.applies ? 'check_box' : 'check_box_outline_blank'"
                    :color="category.applies ? 'positive' : 'grey-5'"
                    size="sm"
                    aria-hidden="true"
                />
                <span :class="{ 'text-weight-medium': category.applies }">{{ category.label }}</span>
                <span class="sr-only">{{ category.applies ? "(applies)" : "(does not apply)" }}</span>
            </template>
        </li>
    </ul>
</template>

<script setup lang="ts">
import type { EisCategory } from "../types/eis-types"

/**
 * Each appointment category, ticked when it applies. The manual ones are checkboxes, enabled
 * only when the user may change them.
 */
defineProps<{ categories: EisCategory[]; canEdit: boolean }>()

const emit = defineEmits<{ change: [flagCode: number, value: boolean] }>()

/** A category EIS administrators set by hand, which has a dvtFlags code. */
type ManualCategory = EisCategory & { flagCode: number }

function isManual(category: EisCategory): category is ManualCategory {
    return category.flagCode !== null
}
</script>

<style scoped>
.eis-categories {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(18rem, 1fr));
    gap: 0.25rem 1.5rem;
    list-style: none;
    padding: 0;
}

.eis-category {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    min-height: 1.75rem;
}
</style>
