/*
 * Plain DOM status toast. Quasar.Notify.create() does not render in the UMD/CDN
 * setup because the app mounts on <body> and Notify's teleport container ends up
 * outside Vue's reactive scope, so notifications are silently dropped.
 */
function showStatusNotification(message) {
    const el = document.createElement("div")
    el.setAttribute("role", "status")
    el.setAttribute("aria-live", "polite")
    el.className = "viper-status-notification"
    el.textContent = message
    document.body.appendChild(el)
    // Trigger reflow so the CSS transition activates
    void el.offsetHeight
    el.classList.add("viper-status-notification--visible")
    // oxlint-disable-next-line no-magic-numbers -- notification display duration in ms
    setTimeout(() => {
        el.classList.remove("viper-status-notification--visible")
        el.addEventListener("transitionend", () => el.remove())
    }, 3000)
}

/*
 * QuasarTable - code to support a quasar table with an edit dialog and add/update/delete functions, with optional server side paging/filtering and export to csv
 */
quasarTableDefaultConfig = {
    // Base of the url and keys of the objects, e.g. a urlBase of "Permissions" and a key of "id" means the following ajax calls will be made
    // GET Permissions - load objects
    // POST Permissions - create a new permission
    // PUT Permissions/5 - update permission with ID 5
    // DELETE Permissions/5 - delete permission with ID 5
    // Multiple keys can be specified in an array of objects. For example, keys=["id", {column: "memberId", urlPrefix="member"}]
    // Would create the PUT and DELETE url Permissions/5/member/12345678
    urlBase: "",
    keys: "id",
    // Query params to append to GET requests, e.g. search=x&startDate=2023-01-01
    query: {},
    // Function to execute after data has been loaded, for example to add or alter columns
    onLoad: "",
    // Function to create the body of a POST or PUT
    createBody: "",
    // Function to select the object
    selectObject: "",
    // Default pagination options
    pagination: { rowsPerPage: 15 },
    // Default rows per page options
    // oxlint-disable-next-line no-magic-numbers -- UI pagination choices
    rowsPerPageOptions: [5, 10, 15, 25, 50, 100, 0],
    // Set to true to use server side pagination
    serverSidePagination: false,
    // Columns definition, required if exporting is enabled
    columns: [],
    excludeFromExport: [],
    // Key to use for session storage, e.g. to persist pagination and table sorting
    sessionKey: null,
}
class quasarTable {
    constructor(config) {
        // Override default config properties with input
        Object.assign(this, { ...quasarTableDefaultConfig, ...config })
        // The data for the rows being shown
        this.rows = []
        // If true, the data is being (re)loaded
        this.loading = true
        // The object being edited
        this.object = {}
        // True if a form to edit the selected object is being shown
        this.showForm = false
        // If showing the form, this is true if updating an existing object, and false if creating a new object
        this.editing = false
        // Errors to be displayed on a form
        this.errors = {}
        // Filter variable, necessary for server side pagination
        this.filter = ""
        // When loading, store a reference to the vueApp
        this.vueApp = null
    }

    // Load data. For server side pagination, send the pagination options as query params.
    load(vueApp) {
        if (vueApp && this.vueApp === null) {
            this.vueApp = vueApp
        }

        if (this.sessionKey !== null && this.sessionKey.length > 0) {
            const pag = getItemFromStorage(`${this.sessionKey}_pagination`)
            if (pag) {
                this.pagination = pag
            }
        }

        let queryParams = ""
        this.loading = true
        if (this.serverSidePagination) {
            const queryParamObject = {
                perPage: this.pagination.rowsPerPage,
                page: this.pagination.page,
                sortOrder: (this.pagination.sortBy || "") + (this.pagination.descending ? " desc" : ""),
                filter: this.filter || "",
            }
            queryParams = `?${new URLSearchParams(queryParamObject)}`
        }
        viperFetch(vueApp, this.urlBase + queryParams, {}, [])
            // oxlint-disable-next-line prefer-await-to-then, always-return -- promise chain is the established pattern here
            .then((r) => {
                if (r) {
                    if (this.serverSidePagination) {
                        // Record data and pagination
                        this.rows = r.result
                        this.pagination.rowsNumber = r.pagination.totalRecords
                        this.pagination.rowsPerPage = r.pagination.perPage
                        this.pagination.page = r.pagination.page
                    } else {
                        this.rows = r
                    }
                }

                if (this.onLoad) {
                    this.onLoad(this.rows, vueApp)
                }
            })
            // oxlint-disable-next-line prefer-await-to-then, always-return -- promise chain is the established pattern here
            .then(() => {
                this.loading = false
                this.clear()
            })
            // oxlint-disable-next-line prefer-await-to-then, prefer-await-to-callbacks -- promise chain is the established pattern here
            .catch((error) => showViperFetchError(this.vueApp, error, this.errors))
    }

