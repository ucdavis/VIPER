<template>
    <h2>Link Collections</h2>

    <q-form>
        <div class="row items-center q-mb-md">
            <div class="col-12 col-sm-8 col-md-4 col-lg-2">
                <q-select
                    v-model="collectionId"
                    map-options
                    emit-value
                    dense
                    options-dense
                    class="q-mx-lg"
                    label="Link Collection"
                    :options="collections"
                    option-value="linkCollectionId"
                    option-label="linkCollection"
                >
                </q-select>
            </div>
            <div class="col-auto">
                <q-btn
                    color="green"
                    icon="add"
                    label="New Collection"
                    no-caps
                    dense
                    class="q-mx-lg q-pr-md"
                    @click="showCollectionDialog = true"
                />
            </div>
            <div
                class="col-auto"
                v-if="collectionId != null"
            >
                <q-btn
                    color="primary"
                    icon="edit"
                    label="Edit Collection"
                    dense
                    no-caps
                    class="q-mx-lg q-pr-md"
                    @click="showCollectionDialog = true"
                />
            </div>
        </div>
    </q-form>

    <!-- Create/Edit Dialog -->
    <q-dialog v-model="showCollectionDialog">
        <q-card style="min-width: 350px">
            <q-card-section>
                <div class="text-h6">{{ collectionId != null ? "Edit" : "Create" }} Collection</div>
            </q-card-section>

            <q-card-section class="q-pt-none">
                <q-input
                    dense
                    v-model="collectionData.linkCollection"
                    label="Collection Name"
                    :rules="[(val) => (val && val.length > 0) || 'Please enter collection name']"
                    ref="collectionNameInput"
                />
            </q-card-section>

            <q-card-section
                v-if="collectionId != null"
                class="q-py-none p-my-none"
            >
                <strong>Tag Categories</strong>
                <div class="row q-mt-none q-pt-none">
                    <q-input
                        dense
                        v-model="addTag"
                        class="col-auto"
                    />
                    <q-btn
                        flat
                        label="Add"
                        dense
                        no-caps
                        color="green"
                        class="q-ml-lg q-pr-md"
                        icon="add"
                        @click="createTag"
                    />
                </div>

                <draggable
                    :list="collectionTags"
                    item-key="linkCollectionTagCategoryId"
                    @end="onDragEnd"
                    class="list-group"
                >
                    <template #item="{ element }">
                        <div class="row items-center">
                            <div class="col-auto">
                                <q-icon
                                    class="handle"
                                    name="drag_handle"
                                />
                            </div>
                            <div class="col-auto">
                                <q-input
                                    dense
                                    readonly
                                    v-model="element.linkCollectionTagCategory"
                                />
                            </div>
                            <q-btn
                                flat
                                label="Delete"
                                dense
                                no-caps
                                color="red-5"
                                class="q-ml-lg q-pr-md col-auto"
                                icon="delete"
                                @click="deleteTag(element.linkCollectionTagCategoryId)"
                            />
                        </div>
                    </template>
                </draggable>
            </q-card-section>

            <q-card-actions
                align="right"
                class="text-primary"
            >
                <q-btn
                    label="Cancel"
                    @click="cancelCollectionDialog"
                    dense
                    no-caps
                    class="q-pr-md"
                />
                <q-btn
                    label="Save"
                    @click="saveCollection"
                    color="primary"
                    dense
                    no-caps
                    class="q-pr-md"
                />
                <q-btn
                    label="Delete Collection"
                    @click="deleteCollection"
                    dense
                    no-caps
                    color="red-5"
                    class="q-pr-md"
                />
            </q-card-actions>
        </q-card>
    </q-dialog>

    <template v-if="collectionId != 0 && collectionId != null">
        <h2>Links in {{ collection?.linkCollection }}</h2>
        <q-btn
            dense
            color="green"
            icon="add"
            label="Add Link"
            @click="showLinkDialog = true"
            no-caps
            class="q-pr-md q-mt-sm"
        />
        <q-dialog
            v-model="showLinkDialog"
            persistent
        >
            <q-card style="min-width: 350px">
                <q-card-section>
                    <div class="text-h6">{{ link.linkId > 0 ? "Edit" : "Create" }} Link</div>
                </q-card-section>
                <q-card-section>
                    <q-input
                        dense
                        outlined
                        v-model="link.url"
                        label="URL"
                    />

                    <q-input
                        dense
                        outlined
                        v-model="link.title"
                        label="Title"
                    />

                    <q-input
                        dense
                        outlined
                        v-model="link.description"
                        type="textarea"
                        class="col-12 col-lg-3"
                        label="Description"
                    />
                    <template
                        v-for="tag in collectionTags"
                        :key="tag.linkCollectionTagCategoryId"
                    >
                        {{ tag.linkCollectionTagCategory }}:
                        <q-input
                            dense
                            outlined
                            v-model="link.tags[tag.linkCollectionTagCategoryId]"
                            label="Tags, comma separated"
                            stack-label
                        />
                    </template>
                </q-card-section>
                <q-card-actions align="right">
                    <q-btn
                        label="Cancel"
                        @click="cancelLinkDialog"
                        dense
                        no-caps
                        class="q-pr-md"
                        color="secondary"
                    />
                    <q-btn
                        label="Save"
                        @click="saveLink"
                        dense
                        no-caps
                        color="primary"
                        class="q-pr-md"
                    />
                    <q-btn
                        label="Delete Link"
                        @click="deleteLink"
                        dense
                        no-caps
                        color="red-5"
                        class="q-pr-md"
                    />
                </q-card-actions>
            </q-card>
        </q-dialog>

        <div
            id="allLinks"
            class="q-mt-md"
        >
            <div class="row link-row header">
                <div class="col-auto q-mr-md">
                    <q-btn
                        dense
                        flat
                        no-caps
                        size="sm"
                        color="secondary"
                        icon="edit"
                    />
                </div>
                <div class="col-lg-2">URL</div>
                <div class="col-lg-2">Title</div>
                <div class="col-lg-4">Description</div>
                <div class="col-lg-3">Tags</div>
            </div>
            <draggable
                :list="links"
                item-key="linkId"
                @end="linkOrder"
                handle=".handle"
            >
                <template #item="{ element }">
                    <div class="row link-row">
                        <div class="col-auto q-mr-md">
                            <q-icon
                                class="handle"
                                name="drag_handle"
                            />
                            <q-btn
                                dense
                                no-caps
                                size="sm"
                                color="secondary"
                                icon="edit"
                                @click="clickLink(element)"
                            />
                        </div>
                        <div class="col-lg-2 link-url">{{ element.url }}</div>
                        <div class="col-lg-2">{{ element.title }}</div>
                        <div class="col-lg-4">{{ element.description }}</div>
                        <div class="col-lg-3">
                            <template
                                v-for="tag in collectionTags"
                                :key="tag.linkCollectionTagCategoryId"
                            >
                                <div
                                    v-if="element.tags !== undefined"
                                    class="row"
                                >
                                    <div class="col-3">
                                        <strong>{{ tag.linkCollectionTagCategory }}:</strong>
                                    </div>
                                    <div class="col-9">
                                        <span
                                            v-for="t in element.tags[tag.linkCollectionTagCategoryId] || ''"
                                            :key="tag.linkCollectionTagCategoryId.toString() + ' ' + t.toString()"
                                        >
                                            {{ t }}
                                        </span>
                                    </div>
                                </div>
                            </template>
                        </div>
                    </div>
                </template>
            </draggable>
        </div>
    </template>
