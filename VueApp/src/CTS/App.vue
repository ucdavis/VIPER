<template>
    <main>
        <component :is="$route.meta.layout || 'div'"
                   nav="cts" navarea="true" highlightedTopNav="Curriculum">
        </component>
    </main>
</template>

<script>
    export default {
        name: 'CTSApplication'
       
    }
</script>
