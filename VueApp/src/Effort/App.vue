<template>
    <main>
        <component
            :is="$route.meta.layout || 'div'"
            nav="viper-effort"
            :navarea="true"
            highlighted-top-nav="Faculty"
        />
    </main>
    <GenericError />
</template>

<script lang="ts">
import GenericError from "@/components/GenericError.vue"
export default {
    name: "EffortApplication",
    components: {
        GenericError,
    },
}
</script>
