<template>
    <h2>{{ linkCollection?.linkCollection }}</h2>

    <div
        v-if="!loaded"
        class="text-center q-my-lg"
    >
        <q-spinner-dots
            size="3rem"
            color="primary"
        />
        <div class="q-mt-md text-body1">Loading...</div>
    </div>

    <template v-else>
        <q-expansion-item
            v-model="filtersExpandedComputed"
            icon="filter_list"
            label="Filters"
            header-class="bg-grey-2 lt-sm"
            class="q-mb-md"
        >
            <q-form>
                <div class="row q-pa-sm q-gutter-md">
                    <q-input
                        v-model="search"
                        dense
                        outlined
                        label="Search"
                        stack-label
                        clearable
                        debounce="300"
                        class="col-12 col-sm-6 col-md-3 col-lg-2"
                    >
                        <template #append>
                            <q-icon name="search" />
                        </template>
                    </q-input>
                    <template
                        v-for="tf in tagFilters"
                        :key="tf.linkCollectionTagCategoryId"
                    >
                        <q-select
                            v-model="tf.selected"
                            dense
                            options-dense
                            outlined
                            :options="tf.options"
                            :label="tf.linkCollectionTagCategory"
                            stack-label
                            clearable
                            class="col-12 col-sm-6 col-md-3 col-lg-2 col-xl-1"
                        ></q-select>
                    </template>
                </div>
            </q-form>
        </q-expansion-item>

        <StatusBanner
            v-if="filteredLinks.length === 0"
            type="info"
        >
            No links found.
        </StatusBanner>

        <template v-else-if="props.groupByTagCategory == null || props.groupByTagCategory.length == 0">
            <div
                class="q-pa-md row q-gutter-md"
                v-if="linkCollection != null"
            >
                <template
                    v-for="li in filteredLinks"
                    :key="li.linkId"
                >
                    <LinkComponent
                        :link="li"
                        :link-collection="linkCollection"
                    ></LinkComponent>
                </template>
            </div>
        </template>
        <template v-else-if="linkCollection != null">
            <template
                v-for="groupBy in groupByValues"
                :key="groupBy"
            >
                <div
                    v-if="(groupedLinks.get(groupBy)?.length ?? 0) > 0"
                    class="q-pa-md row q-gutter-md"
                >
                    <div class="col-12">
                        <h3 class="q-mt-lg q-mb-sm">{{ groupBy }}</h3>
                    </div>
                    <LinkComponent
                        v-for="li in groupedLinks.get(groupBy)"
                        :key="li.linkId"
                        :link="li"
                        :link-collection="linkCollection"
                    />
                </div>
            </template>
        </template>
    </template>
</template>

<script setup lang="ts">
import type { Ref } from "vue"
import { ref, computed, watch } from "vue"
import { useQuasar } from "quasar"
import type { Link, LinkCollection, LinkTag, LinkTagFilter } from "@/CMS/types"
import { useFetch } from "@/composables/ViperFetch"
import { default as LinkComponent } from "@/CMS/components/Link.vue"
import StatusBanner from "@/components/StatusBanner.vue"

const $q = useQuasar()

const props = withDefaults(
    defineProps<{
        linkCollectionName: string
        groupByTagCategory?: string | null
    }>(),
    {
        groupByTagCategory: null,
    },
)
const tagFilters: Ref<LinkTagFilter[]> = ref([])
const linkCollection: Ref<LinkCollection | null> = ref(null)
const search: Ref<string> = ref("")

const links: Ref<Link[]> = ref([])
const filteredLinks: Ref<Link[]> = ref([])
const loaded: Ref<boolean> = ref(false)

const groupByValues: Ref<string[]> = ref([])
const groupById: Ref<number | null> = ref(null)

const filtersExpanded = ref(false)
const filtersExpandedComputed = computed({
    get: () => ($q.screen.gt.xs ? true : filtersExpanded.value),
    set: (val: boolean) => {
        filtersExpanded.value = val
    },
})

