<template>
    <q-drawer
        v-model="localDrawerOpen"
        show-if-above
        elevated
        side="left"
        :mini="!localDrawerOpen"
        no-mini-animation
        :width="300"
        id="mainLeftDrawer"
        class="no-print"
    >
        <div
            class="q-pa-sm"
            id="leftNavMenu"
        >
            <q-btn
                dense
                round
                unelevated
                color="secondary"
                icon="close"
                aria-label="Close navigation menu"
                class="float-right lt-md"
                @click="localDrawerOpen = false"
            />
            <h2>Evaluate</h2>

            <q-list
                dense
                separator
            >
                <q-item
                    clickable
                    v-ripple
                    :to="{ name: 'Evaluate' }"
                    class="leftNavLink"
                >
                    <q-item-section>
                        <q-item-label lines="1">Evaluate</q-item-label>
                    </q-item-section>
                </q-item>

                <q-item
                    clickable
                    v-ripple
                    :href="viperOneUrl + 'eval/default.cfm'"
                    rel="noopener noreferrer"
                    class="leftNavLink"
                >
                    <q-item-section>
                        <q-item-label
                            lines="1"
                            class="external-label"
                        >
                            <span>VIPER Eval360</span>
                        </q-item-label>
                    </q-item-section>
                </q-item>
            </q-list>
        </div>
    </q-drawer>
</template>

<script setup lang="ts">
import { ref, watch, inject } from "vue"

const props = defineProps<{
    drawerOpen: boolean
    onDrawerChange: (value: boolean) => void
}>()

const viperOneUrl = inject<string>("viperOneUrl", "")

const localDrawerOpen = ref(props.drawerOpen)

watch(
    () => props.drawerOpen,
    (newValue) => {
        localDrawerOpen.value = newValue
    },
)

watch(localDrawerOpen, (newValue) => {
    props.onDrawerChange(newValue)
})
</script>

<style scoped>
.external-label {
    display: inline-flex;
    align-items: center;
}
</style>

