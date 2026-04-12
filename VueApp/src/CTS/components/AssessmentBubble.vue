<script setup lang="ts">
import { computed } from "vue"

const props = withDefaults(
    defineProps<{
        maxValue: number
        value: number
        text?: string
        id?: number
    }>(),
    {
        text: undefined,
        id: undefined,
    },
)

const emit = defineEmits<{
    "bubble-click": [id: number]
}>()

const levelClass = computed(() => {
    if (props.maxValue === 5 && props.value >= 1 && props.value <= 5) {
        return `assessmentBubble--level-${props.value}`
    }
    return ""
})

const displayValue = computed(() => (props.value >= 1 && props.value <= props.maxValue ? props.value : "?"))

const staticLabel = computed(() => `Rating ${displayValue.value} of ${props.maxValue}`)
const clickableLabel = computed(() => `${staticLabel.value}, open details`)

function clickBubble() {
    if (props.id !== undefined) {
        emit("bubble-click", props.id)
    }
}
</script>
<template>
    <button
        v-if="id !== undefined"
        type="button"
        :class="['assessmentBubble', 'assessmentBubble--clickable', levelClass]"
        :aria-label="clickableLabel"
        @click="clickBubble"
    >
        <span aria-hidden="true">{{ displayValue }}</span>
        <q-tooltip
            style="white-space: pre-wrap"
            class="text-body2"
        >
            <strong>Click to open details</strong>
            <br />
            {{ props.text }}
        </q-tooltip>
    </button>
    <span
        v-else
        :class="['assessmentBubble', levelClass]"
        role="img"
        :aria-label="staticLabel"
    >
        <span aria-hidden="true">{{ displayValue }}</span>
    </span>
</template>
<style scoped>
.assessmentBubble {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 1.75rem;
    height: 1.75rem;
    margin: 0 0.125rem;
    border: 0.0625rem solid var(--ucdavis-blue-100);
    border-radius: 50%;
    background-color: var(--ucdavis-blue-70);
    color: white;
    font-weight: 700;
    font-size: 0.875rem;
    line-height: 1;
    padding: 0;
    vertical-align: middle;
}

.assessmentBubble--clickable {
    cursor: pointer;
}

.assessmentBubble--clickable:hover,
.assessmentBubble--clickable:focus {
    filter: brightness(1.1);
}

.assessmentBubble--clickable:focus-visible {
    outline: 0.125rem solid var(--ucdavis-gold-90);
    outline-offset: 0.125rem;
}

.assessmentBubble--level-1 {
    background-color: var(--ucdavis-blue-70);
}

.assessmentBubble--level-2 {
    background-color: var(--ucdavis-blue-80);
}

.assessmentBubble--level-3 {
    background-color: var(--ucdavis-blue-90);
}

.assessmentBubble--level-4 {
    background-color: var(--ucdavis-blue-100);
}

.assessmentBubble--level-5 {
    background-color: #011a38;
}
</style>
