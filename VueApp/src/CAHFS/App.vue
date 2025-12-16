<template>
    <main>
        <component
            :is="$route.meta.layout || 'div'"
            nav="cahfs-nav"
            :navarea="false"
            highlighted-top-nav="CAHFS"
            :breadcrumbs="$route.meta?.breadcrumbs"
			:show-view-when-not-logged-in="$route.meta.showViewWhenNotLoggedIn || false"
        >
        </component>
    </main>
    <GenericError></GenericError>
</template>

<script>
import GenericError from "@/components/GenericError.vue"
export default {
    name: "CAHFSApplication",
    components: {
        GenericError,
    },
}
</script>
