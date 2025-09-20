<template>
    <div class="no-access-container">
        <q-card class="no-access-card">
            <q-card-section class="text-center">
                <q-icon
                    name="block"
                    :size="ACCESS_DENIED_ICON_SIZE"
                    color="negative"
                    class="q-mb-md"
                />
                <h3>Access Denied</h3>
                <p>{{ message }}</p>
                <p
                    v-if="subtitle"
                    class="text-caption"
                >
                    {{ subtitle }}
                </p>
                <div
                    v-if="$slots.actions"
                    class="q-mt-md"
                >
                    <slot name="actions" />
                </div>
            </q-card-section>
        </q-card>
    </div>
</template>

<script setup lang="ts">
const ACCESS_DENIED_ICON_SIZE = "48px"

interface Props {
    message: string
    subtitle?: string
}

withDefaults(defineProps<Props>(), {
    subtitle: undefined,
})
</script>

<style scoped>
.no-access-container {
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 300px;
    margin: 2rem 0;
}

.no-access-card {
    max-width: 500px;
    padding: 1rem;
}

.no-access-card h3 {
    color: var(--q-negative);
    margin: 0.5rem 0 1rem;
}
</style>