async function getLinkCollection() {
    const { get } = useFetch()
    const baseUrl = import.meta.env.VITE_API_URL + "cms/linkcollections/"
    const r = await get(baseUrl + "?linkCollectionName=" + encodeURIComponent(props.linkCollectionName))
    linkCollection.value = r.success && Array.isArray(r.result) ? ((r.result[0] as LinkCollection) ?? null) : null
    if (linkCollection.value) {
        await loadLinks(linkCollection.value.linkCollectionId)
        await loadFilters()
    }
    loaded.value = true
}

async function loadLinks(collectionId: number) {
    const { get } = useFetch()
    let baseUrl = import.meta.env.VITE_API_URL + "cms/linkCollections/" + collectionId + "/links"
    if (props.groupByTagCategory !== null && props.groupByTagCategory.length > 0) {
        baseUrl += "?groupByTagCategory=" + encodeURIComponent(props.groupByTagCategory)
    }

    const r = await get(baseUrl)
    links.value = r.result as Link[]
    filteredLinks.value = links.value
}

function loadFilters() {
    if (linkCollection.value !== null) {
        for (const tagCat of linkCollection.value.linkCollectionTagCategories) {
            tagFilters.value.push({
                linkCollectionTagCategoryId: tagCat.linkCollectionTagCategoryId,
                linkCollectionTagCategory: tagCat.linkCollectionTagCategory,
                options: [],
                selected: null,
            })
        }
        for (const l of links.value) {
            for (const lt of l.linkTags) {
                const filter = tagFilters.value.find(
                    (f) => f.linkCollectionTagCategoryId === lt.linkCollectionTagCategoryId,
                )
                if (filter) {
                    filter.options.push(lt.value)
                }
            }
        }
        for (const tf of tagFilters.value) {
            tf.options = tf.options.sort()
        }

        for (const tagOptions of tagFilters.value) {
            tagOptions.options = [...new Set(tagOptions.options)]
            if (
                props.groupByTagCategory !== null &&
                props.groupByTagCategory === tagOptions.linkCollectionTagCategory
            ) {
                groupByValues.value = tagOptions.options.sort()
                groupById.value = tagOptions.linkCollectionTagCategoryId
            }
        }
    }
}

// Bucket the filtered links by their value in the group-by category once per render,
// so the template does an O(1) Map.get per group instead of re-filtering every link
// (twice) for every group.
const groupedLinks = computed(() => {
    const buckets = new Map<string, Link[]>()
    if (groupById.value === null) {
        return buckets
    }
    for (const value of groupByValues.value) {
        buckets.set(value, [])
    }
    for (const link of filteredLinks.value) {
        const seen = new Set<string>()
        for (const tag of link.linkTags) {
            if (tag.linkCollectionTagCategoryId === groupById.value && !seen.has(tag.value)) {
                seen.add(tag.value)
                buckets.get(tag.value)?.push(link)
            }
        }
    }
    return buckets
})

function applyFilters() {
    filteredLinks.value = links.value
    for (const tf of tagFilters.value) {
        if (tf.selected !== null && tf.selected !== "") {
            filteredLinks.value = filteredLinks.value.filter((fl: Link) => {
                const findTag = fl.linkTags.find((lt: LinkTag) => {
                    return lt.linkCollectionTagCategoryId === tf.linkCollectionTagCategoryId && lt.value === tf.selected
                })
                return findTag !== undefined
            })
        }
    }
    if (search.value !== null && search.value.length) {
        let sv = search.value.toLowerCase()
        filteredLinks.value = filteredLinks.value.filter((fl: Link) => {
            return (
                fl.title.toLowerCase().includes(sv) ||
                fl.description.toLowerCase().includes(sv) ||
                fl.linkTags.some((t: LinkTag) => t.value.toLowerCase().includes(sv))
            )
        })
    }
}

watch(
    tagFilters,
    () => {
        applyFilters()
    },
    { deep: true },
)
watch(search, () => {
    applyFilters()
})

getLinkCollection()
</script>
