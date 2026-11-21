<script setup lang="ts">
import { VIPER2_INFO_URL } from "@/config/external-links"

withDefaults(
    defineProps<{
        className?: string
    }>(),
    {
        className: "gt-sm text-white q-py-none q-ml-md self-end",
    },
)

const environment = import.meta.env.VITE_ENVIRONMENT
const viperHome = import.meta.env.VITE_VIPER_HOME
</script>

<template>
    <q-btn
        flat
        dense
        no-caps
        :class="className"
        :href="viperHome"
    >
        <span class="mainLayoutViper">VIPER 2.0</span>
        <q-badge
            v-if="environment == 'DEVELOPMENT'"
            color="negative"
            role="presentation"
            class="mainLayoutViperMode"
            >Development</q-badge
        >
        <q-badge
            v-if="environment == 'TEST'"
            color="negative"
            role="presentation"
            class="mainLayoutViperMode"
            >Test</q-badge
        >
    </q-btn>
    <q-btn
        flat
        dense
        round
        size="sm"
        icon="info_outline"
        class="gt-sm self-end"
        :href="VIPER2_INFO_URL"
        target="_blank"
        rel="noopener"
        aria-label="About VIPER 2.0 (opens in new window)"
    >
        <q-tooltip>About VIPER 2.0</q-tooltip>
    </q-btn>
</template>
