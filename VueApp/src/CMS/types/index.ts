type ContentBlock = {
    contentBlockId: number
    content: string
    title: string | null
}

type CmsFile = {
    fileGuid: string
    fileName: string
    folder: string | null
    friendlyName: string
    encrypted: boolean
    description: string
    allowPublicAccess: boolean
    oldUrl: string | null
    modifiedOn: string
    modifiedBy: string
    deletedOn: string | null
    permissions: string[]
    people: CmsFilePerson[]
    url: string
    friendlyUrl: string
}

type CmsFilePerson = {
    iamId: string
    name: string | null
}

type CmsPersonOption = {
    iamId: string
    name: string
    loginId: string | null
    mailId: string | null
}

type CmsFileAudit = {
    auditId: number
    timestamp: string
    loginid: string | null
    action: string
    detail: string | null
    fileGuid: string | null
    filePath: string | null
    iamId: string | null
    fileMetaData: string | null
    clientData: string | null
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

export type {
    ContentBlock,
    CmsFile,
    CmsFilePerson,
    CmsPersonOption,
    CmsFileAudit,
    LinkCollection,
    LinkCollectionTagCategory,
    Link,
    LinkTag,
    LinkTagFilter,
    LinkWithTags,
}
