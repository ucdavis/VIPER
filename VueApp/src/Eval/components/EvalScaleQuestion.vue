<template>
    <q-card
        flat
        bordered
        class="q-mb-sm overflow-hidden"
    >
        <div class="question-text q-px-sm q-py-xs">
            <div class="text-weight-medium">{{ questionText }}</div>
            <ul
                v-if="iscs && iscs.length"
                class="isc-list"
            >
                <li
                    v-for="isc in iscs"
                    :key="isc.name"
                >
                    {{ isc.name }}
                </li>
            </ul>
        </div>
        <div class="scale-row">
            <div
                v-for="seg in SEGMENTS"
                :key="seg.key"
                class="scale-seg"
                :style="{ flex: seg.flex }"
            >
                <div :class="`scale-head scale-head--${seg.key}`">
                    <span>{{ seg.label }}</span>
                    <q-icon
                        v-if="levelDescription(seg.levelId)"
                        name="info"
                        class="scale-info-icon"
                    >
                        <q-tooltip
                            anchor="top middle"
                            self="bottom middle"
                            max-width="320px"
                            class="scale-tooltip"
                        >
                            <!-- Description is sanitized server-side by MilestonesController via HtmlSanitizerService -->
                            <!-- eslint-disable-next-line vue/no-v-html -->
                            <div v-html="levelDescription(seg.levelId)" />
                        </q-tooltip>
                    </q-icon>
                </div>
                <div :class="`scale-opts scale-opts--${seg.key}`">
                    <label
                        v-for="val in seg.values"
                        :key="val"
                        class="scale-opt"
                        :for="`q_${questionId}_${val}`"
                    >
                        <span class="scale-num">{{ val }}</span>
                        <input
                            :id="`q_${questionId}_${val}`"
                            type="radio"
                            :name="`q_${questionId}`"
                            :value="val"
                            v-model="model"
                        />
                    </label>
                </div>
            </div>
        </div>
    </q-card>
</template>

<script setup lang="ts">
import { computed } from "vue"
import type { MilestoneLevel } from "@/CTS/types"

const props = defineProps<{
    questionId: number | string
    questionText: string
    modelValue: number | null
    levels: MilestoneLevel[]
    iscs?: { name: string }[]
}>()

const emit = defineEmits<{
    "update:modelValue": [value: number | null]
}>()

const model = computed({
    get: () => props.modelValue,
    set: (val) => emit("update:modelValue", val),
})

function levelDescription(levelId: number) {
    return props.levels.find((l) => l.levelId === levelId)?.description?.trim() || null
}

const SEGMENTS = [
    { key: "pre", levelId: 25, label: "Pre-Nov.", flex: 1, values: [1] },
    { key: "nov", levelId: 26, label: "Novice", flex: 6, values: [2, 3, 4, 5, 6, 7] },
    { key: "adv", levelId: 27, label: "Advanced Beginner", flex: 6, values: [8, 9, 10, 11, 12, 13] },
    { key: "comp", levelId: 28, label: "Competent", flex: 6, values: [14, 15, 16, 17, 18, 19] },
    { key: "prof", levelId: 29, label: "Prof.", flex: 1, values: [20] },
]
</script>

<style scoped>
.question-text {
    background: #deeaf3; /* var(--ucdavis-blue-10); */
    border-bottom: 1px solid var(--ucdavis-blue-20);
    font-size: 0.85rem;
    line-height: 1.45;
    color: #1a3a52; /* var(--ucdavis-blue-100); */
}

.isc-list {
    margin: 4px 0 0;
    padding-left: 1.25rem;
    font-size: 0.8rem;
    font-weight: 400;
}

/*
 .q-text {
    padding: 9px 14px;
    font-weight: 600;
    font-size: 13px;
    line-height: 1.45;
    background: #deeaf3;
    border-bottom: 1px solid #9bbfd4;
    color: #1a3a52;
  }
*/

.scale-row {
    display: flex;
    width: 100%;
}

.scale-seg {
    display: flex;
    flex-direction: column;
    border-right: 1px solid rgb(255 255 255 / 35%);
    min-width: 0;
}

.scale-seg:last-child {
    border-right: none;
}

.scale-head {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 3px;
    text-align: center;
    font-size: 0.8rem;
    font-weight: 700;
    padding: 3px 2px;
    border-bottom: 1px solid rgb(255 255 255 / 30%);
    white-space: nowrap;
    overflow: hidden;
    line-height: 1.2;
}

.scale-head span {
    overflow: hidden;
    text-overflow: ellipsis;
}

.scale-info-icon {
    cursor: help;
    flex-shrink: 0;
}

.scale-opts {
    display: flex;
    flex-direction: row;
    justify-content: center;
    align-items: center;
    flex: 1;
    padding: 4px 2px;
}

.scale-opt {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 1px;
    cursor: pointer;
    padding: 3px 4px;
    border-radius: 3px;
    flex: 1;
    min-width: 24px;
}

.scale-opt:hover, .scale-opt:focus {
    background: rgb(0 0 0 / 9%);
}

.scale-num {
    font-size: 0.8rem;
    font-weight: 700;
    line-height: 1;
}

input[type="radio"] {
    margin: 0;
    cursor: pointer;
    width: 13px;
    height: 13px;
}

/* Segment header colors — UC Davis blue palette 
.scale-head--pre {
    background: var(--ucdavis-blue-10);
    color: var(--ucdavis-blue-100);
}
.scale-head--nov {
    background: var(--ucdavis-blue-30);
    color: var(--ucdavis-blue-100);
}
.scale-head--adv {
    background: var(--ucdavis-blue-60);
    color: #fff;
}
.scale-head--comp {
    background: var(--ucdavis-blue-80);
    color: #fff;
}
.scale-head--prof {
    background: var(--ucdavis-blue-100);
    color: #fff;
}
*/

.scale-head--pre {
    background: #afd3e8;
}

.scale-head--nov {
    background: #6baed6;
    color: #1a3a52;
}

.scale-head--adv {
    background: #2077b0;
    color: #fff;
}

.scale-head--comp {
    background: #1a5f8a;
    color: #fff;
}

.scale-head--prof {
    background: #0d3557;
    color: #fff;
}

/* Segment body background — tinted from each blue 
.scale-opts--pre {
    background: color-mix(in srgb, var(--ucdavis-blue-10) 40%, white);
}
.scale-opts--nov {
    background: color-mix(in srgb, var(--ucdavis-blue-20) 60%, white);
}
.scale-opts--adv {
    background: color-mix(in srgb, var(--ucdavis-blue-30) 60%, white);
}
.scale-opts--comp {
    background: color-mix(in srgb, var(--ucdavis-blue-50) 40%, white);
}
.scale-opts--prof {
    background: color-mix(in srgb, var(--ucdavis-blue-70) 40%, white);
} */

.scale-opts--pre {
    background: #eef6fb;
}

.scale-opts--nov {
    background: #d4eaf7;
}

.scale-opts--adv {
    background: #b3d5eb;
}

.scale-opts--comp {
    background: #7fb3d3;
}

.scale-opts--prof {
    background: #357898;
    color: #fff;
}
</style>

<!-- QTooltip content is rendered through a portal outside this component's DOM subtree,
     so scoped styles can't reliably target it; this block must stay global. -->
<style>
.scale-tooltip {
    font-size: 0.95rem;
    line-height: 1.4;
    background: #f5fafd;
    color: #000;
}
</style>

