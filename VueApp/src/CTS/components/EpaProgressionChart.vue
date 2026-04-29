<script setup lang="ts">
import { computed } from "vue"
import type { Assessment } from "@/CTS/types"

type ChartSize = "compact" | "large"

const props = withDefaults(
    defineProps<{
        assessments: Assessment[]
        size?: ChartSize
    }>(),
    { size: "compact" },
)

const DENSE_THRESHOLD = 8

const geometry = computed(() => {
    if (props.size === "large") {
        return {
            VW: 720,
            VH: 360,
            LBL: 24,
            TOP: 24,
            LANE_H: 56,
            MONTH_FONT: 12,
            DOT_R_DEFAULT: 7,
            DOT_R_DENSE: 5.5,
            DOT_STROKE: 2.5,
            MIN_TICK_SPACING: 56,
        }
    }
    return {
        VW: 214,
        VH: 132,
        LBL: 12,
        TOP: 10,
        LANE_H: 20,
        MONTH_FONT: 7.5,
        DOT_R_DEFAULT: 4.5,
        DOT_R_DENSE: 3.5,
        DOT_STROKE: 1.5,
        MIN_TICK_SPACING: 26,
    }
})

const CH = computed(() => geometry.value.LANE_H * 5)
const CW = computed(() => geometry.value.VW - geometry.value.LBL - 6)

const sorted = computed(() =>
    [...props.assessments].sort((a, b) => new Date(a.encounterDate).getTime() - new Date(b.encounterDate).getTime()),
)

const dotRadius = computed(() =>
    sorted.value.length >= DENSE_THRESHOLD ? geometry.value.DOT_R_DENSE : geometry.value.DOT_R_DEFAULT,
)

const dateBounds = computed(() => {
    if (sorted.value.length === 0) {
        const now = Date.now()
        return { min: now, max: now }
    }
    const times = sorted.value.map((a) => new Date(a.encounterDate).getTime())
    let min = Math.min(...times)
    let max = Math.max(...times)
    if (min === max) {
        const dayMs = 86_400_000
        min -= dayMs * 7
        max += dayMs * 7
    } else {
        const pad = (max - min) * 0.05
        min -= pad
        max += pad
    }
    return { min, max }
})

function dotX(t: number): number {
    const { min, max } = dateBounds.value
    const range = max - min || 1
    return geometry.value.LBL + ((t - min) / range) * CW.value
}

function dotY(level: number): number {
    return geometry.value.TOP + (5 - level) * geometry.value.LANE_H + geometry.value.LANE_H / 2
}

const monthTicks = computed(() => {
    const { min, max } = dateBounds.value
    const start = new Date(min)
    const all: { x: number; label: string }[] = []
    const cursor = new Date(start.getFullYear(), start.getMonth(), 1)
    while (cursor.getTime() <= max) {
        if (cursor.getTime() >= min) {
            all.push({
                x: dotX(cursor.getTime()),
                label: cursor.toLocaleDateString("en-US", { month: "short" }),
            })
        }
        cursor.setMonth(cursor.getMonth() + 1)
    }
    if (all.length <= 1) return all

    const targetCount = Math.max(2, Math.floor(CW.value / geometry.value.MIN_TICK_SPACING))
    const step = Math.max(1, Math.ceil(all.length / targetCount))
    return all.filter((_, i) => i % step === 0 || i === all.length - 1)
})

const dots = computed(() =>
    sorted.value.map((a) => ({
        cx: dotX(new Date(a.encounterDate).getTime()),
        cy: dotY(a.levelValue),
        levelValue: a.levelValue,
    })),
)

