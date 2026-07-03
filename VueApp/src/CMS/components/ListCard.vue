<template>
    <div class="col-12 q-pa-xs">
        <q-card
            flat
            bordered
            class="list-card"
        >
            <q-card-section class="q-gutter-y-xs">
                <slot name="header" />
                <slot />
            </q-card-section>
            <template v-if="$slots.actions">
                <q-separator />
                <q-card-actions align="right">
                    <slot name="actions" />
                </q-card-actions>
            </template>
        </q-card>
    </div>
</template>
