<script setup lang="ts">
    import type { Ref } from 'vue'
    import { ref, watch, defineProps, defineEmits } from 'vue'
    import type { Student } from '@/CTS/types'
    import { useFetch } from '@/composables/ViperFetch'

    const props = defineProps({
        selectedFilter: {
            type: String
        },
        serviceId: {
            type: Number
        },
        clearStudent: {
            type: Boolean
        },
    })

    const students = ref([]) as Ref<Student[]>
    const allStudents = ref([]) as Ref<Student[]>
    const studentsOnService = ref([]) as Ref<Student[]>
    const studentsLoadedForServiceId = ref(0)
    const selectedStudent = ref({ lastName: "", firstName: "", mailId: "", personId: 0 }) as Ref<Student>
    const studentOptionsType = ref(props.selectedFilter)

    const baseUrl = import.meta.env.VITE_API_URL + "cts/"
    const studentsUrl = import.meta.env.VITE_API_URL + "students/"
    const photoBaseUrl = "https://viper.vetmed.ucdavis.edu/public/utilities/getbase64image.cfm?mailid="

    const emit = defineEmits(['studentChange'])

    const handleStudentChange = (event: any) => {
        emit('studentChange', selectedStudent.value?.personId ?? 0)
    }

    /* Get students by service, class level, or all */
    async function getStudents() {
        const { get } = useFetch()
        //load all students and students on service, if necessary
        if (allStudents.value.length == 0) {
            const r= await get(studentsUrl + "dvm")
            allStudents.value = r.result
        }

        if (props.serviceId && props.serviceId != studentsLoadedForServiceId.value) {
            var d = new Date().toJSON().split("T")[0]
            const r = await get(baseUrl + "clinicalschedule/student?serviceId=" + props.serviceId + "&startDate=" + d + "&endDate=" + d)
            studentsOnService.value = r.result
            studentsLoadedForServiceId.value = props.serviceId
        }

        switch (studentOptionsType.value) {
            case "Service":
                students.value = studentsOnService.value
                break;
            case "V4":
                students.value = allStudents.value.filter(s => s.classLevel == 'V4')
                break;
            default:
                students.value = allStudents.value
                break;
        }
    }
    function studentSearch(val: string, update: any, abort: any) {
        if (val === '') {
            update(() => getStudents())
            return
        }
        update(() => {
            const s = val.toLowerCase()
            students.value = allStudents.value
                .filter(v => (v.firstName.toLowerCase() + v.lastName.toLowerCase()).indexOf(s) > -1)
        })
    }
    watch(props, () => {
        if (props.clearStudent) {
            selectedStudent.value = { lastName: "", firstName: "", mailId: "", personId: 0 } as Student
        }
        getStudents()
        
    })
    watch(selectedStudent, (e) => {
        handleStudentChange(e)
    })
    getStudents()
</script>

<template>
    <div class="row items-center">
        <div class="col-12 col-md-8">
            <q-select dense options-dense label="Student" class="q-mr-md items-center"
                      :outlined="selectedStudent == null" :borderless="selectedStudent != null" :stack-label="false"
                      use-input input-debounce="300" @filter="studentSearch" clearable
                      v-model="selectedStudent" :options="students" option-label="lastName" option-value="personId">
                <template v-slot:selected>
                    {{ selectedStudent?.lastName }}{{ selectedStudent?.lastName?.length ? ',' : '' }} {{selectedStudent?.firstName}}
                </template>
                <template v-slot:after>
                    <q-avatar v-show="selectedStudent?.mailId" rounded class="fit">
                        <q-img :src="photoBaseUrl + selectedStudent?.mailId +'&altphoto=1'"
                               class="smallPhoto rounded-borders" loading="eager" :no-spinner="true"></q-img>
                    </q-avatar>
                </template>
                <template v-slot:no-option>
                    <div class="q-pa-sm">No students found matching the filter</div>
                </template>
                <template v-slot:option="std">
                    <q-item v-bind="std.itemProps">
                        <!--
                        <q-item-section avatar>
                            <q-avatar rounded>
                                <q-img :src="photoBaseUrl + std.opt.mailId +'&altphoto=1'"
                                       class="smallPhoto" loading="eager" no-spinner="true"></q-img>
                            </q-avatar>
                        </q-item-section>
                        -->
                        <q-item-section>
                            <q-item-label>{{std.opt.lastName}}, {{std.opt.firstName}}</q-item-label>
                        </q-item-section>
                    </q-item>
                </template>
            </q-select>
        </div>
        <div class="col-auto">
            <q-btn-toggle v-model="studentOptionsType" push toggle-color="primary"
                          :options="[{label: 'Service', value: 'Service'}, {label: 'V4', value: 'V4'}, {label: 'All', value: 'All'}]">
            </q-btn-toggle>
        </div>
    </div>
</template>