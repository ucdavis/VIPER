<template>
    <!-- Nothing to navigate between with one destination, and nothing to offer when a search has
         emptied them all. -->
    <q-btn
        v-if="targets.length > 1"
        dense
        flat
        no-caps
        icon="list"
        label="Jump to section"
        class="text-body2"
    >
        <!--
            A floating menu rather than an inline panel. This component renders inside the sticky
            filter bar, whose measured height is published as --phone-list-filter-height and is what
            every jump target's scroll-margin is built from. An inline panel changes that height on
            open, and the republished value always lags the click, so the browser scrolls using a
            stale margin and lands short of the heading. A portalled menu never enters the bar's
            flow, so the height it publishes stays true.

            transition-duration 0 because motion is near-absent here by design, and because an
            animated close would still be running when the browser follows the link.

            no-refocus because closing would otherwise pull focus back to this button and undo the
            fragment navigation's move to the heading. Focus on open is left alone: a keyboard user
            opening the menu should land in it.
        -->
        <q-menu
            :transition-duration="0"
            no-refocus
            auto-close
        >
            <nav aria-label="Phone list sections">
                <ul class="section-jump-links q-pa-sm q-ma-none">
                    <li
                        v-for="target in targets"
                        :key="target.id"
                    >
                        <!-- A plain anchor, not a router-link: the browser's own fragment navigation
                             honours the scroll-margin that clears the header and filter bar, and moves
                             focus to the heading. The router's scrollBehavior would position with
                             window.scrollTo, which ignores scroll-margin entirely. -->
                        <a
                            :href="`#${target.id}`"
                            class="text-primary"
                            >{{ target.label }}</a
                        >
                    </li>
                </ul>
            </nav>
        </q-menu>
    </q-btn>
</template>

<script setup lang="ts">
export type JumpTarget = { id: string; label: string }

defineProps<{ targets: JumpTarget[] }>()
</script>

<style scoped>
/* A wrapping row of links rather than a stacked list: a dozen sections stacked would be taller
   than the content they navigate to. */
.section-jump-links {
    display: flex;
    flex-wrap: wrap;
    gap: 0.25rem 1rem;
    list-style: none;
}
</style>
