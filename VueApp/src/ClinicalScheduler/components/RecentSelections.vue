<template>
    <q-card
        class="q-mb-md recent-selections-card"
        flat
        bordered
    >
        <q-card-section class="q-pa-sm q-pb-md">
            <div class="column q-gutter-sm">
                <!-- Recent items section -->
                <div>
                    <div class="row items-center q-gutter-sm q-mb-xs">
                        <div class="text-body2 text-weight-medium">
                            {{ recentLabel }}
                        </div>
                        <!-- Clear selection button -->
                        <q-btn
                            v-if="selectedItem"
                            flat
                            color="grey-7"
                            size="xs"
                            @click="clearSelection"
                            :title="`Clear ${itemType} selection`"
                            class="q-px-sm"
                        >
                            Clear selection
                        </q-btn>
                    </div>
                    <div class="q-gutter-none">
                        <!-- Show chips when items exist -->
                        <q-chip
                            v-for="item in items"
                            :key="getItemKey(item)"
                            :color="isSelected(item) ? 'primary' : undefined"
                            :text-color="isSelected(item) ? 'white' : 'dark'"
                            :outline="!isSelected(item)"
                            clickable
                            size="sm"
                            class="q-mr-xs"
                            @click="selectItem(item)"
                        >
                            {{ getItemDisplayName(item) }}
                        </q-chip>

                        <!-- Show empty state message when no items and not loading -->
                        <div
                            v-if="!isLoading && items.length === 0"
                            class="text-grey-6 text-body2 q-py-sm"
                        >
                            {{ emptyStateMessage }}
                        </div>
                    </div>
                </div>

                <!-- Dropdown section -->
                <div>
                    <div
                        class="text-body2 text-weight-medium"
                        :class="`q-mb-${labelSpacing}`"
                    >
                        {{ addNewLabel }}
                    </div>
                    <div
                        v-if="selectorSpacing !== 'none'"
                        :class="`q-mb-${selectorSpacing}`"
                    >
                        <slot name="selector" />
                    </div>
                    <div v-else>
                        <slot name="selector" />
                    </div>
                </div>
            </div>
        </q-card-section>
    </q-card>
</template>

<script setup lang="ts" generic="T extends Record<string, any>">
const props = withDefaults(
    defineProps<{
        items: T[]
        selectedItem: T | null
        recentLabel: string
        addNewLabel: string
        itemType: string
        itemKeyField: keyof T
        itemDisplayField: keyof T
        labelSpacing?: "xs" | "sm" | "md" | "lg"
        selectorSpacing?: "none" | "xs" | "sm" | "md" | "lg"
        isLoading?: boolean
        emptyStateMessage?: string
    }>(),
    {
        labelSpacing: "xs",
        selectorSpacing: "none",
        isLoading: false,
        emptyStateMessage: "",
    },
)

const emit = defineEmits<{
    "select-item": [item: T]
    "clear-selection": []
}>()

function getItemKey(item: T): string | number {
    return item[props.itemKeyField] as string | number
}

function getItemDisplayName(item: T): string {
    return String(item[props.itemDisplayField])
}

function isSelected(item: T): boolean {
    if (!props.selectedItem) return false
    return getItemKey(item) === getItemKey(props.selectedItem)
}

function selectItem(item: T): void {
    emit("select-item", item)
}

function clearSelection(): void {
    emit("clear-selection")
}
</script>

<style scoped>
.recent-selections-card {
    position: sticky;
    top: 130px; /* Desktop: Blue header + yellow navigation + padding (35px + 60px + 35px) */
    z-index: 100;
    background-color: #f8f9fa; /* Light gray to make the box stand out from white week boxes */
}

/* Responsive positioning for tablet screens */

/* Quasar breakpoint: gt-sm (yellow header visible when > 1023px) */
@media (width <= 1023px) and (width >= 600px) {
    .recent-selections-card {
        top: 90px; /* Tablet: Blue header + padding when yellow header collapses */
    }
}

/* Mobile screens need less spacing */

/* Quasar xs breakpoint: mobile screens */
@media (width <= 599px) {
    .recent-selections-card {
        top: 65px; /* Mobile: Reduced spacing for smaller screens */
    }
}

/* Ensure the card doesn't get too tall on scroll */
@media (height <= 800px) {
    .recent-selections-card {
        max-height: calc(100vh - 200px);
        overflow-y: auto;
    }
}
</style>
