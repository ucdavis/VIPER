<template>
    Assessments
</template>
<script setup lang="ts">
    import { useUserStore } from '@/store/UserStore'
</script>
<script lang="ts">
    
    export default {
        name: "Assessments",
        computed: {
            userStore: () => useUserStore()
        }

    }
</script>