</template>

<script setup lang="ts">
import { ref, inject, watch } from "vue"
import type { Ref } from "vue"
import draggable from "vuedraggable"
import type { LinkCollection, Link, LinkCollectionTagCategory, LinkWithTags } from "@/CMS/types/"
import { useFetch } from "@/composables/ViperFetch"

const apiURL = (inject("apiURL") + "cms/linkCollections/") as string
const collections: Ref<LinkCollection[]> = ref([])
const collectionId: Ref<number | null> = ref(null)
const collection: Ref<LinkCollection | null> = ref(null)
const collectionData: Ref<any> = ref({})
const showCollectionDialog = ref(false)
const collectionNameInput: Ref<any> = ref(null)

const collectionTags: Ref<LinkCollectionTagCategory[]> = ref([])
const addTag: Ref<string> = ref("")

const links: Ref<LinkWithTags[]> = ref([])
const link: Ref<LinkWithTags> = ref({ linkId: 0, url: "", title: "", description: "", tags: {}, sortOrder: 0 })
const showLinkDialog: Ref<boolean> = ref(false)

//collections
async function loadCollections() {
    const { get } = useFetch()
    const res = await get(apiURL)
    collections.value = res.result

    if (collections.value.length > 0) {
        collection.value = collections.value[0] !== undefined ? collections.value[0] : null
        collectionId.value = collections.value[0] !== undefined ? collections.value[0].linkCollectionId : null
    }
}

async function saveCollection() {
    if (collectionData.value.linkCollection == null || collectionData.value.linkCollection.trim().length == 0) {
        collectionNameInput.value.focus()
        return
    }

    const { put, post } = useFetch()

    if (collection.value != null && collection.value.linkCollectionId > 0) {
        await put(apiURL + collection.value.linkCollectionId, { linkCollection: collectionData.value.linkCollection })
    } else {
        await post(apiURL, { linkCollection: collectionData.value.linkCollection })
    }

    await loadCollections()
    cancelCollectionDialog()
}

