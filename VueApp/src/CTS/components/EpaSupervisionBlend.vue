<script setup lang="ts">
import { computed } from "vue"
import type { Assessment, Epa } from "@/CTS/types"
import { useDateFunctions } from "@/composables/DateFunctions"
import { inflect } from "inflection"

const props = defineProps<{
    epa: Epa
    assessments: Assessment[]
}>()

const { formatDate } = useDateFunctions()

const total = computed(() => props.assessments.length)

// Chronological order — drives both the bar segments and the comment list
// so the leftmost segment lines up with the first comment shown.
const sortedChronological = computed(() =>
    [...props.assessments].sort((a, b) => new Date(a.encounterDate).getTime() - new Date(b.encounterDate).getTime()),
)

// One bar segment per assessment, equal width, in chronological order.
const segments = computed(() => {
    const width = total.value > 0 ? 100 / total.value : 0
    return sortedChronological.value.map((a) => ({
        encounterId: a.encounterId,
        levelValue: a.levelValue,
        levelName: a.levelName,
        date: a.encounterDate,
        widthPct: width,
    }))
})

// Legend totals: counts per supervision level, ordered by rank.
const legendTotals = computed(() => {
    const map = new Map<number, { levelValue: number; levelName: string; count: number }>()
    for (const a of props.assessments) {
        const existing = map.get(a.levelValue)
        if (existing) {
            existing.count++
        } else {
            map.set(a.levelValue, { levelValue: a.levelValue, levelName: a.levelName, count: 1 })
        }
    }
    return [...map.values()].sort((a, b) => a.levelValue - b.levelValue)
})

const countLabel = computed(() => {
    const n = total.value
    if (n === 0) return "no assessments"
    return `${n} ${inflect("assessment", n)}`
})
</script>
<template>
    <article class="epaBlend">
        <header class="epaBlendHeader">
            <h3 class="epaBlendTitle">{{ epa.name }}</h3>
            <div class="epaBlendCount">{{ countLabel }}</div>
        </header>

        <div
            v-if="total === 0"
            class="epaBlendEmpty"
        >
            No assessments recorded yet.
        </div>

        <template v-else>
            <div
                class="epaBlendBar"
                role="img"
                :aria-label="`Supervision context for each assessment of ${epa.name}, in chronological order`"
            >
                <div
                    v-for="seg in segments"
                    :key="seg.encounterId"
                    :class="['epaBlendSegment', `epaBlendSegment--lv-${seg.levelValue}`]"
                    :style="{ width: `${seg.widthPct}%` }"
                    :title="`${seg.levelName} — ${formatDate(seg.date.toString())}`"
                />
            </div>

            <ul class="epaBlendLegend">
                <li
                    v-for="lt in legendTotals"
                    :key="lt.levelValue"
                    class="epaBlendLegendItem"
                >
                    <span :class="['epaBlendSwatch', `epaBlendSegment--lv-${lt.levelValue}`]" />
                    <span class="epaBlendLegendLabel">{{ lt.levelName }}</span>
                    <span class="epaBlendLegendCount">{{ lt.count }}</span>
                </li>
            </ul>

            <div class="epaBlendComments epaBlendComments--withBar">
                <div
                    v-for="a in sortedChronological"
                    :key="a.encounterId"
                    class="epaBlendComment"
                >
                    <div
                        v-if="a.comment"
                        class="epaBlendCommentText"
                    >
                        {{ a.comment }}
                    </div>
                    <div
                        v-else
                        class="epaBlendCommentMissing"
                    >
                        (No comment recorded)
                    </div>
                    <div class="epaBlendCommentMeta">
                        <strong>{{ a.enteredByName }}</strong>
                        <span v-if="a.serviceName"> &middot; {{ a.serviceName }}</span>
                        &middot; {{ formatDate(a.encounterDate.toString()) }}
                    </div>
                </div>
            </div>
        </template>
    </article>
</template>
