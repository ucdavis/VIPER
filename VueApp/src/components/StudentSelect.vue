<script setup lang="ts">
    import type { Ref, PropType } from 'vue'
    import { ref, watch, defineProps, defineEmits } from 'vue'
    import type { Student } from '@/CTS/types'
    import { useFetch } from '@/composables/ViperFetch'

    const props = defineProps({
        //which filter should be selected when the component loads. this value will be assigned to studentOptionsType
        selectedFilter: {
            type: String,
            default: "all"
        },
        //this is the service id that will be used to show the students on a service option type
        //if not provided, service studentOptionsType button will not be shown
        serviceId: {
            type: Number
        },
        //pass in clear student to force clear the selected student (for example, if some change to the parent has made the selection invalid)
        clearStudent: {
            type: Boolean
        },
        //the class years to show, or "all" to show all class years, or "active" to show class years with active students
        //if not provided, class year select will not be shown
        classYears: {
            type: Array as PropType<string[]>
        },
        //defines the behavior for the all students button. "active", "inactive", "all", or "hide".
        //defaults to active. if set to hide, the all button will not be shown.
        allStudents: {
            type: String,
            default: "active"
        },
        //if allStudents behavior is "all" and statusToggle is true, show active and inactive buttons, instead of the all students button
        statusToggle: {
            type: Boolean
        },
        //class levels to show, V1 to V4. if not provided, class level buttons will not be shown
        classLevel: {
            type: Array as PropType<string[]>
        },
        //if class levels are defined, this is the term that will be used to look up class level
        termCode: {
            type: Number
        },
        //display options outlined and borderless
        outlined: {
            type: Boolean,
            default: true
        },
        borderless: {
            type: Boolean,
            default: false
        },
    })

    //use our fetch wrapper
    const { get } = useFetch()
    const baseUrl = import.meta.env.VITE_API_URL + "cts/"
    const studentsUrl = import.meta.env.VITE_API_URL + "students/"
    //TODO: get photos from localhost or test
    const photoBaseUrl = "https://viper.vetmed.ucdavis.edu/public/utilities/getbase64image.cfm?mailid="

    //contains the students currently being shown in the select list
    const students = ref(null) as Ref<Student[] | null>
    //contains the students that are shown when all students is selected.
    //if allStudents is "all" and statusToggle is true, contains both inactive and active students
    const allStudents = ref([]) as Ref<Student[]>
    //keep track of the students on the service, as well as the service id the students were loaded for
    const studentsOnService = ref([]) as Ref<Student[]>
    const studentsLoadedForServiceId = ref(0)
    //keep track of students by class year, and the class year used to load students
    const classYearOptions = ref([]) as Ref<String[]>
    const studentsForClassYear = ref([]) as Ref<Student[]>
    const studentsLoadedForClassYear = ref(0)
    const selectedClassYear = ref(null) as Ref<number | null>
    //the student currently selected
    const selectedStudent = ref(null) as Ref<Student | null>//{ lastName: "", firstName: "", mailId: "", personId: 0 }
    //the select options button
    const studentOptionsType = ref(props.selectedFilter)
    const studentOptions = ref([]) as Ref<{ label: string, value: string }[]>
    const loading = ref(false)
    const borderless = ref(props.borderless)
    const outlined = ref(props.outlined)

    //we will emit an event named 'studentChange' when the selected student is changed
    const emit = defineEmits(['studentChange'])
    const handleStudentChange = (event: any) => {
        emit('studentChange', selectedStudent.value?.personId ?? 0)
    }

    /* Set up the studentOptions object to have the options that should be used for filtering the student list */
    async function setupFilterOptions() {
        let options = []
        if (props.serviceId) {
            options.push("Service")
        }
        if (props.allStudents == "all" && props.statusToggle) {
            options.push("Active")
            options.push("Inactive")
        }
        else if (props.allStudents != "hide") {
            options.push("All")
        }
        if (props.classLevel && props.classLevel.length) {
            props.classLevel.forEach((cl) => {
                if (["V1", "V2", "V3", "V4"].indexOf(cl) >= 0) {
                    options.push(cl)
                }
            })
        }

        setupClassYears()
        if (props.classYears != undefined && props.classYears.length > 0) {
            options.push("Class of")
        }

        if (options.length > 1) {
            studentOptions.value = options.map(s => ({ label: s, value: s, slot: s.toLowerCase().replace(" ", "-") }))
        }
    }

    /* Take class year options and create the list for the class years dropdown */
    async function setupClassYears() {
        let classYears = []
        if (props.classYears != undefined && props.classYears.length > 0) {
            if (props.classYears.length == 1 && props.classYears[0] == "all") {
                const r = await get(studentsUrl + "dvm/classYears?activeOnly=false")
                classYears = r.result
            }
            else if (props.classYears.length == 1 && props.classYears[0] == "active") {
                const r = await get(studentsUrl + "dvm/classYears")
                classYears = r.result
            }
            else {
                classYears = props.classYears
            }
        }
        classYears.sort((c1: string, c2: string) => parseInt(c2) - parseInt(c1))
        classYearOptions.value = classYears
    }

    /* Get students by service, class level, or all */
    async function getStudents() {
        let optionsType = studentOptionsType.value
        if (optionsType?.startsWith('Class')) {
            optionsType = "ByClass"
        }

        switch (studentOptionsType.value) {
            case "Service":
                students.value = await getServiceStudents()
                break;
            case "V4":
            case "V3":
            case "V2":
            case "V1":
                students.value = (await getAllStudents()).filter(s => s.classLevel == studentOptionsType.value)
                break;
            case "Class of":
                students.value = await getStudentsByClassYear()
                break;
            case "All":
            case "Active":
            case "Inactive":
                students.value = await getAllStudents()
                break;
            default:
                break;
        }
    }

    //get 'all' students - may include active, inactive, or both depending on props
    async function getAllStudents() {
        //cache allStudents - only load once
        if (allStudents.value.length == 0) {
            let u = studentsUrl + "dvm"
            if (props.allStudents == "inactive" || props.allStudents == "all") {
                u += "?includeAllClassYears=true"
            }
            loading.value = true
            const r = await get(u)
            allStudents.value = r.result
            loading.value = false
        }
        switch (studentOptionsType.value) {
            case "Active":
                return allStudents.value.filter(s => s.currentClassYear && s.active)
            case "Inactive":
                return allStudents.value.filter(s => !s.currentClassYear || !s.active)
            default:
                return allStudents.value
        }
    }

    //get students currently on given service id
    async function getServiceStudents() {
        if (props.serviceId && props.serviceId != studentsLoadedForServiceId.value) {
            var d = new Date().toJSON().split("T")[0]
            loading.value = true
            const r = await get(baseUrl + "clinicalschedule/student?serviceId=" + props.serviceId + "&startDate=" + d + "&endDate=" + d)
            studentsOnService.value = r.result
            studentsLoadedForServiceId.value = props.serviceId
            loading.value = false
        }
        return studentsOnService.value
    }

    //get students by class year
    async function getStudentsByClassYear() {
        if (selectedClassYear.value && selectedClassYear.value > 0) {
            if (selectedClassYear.value != studentsLoadedForClassYear.value) {
                loading.value = true
                const r = await get(studentsUrl + "dvm?classYear=" + selectedClassYear.value.toString())
                studentsForClassYear.value = r.result
                studentsLoadedForClassYear.value = selectedClassYear.value
                loading.value = false
            }
            return studentsForClassYear.value
        }
        return []
    }

    //filter students - start with whatever student population is currently selected and then filter on name
    function studentSearch(val: string, update: any, abort: any) {
        if (students == null) {
            getStudents()
        }
        if (val === '') {
            update(() => getStudents())
            return
        }
        update(() => {
            const s = val.toLowerCase()
            if (students.value != null) {
                students.value = students.value
                    .filter(v => (v.firstName.toLowerCase() + v.lastName.toLowerCase()).indexOf(s) > -1)
            }
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

    setupFilterOptions()
    getStudents()
</script>

<template>
    <div class="row items-center">
        <div class="col-auto">
            <!--:outlined="selectedStudent == null" :borderless="selectedStudent != null" -->
            <q-select dense options-dense clearable
                      label="Student"
                      v-model="selectedStudent"
                      :options="students ?? []" option-label="lastName" option-value="personId"
                      class="q-mr-md items-center"
                      :loading="loading"
                      :stack-label="false"
                      :outlined="props.outlined"
                      :borderless="props.borderless"
                      use-input input-debounce="300" @filter="studentSearch">
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
            <q-btn-toggle v-model="studentOptionsType" push toggle-color="primary" toggle-text-color="white" no-caps
                          :options="studentOptions">
                <template v-slot:class-of>
                    <div class="row items-center no-wrap" v-if="studentOptionsType == 'Class of'">
                        <!--v-if="studentOptionsType == 'Class of'"-->
                        <q-select v-model="selectedClassYear" dense options-dense right
                                  :options="classYearOptions" emit-value class="q-ml-sm q-py-none" input-class="q-py-none"
                                  label-color="white" color="white" dark>
                            <template v-if="selectedClassYear" v-slot:append>
                                <q-icon name="cancel" @click.stop.prevent="selectedClassYear = null" class="cursor-pointer" size="sm" />
                            </template>
                        </q-select>
                    </div>
                </template>
            </q-btn-toggle>
        </div>
    </div>
</template>