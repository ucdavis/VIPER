<script setup lang="ts">
import { computed } from "vue"

const props = withDefaults(
    defineProps<{
        maxValue: number
        value: number
        levelName?: string
        text?: string
        id?: number
    }>(),
    {
        levelName: "",
        text: undefined,
        id: undefined,
    },
)

const emit = defineEmits<{
    "bubble-click": [id: number]
}>()

const classes5 = [
    "assessmentBubble5_1",
    "assessmentBubble5_2",
    "assessmentBubble5_3",
    "assessmentBubble5_4",
    "assessmentBubble5_5",
]

const isValidValue = computed(() => props.maxValue === 5 && props.value > 0 && props.value <= 5)

const bubbleClasses = computed(() => {
    const classes: string[] = ["assessmentBubble"]
    if (isValidValue.value) classes.push(classes5[props.value - 1] ?? "")
    return classes
})

// Decorative span variant: when no levelName is provided the bubble carries no
// meaning for assistive tech, so drop the role="img" + empty aria-label and
// treat it as purely decorative (aria-hidden). The clickable variant always
// has a meaningful action label so it never falls through to this branch.
const spanIsDecorative = computed(() => props.levelName.length === 0)

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
            :class="bubbleClasses"
            aria-hidden="true"
        />
        <q-tooltip class="text-body2">
            <div><strong>Click to open details</strong></div>
            <div class="assessmentBubbleTooltipText">{{ props.text }}</div>
        </q-tooltip>
    </button>
    <span
        v-else-if="spanIsDecorative"
        :class="bubbleClasses"
        aria-hidden="true"
    />
    <span
        v-else
        :class="bubbleClasses"
        role="img"
        :aria-label="levelName"
    />
</template>
