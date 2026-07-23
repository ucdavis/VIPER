<template>
    <div
        ref="listEl"
        class="sortable-list"
    >
        <div
            v-if="$slots.header"
            class="sortable-row sortable-row--header"
            aria-hidden="true"
        >
            <span class="sortable-row__handle" />
            <div class="sortable-row__body">
                <slot name="header" />
            </div>
            <div class="sortable-row__controls" />
        </div>

        <VueDraggable
            v-model="model"
            :animation="reducedMotion === 'reduce' ? 0 : 200"
            :disabled="disabled"
            handle=".sortable-row__handle"
            ghost-class="sortable-row--ghost"
            class="sortable-list__items"
            @end="onDragEnd"
        >
            <div
                v-for="(item, index) in model"
                :key="keyOf(item)"
                class="sortable-row"
                :class="[rowClass?.(item, index), { 'sortable-row--moved': keyOf(item) === justMovedKey }]"
            >
                <span
                    class="sortable-row__handle"
                    :class="{ 'sortable-row__handle--disabled': disabled }"
                    aria-hidden="true"
                >
                    <q-icon name="drag_handle" />
                    <q-tooltip v-if="!disabled">{{ handleLabel }}</q-tooltip>
                </span>

                <div class="sortable-row__body">
                    <slot
                        name="row"
                        :item="item"
                        :index="index"
                    />
                </div>

                <div class="sortable-row__controls">
                    <q-btn
                        dense
                        flat
                        no-caps
                        size="sm"
                        color="secondary"
                        icon="arrow_upward"
                        :aria-label="moveUpLabel"
                        :disable="disabled || index === 0"
                        @click="onMoveUp(index)"
                    >
                        <q-tooltip>{{ moveUpLabel }}</q-tooltip>
                    </q-btn>
                    <q-btn
                        dense
                        flat
                        no-caps
                        size="sm"
                        color="secondary"
                        icon="arrow_downward"
                        :aria-label="moveDownLabel"
                        :disable="disabled || index === model.length - 1"
                        @click="onMoveDown(index)"
                    >
                        <q-tooltip>{{ moveDownLabel }}</q-tooltip>
                    </q-btn>
                    <slot
                        name="actions"
                        :item="item"
                        :index="index"
                    />
                </div>
            </div>
        </VueDraggable>

        <slot
            v-if="model.length === 0"
            name="empty"
        />

        <!-- Screen readers hear the result of every reorder; the drag handle is a
             mouse/touch affordance, so non-visual users rely on the move buttons + this. -->
        <div
            class="sr-only"
            role="status"
            aria-live="polite"
        >
            {{ announcement }}
        </div>
    </div>
</template>

<script setup lang="ts" generic="T extends Record<string, any>">
import { nextTick, ref } from "vue"
import { usePreferredReducedMotion } from "@vueuse/core"
import { VueDraggable } from "vue-draggable-plus"
import { useReorder, type ReorderKey } from "@/composables/use-reorder"

const model = defineModel<T[]>({ required: true })

const {
    itemKey,
    disabled = false,
    handleLabel = "Drag to reorder",
    moveUpLabel = "Move up",
    moveDownLabel = "Move down",
    rowClass = undefined,
    announce = undefined,
} = defineProps<{
    /** Field name or getter for each item's stable, unique key. */
    itemKey: keyof T | ((item: T) => ReorderKey)
    disabled?: boolean
    handleLabel?: string
    moveUpLabel?: string
    moveDownLabel?: string
    /** Extra class(es) per row, e.g. to tint a section header. */
    rowClass?: (item: T, index: number) => unknown
    /** Builds the screen-reader announcement after a move. */
    announce?: (item: T, newIndex: number, total: number) => string
}>()

const emit = defineEmits<{
    reorder: [item: T, newIndex: number, oldIndex: number]
}>()

defineSlots<{
    /** Optional column-header row, aligned to the row bodies below. */
    header?: () => unknown
    /** Per-row content. */
    row: (props: { item: T; index: number }) => unknown
    /** Per-row actions (edit, delete) placed after the move buttons. */
    actions?: (props: { item: T; index: number }) => unknown
    /** Shown in place of the list when it is empty. */
    empty?: () => unknown
}>()

function keyOf(item: T): ReorderKey {
    return typeof itemKey === "function" ? itemKey(item) : (item[itemKey] as ReorderKey)
}

const announcement = ref("")
function announceMove(item: T, newIndex: number) {
    const text = announce
        ? announce(item, newIndex, model.value.length)
        : `Moved to position ${newIndex + 1} of ${model.value.length}`
    // Clear first so an identical message (e.g. nudged back to the same spot) re-announces.
    announcement.value = ""
    void nextTick(() => {
        announcement.value = text
    })
}

