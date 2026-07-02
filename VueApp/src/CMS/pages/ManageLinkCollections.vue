<template>
    <div class="link-collections-page">
        <h1 class="q-mt-none q-mb-sm">Manage Link Collections</h1>

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
                        label="Add Collection"
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
            persistent
            aria-labelledby="collection-dialog-title"
            @keydown.escape="handleCollectionClose"
        >
            <q-card class="dialog-card">
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
                        @click="handleCollectionClose"
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
                            label="New tag category"
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

                    <SortableList
                        v-model="draftTags"
                        item-key="linkCollectionTagCategoryId"
                        class="q-mt-sm"
                        move-up-label="Move tag up"
                        move-down-label="Move tag down"
                        :announce="announceTag"
                    >
                        <template #row="{ item }">
                            <q-input
                                dense
                                readonly
                                v-model="item.linkCollectionTagCategory"
                            />
                        </template>
                        <template #actions="{ item }">
                            <q-btn
                                dense
                                flat
                                no-caps
                                size="sm"
                                color="negative"
                                icon="delete"
                                aria-label="Delete tag category"
                                @click="deleteTag(item.linkCollectionTagCategoryId)"
                            >
                                <q-tooltip>Delete</q-tooltip>
                            </q-btn>
                        </template>
                    </SortableList>
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
                class="q-mt-sm q-pr-md"
            />
            <q-dialog
                v-model="showLinkDialog"
                persistent
                aria-labelledby="link-dialog-title"
                @keydown.escape="cancelLinkDialog"
            >
                <q-card class="dialog-card">
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
                    <q-card-section
                        v-if="linkFormError"
                        class="q-pt-none"
                    >
                        <StatusBanner type="error">
                            {{ linkFormError }}
                        </StatusBanner>
                    </q-card-section>
                    <q-card-actions align="right">
                        <q-btn
                            v-if="link.linkId > 0"
                            label="Delete Link"
                            @click="deleteLink"
                            dense
                            no-caps
                            color="negative"
                        />
                        <q-btn
                            label="Save"
                            @click="saveLink"
                            dense
                            no-caps
                            color="primary"
                        />
                    </q-card-actions>
                </q-card>
            </q-dialog>

            <div
                id="allLinks"
                class="q-mt-md"
            >
                <SortableList
                    v-model="links"
                    item-key="linkId"
                    class="links-list"
                    move-up-label="Move link up"
                    move-down-label="Move link down"
                    :announce="announceLink"
                    @reorder="linkOrder"
                >
                    <template #header>
                        <div class="link-grid">
                            <div>URL</div>
                            <div>Title</div>
                            <div>Description</div>
                            <div>Tags</div>
                        </div>
                    </template>

                    <template #row="{ item }">
                        <div class="link-grid">
                            <div class="link-field url-field">
                                <div class="field-label">URL</div>
                                <div class="link-url">{{ item.url }}</div>
                            </div>
                            <div class="link-field title-field">
                                <div class="field-label">Title</div>
                                <div>{{ item.title }}</div>
                            </div>
                            <div class="link-field desc-field">
                                <div class="field-label">Description</div>
                                <div>{{ item.description }}</div>
                            </div>
                            <div class="link-field tag-field">
                                <template
                                    v-for="tag in collectionTags"
                                    :key="tag.linkCollectionTagCategoryId"
                                >
                                    <div
                                        v-if="item.tags !== undefined"
                                        class="tag-row"
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
                                                        item,
                                                        tag.linkCollectionTagCategoryId,
                                                    )"
                                                    :key="
                                                        tag.linkCollectionTagCategoryId.toString() + ' ' + t.toString()
                                                    "
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
                    </template>

                    <template #actions="{ item }">
                        <EditButton
                            entity-name="link"
                            @click="clickLink(item)"
                        />
                        <q-btn
                            dense
                            flat
                            no-caps
                            size="sm"
                            color="negative"
                            icon="delete"
                            aria-label="Delete link"
                            @click="deleteLinkRow(item)"
                        >
                            <q-tooltip>Delete</q-tooltip>
                        </q-btn>
                    </template>
                </SortableList>
            </div>
        </template>
    </div>
