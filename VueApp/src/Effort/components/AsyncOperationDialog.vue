<script setup lang="ts">
withDefaults(
    defineProps<{
        modelValue: boolean
        title: string
        subtitle?: string
        maxWidth?: string
        isLoading: boolean
        isCommitting: boolean
        loadError?: string | null
        progress?: number
        progressTitle?: string
        progressPhase?: string
        progressDetail?: string
        progressColor?: string
        loadingMessage?: string
    }>(),
    {
        subtitle: undefined,
        maxWidth: "1000px",
        loadError: null,
        progress: 0,
        progressTitle: "",
        progressPhase: "",
        progressDetail: "",
        progressColor: "primary",
        loadingMessage: "Generating preview...",
    },
)

defineEmits<{
    retry: []
    close: []
}>()
</script>

<template>
    <q-dialog
        :model-value="modelValue"
        persistent
        maximized-on-mobile
        @keydown.escape="$emit('close')"
    >
        <q-card :style="`width: 100%; max-width: ${maxWidth}; position: relative`">
            <q-btn
                icon="close"
                flat
                round
                dense
                class="absolute-top-right q-ma-sm"
                style="z-index: 1"
                aria-label="Close dialog"
                @click="$emit('close')"
            />
            <q-card-section class="q-pb-none q-pr-xl">
                <div class="text-h6">{{ title }}</div>
                <div
                    v-if="subtitle"
                    class="text-caption text-grey-7"
                >
                    {{ subtitle }}
                </div>
            </q-card-section>

            <slot name="before-body" />

            <!-- Loading State (Preview) -->
            <q-card-section
                v-if="isLoading"
                class="text-center q-py-xl"
            >
                <q-spinner-dots
                    size="50px"
                    color="primary"
                />
                <div class="q-mt-md text-grey-7">{{ loadingMessage }}</div>
            </q-card-section>

            <!-- Committing State (Progress) -->
            <q-card-section
                v-else-if="isCommitting"
                class="q-py-xl"
            >
                <div class="text-h6 q-mb-md text-center">{{ progressTitle }}</div>
                <q-linear-progress
                    :value="progress"
                    size="25px"
                    :color="progressColor"
                    class="q-mb-md"
                >
                    <div class="absolute-full flex flex-center">
                        <q-badge
                            color="white"
                            :text-color="progressColor"
                            :label="`${Math.round(progress * 100)}%`"
                        />
                    </div>
                </q-linear-progress>
                <div class="text-center text-grey-7">{{ progressPhase }}</div>
                <div
                    v-if="progressDetail"
                    class="text-center text-caption text-grey-6 q-mt-xs"
                >
                    {{ progressDetail }}
                </div>
            </q-card-section>

            <!-- Error State -->
            <q-card-section
                v-else-if="loadError"
                class="text-center q-py-xl"
            >
                <q-icon
                    name="error"
                    color="negative"
                    size="48px"
                />
                <div class="q-mt-md text-negative">{{ loadError }}</div>
                <q-btn
                    label="Retry"
                    color="primary"
                    class="q-mt-md"
                    @click="$emit('retry')"
                />
            </q-card-section>

            <!-- Preview content -->
            <slot v-else />
        </q-card>
    </q-dialog>
</template>
