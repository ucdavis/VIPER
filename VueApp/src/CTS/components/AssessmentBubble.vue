<script setup lang="ts">
import { computed } from "vue"

const props = defineProps({
    maxValue: {
        type: Number,
        required: true,
    },
    value: {
        type: Number,
        required: true,
    },
    levelName: {
        type: String,
        default: "",
    },
    text: {
        type: String,
        default: undefined,
    },
    id: {
        type: Number,
        default: undefined,
    },
})

const emit = defineEmits(["bubble-click"])

const classes5 = [
    "assessmentBubble5_1",
    "assessmentBubble5_2",
    "assessmentBubble5_3",
    "assessmentBubble5_4",
    "assessmentBubble5_5",
]

const isValidValue = computed(() => props.maxValue === 5 && props.value > 0 && props.value <= 5)

const bubbleClass = computed(() => (isValidValue.value ? (classes5[props.value - 1] ?? "") : ""))

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
        class="assessmentBubbleTrigger"
        :aria-label="levelName ? `${levelName}, open assessment details` : 'Open assessment details'"
        @click="clickBubble"
    >
        <span
            :class="['assessmentBubble', bubbleClass]"
            aria-hidden="true"
        />
        <q-tooltip class="text-body2">
            <div><strong>Click to open details</strong></div>
            <div class="assessmentBubbleTooltipText">{{ props.text }}</div>
        </q-tooltip>
    </button>
    <span
        v-else
        :class="['assessmentBubble', bubbleClass]"
        role="img"
        :aria-label="levelName"
    />
</template>
