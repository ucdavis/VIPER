<template>
    <div class="link-collections-page">
        <h2 class="text-primary q-mb-sm">Link Collections</h2>

        <q-form>
            <div class="row items-center q-mb-md q-col-gutter-sm">
                <div class="col-12 col-sm-8 col-md-5 col-lg-3">
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
                    <div class="row q-mt-none q-pt-none q-col-gutter-sm">
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

                    <VueDraggable
                        v-model="collectionTags"
                        @end="onDragEnd"
                        class="list-group"
                    >
                        <div
                            v-for="element in collectionTags"
                            :key="element.linkCollectionTagCategoryId"
                            class="row items-center q-col-gutter-sm"
                        >
                            <div class="col-auto">
                                <q-icon
                                    class="handle"
                                    name="drag_handle"
                                />
                            </div>
                            <div class="col">
                                <q-input
                                    dense
                                    readonly
                                    v-model="element.linkCollectionTagCategory"
                                />
                            </div>
                            <div class="col-auto">
                                <q-btn
                                    flat
                                    label="Delete"
                                    dense
                                    no-caps
                                    color="red-5"
                                    class="q-ml-lg q-pr-md"
                                    icon="delete"
                                    @click="deleteTag(element.linkCollectionTagCategoryId)"
                                />
                            </div>
                        </div>
                    </VueDraggable>
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
            <h2 class="text-primary q-mt-lg q-mb-sm">Links in {{ collection?.linkCollection }}</h2>
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
                <div class="row link-row header items-center q-col-gutter-sm desktop-only link-grid">
                    <div class="col-12 col-md-auto">
                        <q-btn
                            dense
                            flat
                            no-caps
                            size="sm"
                            color="secondary"
                            icon="edit"
                        />
                    </div>
                    <div class="col-12 col-md-3 col-lg-2">URL</div>
                    <div class="col-12 col-md-3 col-lg-2">Title</div>
                    <div class="col-12 col-md-6 col-lg-4">Description</div>
                    <div class="col-12 col-lg-3">Tags</div>
                </div>
                <VueDraggable
                    v-model="links"
                    @end="linkOrder"
                    handle=".handle"
                >
                    <div
                        v-for="element in links"
                        :key="element.linkId"
                        class="row link-row items-start q-col-gutter-sm link-card link-grid"
                    >
                        <div class="col-12 col-md-auto link-actions q-gutter-xs">
                            <div class="action-buttons">
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
                        </div>
                        <div class="col-12 col-md-3 col-lg-2 link-field url-field">
                            <div class="field-label">URL</div>
                            <div class="link-url">{{ element.url }}</div>
                        </div>
                        <div class="col-12 col-md-3 col-lg-2 link-field title-field">
                            <div class="field-label">Title</div>
                            <div>{{ element.title }}</div>
                        </div>
                        <div class="col-12 col-md-6 col-lg-4 link-field desc-field">
                            <div class="field-label">Description</div>
                            <div>{{ element.description }}</div>
                        </div>
                        <div class="col-12 col-lg-3 tag-field">
                            <template
                                v-for="tag in collectionTags"
                                :key="tag.linkCollectionTagCategoryId"
                            >
                                <div
                                    v-if="element.tags !== undefined"
                                    class="row tag-row q-col-gutter-xs"
                                >
                                    <div class="tag-row-inner">
                                        <div class="field-label">{{ tag.linkCollectionTagCategory }}</div>
                                        <div class="tag-chips">
                                            <q-chip
                                                dense
                                                square
                                                color="primary"
                                                text-color="white"
                                                v-for="t in getTagsForCategory(
                                                    element,
                                                    tag.linkCollectionTagCategoryId,
                                                )"
                                                :key="tag.linkCollectionTagCategoryId.toString() + ' ' + t.toString()"
                                                class="q-mr-xs q-mb-xs"
                                            >
                                                {{ t }}
                                            </q-chip>
                                        </div>
                                    </div>
                                </div>
                            </template>
                        </div>
                    </div>
                </VueDraggable>
            </div>
        </template>
    </div>
</template>

<script setup lang="ts">
import { ref, inject, watch } from "vue"
import { useQuasar } from "quasar"
import type { Ref } from "vue"
import { VueDraggable } from "vue-draggable-plus"
import type { LinkCollection, Link, LinkCollectionTagCategory, LinkWithTags } from "@/CMS/types/"
import { useFetch } from "@/composables/ViperFetch"

const apiURL = (inject("apiURL") + "cms/linkCollections/") as string
const $q = useQuasar()
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
    if (collectionData.value.linkCollection === null || collectionData.value.linkCollection.trim().length === 0) {
        collectionNameInput.value.focus()
        return
    }

    const { put, post } = useFetch()

    if (collection.value !== null && collection.value.linkCollectionId > 0) {
        await put(apiURL + collection.value.linkCollectionId, { linkCollection: collectionData.value.linkCollection })
    } else {
        await post(apiURL, { linkCollection: collectionData.value.linkCollection })
    }

    await loadCollections()
    cancelCollectionDialog()
}

