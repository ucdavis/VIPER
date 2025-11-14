<template>
    <q-list separator>
        <q-item
            v-for="(student, index) in students"
            :key="student.mailId"
            clickable
            @click="emit('student-click', index)"
        >
            <q-item-section avatar>
                <q-avatar size="60px">
                    <q-img
                        :src="getPhotoUrl(student)"
                        spinner-color="primary"
                    >
                        <template #error>
                            <div class="absolute-full flex flex-center bg-grey-3">
                                <q-icon
                                    name="person"
                                    size="30px"
                                    color="grey-6"
                                />
                            </div>
                        </template>
                    </q-img>
                </q-avatar>
            </q-item-section>

            <q-item-section>
                <q-item-label>{{ student.displayName }}</q-item-label>
                <q-item-label caption>{{ student.mailId }}@ucdavis.edu</q-item-label>
            </q-item-section>

            <q-item-section
                v-if="student.secondaryTextLines.length > 0"
                side
            >
                <q-item-label
                    caption
                    class="text-grey-7"
                >
                    {{ student.secondaryTextLines.join(" • ") }}
                </q-item-label>
            </q-item-section>

            <q-item-section
                v-if="student.isRossStudent"
                side
            >
                <q-badge color="primary"> Ross Student </q-badge>
            </q-item-section>
        </q-item>
    </q-list>
</template>

<script setup lang="ts">
import type { StudentPhoto } from "../../services/photo-gallery-service"
import { getPhotoUrl } from "../../composables/use-photo-url"

defineProps<{
    students: StudentPhoto[]
}>()

const emit = defineEmits<{
    "student-click": [index: number]
}>()
</script>
