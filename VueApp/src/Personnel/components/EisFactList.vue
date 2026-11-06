<template>
    <dl class="eis-facts">
        <template v-for="(row, rowIndex) in rows" :key="rowIndex">
            <div v-for="(fact, index) in row" :key="fact.label" class="eis-fact" :class="{ 'eis-fact--row-start': index === 0 }">
                <dt>{{ fact.label }}</dt>
                <dd>
                    <a v-if="fact.href" :href="fact.href">{{ fact.value }}</a>
                    <template v-else>{{ fact.value }}</template>
                </dd>
            </div>
        </template>
    </dl>
</template>

<script setup lang="ts">
import type { EisFactRow } from "../utils/eis-facts"

/**
 * Labelled values in at most two columns, one row per group of related facts, as the legacy
 * EIS header laid them out. On phones the columns stack. A fact with an href shows its value as
 * a link; a null value shows as blank.
 */
defineProps<{ rows: EisFactRow[] }>()
</script>

<style scoped>
.eis-facts {
    display: grid;
    grid-template-columns: minmax(0, 1fr);
    gap: 0.5rem 1.5rem;
    max-width: 50rem;
    margin: 0;
}

@media (width >= 600px) {
    .eis-facts {
        grid-template-columns: repeat(2, minmax(0, 1fr));
    }

    /* Each row's first fact starts a new line, so a lone fact leaves the second column empty. */
    .eis-fact--row-start {
        grid-column-start: 1;
    }
}

.eis-fact dt {
    font-weight: 600;
}

.eis-fact dd {
    margin: 0;
}
</style>