<script setup lang="ts">
    import type { Ref } from 'vue'
    import { ref } from 'vue'
    import { useFetch } from '@/composables/ViperFetch'

    const users = ref([]) as Ref<any[]>
    const { get, post, put } = useFetch()

    //should probably define a type here
    const newUser = ref({ pKey: "", firstName: "", lastName: "", clientId: "", iamId: "", mothraId: "", loginId: "", mailId: "", pidm: "" }) as Ref<any>
    const editUser = ref({ pKey: "", firstName: "", lastName: "", clientId: "", iamId: "", mothraId: "", loginId: "", mailId: "", pidm: "" }) as Ref<any>

    function clearEditUser() {
        editUser.value = { pKey: "", firstName: "", lastName: "", clientId: "", iamId: "", mothraId: "", loginId: "", mailId: "", pidm: "" }
    }

    function clearNewUser() {
        newUser.value = { pKey: "", firstName: "", lastName: "", clientId: "", iamId: "", mothraId: "", loginId: "", mailId: "", pidm: "" }
    }

    async function save(user: any) {
        //should have error handling here
        if (user.pKey == '') {
            await post("/api/fakeUsers/", user)
            clearNewUser()
        }
        else {
            await put("/api/fakeUsers/" + user.pKey, user)
            clearEditUser()
        }
        let userList = await get("/api/fakeUsers")
        users.value = userList.result
    }

    async function loadUsers() {
        let userList = await get("/api/fakeUsers")
        users.value = userList.result
    }

    loadUsers()
</script>
<template>
    <h2>Fake User Management</h2>
    <q-markup-table dense>
        <thead>
            <tr>
                <th></th>
                <th class="text-left">Key</th>
                <th class="text-left">First Name</th>
                <th class="text-left">Last Name</th>
                <th class="text-left">ClientId</th>
                <th class="text-left">IamId</th>
                <th class="text-left">MothraId</th>
                <th class="text-left">LoginId</th>
                <th class="text-left">MailiId</th>
                <th class="text-left">Pidm</th>
                <th></th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td></td>
                <td></td>
                <td>
                    <q-input type="text" outlined dense v-model="newUser.firstName" label="First Name"></q-input>
                </td>
                <td>
                    <q-input type="text" outlined dense v-model="newUser.lastName" label="Last Name"></q-input>
                </td>
                <td>
                    <q-input type="text" outlined dense v-model="newUser.clientId" label="Client ID" maxlength="9"></q-input>
                </td>
                <td>
                    <q-input type="text" outlined dense v-model="newUser.iamId" label="IAM ID" maxlength="11"></q-input>
                </td>
                <td>
                    <q-input type="text" outlined dense v-model="newUser.mothraId" label="Mothra ID" maxlength="8"></q-input>
                </td>
                <td>
                    <q-input type="text" outlined dense v-model="newUser.loginId" label="Login ID" maxlength="8"></q-input>
                </td>
                <td>
                    <q-input type="text" outlined dense v-model="newUser.mailId" label="Mail ID" maxlength="20"></q-input>
                </td>
                <td>
                    <q-input type="text" outlined dense v-model="newUser.pidm" label="PIDM" maxlength="8"></q-input>
                </td>
                <td>
                    <q-btn type="submit" dense outlined color="primary" no-caps padding="sm md" label="Create User" @click="save(newUser)"></q-btn>
                </td>
            </tr>
            
            <tr v-for="u in users">
                <template v-if="editUser.pKey != u.pKey">
                    <td>
                        <q-btn icon="edit" size="sm" dense outlined color="primary" @click="editUser = u" v-if="!u.isProtected"></q-btn>
                    </td>
                    <td>{{ u.pKey }} </td>
                    <td>{{ u.firstName }}</td>
                    <td>{{ u.lastName }}</td>
                    <td>{{ u.clientId }}</td>
                    <td>{{ u.iamId }}</td>
                    <td>{{ u.mothraId }}</td>
                    <td>{{ u.loginId}}</td>
                    <td>{{ u.mailId }}</td>
                    <td>{{ u.pidm }}</td>
                    <td></td>
                </template>
                <template v-else>
                    <td>
                        <q-btn icon="refresh" size="sm" dense outlined color="primary" @click="clearEditUser()"></q-btn>
                    </td>
                    <td>
                        {{ editUser.pKey }}
                    </td>
                    <td>
                        <q-input type="text" outlined dense v-model="editUser.firstName" label="First Name"></q-input>
                    </td>
                    <td>
                        <q-input type="text" outlined dense v-model="editUser.lastName" label="Last Name"></q-input>
                    </td>
                    <td>
                        <q-input type="text" outlined dense v-model="editUser.clientId" label="Client ID" maxlength="9"></q-input>
                    </td>
                    <td>
                        <q-input type="text" outlined dense v-model="editUser.iamId" label="IAM ID" maxlength="11"></q-input>
                    </td>
                    <td>
                        <q-input type="text" outlined dense v-model="editUser.mothraId" label="Mothra ID" maxlength="8"></q-input>
                    </td>
                    <td>
                        <q-input type="text" outlined dense v-model="editUser.loginId" label="Login ID" maxlength="8"></q-input>
                    </td>
                    <td>
                        <q-input type="text" outlined dense v-model="editUser.mailId" label="Mail ID" maxlength="20"></q-input>
                    </td>
                    <td>
                        <q-input type="text" outlined dense v-model="editUser.pidm" label="PIDM" maxlength="8"></q-input>
                    </td>
                    <td>
                        <q-btn type="submit" dense outlined color="primary" no-caps padding="sm md" label="Save" @click="save(editUser)"></q-btn>
                    </td>
                </template>
            </tr>
        </tbody>
    </q-markup-table>
</template>