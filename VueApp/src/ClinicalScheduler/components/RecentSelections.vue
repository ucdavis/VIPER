<template>
    <q-card
        class="q-mb-md"
        flat
        bordered
    >
        <q-card-section class="q-pa-sm">
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
interface Props<T> {
    items: T[]
    selectedItem: T | null
    recentLabel: string
    addNewLabel: string
    itemType: string
    itemKeyField: keyof T
    itemDisplayField: keyof T
    labelSpacing?: "xs" | "sm" | "md" | "lg"
    selectorSpacing?: "none" | "xs" | "sm" | "md" | "lg"
}

interface Emits<T> {
    (e: "select-item", item: T): void
    (e: "clear-selection"): void
}

const props = withDefaults(defineProps<Props<T>>(), {
    labelSpacing: "xs",
    selectorSpacing: "none",
})
const emit = defineEmits<Emits<T>>()

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