    savePagination(v) {
        this.pagination = v
        if (this.sessionKey !== null && this.sessionKey.length > 0) {
            putItemInStorage(`${this.sessionKey}_pagination`, this.pagination)
        }
    }

    // Select an item for editing
    selectRow(item) {
        this.editing = true
        this.object = item
        if (this.selectObject) {
            this.selectObject(item)
        }
        this.showForm = true
    }

    // Clear the selected item
    clear() {
        this.editing = false
        this.object = {}
        this.showForm = false
        this.errors = {}
    }

    // Submit (create or update) the selected item
    async submit(vueApp) {
        const bodyObject = this.createBody ? this.createBody(vueApp, this.object) : this.object
        await (this.editing
            ? this.update(vueApp, bodyObject)
            : this.create(vueApp, bodyObject))
    }

    async create(vueApp, bodyObject) {
        const result = await viperFetch(
            vueApp,
            this.urlBase,
            {
                method: "POST",
                body: JSON.stringify(bodyObject),
                headers: { "Content-Type": "application/json" },
            },
            [() => this.load(this)],
            this.errors,
        )
        if (result !== undefined) {
            showStatusNotification("Item created")
        }
    }

    async update(vueApp, bodyObject) {
        const result = await viperFetch(
            vueApp,
            this.getUpdateURL(),
            {
                method: "PUT",
                body: JSON.stringify(bodyObject),
                headers: { "Content-Type": "application/json" },
            },
            [() => this.load(this)],
            this.errors,
        )
        if (result !== undefined) {
            showStatusNotification("Item updated")
        }
    }

    // Delete the selected item (with confirmation dialog)
    async delete(vueApp) {
        return new Promise((resolve) => {
            vueApp.$q.dialog({
                title: "Confirm Delete",
                message: "Are you sure you want to delete this item?",
                cancel: true,
                persistent: true,
            })
                .onOk(async () => {
                    const result = await viperFetch(
                        vueApp,
                        this.getUpdateURL(),
                        { method: "DELETE" },
                        [() => this.load(this)],
                        this.errors,
                    )
                    if (result !== undefined) {
                        showStatusNotification("Item deleted")
                    }
                    resolve(result !== undefined)
                })
                .onCancel(() => {
                    resolve(false)
                })
        })
    }

    // Get the URL to create or update the item
    // See comment under quasarTableDefaultConfig.urlBase
    getUpdateURL() {
        let url = this.urlBase
        if (Array.isArray(this.keys)) {
            for (const k of this.keys) {
                if (typeof k === "object" && k !== null && !Array.isArray(k)) {
                    if (k?.urlPrefix) {
                        url += `/${k.urlPrefix}`
                    }
                    url += `/${this.object[k.column]}`
                } else {
                    url += `/${this.object[k]}`
                }
            }
        } else {
            url += `/${this.object[this.keys]}`
        }
        return url
    }

    // Function called for server side paging when pagination options or filter options are changed
    request(props, vueApp) {
        this.pagination.page = props.pagination.page
        this.pagination.rowsPerPage = props.pagination.rowsPerPage
        if (props.pagination.sortBy) {
            this.pagination.sortOrder = props.pagination.sortBy
            this.pagination.descending = props.pagination.descending
        } else {
            this.pagination.sortOrder = ""
        }

        this.load(vueApp)
    }

    // Export function
    exportTable() {
        // Naive encoding to csv format
        const columnsMinusExcludes = this.columns.filter((c) => !this.excludeFromExport.some((e) => e === c.name))
        const content = [
            columnsMinusExcludes.map((col) => wrapCsvValue(col.label)),
            ...this.rows.map((row) =>
                columnsMinusExcludes
                    .map((col) =>
                        wrapCsvValue(
                            typeof col.field === "function"
                                ? col.field(row)
                                : row[col.field === undefined ? col.name : col.field],
                            col.format,
                            row,
                        ),
                    )
                    .join(","),
            ),
        ].join("\r\n")

        const { useQuasar, exportFile } = Quasar
        const $q = useQuasar()

        const status = exportFile("table-export.csv", content, "text/csv")

        if (status !== true) {
            $q.notify({
                message: "Browser denied file download...",
                color: "negative",
                icon: "warning",
            })
        }
    }
}

// Escape double quotes (" -> "") and new lines in the content
function wrapCsvValue(val, formatFn, row) {
    let formatted = formatFn === undefined ? val : formatFn(val, row)

    formatted = formatted === undefined || formatted === null ? "" : String(formatted)

    formatted = formatted
        .split('"')
        .join('""')
        /**
         * Excel accepts \n and \r in strings, but some other CSV parsers do not
         * Uncomment the next two lines to escape new lines
         */
        .split("\n")
        .join(String.raw`\n`)
        .split("\r")
        .join(String.raw`\r`)

    return `"${formatted}"`
}
