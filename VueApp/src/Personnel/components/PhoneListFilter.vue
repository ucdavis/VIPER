<template>
    <div
        ref="bar"
        class="phone-list-filter q-mb-md"
    >
        <q-input
            v-model="search"
            class="q-ml-xs q-mr-xs"
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
    </div>
</template>

<script setup lang="ts">
import { useTemplateRef, watchEffect } from "vue"
import { useElementSize } from "@vueuse/core"

// The label prop is the accessible name as well as the visible one: QInput copies it to the
// native input's aria-label, so no separate aria-label is needed or wanted here.
const search = defineModel<string>({ required: true })

// Published rather than guessed at, so .sticky-filter-offset can be exactly this bar plus the
// header above it. The height is not a constant: the field is a fixed 40px but the padding is in
// rem, and the root font-size steps from 14px to 16px at 768px.
const barRef = useTemplateRef<HTMLElement>("bar")
const { height } = useElementSize(barRef)
watchEffect(() => {
    document.documentElement.style.setProperty("--phone-list-filter-height", `${Math.round(height.value)}px`)
})
</script>

<style scoped>
/*
 * Pinned only where it earns its place. Below 1024px the tables render as stacked card lists, so
 * the page is tall enough that the filter would otherwise scroll away on the first swipe; at
 * desktop widths the table fits and a pinned bar would only take space.
 *
 * The min-height guard is the WCAG 1.4.10 concern: on a short viewport - a landscape phone, or a
 * zoomed page - a bar fixed to the top eats a large share of what is left to read. Under 480px of
 * height it scrolls with the page like anything else.
 *
 * Focus targets that the browser scrolls into view can land underneath a sticky bar. The paired
 * .sticky-filter-offset utility in base.css gives them the scroll-margin to clear it.
 */
@media (width <= 1023.98px) and (height >= 480px) {
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

        /* Opaque: cards scrolling underneath must never show through the field. */
        background-color: var(--surface, #fff);
        padding-block: 0.5rem;
    }
}
</style>
