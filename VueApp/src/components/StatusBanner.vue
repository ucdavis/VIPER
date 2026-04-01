<template>
    <q-banner
        v-if="visible"
        :class="bannerClass"
        :role="ariaRole"
        rounded
    >
        <template #avatar>
            <q-icon
                :name="resolvedIcon"
                :class="iconClass"
            />
        </template>
        <div class="text-body2">
            <slot />
        </div>
        <template
            v-if="$slots.action || dismissible"
            #action
        >
            <slot name="action" />
            <q-btn
                v-if="dismissible"
                flat
                dense
                round
                icon="close"
                aria-label="Dismiss"
                :class="iconClass"
                @click="visible = false"
            />
        </template>
    </q-banner>
</template>

<script setup lang="ts">
import { computed, ref } from "vue"

type BannerType = "success" | "error" | "warning" | "info"

const {
    type,
    icon = "",
    dismissible = false,
} = defineProps<{
    type: BannerType
    icon?: string
    dismissible?: boolean
}>()

const visible = ref(true)

const typeConfig: Record<BannerType, { icon: string; bg: string; iconCls: string }> = {
    success: { icon: "check_circle", bg: "status-banner-success", iconCls: "text-positive" },
    error: { icon: "error", bg: "status-banner-error", iconCls: "text-negative" },
    warning: { icon: "warning", bg: "status-banner-warning", iconCls: "status-banner-warning-icon" },
    info: { icon: "info", bg: "status-banner-info", iconCls: "status-banner-info-icon" },
}

const config = computed(() => typeConfig[type])

const resolvedIcon = computed(() => icon ?? config.value.icon)
const bannerClass = computed(() => `${config.value.bg} q-mb-md`)
const iconClass = computed(() => config.value.iconCls)
const ariaRole = computed(() => (type === "error" || type === "warning" ? "alert" : "status"))
</script>

<style scoped>
.status-banner-success {
    background-color: color-mix(in srgb, var(--q-positive) 12%, white);
    border-left: 0.25rem solid var(--q-positive);
    color: var(--q-positive);
}

.status-banner-error {
    background-color: color-mix(in srgb, var(--q-negative) 12%, white);
    border-left: 0.25rem solid var(--q-negative);
    color: var(--q-negative);
}

.status-banner-warning {
    background-color: color-mix(in srgb, var(--q-warning) 15%, white);
    border-left: 0.25rem solid var(--q-warning);
    color: #5d4600;
}

.status-banner-warning-icon {
    color: #5d4600;
}

.status-banner-info {
    background-color: color-mix(in srgb, var(--q-info) 12%, white);
    border-left: 0.25rem solid var(--q-info);
    color: var(--ucdavis-blue-80, #335379);
}

.status-banner-info-icon {
    color: var(--ucdavis-blue-80, #335379);
}
</style>
