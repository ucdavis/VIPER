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
                            <q-badge
                                v-if="selectedItemsSet.size > 1"
                                color="primary"
                                class="q-ml-sm"
                            >
                                {{ selectedItemsSet.size }} selected
                            </q-badge>
                        </div>
                        <!-- Clear selection button -->
                        <q-btn
                            v-if="selectedItemsSet.size > 0"
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
                            :color="isItemSelected(item) ? 'primary' : undefined"
                            :text-color="isItemSelected(item) ? 'white' : 'dark'"
                            :outline="!isItemSelected(item)"
                            clickable
                            size="sm"
                            class="q-mr-xs"
                            @click="toggleItemSelection(item)"
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

                    <!-- Helper text for multi-select -->
                    <div
                        v-if="selectedItemsSet.size > 0"
                        class="text-body2 text-grey-7 q-mt-xs"
                    >
                        Click any week to schedule all {{ selectedItemsSet.size }} selected {{ itemType
                        }}{{ selectedItemsSet.size > 1 ? "s" : "" }}
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
import { ref, watch } from "vue"

const props = withDefaults(
    defineProps<{
        items: T[]
        selectedItem?: T | null
        localSelectedItems?: T[]
        multiSelect?: boolean
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
        selectedItem: null,
        localSelectedItems: () => [],
        multiSelect: false,
        labelSpacing: "xs",
        selectorSpacing: "none",
        isLoading: false,
        emptyStateMessage: "",
    },
)

const emit = defineEmits<{
    "select-item": [item: T]
    "select-items": [items: T[]]
    "clear-selection": []
}>()

const selectedItemsSet = ref<Set<string | number>>(new Set())

watch(
    () => props.selectedItem,
    (newItem) => {
        if (!props.multiSelect && newItem) {
            selectedItemsSet.value.clear()
            selectedItemsSet.value.add(getItemKey(newItem))
        }
    },
    { immediate: true },
)

watch(
    () => props.localSelectedItems,
    (newItems) => {
        if (props.multiSelect && newItems) {
            selectedItemsSet.value.clear()
            newItems.forEach((item) => {
                selectedItemsSet.value.add(getItemKey(item))
            })
        }
    },
    { immediate: true, deep: true },
)

function getItemKey(item: T): string | number {
    return item[props.itemKeyField] as string | number
}

function getItemDisplayName(item: T): string {
    return String(item[props.itemDisplayField])
}

function isItemSelected(item: T): boolean {
    return selectedItemsSet.value.has(getItemKey(item))
}

function toggleItemSelection(item: T): void {
    const key = getItemKey(item)

    if (props.multiSelect) {
        // Create a new Set for reactivity
        const newSelectedItems = new Set(selectedItemsSet.value)
        if (newSelectedItems.has(key)) {
            newSelectedItems.delete(key)
        } else {
            newSelectedItems.add(key)
        }
        selectedItemsSet.value = newSelectedItems

        const localSelectedItemsArray = props.items.filter((item) => selectedItemsSet.value.has(getItemKey(item)))
        emit("select-items", localSelectedItemsArray)
    } else {
        selectedItemsSet.value.clear()
        selectedItemsSet.value.add(key)
        emit("select-item", item)
    }
}

function clearSelection(): void {
    selectedItemsSet.value = new Set()
    emit("clear-selection")
    if (props.multiSelect) {
        emit("select-items", [])
    }
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