async function deleteCollection() {
    const { del } = useFetch()
    await del(apiURL + collection.value?.linkCollectionId)
    await loadCollections()
    cancelCollectionDialog()
}

async function cancelCollectionDialog() {
    showCollectionDialog.value = false
}

//links
async function loadLinks() {
    const { get } = useFetch()
    if (collectionId.value) {
        const res = await get(apiURL + collectionId.value + "/links")
        links.value = res.result.map((r: any) => {
            return { ...r, tags: getTagRecord(r) }
        })
    } else {
        links.value = []
    }
}

function getTagRecord(r: Link) {
    let tags: Record<number, string> = {}
    for (var lt of r.linkTags || []) {
        if (tags[lt.linkCollectionTagCategoryId] === undefined) {
            tags[lt.linkCollectionTagCategoryId] = lt.value
        } else {
            tags[lt.linkCollectionTagCategoryId] += "," + lt.value
        }
    }
    return tags
}

async function saveLink() {
    const { post, put } = useFetch()
    if (link.value.linkId == 0) {
        const res = await post(apiURL + collectionId.value + "/links", {
            linkCollectionId: collectionId.value,
            url: link.value.url,
            title: link.value.title,
            description: link.value.description,
            sortOrder: links.value.length + 1,
        })
        link.value.linkId = res.result.linkId
    } else {
        await put(apiURL + collectionId.value + "/links/" + link.value.linkId, {
            linkCollectionId: collectionId.value,
            url: link.value.url,
            title: link.value.title,
            description: link.value.description,
            sortOrder: link.value.sortOrder,
        })
    }

    //put tags
    await put(apiURL + collectionId.value + "/links/" + link.value.linkId + "/tags", link.value.tags)

    link.value = { linkId: 0, url: "", title: "", description: "", tags: {}, sortOrder: 0 }
    showLinkDialog.value = false
    loadLinks()
}

function clickLink(li: LinkWithTags) {
    link.value = li
    showLinkDialog.value = true
}

function cancelLinkDialog() {
    link.value = { linkId: 0, url: "", title: "", description: "", tags: {}, sortOrder: 0 }
    showLinkDialog.value = false
}

async function deleteLink() {
    const { del } = useFetch()
    del(apiURL + collectionId.value + "/links/" + link.value.linkId)
    link.value = { linkId: 0, url: "", title: "", description: "", tags: {}, sortOrder: 0 }
    showLinkDialog.value = false
    loadLinks()
}

function linkOrder() {
    let ordered: any[] = []
    links.value.forEach((link, index) => {
        ordered.push({ linkId: link.linkId, sortOrder: index + 1 })
    })
    const { put } = useFetch()
    put(apiURL + collectionId.value + "/links-order", ordered)
}

//tags
async function loadTags() {
    const { get } = useFetch()
    if (collectionId.value) {
        const res = await get(apiURL + collectionId.value + "/tags")
        collectionTags.value = res.result
    } else {
        collectionTags.value = []
    }
}

async function createTag() {
    const { post } = useFetch()
    if (collectionId.value && addTag.value.trim() != "") {
        await post(apiURL + collectionId.value + "/tags", {
            linkCollectionId: collectionId.value,
            linkCollectionTagCategory: addTag.value,
            sortOrder: collectionTags.value.length + 1,
        })
        addTag.value = ""
        loadTags()
    }
}

async function deleteTag(tagId: number) {
    const { del } = useFetch()
    if (collectionId.value && tagId) {
        await del(apiURL + collectionId.value + "/tags/" + tagId)
        loadTags()
    }
}

function onDragEnd() {
    let ordered: any[] = []
    collectionTags.value.forEach((tag, index) => {
        ordered.push({ linkCollectionTagCategoryId: tag.linkCollectionTagCategoryId, sortOrder: index + 1 })
    })
    const { put } = useFetch()
    put(apiURL + collectionId.value + "/tags/order", ordered)
}

watch(collectionId, () => {
    loadLinks()
    loadTags()
    collection.value = collections.value.find((c) => c.linkCollectionId == collectionId.value) || null
    collectionData.value.linkCollection = collection.value?.linkCollection || ""
})

loadCollections()
</script>

<style scoped>
#allLinks div.link-row {
    border-bottom: 1px solid silver;
    margin-bottom: 4px;
}

#allLinks div.link-row.header {
    font-weight: bold;
}

#allLinks .link-url {
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}
</style>
