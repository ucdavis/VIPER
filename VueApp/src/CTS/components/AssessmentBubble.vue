<script setup lang="ts">
import { ref, watch } from "vue"

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

const classes5 = [
    "assessmentBubble5_1",
    "assessmentBubble5_2",
    "assessmentBubble5_3",
    "assessmentBubble5_4",
    "assessmentBubble5_5",
]

const bubbleClass = ref("")

watch(props, () => {
    setBubbleAttrs()
})

function clickBubble() {
    if (props.id !== undefined) {
        emit("bubble-click", props.id)
    }
}

function setBubbleAttrs() {
    if (props.maxValue === 5 && props.value <= 5 && props.value > 0) {
        const index = props.value - 1
        bubbleClass.value = classes5[index] ?? ""
    }
}

setBubbleAttrs()
</script>
<template>
    <q-icon
        name="circle"
        size="sm"
        :class="'assessmentIcon cursor-pointer ' + bubbleClass"
        @click="clickBubble"
    >
        <q-tooltip
            style="white-space: pre-wrap"
            class="text-body2"
        >
            <strong>Click to open details</strong>
            <br />
            {{ props.text }}
        </q-tooltip>
    </q-icon>
</template>
