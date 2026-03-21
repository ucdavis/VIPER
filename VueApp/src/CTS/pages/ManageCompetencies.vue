<script setup lang="ts">
import { inject, ref, watch } from "vue"
import type { Ref } from "vue"
import type { QTreeNode } from "quasar"
import { useFetch } from "@/composables/ViperFetch"
import type { Competency, Domain } from "@/CTS/types"

const { get, post, put, del } = useFetch()
const apiUrl = inject("apiURL")
const domains = ref([]) as Ref<Domain[]>
const competencies = ref([]) as Ref<Competency[]>
const competencyHierachy = ref([]) as Ref<Competency[]>
const emptyComp = {
    name: "",
    number: "",
    description: "",
    canLinkToStudent: false,
    domainId: null,
    parentId: null,
    competencyId: null,
    domain: null,
    children: null,
} as Competency
const selectedComp = ref(structuredClone(emptyComp)) as Ref<Competency>
const loaded = ref(false)
const showForm = ref(false)
const treeNodes = ref([]) as Ref<QTreeNode[]>
const expanded = ref([]) as Ref<number[]>
const showDescriptions = ref(false)
const tree = ref(null) as Ref<any>

async function load() {
    Promise.resolve([
        get(apiUrl + "cts/domains").then((r) => (domains.value = r.result)),
        get(apiUrl + "cts/competencies").then((r) => (competencies.value = r.result)),
    ])
    await get(apiUrl + "cts/competencies/hierarchy").then((r) => (competencyHierachy.value = r.result))
    treeNodes.value = competencyHierachy.value.map(createTreeNode)
    loaded.value = true
}

function createTreeNode(comp: Competency): QTreeNode {
    return {
        label: comp.number + " " + comp.name,
        body: comp.description ?? "",
        children: comp?.children?.map(createTreeNode),
        competencyId: comp.competencyId,
        comp: comp,
    }
}

async function submitComp() {
    const { success } = selectedComp.value.competencyId
        ? await put(apiUrl + "cts/competencies/" + selectedComp.value.competencyId, selectedComp.value)
        : await post(apiUrl + "cts/competencies", selectedComp.value)

    if (success) {
        await load()
        clearComp()
    }
}

async function deleteComp() {
    const r = await del(apiUrl + "cts/competencies/" + selectedComp.value.competencyId, selectedComp.value)
    if (r.success) {
        await load()
        clearComp()
    }
}

function addChild(comp: Competency) {
    selectedComp.value = structuredClone(emptyComp)
    selectedComp.value.parentId = comp.competencyId
    selectedComp.value.domainId = comp.domainId
    selectedComp.value.number = comp.number
    showForm.value = true
}

function editComp(comp: Competency) {
    selectedComp.value = comp
    showForm.value = true
}

function clearComp() {
    selectedComp.value = structuredClone(emptyComp)
    showForm.value = false
}

function expandTopLevel() {
    competencyHierachy.value.forEach((c: Competency) => {
        expanded.value.push(c.competencyId!)
    })
}

watch(
    () => selectedComp.value.domainId,
    (newVal) => {
        if (
            selectedComp.value.number === null ||
            selectedComp.value.number === "" ||
            selectedComp.value.number.length === 2
        ) {
            const selectedDomain = domains.value.find((d) => d.domainId === newVal)
            if (selectedDomain !== undefined) {
                selectedComp.value.number = selectedDomain.order + "."
            }
        }
    },
)

