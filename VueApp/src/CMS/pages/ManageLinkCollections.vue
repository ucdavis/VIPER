<template>
    <div class="link-collections-page">
        <h1 class="text-h4 text-primary q-mt-none q-mb-sm">Link Collections</h1>

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
                        color="positive"
                        icon="add"
                        label="New Collection"
                        no-caps
                        dense
                        class="q-mx-lg q-pr-md"
                        @click="openNewCollectionDialog"
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
        <q-dialog
            v-model="showCollectionDialog"
            aria-labelledby="collection-dialog-title"
            @hide="cancelCollectionDialog"
        >
            <q-card style="min-width: 350px">
                <q-card-section class="row items-center q-pb-none">
                    <div
                        id="collection-dialog-title"
                        class="text-h6"
                    >
                        {{ collectionId != null ? "Edit" : "Create" }} Collection
                    </div>
                    <q-space />
                    <q-btn
                        icon="close"
                        flat
                        round
                        dense
                        aria-label="Close dialog"
                        v-close-popup
                    />
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
                            color="positive"
                            class="q-ml-lg q-pr-md"
                            icon="add"
                            @click="createTag"
                        />
                    </div>

                    <VueDraggable
                        v-model="draftTags"
                        :animation="200"
                        handle=".handle"
                        class="list-group"
                        @end="onDragEnd"
                    >
                        <div
                            v-for="(element, index) in draftTags"
                            :key="element.linkCollectionTagCategoryId"
                            :class="[
                                'row items-center q-col-gutter-sm',
                                { 'just-moved': justMovedTagId === element.linkCollectionTagCategoryId },
                            ]"
                        >
                            <div class="col-auto">
                                <q-icon
                                    class="handle"
                                    name="drag_handle"
                                >
                                    <q-tooltip>Drag to reorder</q-tooltip>
                                </q-icon>
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
                                    dense
                                    flat
                                    no-caps
                                    size="sm"
                                    color="secondary"
                                    icon="arrow_upward"
                                    aria-label="Move tag up"
                                    :disable="index === 0"
                                    @click="moveTag(index, -1)"
                                >
                                    <q-tooltip>Move up</q-tooltip>
                                </q-btn>
                                <q-btn
                                    dense
                                    flat
                                    no-caps
                                    size="sm"
                                    color="secondary"
                                    icon="arrow_downward"
                                    aria-label="Move tag down"
                                    :disable="index === draftTags.length - 1"
                                    @click="moveTag(index, 1)"
                                >
                                    <q-tooltip>Move down</q-tooltip>
                                </q-btn>
                                <q-btn
                                    flat
                                    label="Delete"
                                    dense
                                    no-caps
                                    color="negative"
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
                        v-if="collectionId != null"
                        label="Delete Collection"
                        @click="deleteCollection"
                        dense
                        no-caps
                        color="negative"
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
                </q-card-actions>
            </q-card>
        </q-dialog>

        <template v-if="collectionId != 0 && collectionId != null">
            <h2 class="text-primary q-mt-lg q-mb-sm">Links in {{ collection?.linkCollection }}</h2>
            <q-btn
                dense
                color="positive"
                icon="add"
                label="Add Link"
                @click="showLinkDialog = true"
                no-caps
                class="q-pr-md q-mt-sm"
            />
            <q-dialog
                v-model="showLinkDialog"
                persistent
                aria-labelledby="link-dialog-title"
                @keydown.escape="cancelLinkDialog"
            >
                <q-card style="min-width: 350px">
                    <q-card-section class="row items-center q-pb-none">
                        <div
                            id="link-dialog-title"
                            class="text-h6"
                        >
                            {{ link.linkId > 0 ? "Edit" : "Create" }} Link
                        </div>
                        <q-space />
                        <q-btn
                            icon="close"
                            flat
                            round
                            dense
                            aria-label="Close dialog"
                            @click="cancelLinkDialog"
                        />
                    </q-card-section>
                    <q-form
                        ref="linkFormRef"
                        class="link-form"
                        greedy
                    >
                        <q-card-section class="q-gutter-y-sm q-pb-none">
                            <q-input
                                dense
                                outlined
                                v-model="link.url"
                                label="URL"
                                class="required-field"
                                maxlength="500"
                                :rules="[
                                    (val) => (val && val.trim().length > 0) || 'URL is required',
                                    (val) => !val || val.length <= 500 || 'URL must be 500 characters or less',
                                    (val) =>
                                        !val ||
                                        isSafeUrl(val) ||
                                        'URL must be a full address starting with http, https, mailto, or tel',
                                ]"
                                aria-required="true"
                                lazy-rules
                                hide-bottom-space
                            />

                            <q-input
                                dense
                                outlined
                                v-model="link.title"
                                label="Title"
                                class="required-field"
                                maxlength="500"
                                :rules="[
                                    (val) => (val && val.trim().length > 0) || 'Title is required',
                                    (val) => !val || val.length <= 500 || 'Title must be 500 characters or less',
                                ]"
                                aria-required="true"
                                lazy-rules
                                hide-bottom-space
                            />

                            <q-input
                                dense
                                outlined
                                v-model="link.description"
                                type="textarea"
                                label="Description"
                                hide-bottom-space
                            />
                        </q-card-section>
                    </q-form>
                    <q-card-section
                        v-if="collectionTags.length > 0"
                        class="q-pt-none"
                    >
                        <fieldset class="tags-fieldset">
                            <legend class="text-subtitle1 q-px-sm">Tags</legend>
                            <div class="text-caption text-grey-7 q-mb-sm">
                                Add additional tags as comma separated values
                            </div>
                            <q-input
                                v-for="tag in collectionTags"
                                :key="tag.linkCollectionTagCategoryId"
                                dense
                                outlined
                                v-model="link.tags[tag.linkCollectionTagCategoryId]"
                                :label="tag.linkCollectionTagCategory"
                                class="q-mb-sm"
                            />
                        </fieldset>
                    </q-card-section>
                    <q-card-actions align="right">
                        <q-btn
                            v-if="link.linkId > 0"
                            label="Delete Link"
                            @click="deleteLink"
                            dense
                            no-caps
                            color="negative"
                            class="q-pr-md"
                        />
                        <q-btn
                            label="Save"
                            @click="saveLink"
                            dense
                            no-caps
                            color="primary"
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
                    <div class="col-12 col-md-auto">Actions</div>
                    <div class="col-12 col-md-3 col-lg-2">URL</div>
                    <div class="col-12 col-md-3 col-lg-2">Title</div>
                    <div class="col-12 col-md-6 col-lg-4">Description</div>
                    <div class="col-12 col-lg-3">Tags</div>
                </div>
                <VueDraggable
                    v-model="links"
                    :animation="200"
                    handle=".handle"
                    @end="linkOrder"
                >
                    <div
                        v-for="(element, index) in links"
                        :key="element.linkId"
                        :class="[
                            'row link-row items-start q-col-gutter-sm link-card link-grid',
                            { 'just-moved': justMovedLinkId === element.linkId },
                        ]"
                    >
                        <div class="col-12 col-md-auto link-actions q-gutter-xs">
                            <div class="action-buttons">
                                <q-icon
                                    class="handle"
                                    name="drag_handle"
                                >
                                    <q-tooltip>Drag to reorder</q-tooltip>
                                </q-icon>
                                <q-btn
                                    dense
                                    no-caps
                                    size="sm"
                                    color="secondary"
                                    icon="edit"
                                    aria-label="Edit link"
                                    @click="clickLink(element)"
                                >
                                    <q-tooltip>Edit</q-tooltip>
                                </q-btn>
                                <q-btn
                                    dense
                                    flat
                                    no-caps
                                    size="sm"
                                    color="secondary"
                                    icon="arrow_upward"
                                    aria-label="Move link up"
                                    :disable="index === 0"
                                    @click="moveLink(index, -1)"
                                >
                                    <q-tooltip>Move up</q-tooltip>
                                </q-btn>
                                <q-btn
                                    dense
                                    flat
                                    no-caps
                                    size="sm"
                                    color="secondary"
                                    icon="arrow_downward"
                                    aria-label="Move link down"
                                    :disable="index === links.length - 1"
                                    @click="moveLink(index, 1)"
                                >
                                    <q-tooltip>Move down</q-tooltip>
                                </q-btn>
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
import { isSafeUrl } from "@/CMS/utils/url"

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

