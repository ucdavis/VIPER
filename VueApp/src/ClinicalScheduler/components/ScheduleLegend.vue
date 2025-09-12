<template>
    <div class="schedule-legend">
        <q-card
            flat
            bordered
        >
            <q-card-section>
                <div class="row items-center q-mb-sm">
                    <q-icon
                        name="info"
                        size="sm"
                        color="primary"
                        class="q-mr-sm"
                    />
                    <div class="text-subtitle1 text-weight-medium">Quick Guide</div>
                </div>

                <!-- How to schedule instructions -->
                <div
                    class="bulk-scheduling-guide q-mb-md"
                    v-if="showBulkGuide"
                >
                    <div class="text-caption text-weight-medium q-mb-xs">How to schedule:</div>
                    <div class="text-caption text-grey-8">
                        <div class="q-mb-xs"><strong>Single week:</strong> Select {{ itemType }}(s) → Click a week</div>
                        <div v-if="!isTouchDevice">
                            <div class="q-mb-xs">
                                <strong>Multiple weeks:</strong> Select {{ itemType }}(s) → Hold
                                <kbd>{{ isMac ? "Option" : "Alt" }}</kbd> → Click first week → Click last week → Click
                                "Schedule Selected" button to apply
                            </div>
                        </div>
                        <div v-else>
                            <div class="q-mb-xs">
                                <strong>Multiple weeks:</strong> Select {{ itemType }}(s) → Long-press a week → Tap
                                other weeks
                            </div>
                            <div class="q-ml-md">→ Click "Schedule Selected" button to apply</div>
                        </div>
                    </div>
                    <q-separator class="q-mt-sm" />
                </div>

                <div class="legend-items">
                    <div class="legend-item">
                        <q-icon
                            name="close"
                            size="xs"
                            color="negative"
                            class="legend-icon"
                            aria-hidden="true"
                        />
                        <span class="legend-text">Remove from schedule</span>
                    </div>

                    <div class="legend-item">
                        <q-icon
                            name="star_outline"
                            size="xs"
                            color="grey-8"
                            class="legend-icon"
                            aria-hidden="true"
                        />
                        <span class="legend-text"
                            >Makes them the primary evaluator (replacing the current one, if there is one)</span
                        >
                    </div>

                    <div class="legend-item">
                        <q-icon
                            name="star"
                            size="xs"
                            color="amber"
                            class="legend-icon"
                            aria-hidden="true"
                        />
                        <span class="legend-text">Current primary evaluator</span>
                    </div>

                    <div
                        v-if="showWarning"
                        class="legend-item"
                    >
                        <q-icon
                            name="warning"
                            size="xs"
                            color="orange"
                            class="legend-icon"
                            aria-hidden="true"
                        />
                        <span class="legend-text">Needs primary evaluator</span>
                    </div>
                </div>
            </q-card-section>
        </q-card>
    </div>
</template>

<script setup lang="ts">
import { computed } from "vue"
import { useQuasar } from "quasar"

interface Props {
    showWarning?: boolean
    showBulkGuide?: boolean
    itemType?: "rotation" | "clinician"
}

withDefaults(defineProps<Props>(), {
    showWarning: false,
    showBulkGuide: false,
    itemType: "rotation",
})

const $q = useQuasar()

// Detect if user is on a touch device
const isTouchDevice = computed(() => {
    return $q.platform.is.mobile || "ontouchstart" in window
})

// Detect if on Mac for showing correct key name
const isMac = computed(() => {
    return $q.platform.is.mac
})
</script>

<style scoped>
.schedule-legend {
    margin-top: 20px;
}

.legend-items {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
    gap: 12px;
    margin-top: 8px;
}

.legend-item {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 13px;
    color: var(--ucdavis-black-50);
}

.legend-icon {
    flex-shrink: 0;
}

.legend-text {
    line-height: 1.4;
}

@media (width <= 600px) {
    .legend-items {
        grid-template-columns: 1fr;
    }
}
</style>