</template>

<script setup lang="ts">
import { ref, inject, computed, watch } from "vue"
import { useQuasar } from "quasar"
import type { Ref } from "vue"
import type { LinkCollection, Link, LinkCollectionTagCategory, LinkWithTags } from "@/CMS/types/"
import { useFetch } from "@/composables/ViperFetch"
import { useUnsavedChanges } from "@/composables/use-unsaved-changes"
import EditButton from "@/CMS/components/EditButton.vue"
import SortableList from "@/components/SortableList.vue"
import StatusBanner from "@/components/StatusBanner.vue"
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

// Guard the collection dialog's editable state (the name plus the staged tag adds,
// deletes, and reorders) so the X/Escape can't silently discard unsaved tag edits —
// mirrors the link dialog's unsaved-changes guard on the same page.
// Iterate the arrays here (map/spread) so the snapshot deep-tracks in-place mutations
// (SortableList reorder, createTag push, deleteTag splice), not just the ref identity.
const collectionDialogState = computed(() => ({
    name: collectionData.value.linkCollection ?? "",
    draftTags: draftTags.value.map((t) => ({ id: t.linkCollectionTagCategoryId, label: t.linkCollectionTagCategory })),
    deletedTagIds: [...deletedTagIds.value],
}))
const { setInitialState: setCollectionBaseline, confirmClose: confirmCollectionClose } =
    useUnsavedChanges(collectionDialogState)

const links: Ref<LinkWithTags[]> = ref([])
const link: Ref<LinkWithTags> = ref({ linkId: 0, url: "", title: "", description: "", tags: {}, sortOrder: 0 })
const showLinkDialog: Ref<boolean> = ref(false)
const linkFormRef: Ref<any> = ref(null)
const linkFormError = ref("")
const { setInitialState: setLinkBaseline, confirmClose: confirmLinkClose } = useUnsavedChanges(link)

// Capture the dialog's clean baseline whenever it opens, for the unsaved-changes guard.
watch(showLinkDialog, (open) => {
    if (open) {
        linkFormError.value = ""
        setLinkBaseline()
    }
})

function announceLink(item: LinkWithTags, newIndex: number, total: number) {
    return `Moved ${item.title || "link"} to position ${newIndex + 1} of ${total}`
}

function announceTag(item: LinkCollectionTagCategory, newIndex: number, total: number) {
    return `Moved tag ${item.linkCollectionTagCategory} to position ${newIndex + 1} of ${total}`
}

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
    // Tag mutations aren't transactional on the server; if any step fails
    // mid-sequence we reload tags so the dialog reflects true server state
    // instead of the stale staged edits the user was working from.
    if (isEdit && collectionId.value !== null) {
        const cid = collectionId.value

        async function handleTagFailure(message: string) {
            $q.notify({ type: "negative", message })
            await loadTags()
            draftTags.value = collectionTags.value.map((t) => ({ ...t }))
            deletedTagIds.value = []
        }

        for (const id of deletedTagIds.value) {
            const delRes = await del(apiURL + cid + "/tags/" + id)
            if (!delRes.success) {
                await handleTagFailure("Failed to delete a tag category")
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
                    await handleTagFailure("Failed to add a tag category")
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
                await handleTagFailure("Failed to update tag order")
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
        // Capture the clean baseline after the staged state is reset, for the guard.
        setCollectionBaseline()
    }
})

