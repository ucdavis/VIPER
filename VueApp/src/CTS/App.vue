<template>
    <main>
        <component
            :is="$route.meta.layout || 'div'"
            nav="cts"
            :navarea="true"
            highlighted-top-nav="Curriculum"
            :breadcrumbs="$route.meta?.breadcrumbs"
        >
        </component>
    </main>
    <GenericError></GenericError>
</template>

<script>
import GenericError from "@/components/GenericError.vue"
export default {
    name: "CTSApplication",
    components: {
        GenericError,
    },
}
</script>
