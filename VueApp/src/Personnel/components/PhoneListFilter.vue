<template>
    <div
        ref="bar"
        class="phone-list-filter q-mb-md q-mt-md"
    >
        <q-input
            v-model="search"
            class="phone-list-filter__input q-mx-xs"
            dense
            outlined
            debounce="300"
            label="Filter Results"
        >
            <template #append>
                <q-icon name="filter_alt" />
            </template>
        </q-input>
        <!-- Anything a page adds here is pinned with the filter, rather than scrolling away and
             leaving the reader to scroll back up to it. Counted in the published height. -->
        <slot />
        <!-- Displays only when no matches are found across the entire page. The div is always
             rendered to assist screen readers, which will see and announce a change of text. After
             the slot, so it reads in the order it is shown: on its own line below the field and
             anything beside it. -->
        <div
            role="status"
            class="phone-list-filter__status text-grey q-mx-xs"
            :class="{ 'q-mt-sm': noMatches }"
        >
            <template v-if="noMatches">No records match "{{ search }}".</template>
        </div>
    </div>
</template>

<script setup lang="ts">
import { useTemplateRef, watchEffect } from "vue"
import { useElementSize } from "@vueuse/core"

// The label prop is the accessible name as well as the visible one: QInput copies it to the
// native input's aria-label, so no separate aria-label is needed or wanted here.
const search = defineModel<string>({ required: true })

defineProps<{
    /** Set by a read-only page when the search has emptied every table. Maintain pages leave it
     * unset: their tables stay shown, each with its own empty line. */
    noMatches?: boolean
}>()

// Enables the scroll padding in assets/phone-list.css to be exactly this bar plus the header
// above it. The height is not a constant since the padding is in rem and the root font-size changes
// at 768px, and the controls either share a line or stack depending on the width.
const barRef = useTemplateRef<HTMLElement>("bar")
const { height } = useElementSize(barRef, undefined, { box: "border-box" })
watchEffect(() => {
    document.documentElement.style.setProperty("--phone-list-filter-height", `${Math.round(height.value)}px`)
})
</script>

<style scoped>
/*
 * The field and whatever a page adds share a line while they fit, and stack on a narrow screen.
 * Where the bar is pinned, every line it saves is a line more of the list. No row gap: the status
 * line is empty most of the time, and brings its own margin when it is not.
 */
.phone-list-filter {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    column-gap: 0.5rem;
}

/* Takes the line's spare width, down to a size a name can still be typed into comfortably. */
.phone-list-filter__input {
    flex: 1 1 20rem;
}

/* Always a line of its own, below the controls. */
.phone-list-filter__status {
    flex-basis: 100%;
}

/*
 * Pinned at every width: the phone lists run to many screens on desktop as well as on a phone, and
 * the jump links are only worth having if they can be reached without scrolling back up for them.
 *
 * The min-height guard is the WCAG 1.4.10 concern: on a short viewport - a landscape phone, or a
 * zoomed page - a bar fixed to the top eats a large share of what is left to read. Under 400px of
 * height it scrolls with the page like anything else.
 *
 * Focus targets that the browser scrolls into view can land underneath a sticky bar. The paired
 * scroll padding in assets/phone-list.css keeps them clear of it.
 */
@media (height >= 400px) {
    .phone-list-filter {
        position: sticky;

        /*
         * Not 0. The app header is position: fixed, so the top of the viewport is behind it -
         * pinning there hides this bar under the header rather than below it. ViperLayout
         * measures the header and publishes its height, since it is only given a fixed 86px
         * minimum at 768px and up.
         */
        top: var(--viper-header-height, 86px);

        /* Under the header's 2000, above page content, which sets no z-index at all. */
        z-index: 2;

        /* Opaque: rows scrolling underneath must never show through the field. */
        background-color: var(--surface, #fff);
        padding-block: 0.5rem;
    }
}
</style>
