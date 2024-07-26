<script setup lang="ts">
    import { ref, inject } from 'vue'
    import type { Ref } from 'vue'
    import type { QTableProps } from 'quasar'
    import { useFetch } from '@/composables/ViperFetch'
    import { exportTable } from '@/composables/QuasarTableUtilities'

    type StudentAssociation = {
        collegeName: string | null,
        majorName: string | null,
        levelName: string | null,
        className: string | null
    }

    type StudentInfo = {
        email: string | null,
        iamId: string | null,
        firstName: string | null,
        lastName: string | null,
        studentType: string[] | null,
        studentAssociations: StudentAssociation[] | null
    }

    type StudentInfoPerLine = {
        email: string | null,
        iamId: string | null,
        firstName: string | null,
        lastName: string | null,
        studentType: string | null,
        collegeSchool: string | null,
        major: string | null,
        level: string | null,
        studentAssociation: StudentAssociation | null
    }

    const emails = ref("") as Ref<string>
    const rows = ref([]) as Ref<StudentInfoPerLine[]>
    const search = ref("")
    const loading = ref(false)
    const cols: QTableProps['columns'] = [
        { name: "email", label: "Email", field: "email", align: "left" },
        { name: "iamId", label: "Iam Id", field: "iamId", align: "left", sortable: true },
        { name: "firstName", label: "First Name", field: "firstName", align: "left", sortable: true },
        { name: "lastName", label: "Last Name", field: "lastName", align: "left", sortable: true },
        { name: "studentType", label: "Student Type", field: "studentType", align: "left", sortable: true },
        { name: "collegeSchool", label: "College / School", field: "collegeSchool", align: "left", sortable: true },
        { name: "major", label: "Major", field: "major", align: "left", sortable: true },
        { name: "level", label: "Level", field: "level", align: "left", sortable: true },
        { name: "studentAssociations", label: "Student Associations (College/Major/Level/Class)", field: "studentAssociation", align: "left", sortable: true },
    ]
    const pagination = ref({ sortBy: "lastName", descending: false, rowsPerPage: 25 }) as Ref<any>
    const stdUrl = inject('apiURL') + "people/biorenderStudents"

    async function getData() {
        rows.value = []
        loading.value = true

        const { post } = useFetch()
        const url = new URL(stdUrl, document.baseURI)
        var emailArray = emails.value.split(",")
        const r = await post(url.toString(), emailArray)
        let newValue = [] as StudentInfoPerLine[]
        //create one line per student association
        r.result.forEach((std: StudentInfo) => {
            std.studentAssociations?.forEach((sa: StudentAssociation, i: number) => {
                newValue.push({
                    iamId: std.iamId,
                    firstName: std.firstName,
                    lastName: std.lastName,
                    studentType: std.studentType == null || std.studentType[i] == null ? "" : std.studentType[i],
                    collegeSchool: sa.collegeName,
                    major: sa.majorName,
                    level: sa.levelName,
                    studentAssociation: sa,
                    email: std.email
                })
            })
        })
        rows.value = newValue
        loading.value = false
    }

    function exportData() {
        exportTable(cols, rows.value, ["studentAssociations"])
    }

</script>
<template>
    <h2>Biorender Student Lookup</h2>
    <q-form class="q-mb-md">
        <div class="row">
            <q-input type="textarea" v-model="emails" dense outlined label="Emails (comma separated)" class="col col-12 col-md-6 col-lg-4"></q-input>
        </div>
        <q-btn dense no-caps color="primary" label="Get data" @click="getData()" class="q-px-md"></q-btn>
    </q-form>

    <q-table :rows="rows"
             :columns="cols"
             row-key="email"
             dense
             v-model:pagination="pagination"
             :filter="search"
             title="Student Info"
             :loading="loading">
        <template v-slot:top-right="props">
            <q-btn dense no-caps label="Export" @click="exportData()" color="primary" class="q-px-md q-mr-md"></q-btn>
            <q-input class="q-ml-xs q-mr-xs" v-model="search" dense outlined debounce="300" placeholder="Filter Results">
                <template v-slot:append>
                    <q-icon name="filter_alt" />
                </template>
            </q-input>
        </template>
        <template v-slot:body-cell-studentAssociations="props">
            <q-td :props="props">
                {{props.row.studentAssociation.collegeName}}
                /
                {{props.row.studentAssociation.majorName}}
                /
                {{props.row.studentAssociation.levelName}}
                /
                {{props.row.studentAssociation.className}}
            </q-td>
        </template>
    </q-table>
</template>