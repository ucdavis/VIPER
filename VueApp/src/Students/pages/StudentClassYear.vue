<script setup lang="ts">
import { ref, inject, watch } from "vue"
import { useRoute } from "vue-router"
import type { Ref } from "vue"
import type { QTableProps } from "quasar"
import { useFetch } from "@/composables/ViperFetch"
import type {
    StudentClassYear as StudentClassYearType,
    StudentClassYearUpdate,
    StudentClassYearProblem,
} from "../types"

type ClassYear = { label: string; value: number }
const route = useRoute()
const { get, put, del } = useFetch()
const apiUrl = inject("apiURL")
const viperUrl = inject("viperOneUrl")

//class year selection
const classYear = ref({ label: "", value: 0 })
const classYearOptions = ref([]) as Ref<ClassYear[]>
const activeOnly = ref(false)

//table columns, rows, properties
const cols: QTableProps["columns"] = [
    { name: "avatar", label: "", field: "", align: "left", style: "width:75px;" },
    { name: "name", label: "Student", field: "", align: "left", sortable: true },
    { name: "classyear", label: "Class Year", field: "personId", align: "left", sortable: true },
    { name: "previousyears", label: "Prev Years", field: "firstName", align: "left", sortable: true },
    { name: "problems", label: "Problems", field: "problems", align: "left", sortable: true },
]
const allStudents = ref([]) as Ref<StudentClassYearProblem[]>
const students = ref([]) as Ref<StudentClassYearProblem[]>
const problemsOnly = ref(false)
const loading = ref(false)
const filter = ref(null) as Ref<string | null>
const nodata = ref("")

//update class year form
const showForm = ref(false)
const selectedStudentName = ref("")
const studentClassYear = ref({
    studentClassYearId: 0,
    personId: 0,
    ross: false,
    leftReason: null,
    leftTerm: null,
    comment: "",
    active: false,
}) as Ref<StudentClassYearUpdate>
const terms = ref([]) as Ref<{ label: string; value: number }[]>
const reasons = ref([]) as Ref<{ label: string; value: number }[]>

async function getStudents() {
    loading.value = true
    students.value = []
    allStudents.value = []
    nodata.value = ""

    //if class year specified, add to url params and get students with this class year
    //TODO: simplify logic here and put on server
    if (classYear?.value && classYear.value.value) {
        let qp = new URLSearchParams(window.location.search)
        qp.set("classYear", classYear.value.value.toString())
        history.pushState(null, "", "?" + qp.toString())
        let u =
            apiUrl + "students/dvm/?classYear=" + classYear.value.value + "&includeAllClassYears=" + !activeOnly.value
        const r = await get(u)
        allStudents.value = r.result

        //get problems
        u = apiUrl + "students/dvm/classYearReport?classYear=" + classYear.value.value
        const problems = await get(u)
        problems.result.forEach((p: any) => {
            var s = allStudents.value.find((std) => std.personId === p.personId)
            if (s !== undefined) {
                s.problems = p.problems
            }
        })
    } else {
        nodata.value = "Select a class year and hit submit"
    }

    students.value = allStudents.value.filter((s) => !problemsOnly.value || s?.problems?.length)
    if (students.value.length === 0 && classYear?.value?.value && classYear.value.value) {
        nodata.value = "No students found. Please import this year."
    }
    loading.value = false
}

function clear() {
    studentClassYear.value = {
        studentClassYearId: 0,
        personId: 0,
        ross: false,
        leftReason: null,
        leftTerm: null,
        comment: "",
        active: false,
        classYear: null,
    }
    showForm.value = false
}

function selectStudent(student: StudentClassYearProblem, cy: StudentClassYearType) {
    selectedStudentName.value = student.fullName
    studentClassYear.value = { ...cy }
    showForm.value = true
}
async function submitStudentClassYear() {
    var u = apiUrl + "students/dvm/" + studentClassYear.value.classYear + "/" + studentClassYear.value.personId
    await put(u, studentClassYear.value)
    getStudents()
    clear()
}
async function deleteStudentClassYear() {
    var u = apiUrl + "students/dvm/studentClassYears/" + studentClassYear.value.studentClassYearId
    await del(u)
    getStudents()
    clear()
}
async function load() {
    //get class year from url
    let cy = route.query.classYear
    if (cy) {
        classYear.value = { label: "Class of " + cy, value: parseInt(cy as string) }
    }
    //get all class years, reasons, terms for select boxes
    classYearOptions.value.push({ label: "", value: 0 })
    for (var i = new Date().getFullYear() + 6; i >= 2016; i--) {
        classYearOptions.value.push({ label: "Class of " + i, value: i })
    }

    get(apiUrl + "curriculum/terms").then((data) => {
        terms.value = data.result.map((t: any) => ({ label: t.description, value: t.termCode }))
    })
    get(apiUrl + "students/dvm/leftReasons").then((data) => {
        reasons.value = data.result.map((r: any) => ({ label: r.reason, value: r.classYearLeftReasonId }))
    })

    //get students
    getStudents()
}

watch(problemsOnly, () => {
    students.value = allStudents.value.filter((s) => !problemsOnly.value || s?.problems?.length)
})

