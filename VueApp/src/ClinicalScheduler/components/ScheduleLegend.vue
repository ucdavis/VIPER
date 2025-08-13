<template>
    <div class="schedule-legend">
        <q-card flat bordered>
            <q-card-section>
                <div class="row items-center q-mb-sm">
                    <q-icon name="info" size="sm" color="primary" class="q-mr-sm" />
                    <div class="text-subtitle1 text-weight-medium">Quick Guide</div>
                </div>
                
                <div class="legend-items">
                    <div class="legend-item">
                        <span class="legend-icon remove-icon">✖</span>
                        <span class="legend-text">Remove from schedule</span>
                    </div>
                    
                    <div class="legend-item">
                        <span class="legend-icon primary-icon">☆</span>
                        <span class="legend-text">Makes them the primary evaluator (replacing the current one, if there is one)</span>
                    </div>
                    
                    <div class="legend-item">
                        <span class="legend-icon primary-indicator">★</span>
                        <span class="legend-text">Current primary evaluator</span>
                    </div>
                    
                    <div v-if="showWarning" class="legend-item">
                        <span class="legend-icon alert-icon">⚠️</span>
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
    showWarning: false
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
    color: #555;
}

.legend-icon {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 24px;
    height: 24px;
    font-weight: bold;
    flex-shrink: 0;
}

.legend-icon.remove-icon {
    color: #dc3545;
    font-size: 16px;
}

.legend-icon.primary-icon {
    color: #333;  /* Dark outline star for action */
    font-size: 20px;
    font-weight: normal;
}

.legend-icon.primary-indicator {
    color: #ffc107;  /* Filled gold star for current primary */
    font-size: 18px;
}

.legend-icon.alert-icon {
    font-size: 18px;
}

.legend-text {
    line-height: 1.4;
}

@media (max-width: 600px) {
    .legend-items {
        grid-template-columns: 1fr;
    }
}
</style>