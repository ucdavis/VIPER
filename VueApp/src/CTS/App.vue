<template>
    <main>
        <component :is="$route.meta.layout || 'div'"
                   nav="cts" navarea="true">
            <router-view></router-view>
        </component>
    </main>
</template>

<script>
    export default {
        name: 'CTSApplication',
    }
</script>
