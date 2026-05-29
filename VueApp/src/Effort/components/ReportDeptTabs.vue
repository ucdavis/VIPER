<template>
    <q-tabs
        v-model="activeDept"
        dense
        align="left"
        class="text-grey-8 tabs-no-fade"
        active-color="primary"
        active-bg-color="grey-2"
        indicator-color="primary"
        narrow-indicator
    >
        <q-tab
            v-for="(dept, idx) in departments"
            :key="dept.department"
            :name="idx"
            :label="dept.department"
        />
    </q-tabs>

    <q-separator />

    <q-tab-panels
        v-model="activeDept"
        animated
    >
        <q-tab-panel
            v-for="(dept, idx) in departments"
            :key="dept.department"
            :name="idx"
            class="q-px-none"
            tabindex="0"
        >
            <!-- Only render content for active panel to avoid browser freeze -->
            <template v-if="idx === activeDept">
                <slot
                    :dept="dept"
                    :index="idx"
                />
            </template>
        </q-tab-panel>
    </q-tab-panels>
</template>

<script setup lang="ts" generic="T extends { department: string }">
import { ref, watch } from "vue"

const props = defineProps<{
    departments: T[]
}>()

defineSlots<{
    default(props: { dept: T; index: number }): unknown
}>()

const activeDept = ref(0)

watch(
    () => props.departments,
    () => {
        activeDept.value = 0
    },
)
</script>
