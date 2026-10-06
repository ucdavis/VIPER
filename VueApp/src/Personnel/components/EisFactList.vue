<template>
    <dl class="eis-facts">
        <div
            v-for="fact in facts"
            :key="fact.label"
            class="eis-fact"
        >
            <dt>{{ fact.label }}</dt>
            <dd>
                <a
                    v-if="fact.href"
                    :href="fact.href"
                    >{{ fact.value }}</a
                >
                <template v-else>{{ fact.value }}</template>
            </dd>
        </div>
    </dl>
</template>

<script setup lang="ts">
/**
 * Labelled values in a responsive grid, as the legacy EIS header laid them out in pairs.
 * A fact with an href shows its value as a link.
 */
interface EisFact {
    label: string
    value: string
    href?: string
}

defineProps<{ facts: EisFact[] }>()
</script>

<style scoped>
.eis-facts {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(16rem, 1fr));
    gap: 0.5rem 1.5rem;
    margin: 0;
}

.eis-fact dt {
    font-weight: 600;
}

.eis-fact dd {
    margin: 0;
}
</style>