// Tag edits are staged locally while the Edit Collection dialog is open;
// they commit to the server on Save, and discard on X/close.
const draftTags: Ref<LinkCollectionTagCategory[]> = ref([])
const deletedTagIds: Ref<number[]> = ref([])
let nextTempTagId = -1

const links: Ref<LinkWithTags[]> = ref([])
const link: Ref<LinkWithTags> = ref({ linkId: 0, url: "", title: "", description: "", tags: {}, sortOrder: 0 })
const showLinkDialog: Ref<boolean> = ref(false)
const linkFormRef: Ref<any> = ref(null)

const justMovedLinkId: Ref<number | null> = ref(null)
const justMovedTagId: Ref<number | null> = ref(null)

//collections
async function loadCollections(preferredId?: number | null) {
    const { get } = useFetch()
    const res = await get(apiURL)
    collections.value = res.success && Array.isArray(res.result) ? res.result : []

    if (collections.value.length > 0) {
        const preferred =
            preferredId !== null && preferredId !== undefined
                ? collections.value.find((c) => c.linkCollectionId === preferredId)
                : undefined
        collection.value = preferred ?? collections.value[0] ?? null
        collectionId.value = collection.value?.linkCollectionId ?? null
    } else {
        collection.value = null
        collectionId.value = null
    }
}

