<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from "vue"
import type { Assessment, Epa } from "@/CTS/types"
import { useDateFunctions } from "@/composables/DateFunctions"
import { inflect } from "inflection"
import EpaProgressionChart from "@/CTS/components/EpaProgressionChart.vue"

const VOICES_THRESHOLD = 5
const CHART_THRESHOLD = 2

const props = defineProps<{
    epa: Epa
    assessments: Assessment[]
}>()

const { formatDate } = useDateFunctions()
const showAll = ref(false)
const showChartDialog = ref(false)
const chartDialogTitleId = computed(() => `chart-dialog-title-${props.epa.epaId ?? "new"}`)

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
    return 6 + (lv - 1) * 5
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
        y: el.getBoundingClientRect().top - gutterTop + 18,
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

onMounted(scheduleGutter)
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
                <div
                    v-for="a in visible"
                    :key="a.encounterId"
                    class="epaVoice"
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
                        <span class="epaVoiceSupervision">{{ a.levelName }}</span>
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
                />
            </button>
        </div>

        <q-dialog
            v-model="showChartDialog"
            :aria-labelledby="chartDialogTitleId"
        >
            <q-card style="width: 50rem; max-width: 92vw">
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
                    />
                </q-card-section>
            </q-card>
        </q-dialog>
    </article>
</template>
