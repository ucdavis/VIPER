export type LinkCollection = {
    linkCollectionId: number,
    linkCollection: string,
    linkCollectionTagCategories: LinkCollectionTagCategory[]
}

export type LinkCollectionTagCategory= {
    linkCollectionTagCategoryId: number,
    linkCollectionTagCategory: string,
    sortOrder: number
}

export type Link = {
    linkId: number,
    url: string,
    title: string,
    description: string,
    tags: LinkTag[],
    order: number
}

export type LinkTag = {
    linkTagId: number,
    linkId: number,
    linkCollectionTagCategoryId: number,
    order: number,
    values: string
}

export type LinkTagFilter = {
    linkCollectionTagCategoryId: number,
    linkCollectionTagCategory: string,
    options: string[],
    selected: string | null
}