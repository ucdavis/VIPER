<template>
    <q-dialog
        :model-value="modelValue"
        aria-labelledby="content-diff-title"
        @update:model-value="emit('update:modelValue', $event)"
    >
        <q-card class="dialog-card-lg">
            <q-card-section class="row items-center q-pb-none">
                <div
                    id="content-diff-title"
                    class="text-h6"
                >
                    {{ title }}
                </div>
                <q-space />
                <q-btn
                    v-close-popup
                    flat
                    round
                    dense
                    icon="close"
                    aria-label="Close"
                />
            </q-card-section>

            <!-- Direction (old vs new) is the most important thing to read, so it sits at body
                 contrast, not as a muted caption. -->
            <q-card-section
                v-if="subtitle"
                class="text-body2 q-pb-none"
            >
                {{ subtitle }}
            </q-card-section>

            <!-- Legend only when there are real changes to read; it ties each color to a version. -->
            <q-card-section
                v-if="hasComparison && hasChanges && !loading"
                class="row items-center q-gutter-sm q-pb-none text-caption"
            >
                <span class="cms-diff-legend"><ins class="diffins">added</ins> (newer)</span>
                <span class="cms-diff-legend"><del class="diffdel">removed</del> (older)</span>
            </q-card-section>

            <q-separator class="q-mt-sm" />

            <q-card-section
                v-if="loading"
                class="diff-body"
            >
                <div
                    class="row items-center"
                    role="status"
                >
                    <q-spinner color="primary" />
                    <span class="sr-only">Loading diff</span>
                </div>
            </q-card-section>

            <!-- Identical versions: state it plainly, no body (there is nothing to diff). -->
            <q-card-section
                v-else-if="hasComparison && !hasChanges"
                class="text-body2 text-grey-8"
                role="status"
            >
                These two versions are identical.
            </q-card-section>

            <template v-else>
                <q-card-section
                    v-if="!hasComparison"
                    class="text-body2 text-grey-8"
                    role="status"
                >
                    {{ emptyMessage }}
                </q-card-section>
                <q-card-section class="scroll diff-body">
                    <!-- diffHtml is re-sanitized server-side after diffing (SanitizeDiff), keeping only ins/del markers -->
                    <!-- eslint-disable vue/no-v-html -->
                    <div
                        v-html="diffHtml"
                        class="content-block cms-diff"
                    ></div>
                    <!-- eslint-enable vue/no-v-html -->
                </q-card-section>
            </template>
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
withDefaults(
    defineProps<{
        modelValue: boolean
        loading?: boolean
        title?: string
        subtitle?: string
        diffHtml?: string
        hasComparison?: boolean
        hasChanges?: boolean
        emptyMessage?: string
    }>(),
    {
        loading: false,
        title: "Content differences",
        subtitle: "",
        diffHtml: "",
        hasComparison: true,
        hasChanges: true,
        emptyMessage: "This is the original version, so there is nothing to compare it against.",
    },
)

const emit = defineEmits<{ "update:modelValue": [value: boolean] }>()
</script>

<style scoped>
.diff-body {
    max-height: 60vh;
}

/* htmldiff.net wraps changes in ins/del with diffins/diffdel/diffmod classes. Underline for
   additions and strikethrough for deletions carry the meaning alongside the tint, so the diff
   stays legible without relying on color. Ink text (--q-dark) on these light tints clears WCAG AA. */
.cms-diff :deep(ins.diffins),
.cms-diff :deep(ins.diffmod),
.cms-diff-legend ins {
    background-color: #d7efdb;
    color: var(--q-dark);
    text-decoration: underline;
}

.cms-diff :deep(del.diffdel),
.cms-diff :deep(del.diffmod),
.cms-diff-legend del {
    background-color: #f6d9dc;
    color: var(--q-dark);
    text-decoration: line-through;
}

.cms-diff :deep(ins.diffins img),
.cms-diff :deep(ins.diffmod img) {
    outline: 2px solid var(--q-positive);
}

.cms-diff :deep(del.diffdel img),
.cms-diff :deep(del.diffmod img) {
    outline: 2px solid var(--q-negative);
}

/* Keep arbitrary historical content (wide tables, large images) inside the dialog. */
.cms-diff :deep(img) {
    max-width: 100%;
    height: auto;
}

.cms-diff :deep(table) {
    max-width: 100%;
}
</style>