async function deleteCollection() {
    const confirmed = await confirmAction(
        "Delete Collection",
        "This will remove the collection and all its links. Continue?",
    )
    if (!confirmed) return
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

function getTagsForCategory(element: LinkWithTags, categoryId: number) {
    const raw = element.tags?.[categoryId]
    if (!raw) {
        return []
    }
    if (Array.isArray(raw)) {
        return raw
    }

    return raw
        .split(",")
        .map((t) => t.trim())
        .filter((t) => t.length > 0)
}

async function saveLink() {
    const { post, put } = useFetch()
    if (link.value.linkId === 0) {
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
    const confirmed = await confirmAction("Delete Link", "This will permanently remove this link. Continue?")
    if (!confirmed) return
    const { del } = useFetch()
    await del(apiURL + collectionId.value + "/links/" + link.value.linkId)
    link.value = { linkId: 0, url: "", title: "", description: "", tags: {}, sortOrder: 0 }
    showLinkDialog.value = false
    loadLinks()
}

async function confirmAction(title: string, message: string) {
    return await new Promise<boolean>((resolve) => {
        $q.dialog({
            title,
            message,
            cancel: {
                label: "No",
                flat: true,
            },
            persistent: true,
            ok: {
                label: "Yes",
                color: "primary",
                unelevated: true,
            },
        })
            .onOk(() => resolve(true))
            .onCancel(() => resolve(false))
            .onDismiss(() => resolve(false))
    })
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
    if (collectionId.value && addTag.value.trim() !== "") {
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
    const confirmed = await confirmAction(
        "Delete Tag",
        "This will remove the tag category from this collection. Continue?",
    )
    if (!confirmed) return
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
    collection.value = collections.value.find((c) => c.linkCollectionId === collectionId.value) || null
    collectionData.value.linkCollection = collection.value?.linkCollection || ""
})

loadCollections()
</script>

<style scoped>
#allLinks {
    background: #fff;
    border-radius: 6px;
    box-shadow: 0 1px 3px rgb(0 0 0 / 8%);
}

#allLinks .link-row {
    border-bottom: 1px solid silver;
    margin-bottom: 4px;
    padding: 8px 12px;
    align-items: flex-start;
    border-radius: 6px;
}

#allLinks .link-row.header {
    font-weight: bold;
    background: #f5f7fb;
    color: var(--q-primary);
    border-radius: 6px;
    text-align: left;
}

#allLinks .link-row.header > div {
    text-align: left;
}

#allLinks .link-url {
    white-space: normal;
    word-break: break-word;
    overflow-wrap: anywhere;
}

.tag-label {
    color: var(--q-primary);
    white-space: nowrap;
}

.tag-row-inner {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 4px;
    width: 100%;
    text-align: left;
    margin-bottom: 6px;
}

.tag-chips {
    display: flex;
    flex-wrap: wrap;
    gap: 4px;
    max-width: 100%;
}

.mobile-only {
    display: block;
    color: var(--q-primary);
    font-weight: 600;
    width: 100%;
}

.link-actions {
    display: flex;
    align-items: center;
}

.action-buttons {
    display: inline-flex;
    align-items: center;
    gap: 6px;
}

.desktop-only {
    display: none;
}

.desktop-only-inline {
    display: none;
}

.field-label {
    font-weight: 600;
    color: var(--q-primary);
    margin-bottom: 2px;
}

.link-field {
    word-break: break-word;
    min-width: 0;
}

.link-card {
    border: 1px solid #e5e7eb;
    border-radius: 8px;
    margin-bottom: 12px;
    background: #fafbff;
    padding: 12px 12px 8px;
}

.desktop-only .tag-chips {
    justify-content: flex-start;
}

@media (width <= 1023px) {
    #allLinks .link-row {
        border-radius: 6px;
        border-color: #e6e8ef;
        padding: 12px 12px 8px;
    }

    .link-actions {
        margin-bottom: 6px;
    }

    .tag-row {
        margin-bottom: 4px;
    }
}

@media (width >= 1024px) {
    .desktop-only {
        display: flex;
    }

    .desktop-only-inline {
        display: inline;
    }

    .mobile-only {
        display: none;
    }

    .link-row.link-grid {
        display: grid;
        grid-template-columns: 70px 3.5fr 1.2fr 1.4fr 2.1fr;
        column-gap: 12px;
        align-items: stretch;
    }

    .link-row.link-grid > div {
        justify-self: stretch;
        width: 100%;
        min-width: 0;
    }

    .link-row.header {
        align-items: center;
    }

    .link-card {
        border: none;
        background: transparent;
        margin-bottom: 0;
        padding: 8px 0;
    }

    .url-field .link-url {
        max-width: 100%;
        white-space: normal;
        word-break: break-word;
        overflow-wrap: anywhere;
    }

    .title-field {
        min-width: 140px;
    }

    .desc-field {
        min-width: 200px;
    }

    .url-field {
        min-width: 360px;
    }

    .tag-field {
        overflow-wrap: anywhere;
        max-width: 100%;
        min-width: 0;
        word-break: break-word;
    }

    .field-label {
        display: none;
    }

    .tag-field .field-label {
        display: block;
    }

    .link-actions {
        flex-direction: row;
        gap: 6px;
        margin-bottom: 0;
    }

    .tag-row {
        margin-bottom: 6px;
        align-items: flex-start;
    }
}
</style>
