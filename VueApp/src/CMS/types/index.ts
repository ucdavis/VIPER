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
    purgeOn: string | null
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

type CmsContentBlock = {
    contentBlockId: number
    content: string
    title: string | null
    system: string
    application: string | null
    page: string | null
    viperSectionPath: string | null
    blockOrder: number | null
    friendlyName: string | null
    allowPublicAccess: boolean
    modifiedOn: string
    modifiedBy: string
    deletedOn: string | null
    permissions: string[]
    files: CmsContentBlockFile[]
}

type CmsContentBlockFile = {
    fileGuid: string
    friendlyName: string
    url: string
}

type CmsContentHistoryItem = {
    contentHistoryId: number
    modifiedOn: string | null
    modifiedBy: string | null
}

type CmsContentHistoryDiff = {
    content: string
    hasComparison: boolean
    hasChanges: boolean
    oldModifiedOn: string | null
    oldModifiedBy: string | null
    newModifiedOn: string | null
    newModifiedBy: string | null
}

// One row in the hub's recent-activity rail, normalized across sources (blocks, files,
// trashed files, left-nav menus); actions are the per-row shortcuts (history, audit, diff).
type RailAction = {
    icon: string
    label: string
    to?: { name: string; query?: Record<string, string> }
    run?: () => void
}

type ActivityItem = {
    key: string
    icon: string
    typeLabel: string
    // Rendered before the time-ago stamp (e.g. "deleted 2 days ago"); empty means plain recency.
    verb?: string
    label: string
    to: { name: string; params?: Record<string, string | number>; query?: Record<string, string> }
    modifiedOn: string
    modifiedBy: string
    actions?: RailAction[]
}

type CmsLeftNavMenu = {
    leftNavMenuId: number
    menuHeaderText: string | null
    system: string
    viperSectionPath: string | null
    page: string | null
    friendlyName: string | null
    modifiedOn: string
    modifiedBy: string
    items: CmsLeftNavItem[]
}

type CmsLeftNavItem = {
    leftNavItemId: number
    menuItemText: string | null
    isHeader: boolean
    url: string | null
    displayOrder: number | null
    permissions: string[]
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

type CmsContentHistoryAudit = {
    contentHistoryId: number
    contentBlockId: number
    title: string | null
    friendlyName: string | null
    page: string | null
    modifiedOn: string | null
    modifiedBy: string | null
    blockDeleted: boolean
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
    CmsContentBlock,
    CmsContentBlockFile,
    CmsContentHistoryItem,
    CmsLeftNavMenu,
    CmsFile,
    CmsFilePerson,
    CmsPersonOption,
    CmsFileAudit,
    CmsContentHistoryAudit,
    CmsContentHistoryDiff,
    RailAction,
    ActivityItem,
    LinkCollection,
    LinkCollectionTagCategory,
    Link,
    LinkTag,
    LinkTagFilter,
    LinkWithTags,
}