const { justMovedKey, moveUp, moveDown, commitDrag } = useReorder<T>(model, {
    getKey: keyOf,
    onReorder: (item, newIndex, oldIndex) => {
        announceMove(item, newIndex)
        emit("reorder", item, newIndex, oldIndex)
    },
})

function onDragEnd(evt: { oldIndex?: number; newIndex?: number }) {
    if (evt.oldIndex === undefined || evt.newIndex === undefined) {
        return
    }
    commitDrag(evt.oldIndex, evt.newIndex)
}

// Drag reorders animate via SortableJS (:animation); button reorders jump instantly,
// so FLIP them — capture positions, reorder, then slide each moved row from where it
// was to where it landed. Makes the change easy to follow without watching closely.
const listEl = ref<HTMLElement>()
const reducedMotion = usePreferredReducedMotion()
const SLIDE_MS = 300

function rowElements(): HTMLElement[] {
    const container = listEl.value?.querySelector(".sortable-list__items")
    return container ? (Array.from(container.children) as HTMLElement[]) : []
}

function animateReorder(reorder: () => void) {
    if (reducedMotion.value === "reduce") {
        reorder()
        return
    }
    const startTops = new Map(rowElements().map((el) => [el, el.getBoundingClientRect().top]))
    reorder()
    void nextTick(() => {
        for (const el of rowElements()) {
            const startTop = startTops.get(el)
            if (startTop === undefined) {
                continue
            }
            const delta = startTop - el.getBoundingClientRect().top
            if (delta === 0) {
                continue
            }
            el.style.transition = "none"
            el.style.transform = `translateY(${delta}px)`
            requestAnimationFrame(() => {
                el.style.transition = `transform ${SLIDE_MS}ms cubic-bezier(0.22, 1, 0.36, 1)`
                el.style.transform = ""
            })
            el.addEventListener(
                "transitionend",
                () => {
                    el.style.transition = ""
                    el.style.transform = ""
                },
                { once: true },
            )
        }
    })
}

function onMoveUp(index: number) {
    animateReorder(() => moveUp(index))
}

function onMoveDown(index: number) {
    animateReorder(() => moveDown(index))
}
</script>

<style scoped>
.sortable-list__items {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.sortable-row {
    display: flex;
    align-items: flex-start;
    gap: 0.5rem;
    border-radius: 0.25rem;
}

.sortable-row--header {
    align-items: center;
}

.sortable-row__handle {
    flex: 0 0 auto;
    width: 1.75rem;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    padding-top: 0.375rem;
    color: var(--ucdavis-black-60);
    cursor: grab;
}

.sortable-row--header .sortable-row__handle {
    padding-top: 0;
}

.sortable-row__handle:active {
    cursor: grabbing;
}

.sortable-row__handle--disabled {
    visibility: hidden;
}

.sortable-row__body {
    flex: 1 1 auto;
    min-width: 0;
}

.sortable-row__controls {
    flex: 0 0 auto;
    min-width: 7.5rem;
    display: flex;
    align-items: center;
    justify-content: flex-end;
    gap: 0.25rem;
    padding-top: 0.125rem;
}

/* Comfortable touch targets for the reorder + row-action buttons (up/down and any
   edit/delete in the actions slot). 23px dense buttons miss the 24px minimum. */
.sortable-row__controls :deep(.q-btn) {
    min-width: 2.25rem;
    min-height: 2.25rem;
}

.sortable-row--header .sortable-row__controls {
    padding-top: 0;
}

.sortable-row--ghost {
    opacity: 0.5;
    background: var(--surface-tint);
}

/* "Just moved" cue: a brand-blue tint and ring that fade out. It is a colour/shadow
   fade (no movement), so it also plays under reduced motion — where the slide is
   skipped — keeping a clear signal that something changed. */
@keyframes sortable-row-flash {
    0% {
        background-color: var(--ucdavis-blue-10);
        box-shadow: inset 0 0 0 0.125rem var(--q-primary);
    }

    100% {
        background-color: transparent;
        box-shadow: inset 0 0 0 0.125rem transparent;
    }
}

.sortable-row--moved {
    animation: sortable-row-flash 1s ease-out;
}

/* On phones the row becomes a stacked card: handle + controls share a top bar,
   the body drops to its own full-width line below. */
@media (width <= 599px) {
    .sortable-row {
        flex-wrap: wrap;
    }

    .sortable-row__handle {
        order: 1;
    }

    .sortable-row__controls {
        order: 2;
        margin-left: auto;
    }

    .sortable-row__body {
        order: 3;
        flex: 1 1 100%;
    }
}
</style>
