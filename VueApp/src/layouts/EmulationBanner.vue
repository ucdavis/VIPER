<script setup lang="ts">
import { useUserStore } from "@/store/UserStore"

const userStore = useUserStore()
const clearEmulationHref = import.meta.env.VITE_VIPER_HOME + "ClearEmulation"
</script>

<template>
    <q-banner
        v-if="userStore.isEmulating"
        dense
        rounded
        inline-actions
        class="bg-warning text-black q-ml-lg"
    >
        <strong>EMULATING:</strong>
        {{ userStore.userInfo.firstName }} {{ userStore.userInfo.lastName }}
        <q-btn
            no-caps
            dense
            :href="clearEmulationHref"
            color="secondary"
            class="text-white q-px-sm q-ml-md"
            >End Emulation</q-btn
        >
    </q-banner>
</template>
