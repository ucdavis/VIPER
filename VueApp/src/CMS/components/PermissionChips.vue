<template>
    <div :class="stacked ? 'column items-start chips-stacked' : ''">
        <template v-if="permissions.length || peopleCount > 0">
            <q-chip
                v-for="p in permissions.slice(0, maxShown)"
                :key="p"
                dense
                square
                size="sm"
            >
                {{ p }}
            </q-chip>
            <q-chip
                v-if="permissions.length > maxShown"
                dense
                square
                clickable
                size="sm"
                color="grey-4"
                :aria-label="`+${permissions.length - maxShown} more, show all ${permissions.length} ${inflect('permission', permissions.length)}`"
            >
                +{{ permissions.length - maxShown }} more
                <q-menu
                    anchor="bottom left"
                    self="top left"
                >
                    <q-list dense>
                        <q-item-label
                            header
                            class="q-py-xs"
                        >
                            Permissions
                        </q-item-label>
                        <q-item
                            v-for="p in permissions"
                            :key="p"
                        >
                            <q-item-section>{{ p }}</q-item-section>
                        </q-item>
                    </q-list>
                </q-menu>
            </q-chip>
            <q-chip
                v-if="peopleCount > 0"
                dense
                square
                size="sm"
                icon="person"
                color="grey-4"
                :aria-label="`${peopleCount} ${inflect('person', peopleCount)}`"
            >
                {{ peopleCount }}
            </q-chip>
        </template>
        <span
            v-else
            class="text-grey-7"
        >
            All VIPER users
        </span>
    </div>
</template>

<script setup lang="ts">
import { inflect } from "inflection"

withDefaults(
    defineProps<{
        permissions: string[]
        peopleCount?: number
        maxShown?: number
        // Stack chips vertically (for narrow table columns) instead of flowing inline.
        stacked?: boolean
    }>(),
    { peopleCount: 0, maxShown: 2, stacked: false },
)
</script>

<style scoped>
/* When stacked, drop the chips' horizontal margins so they sit flush-left
   and tighten the vertical rhythm. */
.chips-stacked .q-chip {
    margin-left: 0;
    margin-right: 0;
}
</style>