function openNewCollectionDialog() {
    collection.value = null
    collectionId.value = null
    collectionData.value = { linkCollection: "" }
    draftTags.value = []
    deletedTagIds.value = []
    addTag.value = ""
    showCollectionDialog.value = true
}

async function saveCollection() {
    if (collectionData.value.linkCollection === null || collectionData.value.linkCollection.trim().length === 0) {
        collectionNameInput.value.focus()
        return
    }

    const { put, post, del } = useFetch()
    const isEdit = collection.value !== null && collection.value.linkCollectionId > 0
    const res = isEdit
        ? await put(apiURL + collection.value!.linkCollectionId, {
              linkCollection: collectionData.value.linkCollection,
          })
        : await post(apiURL, { linkCollection: collectionData.value.linkCollection })

    if (!res.success) {
        $q.notify({ type: "negative", message: `Failed to ${isEdit ? "update" : "create"} collection` })
        return
    }

    // Commit staged tag edits (edit mode only — new collections have no tags yet).
    if (isEdit && collectionId.value !== null) {
        const cid = collectionId.value

        for (const id of deletedTagIds.value) {
            const delRes = await del(apiURL + cid + "/tags/" + id)
            if (!delRes.success) {
                $q.notify({ type: "negative", message: "Failed to delete a tag category" })
                return
            }
        }

        const orderedIds: number[] = []
        for (const tag of draftTags.value) {
            if (tag.linkCollectionTagCategoryId < 0) {
                const addRes = await post(apiURL + cid + "/tags", {
                    linkCollectionId: cid,
                    linkCollectionTagCategory: tag.linkCollectionTagCategory,
                    sortOrder: orderedIds.length + 1,
                })
                if (!addRes.success) {
                    $q.notify({ type: "negative", message: "Failed to add a tag category" })
                    return
                }
                orderedIds.push(addRes.result.linkCollectionTagCategoryId)
            } else {
                orderedIds.push(tag.linkCollectionTagCategoryId)
            }
        }

        if (orderedIds.length > 0) {
            const orderPayload = orderedIds.map((id, i) => ({
                linkCollectionTagCategoryId: id,
                sortOrder: i + 1,
            }))
            const orderRes = await put(apiURL + cid + "/tags/order", orderPayload)
            if (!orderRes.success) {
                $q.notify({ type: "negative", message: "Failed to update tag order" })
                return
            }
        }
    }

    $q.notify({ type: "positive", message: `Collection ${isEdit ? "updated" : "created"}` })
    const targetId = isEdit ? collection.value?.linkCollectionId : (res.result?.linkCollectionId ?? null)
    await loadCollections(targetId)
    await loadTags()
    cancelCollectionDialog()
}

