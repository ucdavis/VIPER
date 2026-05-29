type ContentBlock = {
    contentBlockId: number
    content: string
    title: string | null
}

type LinkCollection = {
    linkCollectionId: number
    linkCollection: string
    linkCollectionTagCategories: LinkCollectionTagCategory[]
}

type LinkCollectionTagCategory = {
    linkCollectionTagCategoryId: number
    linkCollectionTagCategory: string
    sortOrder: number
}

type Link = {
    linkId: number
    url: string
    title: string
    description: string
    linkTags: LinkTag[]
    order: number
}

type LinkTag = {
    linkTagId: number
    linkId: number
    linkCollectionTagCategoryId: number
    sortOrder: number
    value: string
}

type LinkTagFilter = {
    linkCollectionTagCategoryId: number
    linkCollectionTagCategory: string
    options: string[]
    selected: string | null
}

type LinkWithTags = {
    linkId: number
    url: string
    title: string
    description: string
    tags: Record<number, string>
    sortOrder: number
}

export type { ContentBlock, LinkCollection, LinkCollectionTagCategory, Link, LinkTag, LinkTagFilter, LinkWithTags }
