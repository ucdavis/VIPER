<template>
    <q-card class="student-photo-card">
        <q-img
            :src="photoUrl"
            :ratio="3 / 4"
            loading="lazy"
            spinner-color="primary"
            no-default-spinner
            class="cursor-pointer"
            @click="handleClick"
        >
            <template #error>
                <div class="absolute-full flex flex-center bg-grey-3">
                    <q-icon
                        name="person"
                        size="50px"
                        color="grey-6"
                    />
                </div>
            </template>
        </q-img>

        <q-card-section class="text-center q-py-xs q-px-sm card-content">
            <div class="text-body1 text-weight-bold">{{ student.lastName }}</div>
            <div class="text-body1">{{ student.firstName }}</div>
            <div
                v-for="(line, index) in student.secondaryTextLines"
                :key="index"
                class="text-body1 text-grey"
            >
                {{ line }}
            </div>
            <div class="badge-container">
                <q-badge
                    v-if="student.isRossStudent"
                    color="primary"
                    class="text-weight-bold badge-text"
                >
                    Ross Student
                </q-badge>
            </div>
        </q-card-section>
    </q-card>
</template>

<script setup lang="ts">
import { computed } from "vue"
import type { StudentPhoto } from "../../services/photo-gallery-service"
import { getPhotoUrl } from "../../composables/use-photo-url"

const props = defineProps<{
    student: StudentPhoto
    currentIndex?: number
}>()

const emit = defineEmits<{
    click: []
}>()

const photoUrl = computed(() => getPhotoUrl(props.student))

function handleClick(event: Event) {
    event.stopPropagation()
    event.preventDefault()
    emit("click")
}
</script>

<style scoped>
@media screen and (prefers-reduced-motion: reduce) {
    .student-photo-card {
        transition: none;
    }
}

.student-photo-card {
    transition: transform 0.2s;
    display: flex;
    flex-direction: column;
    height: 100%;
}

.student-photo-card:hover,
.student-photo-card:focus {
    transform: translateY(-2px);
    box-shadow: 0 4px 8px rgb(0 0 0 / 15%);
}

.card-content {
    line-height: 1.3;
}

.badge-container {
    min-height: 16px;
    display: flex;
    align-items: center;
    justify-content: center;
}

.badge-text {
    font-size: 14px;
}
</style>
