<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, useId, watch } from "vue"
import type { Assessment, Epa } from "@/CTS/types"
import { useDateFunctions } from "@/composables/DateFunctions"
import { inflect } from "inflection"
import EpaProgressionChart from "@/CTS/components/EpaProgressionChart.vue"

const VOICES_THRESHOLD = 5
const CHART_THRESHOLD = 2

// Gutter SVG layout — coords are inside a 32-unit-wide viewBox; levels 1–5
// fan from left (low trust) to right (high trust) so the spline visually
// trends rightward as a student progresses.
const GUTTER_BASE_X = 6
const GUTTER_LEVEL_SPACING = 5
// Vertical offset (in CSS px) from each voice card's top edge to the dot
// center, chosen to align with the byline's visual midline.
const GUTTER_CARD_VERTICAL_OFFSET = 18

const props = defineProps<{
    epa: Epa
    assessments: Assessment[]
}>()

const { formatDate } = useDateFunctions()
const showAll = ref(false)
const showChartDialog = ref(false)
// useId() yields a per-instance unique id so two EpaVoiceThreads with the
// same (possibly null) epaId don't collide on aria-labelledby targets.
const chartDialogTitleId = `chart-dialog-title-${useId()}`
const activeEncounterId = ref<number | null>(null)
function setActive(id: number) {
    activeEncounterId.value = id
}
function clearActive() {
    activeEncounterId.value = null
}

const sorted = computed(() =>
    [...props.assessments].sort((a, b) => new Date(b.encounterDate).getTime() - new Date(a.encounterDate).getTime()),
)

const visible = computed(() =>
    showAll.value || sorted.value.length <= VOICES_THRESHOLD ? sorted.value : sorted.value.slice(0, VOICES_THRESHOLD),
)

const hiddenCount = computed(() => sorted.value.length - visible.value.length)
const showChart = computed(() => sorted.value.length >= CHART_THRESHOLD)
const isSparse = computed(() => sorted.value.length <= 1)
const showGutterSvg = computed(() => sorted.value.length >= 2)

const countLabel = computed(() => {
    const n = sorted.value.length
    if (n === 0) return "no assessments"
    return `${n} ${inflect("assessment", n)}`
})

const gutterRef = ref<HTMLElement | null>(null)
const gutterPath = ref("")
const gutterDots = ref<{ x: number; y: number }[]>([])
const gutterHeight = ref(0)

function lvX(lv: number): number {
    return GUTTER_BASE_X + (lv - 1) * GUTTER_LEVEL_SPACING
}

function buildGutter() {
    const gutter = gutterRef.value
    if (!gutter) return
    const body = gutter.parentElement
    if (!body) return

    const voiceEls = Array.from(body.querySelectorAll<HTMLElement>(".epaVoice"))
    if (voiceEls.length < 2) {
        gutterPath.value = ""
        gutterDots.value = []
        gutterHeight.value = 0
        return
    }
    const totalH = gutter.offsetHeight
    if (totalH === 0) return

    const gutterTop = gutter.getBoundingClientRect().top
    const pts = voiceEls.map((el, i) => ({
        x: lvX(visible.value[i]?.levelValue ?? 3),
        y: el.getBoundingClientRect().top - gutterTop + GUTTER_CARD_VERTICAL_OFFSET,
    }))

    let d = `M ${pts[0].x} ${pts[0].y}`
    for (let i = 1; i < pts.length; i++) {
        const p0 = pts[Math.max(0, i - 2)]
        const p1 = pts[i - 1]
        const p2 = pts[i]
        const p3 = pts[Math.min(pts.length - 1, i + 1)]
        const cp1x = +(p1.x + (p2.x - p0.x) / 6).toFixed(1)
        const cp1y = +(p1.y + (p2.y - p0.y) / 6).toFixed(1)
        const cp2x = +(p2.x - (p3.x - p1.x) / 6).toFixed(1)
        const cp2y = +(p2.y - (p3.y - p1.y) / 6).toFixed(1)
        d += ` C ${cp1x} ${cp1y}, ${cp2x} ${cp2y}, ${p2.x} ${p2.y}`
    }

    gutterPath.value = d
    gutterDots.value = pts
    gutterHeight.value = totalH
}

function scheduleGutter() {
    nextTick(() => buildGutter())
}