// Smooth path through every dot.
// 2 points → quadratic with a vertical bulge (up if level rises, down if it falls).
// 3+ points → Catmull-Rom spline converted to cubic Béziers.
const splinePath = computed(() => {
    const pts = dots.value
    if (pts.length < 2) return ""

    if (pts.length === 2) {
        const [p0, p1] = pts
        const midX = (p0.cx + p1.cx) / 2
        const midY = (p0.cy + p1.cy) / 2
        const bulge = geometry.value.LANE_H * 0.3
        // SVG: smaller y = higher on screen. Higher level value = smaller cy.
        // dy > 0 → second dot is lower (rating dropped); arch downward.
        // dy < 0 → second dot is higher (rating rose); arch upward.
        const dy = p1.cy - p0.cy
        const cpy = dy === 0 ? midY : midY + Math.sign(dy) * bulge
        return `M ${p0.cx.toFixed(1)} ${p0.cy.toFixed(1)} Q ${midX.toFixed(1)} ${cpy.toFixed(1)} ${p1.cx.toFixed(1)} ${p1.cy.toFixed(1)}`
    }

    let d = `M ${pts[0].cx.toFixed(1)} ${pts[0].cy.toFixed(1)}`
    for (let i = 1; i < pts.length; i++) {
        const p0 = pts[Math.max(0, i - 2)]
        const p1 = pts[i - 1]
        const p2 = pts[i]
        const p3 = pts[Math.min(pts.length - 1, i + 1)]
        const cp1x = (p1.cx + (p2.cx - p0.cx) / 6).toFixed(1)
        const cp1y = (p1.cy + (p2.cy - p0.cy) / 6).toFixed(1)
        const cp2x = (p2.cx - (p3.cx - p1.cx) / 6).toFixed(1)
        const cp2y = (p2.cy - (p3.cy - p1.cy) / 6).toFixed(1)
        d += ` C ${cp1x} ${cp1y}, ${cp2x} ${cp2y}, ${p2.cx.toFixed(1)} ${p2.cy.toFixed(1)}`
    }
    return d
})

const dotFillClasses = ["", "cts-dot-1", "cts-dot-2", "cts-dot-3", "cts-dot-4", "cts-dot-5"]
</script>
<template>
    <div :class="['epaChartPanel', `epaChartPanel--${size}`]">
        <svg
            :viewBox="`0 0 ${geometry.VW} ${geometry.VH}`"
            :width="geometry.VW"
            :height="geometry.VH"
            preserveAspectRatio="xMidYMid meet"
            aria-hidden="true"
        >
            <line
                :x1="geometry.LBL"
                :y1="geometry.TOP + CH"
                :x2="geometry.LBL + CW"
                :y2="geometry.TOP + CH"
                stroke="rgba(2,40,81,0.1)"
                stroke-width="0.75"
            />
            <text
                v-for="(tick, i) in monthTicks"
                :key="`m-${i}`"
                :x="tick.x.toFixed(1)"
                :y="geometry.TOP + CH + geometry.MONTH_FONT + 4"
                text-anchor="middle"
                fill="#adb5bd"
                :font-size="geometry.MONTH_FONT"
            >
                {{ tick.label }}
            </text>
            <path
                v-if="splinePath"
                :d="splinePath"
                fill="none"
                stroke="rgba(2,40,81,0.2)"
                stroke-width="1"
                stroke-linecap="round"
            />
            <circle
                v-for="(dot, i) in dots"
                :key="`d-${i}`"
                :cx="dot.cx.toFixed(1)"
                :cy="dot.cy.toFixed(1)"
                :r="dotRadius"
                :class="dotFillClasses[dot.levelValue]"
                stroke="white"
                :stroke-width="geometry.DOT_STROKE"
            />
        </svg>
    </div>
</template>

<style scoped>
.epaChartPanel--compact svg {
    width: 13.375rem;
    height: 9.25rem;
}

.epaChartPanel--large svg {
    width: 100%;
    height: auto;
    max-width: 45rem;
}

.cts-dot-1 {
    fill: rgb(62 127 238 / 30%);
}

.cts-dot-2 {
    fill: rgb(62 127 238 / 70%);
}

.cts-dot-3 {
    fill: rgb(62 127 238 / 100%);
}

.cts-dot-4 {
    fill: rgb(0 44 175 / 80%);
}

.cts-dot-5 {
    fill: rgb(11 3 139 / 100%);
}
</style>
