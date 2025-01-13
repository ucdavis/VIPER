<script setup lang="ts">
    import { defineProps, defineEmits, ref, watch } from 'vue'

    const props = defineProps({
        maxValue: {
            type: Number,
            required: true
        },
        value: {
            type: Number,
            required: true
        },
        text: {
            type: String
        },
        type: {
            type: String,
            default: "bubble"
        },
        id: {
            type: Number,
            required: false
        },
    })

    const emit = defineEmits(["bubbleClick"])

    const classes5 = ["assessmentBubble5_1", "assessmentBubble5_2", "assessmentBubble5_3", "assessmentBubble5_4", "assessmentBubble5_5"]
    const classes5_closer = ["assessmentBubbleCloser5_1", "assessmentBubbleCloser5_2", "assessmentBubbleCloser5_3", "assessmentBubbleCloser5_4", "assessmentBubbleCloser5_5"]
    const clockIcons5 = ["sym_o_clock_loader_10", "sym_o_clock_loader_40", "sym_o_clock_loader_60", "sym_o_clock_loader_80", "circle"]
    const barIcons5 = ["horizontal_rule", "density_large", "density_medium", "desnity_small", "format_align_justify"]
    
    const bubbleClass = ref("")
    const bubbleIcon = ref("")

    watch(props, () => {
        setBubbleAttrs()
    })

    function clickBubble() {
        if (props.id !== undefined) {
            emit("bubbleClick", props.id)
        }
    }

    function setBubbleAttrs() {
        if (props.maxValue == 5 && props.value <= 5 && props.value > 0) {
            switch(props.type) {
                case "clock":
                    bubbleIcon.value = clockIcons5[props.value - 1]
                    bubbleClass.value = classes5_closer[props.value - 1]
                    break;
                case "bar":
                    bubbleIcon.value = barIcons5[props.value - 1]
                    bubbleClass.value = classes5_closer[props.value - 1]
                    break;
                case "bubble":
                default:
                    bubbleIcon.value = "circle"
                    bubbleClass.value = classes5[props.value - 1]
                    break;

            }
        }
    }

    setBubbleAttrs()
</script>
<template>
    <q-icon :name="bubbleIcon" size="sm" :class="'assessmentIcon cursor-pointer ' + bubbleClass" @click="clickBubble">
        <q-tooltip style="white-space:pre-wrap;" class="text-body2">
            <strong>Click to open details</strong>
            <br />
            {{ props.text }}
        </q-tooltip>
    </q-icon>
</template>