// Watch the body for layout changes — window resize, font load, container
// width changes — and rebuild the gutter spline so it stays aligned with the
// voice cards' new positions.
let resizeObserver: ResizeObserver | null = null
onMounted(() => {
    scheduleGutter()
    const body = gutterRef.value?.parentElement
    if (body && typeof ResizeObserver !== "undefined") {
        resizeObserver = new ResizeObserver(scheduleGutter)
        resizeObserver.observe(body)
    }
})
onBeforeUnmount(() => {
    resizeObserver?.disconnect()
    resizeObserver = null
})
watch(visible, scheduleGutter)
</script>
<template>
    <article :class="['epaThread', { 'epaThread--sparse': isSparse }]">
        <header class="epaThreadHeader">
            <h3 class="epaThreadTitle">{{ epa.name }}</h3>
            <div class="epaThreadCount">{{ countLabel }}</div>
        </header>

        <div
            v-if="sorted.length === 0"
            class="epaThreadEmpty"
        >
            No assessments recorded yet.
        </div>

        <div
            v-else
            class="epaThreadContent"
        >
            <div class="epaThreadBody">
                <div
                    ref="gutterRef"
                    class="epaThreadGutter"
                >
                    <svg
                        v-if="showGutterSvg && gutterPath"
                        :viewBox="`0 0 32 ${gutterHeight}`"
                        :height="gutterHeight"
                        width="32"
                        aria-hidden="true"
                    >
                        <path
                            :d="gutterPath"
                            fill="none"
                            stroke="#ffc519"
                            stroke-opacity="0.65"
                            stroke-width="2"
                            stroke-linecap="round"
                        />
                        <circle
                            v-for="(p, i) in gutterDots"
                            :key="i"
                            :cx="p.x"
                            :cy="p.y"
                            r="4"
                            fill="#022851"
                            stroke="#fbfaf8"
                            stroke-width="2.5"
                        />
                    </svg>
                </div>
                <!-- Hover/focus on a review only highlights styling and syncs
                     the chart dot; no click action, so role="button" would be
                     misleading. Focus pair satisfies the keyboard equivalent. -->
                <!-- eslint-disable-next-line vuejs-accessibility/no-static-element-interactions -->
                <div
                    v-for="a in visible"
                    :key="a.encounterId"
                    :class="['epaVoice', { 'epaVoice--active': activeEncounterId === a.encounterId }]"
                    tabindex="0"
                    @mouseenter="setActive(a.encounterId)"
                    @mouseleave="clearActive"
                    @focusin="setActive(a.encounterId)"
                    @focusout="clearActive"
                >
                    <div
                        v-if="a.comment"
                        class="epaVoiceQuote"
                    >
                        {{ a.comment }}
                    </div>
                    <div
                        v-else
                        class="epaVoiceNoComment"
                    >
                        (No comment recorded)
                    </div>
                    <div class="epaVoiceByline">
                        <strong>&mdash; {{ a.enteredByName }}</strong>
                        <span v-if="a.serviceName"> &middot; {{ a.serviceName }}</span>
                        &middot; {{ formatDate(a.encounterDate.toString()) }}
                        <span class="epaVoiceSupervision">
                            <span
                                :class="['epaVoiceLevelDot', `cts-dot-${a.levelValue}`]"
                                aria-hidden="true"
                            ></span>
                            {{ a.levelName }}
                        </span>
                    </div>
                </div>
                <button
                    v-if="hiddenCount > 0"
                    type="button"
                    class="epaThreadMore"
                    :aria-expanded="showAll"
                    @click="showAll = true"
                >
                    show {{ hiddenCount }} earlier {{ inflect("assessment", hiddenCount) }}
                </button>
                <button
                    v-else-if="showAll && sorted.length > VOICES_THRESHOLD"
                    type="button"
                    class="epaThreadMore"
                    :aria-expanded="showAll"
                    @click="showAll = false"
                >
                    show fewer
                </button>
            </div>

            <button
                v-if="showChart"
                type="button"
                class="epaChartButton"
                :aria-label="`Expand progression chart for ${epa.name}`"
                @click="showChartDialog = true"
            >
                <EpaProgressionChart
                    :assessments="sorted"
                    size="compact"
                    :active-encounter-id="activeEncounterId"
                    @hover-dot="setActive"
                    @leave-dot="clearActive"
                />
                <span class="epaChartExpandHint">click to expand</span>
            </button>
        </div>

        <q-dialog
            v-model="showChartDialog"
            :aria-labelledby="chartDialogTitleId"
        >
            <q-card class="epaChartDialogCard">
                <q-card-section class="row items-center q-pb-none">
                    <div
                        :id="chartDialogTitleId"
                        class="text-h6"
                    >
                        Progression — {{ epa.name }}
                    </div>
                    <q-space />
                    <q-btn
                        icon="close"
                        flat
                        round
                        dense
                        aria-label="Close progression chart"
                        v-close-popup
                    />
                </q-card-section>
                <q-card-section>
                    <EpaProgressionChart
                        :assessments="sorted"
                        size="large"
                        :active-encounter-id="activeEncounterId"
                        @hover-dot="setActive"
                        @leave-dot="clearActive"
                    />
                </q-card-section>
            </q-card>
        </q-dialog>
    </article>
</template>
