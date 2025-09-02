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

                <div class="legend-items">
                    <div class="legend-item">
                        <q-icon
                            name="close"
                            size="sm"
                            color="negative"
                            class="legend-icon"
                            aria-hidden="true"
                        />
                        <span class="legend-text">Remove from schedule</span>
                    </div>

                    <div class="legend-item">
                        <q-icon
                            name="star_outline"
                            size="sm"
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
                            size="sm"
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
                            size="sm"
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
interface Props {
    showWarning?: boolean
}

withDefaults(defineProps<Props>(), {
    showWarning: false,
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
