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

// Result of the pre-upload name check: whether the destination name is taken and, if so, the
// suggested free name plus details of the record it collides with (for the overwrite/reuse prompt).
type CmsFileNameCheck = {
    inUse: boolean
    suggestedName: string
    existingFileGuid: string | null
    existingFriendlyName: string | null
    existingDeleted: boolean
    existingModifiedOn: string | null
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
    // Holders of any of these permissions may edit this block's content and files (but not its
    // settings) even without ManageContentBlocks. Managers manage this list; delegated editors
    // receive it but never see it in the UI.
    editPermissions: string[]
    files: CmsContentBlockFile[]
}

type CmsContentBlockFile = {
    fileGuid: string
    friendlyName: string
    url: string
}

// A block the current user may edit via delegation (GET /api/CMS/content/editable). It carries just
// enough to render the hub's "Blocks you can edit" list and link to the editor.
type EditableBlock = {
    contentBlockId: number
    title: string | null
    friendlyName: string | null
    viperSectionPath: string | null
    page: string | null
    modifiedOn: string
    modifiedBy: string
}

// A managed file the user may attach to a block, from GET /api/CMS/content/attachable-files. Deliberately
// slimmer than CmsFile: the search only needs the GUID to attach and a name to display.
type AttachableFile = {
    fileGuid: string
    friendlyName: string
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
    EditableBlock,
    AttachableFile,
    CmsContentHistoryItem,
    CmsLeftNavMenu,
    CmsFile,
    CmsFilePerson,
    CmsFileNameCheck,
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