load()
</script>
<template>
    <h2>Manage Competencies</h2>
    <q-dialog
        v-model="showForm"
        @hide="clearComp()"
    >
        <q-card
            style="width: 500px; max-width: 80vw"
            class="q-pa-sm"
        >
            <q-form
                @submit="submitComp"
                v-model="selectedComp"
            >
                <div class="row">
                    <q-input
                        dense
                        outlined
                        v-model="selectedComp.number"
                        label="Number"
                        class="col-12 col-md-3"
                    ></q-input>
                    <q-input
                        dense
                        outlined
                        v-model="selectedComp.name"
                        label="Name"
                        class="col-12 col-md-9"
                    ></q-input>
                </div>
                <div class="row">
                    <q-select
                        dense
                        options-dense
                        outlined
                        v-model="selectedComp.domainId"
                        label="Domain"
                        map-options
                        emit-value
                        :option-label="(opt) => opt.order + '. ' + opt.name"
                        option-value="domainId"
                        :options="domains"
                        class="col-12"
                    ></q-select>
                </div>
                <div class="row">
                    <q-select
                        dense
                        options-dense
                        outlined
                        v-model="selectedComp.parentId"
                        label="Parent"
                        map-options
                        emit-value
                        :option-label="(opt) => opt.number + ' ' + opt.name"
                        option-value="competencyId"
                        :options="competencies"
                        class="col-12"
                    ></q-select>
                </div>
                <div class="row">
                    <q-toggle
                        v-model="selectedComp.canLinkToStudent"
                        label="Can link to student"
                    ></q-toggle>
                </div>
                <div class="row">
                    <q-input
                        type="textarea"
                        dense
                        outlined
                        v-model="selectedComp.description"
                        label="Description"
                        class="col-12"
                    ></q-input>
                </div>
                <div class="row q-mt-md">
                    <q-btn
                        type="submit"
                        dense
                        no-caps
                        label="Submit"
                        color="primary"
                        class="q-px-md q-mx-md col"
                    ></q-btn>
                    <q-btn
                        type="button"
                        dense
                        no-caps
                        label="Cancel"
                        color="secondary"
                        class="q-px-md q-mx-md col"
                        @click="clearComp()"
                    ></q-btn>
                    <q-btn
                        type="button"
                        dense
                        no-caps
                        label="Delete"
                        v-if="selectedComp.competencyId != null"
                        color="red-5"
                        class="q-px-md q-mx-md col"
                        @click="deleteComp()"
                    ></q-btn>
                </div>
            </q-form>
        </q-card>
    </q-dialog>

    <h3>
        Existing Competencies
        <span class="text-body2">
            <q-toggle
                v-model="showDescriptions"
                label="Show Descriptions"
            ></q-toggle>
            <q-btn
                dense
                no-caps
                class="q-px-sm q-ml-md"
                color="secondary"
                label="Expand all"
                @click="tree.expandAll()"
            ></q-btn>
            <q-btn
                dense
                no-caps
                class="q-px-sm q-ml-md"
                color="secondary"
                label="Expand Competencies"
                @click="expandTopLevel()"
            ></q-btn>
            <q-btn
                dense
                no-caps
                class="q-px-sm q-ml-md"
                color="secondary"
                label="Collapse all"
                @click="tree.collapseAll()"
            ></q-btn>
            <q-btn
                dense
                no-caps
                class="q-px-sm q-ml-md"
                color="green"
                icon="add"
                label="Add Competency"
                @click="showForm = true"
            ></q-btn>
        </span>
    </h3>
    <q-tree
        :nodes="treeNodes"
        node-key="competencyId"
        v-model:expanded="expanded"
        dense
        v-if="loaded"
        ref="tree"
    >
        <template #default-header="prop">
            <div class="row full-width items-center">
                <div
                    :class="'col-auto q-mr-sm ' + (prop.node.children.length == 0 ? 'q-ml-sm' : '')"
                    @click.stop
                >
                    <q-btn
                        dense
                        flat
                        size="sm"
                        icon="add"
                        color="green"
                        @click="addChild(prop.node.comp)"
                        title="Add child of this competency"
                    ></q-btn>
                </div>
                <div
                    class="col-auto q-mr-sm"
                    @click.stop
                >
                    <q-btn
                        dense
                        flat
                        size="sm"
                        icon="edit"
                        color="grey"
                        @click="editComp(prop.node.comp)"
                        title="Edit"
                    ></q-btn>
                </div>
                <div class="col-9 col-sm-10">
                    <span :class="prop.node.comp.type == 'Competency' ? 'text-weight-bold' : ''">{{
                        prop.node.label
                    }}</span>
                    <q-icon
                        name="school"
                        color="green"
                        v-if="prop.node.comp.canLinkToStudent"
                        class="q-ml-md"
                    ></q-icon>
                    <div v-if="showDescriptions">{{ prop.node.body }}</div>
                </div>
            </div>
        </template>
    </q-tree>
</template>