// X/Escape close: confirm before discarding staged name/tag edits, then clean up.
async function handleCollectionClose() {
    if (!(await confirmCollectionClose())) return
    cancelCollectionDialog()
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
    for (const lt of r.linkTags || []) {
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
    linkFormError.value = ""
    const valid = await linkFormRef.value?.validate()
    if (!valid) {
        linkFormError.value = "Please complete the required fields before saving this link."
        return
    }
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
        linkFormError.value = saveRes.errors?.[0] ?? `Failed to ${isEdit ? "update" : "create"} link`
        return
    }

    const tagsRes = await put(apiURL + collectionId.value + "/links/" + link.value.linkId + "/tags", link.value.tags)
    if (!tagsRes.success) {
        linkFormError.value = "Link saved but tags failed to update"
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

async function cancelLinkDialog() {
    if (!(await confirmLinkClose())) return
    link.value = { linkId: 0, url: "", title: "", description: "", tags: {}, sortOrder: 0 }
    showLinkDialog.value = false
    linkFormError.value = ""
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

// Row-level delete (the dialog keeps its own Delete for the link being edited).
async function deleteLinkRow(li: LinkWithTags) {
    const confirmed = await confirmAction(
        "Delete Link",
        `This will permanently remove "${li.title}". Continue?`,
        "Delete Link",
        "negative",
    )
    if (!confirmed) return
    const { del } = useFetch()
    const res = await del(apiURL + collectionId.value + "/links/" + li.linkId)
    if (!res.success) {
        $q.notify({ type: "negative", message: "Failed to delete link" })
        return
    }
    $q.notify({ type: "positive", message: "Link deleted" })
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
        return
    }
    // Reorder persists immediately; confirm it. `group` collapses rapid nudges into one toast.
    $q.notify({ type: "positive", message: "Order saved", group: "link-order", timeout: 1500 })
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

watch(collectionId, () => {
    loadLinks()
    loadTags()
    collection.value = collections.value.find((c) => c.linkCollectionId === collectionId.value) || null
    collectionData.value.linkCollection = collection.value?.linkCollection || ""
})

loadCollections()
</script>

<style scoped>
.dialog-card {
    min-width: 21.875rem;
    max-width: 95vw;
}

#allLinks {
    background: var(--surface);
    border: 1px solid var(--ucdavis-black-10);
    border-radius: 0.25rem;
    padding: 0.5rem 0.75rem;
}

.link-url {
    white-space: normal;
    word-break: break-word;
    overflow-wrap: anywhere;
}

.tag-row-inner {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 0;
    width: 100%;
    text-align: left;
    margin-bottom: 0.375rem;
}

.tag-chips {
    display: flex;
    flex-wrap: wrap;
    gap: 0.25rem;
    max-width: 100%;
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

.tag-row {
    margin-bottom: 0.25rem;
}

/* The column header only reads as one once the fields line up in a grid (desktop). */
.links-list :deep(.sortable-row--header) {
    display: none;
    font-weight: bold;
    color: var(--q-primary);
}

/* Card-per-row on small and medium screens. */
.links-list :deep(.sortable-row) {
    border: 1px solid var(--ucdavis-black-10);
    border-radius: 0.25rem;
    padding: 0.75rem;
    background: var(--surface-tint-raised);
}

/* Four actions here (up/down/edit/delete) at touch size; widen the gutter so the
   header columns still line up with the rows. */
.links-list :deep(.sortable-row__controls) {
    min-width: 10rem;
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
    border-radius: 0.25rem;
    padding: 0.75rem 1rem 0.25rem;
}

.tags-fieldset legend {
    color: var(--q-primary);
    font-weight: 500;
}

@media (width >= 1024px) {
    /* Tabular layout: four aligned columns, labels in the header row instead of per field. */
    .link-grid {
        display: grid;
        grid-template-columns: 2.5fr 1.2fr 2.5fr 1.5fr;
        column-gap: 0.75rem;
        align-items: start;
    }

    .links-list :deep(.sortable-row--header) {
        display: flex;
    }

    .links-list :deep(.sortable-list__items) {
        gap: 0;
    }

    .links-list :deep(.sortable-row) {
        border: none;
        border-bottom: 1px solid var(--ucdavis-black-10);
        border-radius: 0;
        padding: 0.5rem 0;
        background: transparent;
    }

    /* The visual column header is aria-hidden, so keep the URL/Title/Description labels
       in the a11y tree (visually hidden) instead of display:none, so screen readers
       still announce which value is which. Tag-category labels stay visible. */
    .link-field:not(.tag-field) .field-label {
        position: absolute;
        width: 1px;
        height: 1px;
        overflow: hidden;
        clip-path: inset(50%);
        white-space: nowrap;
    }

    .tag-field .field-label {
        display: block;
    }

    .tag-row {
        margin-bottom: 0.375rem;
    }
}
</style>