async function deleteCollection() {
    const confirmed = await confirmAction(
        "Delete Collection",
        "This will remove the collection and all its links. Continue?",
        "Delete Collection",
        "negative",
    )
    if (!confirmed) return
    const { del } = useFetch()
    const res = await del(apiURL + collection.value?.linkCollectionId)
    if (!res.success) {
        $q.notify({ type: "negative", message: "Failed to delete collection" })
        return
    }
    $q.notify({ type: "positive", message: "Collection deleted" })
    await loadCollections()
    cancelCollectionDialog()
}

async function cancelCollectionDialog() {
    showCollectionDialog.value = false
    // Revert any unsaved name edits to the currently-selected collection's name
    collectionData.value.linkCollection = collection.value?.linkCollection || ""
    draftTags.value = []
    deletedTagIds.value = []
    addTag.value = ""
}

watch(showCollectionDialog, (open) => {
    if (open) {
        draftTags.value = collectionId.value === null ? [] : collectionTags.value.map((t) => ({ ...t }))
        deletedTagIds.value = []
        addTag.value = ""
    }
})

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
    const valid = await linkFormRef.value?.validate()
    if (!valid) return
    const { post, put } = useFetch()
    const isEdit = link.value.linkId !== 0
    let saveRes
    if (!isEdit) {
        saveRes = await post(apiURL + collectionId.value + "/links", {
            linkCollectionId: collectionId.value,
            url: link.value.url,
            title: link.value.title,
            description: link.value.description,
            sortOrder: links.value.length + 1,
        })
        if (saveRes.success) link.value.linkId = saveRes.result.linkId
    } else {
        saveRes = await put(apiURL + collectionId.value + "/links/" + link.value.linkId, {
            linkCollectionId: collectionId.value,
            url: link.value.url,
            title: link.value.title,
            description: link.value.description,
            sortOrder: link.value.sortOrder,
        })
    }

    if (!saveRes.success) {
        $q.notify({ type: "negative", message: `Failed to ${isEdit ? "update" : "create"} link` })
        return
    }

    const tagsRes = await put(apiURL + collectionId.value + "/links/" + link.value.linkId + "/tags", link.value.tags)
    if (!tagsRes.success) {
        $q.notify({ type: "negative", message: "Link saved but tags failed to update" })
        return
    }

    $q.notify({ type: "positive", message: `Link ${isEdit ? "updated" : "created"}` })
    link.value = { linkId: 0, url: "", title: "", description: "", tags: {}, sortOrder: 0 }
    showLinkDialog.value = false
    loadLinks()
}

function clickLink(li: LinkWithTags) {
    link.value = { ...li, tags: { ...li.tags } }
    showLinkDialog.value = true
}

function cancelLinkDialog() {
    link.value = { linkId: 0, url: "", title: "", description: "", tags: {}, sortOrder: 0 }
    showLinkDialog.value = false
    linkFormRef.value?.resetValidation()
}

async function deleteLink() {
    const confirmed = await confirmAction(
        "Delete Link",
        "This will permanently remove this link. Continue?",
        "Delete Link",
        "negative",
    )
    if (!confirmed) return
    const { del } = useFetch()
    const res = await del(apiURL + collectionId.value + "/links/" + link.value.linkId)
    if (!res.success) {
        $q.notify({ type: "negative", message: "Failed to delete link" })
        return
    }
    $q.notify({ type: "positive", message: "Link deleted" })
    link.value = { linkId: 0, url: "", title: "", description: "", tags: {}, sortOrder: 0 }
    showLinkDialog.value = false
    loadLinks()
}

async function confirmAction(title: string, message: string, okLabel: string, okColor = "primary") {
    return await new Promise<boolean>((resolve) => {
        $q.dialog({
            title,
            message,
            cancel: {
                label: "Cancel",
                flat: true,
            },
            persistent: true,
            ok: {
                label: okLabel,
                color: okColor,
                unelevated: true,
            },
        })
            .onOk(() => resolve(true))
            .onCancel(() => resolve(false))
            .onDismiss(() => resolve(false))
    })
}

