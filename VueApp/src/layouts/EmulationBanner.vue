<script setup lang="ts">
import StatusBanner from "@/components/StatusBanner.vue"
import { useUserStore } from "@/store/UserStore"

const userStore = useUserStore()
const clearEmulationHref = import.meta.env.VITE_VIPER_HOME + "ClearEmulation"
</script>

<template>
    <StatusBanner
        v-if="userStore.isEmulating"
        type="warning"
        class="q-ml-lg"
    >
        <strong>EMULATING:</strong>
        {{ userStore.userInfo.firstName }} {{ userStore.userInfo.lastName }}
        <template #action>
            <q-btn
                no-caps
                dense
                :href="clearEmulationHref"
                color="secondary"
                class="text-white q-px-sm q-ml-md"
                >End Emulation</q-btn
            >
        </template>
    </StatusBanner>
</template>
