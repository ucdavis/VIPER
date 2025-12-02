<template>
    <main>
        <component
            :is="$route.meta.layout || 'div'"
            nav="cms"
            :navarea="true"
            highlighted-top-nav="Viper Home"
            :breadcrumbs="$route.meta?.breadcrumbs"
        >
        </component>
    </main>
    <GenericError></GenericError>
</template>

<script>
import GenericError from "@/components/GenericError.vue"
export default {
    name: "CMSApplication",
    components: {
        GenericError,
    },
}
</script>