async function linkOrder() {
    const ordered = links.value.map((link, index) => ({ linkId: link.linkId, sortOrder: index + 1 }))
    const { put } = useFetch()
    const res = await put(apiURL + collectionId.value + "/links-order", ordered)
    if (!res.success) {
        $q.notify({ type: "negative", message: "Failed to save link order" })
        await loadLinks()
    }
}

function moveLink(index: number, direction: -1 | 1) {
    const newIndex = index + direction
    if (newIndex < 0 || newIndex >= links.value.length) return
    const item = links.value[index]
    if (item === undefined) return
    links.value.splice(index, 1)
    links.value.splice(newIndex, 0, item)
    justMovedLinkId.value = item.linkId
    setTimeout(() => {
        if (justMovedLinkId.value === item.linkId) justMovedLinkId.value = null
    }, 600)
    void linkOrder()
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

function createTag() {
    if (!collectionId.value || addTag.value.trim() === "") return
    draftTags.value.push({
        linkCollectionTagCategoryId: nextTempTagId--,
        linkCollectionTagCategory: addTag.value.trim(),
        sortOrder: draftTags.value.length + 1,
    })
    addTag.value = ""
}

function deleteTag(tagId: number) {
    const idx = draftTags.value.findIndex((t) => t.linkCollectionTagCategoryId === tagId)
    if (idx < 0) return
    const removed = draftTags.value[idx]
    if (removed && removed.linkCollectionTagCategoryId > 0) {
        deletedTagIds.value.push(removed.linkCollectionTagCategoryId)
    }
    draftTags.value.splice(idx, 1)
}

function onDragEnd() {
    // No-op: draft reorders commit on Save.
}

function moveTag(index: number, direction: -1 | 1) {
    const newIndex = index + direction
    if (newIndex < 0 || newIndex >= draftTags.value.length) return
    const item = draftTags.value[index]
    if (item === undefined) return
    draftTags.value.splice(index, 1)
    draftTags.value.splice(newIndex, 0, item)
    justMovedTagId.value = item.linkCollectionTagCategoryId
    setTimeout(() => {
        if (justMovedTagId.value === item.linkCollectionTagCategoryId) justMovedTagId.value = null
    }, 600)
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
    gap: 0;
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
    flex-wrap: wrap;
    align-items: center;
    gap: 6px;
}

.handle {
    cursor: grab;
}

.handle:active {
    cursor: grabbing;
}

@media screen and (prefers-reduced-motion: reduce) {
    .reorder-move {
        transition: none;
    }
}

.required-field :deep(.q-field__label)::after {
    content: " *";
    color: var(--q-negative);
}

.link-form :deep(.q-field--error .q-field__bottom) {
    padding-top: 0.25rem;
}

.link-form :deep(.q-field--error .q-field__messages) {
    display: inline-flex;
    align-items: center;
    background: var(--q-negative);
    color: white;
    flex: none;
    width: fit-content;
    padding: 0.125rem 0.5rem 0.125rem 0.25rem;
    border-radius: 1rem;
    font-size: 0.75rem;
    gap: 0.125rem;
}

.link-form :deep(.q-field--error .q-field__messages)::before {
    content: "error";
    font-family: "Material Icons";
    font-size: 1rem;
    line-height: 1;
}

.tags-fieldset {
    border: 1px solid var(--q-primary);
    border-radius: 4px;
    padding: 0.75rem 1rem 0.25rem;
}

.tags-fieldset legend {
    color: var(--q-primary);
    font-weight: 500;
}

@keyframes just-moved-flash {
    0% {
        background-color: var(--q-secondary);
    }

    100% {
        background-color: transparent;
    }
}

@media screen and (prefers-reduced-motion: reduce) {
    .just-moved {
        animation: none;
    }
}

.just-moved {
    animation: just-moved-flash 0.6s ease-out;
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
    margin-bottom: 0;
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
        grid-template-columns: 70px 2.5fr 1.2fr 2.5fr 1.5fr;
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