load()
</script>
<template>
    <div class="row q-mb-sm">
        <div class="col">
            <h2>Student Class Years</h2>
        </div>
    </div>

    <q-dialog
        v-model="showForm"
        @hide="clear"
    >
        <q-card style="width: 500px; max-width: 80vw">
            <q-form
                @submit="submitStudentClassYear"
                v-model="studentClassYear"
            >
                <q-card-section>
                    <div class="text-h6">
                        Updating record for {{ selectedStudentName }} Class of {{ studentClassYear.classYear }}
                    </div>
                    If you change the class year one the current class year, a new record will be created and the
                    current class year one will be marked as inactive with the reasons and term below.
                </q-card-section>
                <q-card-section>
                    <q-checkbox
                        v-model="studentClassYear.active"
                        label="Current class year"
                    ></q-checkbox>
                    <br />
                    <q-checkbox
                        v-model="studentClassYear.ross"
                        label="Ross Student"
                    ></q-checkbox>
                    <div>If changing class year, please fill out below.</div>
                    <q-select
                        outlined
                        dense
                        options-dense
                        label="Class Year"
                        v-model="studentClassYear.classYear"
                        emit-value
                        :options="classYearOptions"
                    ></q-select>
                    <q-select
                        outlined
                        dense
                        options-dense
                        clearable
                        label="Reason Left"
                        v-model="studentClassYear.leftReason"
                        emit-value
                        map-options
                        :options="reasons"
                    ></q-select>
                    <q-select
                        outlined
                        dense
                        options-dense
                        clearable
                        label="Term Left"
                        v-model="studentClassYear.leftTerm"
                        emit-value
                        map-options
                        :options="terms"
                    ></q-select>
                    <q-input
                        type="textarea"
                        outlined
                        dense
                        label="Comment"
                        v-model="studentClassYear.comment"
                    ></q-input>
                </q-card-section>
                <q-card-actions align="evenly">
                    <q-btn
                        no-caps
                        label="Save"
                        type="submit"
                        padding="xs sm"
                        color="primary"
                    ></q-btn>
                    <q-btn
                        no-caps
                        label="Delete"
                        type="button"
                        padding="xs md"
                        @click="deleteStudentClassYear"
                        color="red"
                    ></q-btn>
                </q-card-actions>
            </q-form>
        </q-card>
    </q-dialog>

    <q-form
        @submit="
            () => {
                return false
            }
        "
    >
        <div class="row q-mb-md">
            <div class="col col-md-6 col-lg-3">
                <q-select
                    outlined
                    dense
                    options-dense
                    label="Class Year"
                    v-model="classYear"
                    :options="classYearOptions"
                >
                </q-select>
                <q-toggle
                    v-model="activeOnly"
                    label="Only students currently in this class"
                ></q-toggle>
                <br />
                <q-btn
                    no-caps
                    color="primary"
                    label="Submit"
                    class="q-mt-md"
                    @click="getStudents"
                ></q-btn>
            </div>
            <div class="col col-md-2 col-lg-2 offset-md-3">
                <q-btn
                    v-if="classYear.value"
                    :label="'Import students into ' + classYear?.label"
                    dense
                    no-caps
                    color="secondary"
                    :to="'StudentClassYearImport?classYear=' + classYear.value"
                ></q-btn>
            </div>
        </div>
    </q-form>

    <q-table
        :rows="students"
        row-key="personId"
        :columns="cols"
        :title="classYear.label"
        :pagination="{ rowsPerPage: 0 }"
        :no-data-label="nodata"
        :loading="loading"
        :filter="filter"
        dense
    >
        <template #top-right>
            <q-toggle
                v-model="problemsOnly"
                label="Problems Only"
                class="q-mr-md"
            ></q-toggle>
            <q-input
                v-model="filter"
                dense
                outlined
                debounce="300"
                placeholder="Filter results"
                class="q-ml-xs q-mr-xs"
            >
                <template #append>
                    <q-icon name="filter_alt" />
                </template>
            </q-input>
        </template>
        <template #body-cell-avatar="props">
            <q-td :props="props">
                <q-avatar square>
                    <img
                        :src="viperUrl + 'public/utilities/getbase64image.cfm?altphoto=1&mailId=' + props.row.mailId"
                        class="smallPhoto"
                        alt="Student photo"
                    />
                </q-avatar>
            </q-td>
        </template>
        <template #body-cell-name="props">
            <q-td :props="props">
                <a> {{ props.row.lastName }}, {{ props.row.firstName }} </a>
            </q-td>
        </template>
        <template #body-cell-classyear="props">
            <q-td :props="props">
                <span
                    v-for="cy in props.row.classYears"
                    :key="cy.studentClassYearId"
                >
                    <q-btn
                        dense
                        no-caps
                        :label="cy.classYear"
                        v-if="cy.active"
                        color="primary"
                        class="q-px-sm"
                        @click="selectStudent(props.row, cy)"
                    >
                        <q-badge
                            v-if="cy.ross"
                            color="red"
                            class="q-mx-sm"
                            >Ross</q-badge
                        >
                        <q-badge
                            color="orange"
                            class="q-mx-sm"
                            v-if="cy.leftReason"
                            >{{ cy.leftReasonText }}</q-badge
                        >
                    </q-btn>
                </span>
            </q-td>
        </template>
        <template #body-cell-previousyears="props">
            <q-td :props="props">
                <span
                    v-for="cy in props.row.classYears"
                    :key="cy.studentClassYearId"
                >
                    <q-btn
                        dense
                        no-caps
                        :label="cy.classYear"
                        v-if="!cy.active"
                        color="secondary"
                        class="q-px-sm"
                        @click="selectStudent(props.row, cy)"
                    >
                        <q-badge
                            v-if="cy.ross"
                            color="red"
                            class="q-mx-sm"
                            >Ross</q-badge
                        >
                        <q-badge
                            color="orange"
                            class="q-mx-sm"
                            v-if="cy.leftReason"
                            >{{ cy.leftReasonText }}</q-badge
                        >
                    </q-btn>
                </span>
            </q-td>
        </template>
    </q-table>
</template>
