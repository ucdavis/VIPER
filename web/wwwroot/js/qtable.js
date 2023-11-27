/*
 * quasarTable - code to support a quasar table with an edit dialog and add/update/delete functions, with optional server side paging/filtering and export to csv
 */
quasarTableDefaultConfig = {
    //base of the url and keys of the objects, e.g. a urlBase of "Permissions" and a key of "id" means the following ajax calls will be made
    //GET Permissions - load objects
    //POST Permissions - create a new permission
    //PUT Permissions/5 - update permission with ID 5
    //DELETE Permissions/5 - delete permission with ID 5
    //multiple keys can be specified in an array of objects. For example, keys=["id", {column: "memberId", urlPrefix="member"}]
    //would create the PUT and DELETE url Permissions/5/member/12345678
    urlBase: "",
    keys: "id",
    //query params to append to GET requests, e.g. search=x&startDate=2023-01-01
    query: {},
    //function to execute after data has been loaded, for example to add or alter columns
    onLoad: "",
    //function to create the body of a POST or PUT 
    createBody: "",
    //function to select the object
    selectObject: "",
    //default pagination options
    pagination: { rowsPerPage: 15 },
    //default rows per page options
    rowsPerPageOptions: [5, 10, 15, 25, 50, 100, 0],
    //set to true to use server side pagination
    serverSidePagination: false,
    //columns definition, required if exporting is enabled
    columns: [],
    excludeFromExport: [],
    //key to use for session storage, e.g. to persist pagination and table sorting
    sessionKey: null
}
class quasarTable {
    constructor(config) {
        //override default config properties with input
        Object.assign(this, { ...quasarTableDefaultConfig, ...config })
        //the data for the rows being shown
        this.rows = []
        //if true, the data is being (re)loaded
        this.loading = true
        //the object being edited
        this.object = {}
        //true if a form to edit the selected object is being shown
        this.showForm = false
        //if showing the form, this is true if updating an existing object, and false if creating a new object
        this.editing = false
        //errors to be displayed on a form
        this.errors = {}
        //filter variable, necessary for server side pagination
        this.filter = ""
        //when loading, store a reference to the vueApp
        this.vueApp = null
    }

    //Load data. For server side pagination, send the pagination options as query params.
    load(vueApp) {
        if (vueApp != null && this.vueApp == null) {
            this.vueApp = vueApp
        }

        if (this.sessionKey != null && this.sessionKey.length) {
            var pag = getItemFromStorage(this.sessionKey + "_pagination")
            if (pag) {
                this.pagination = pag
            }
        }
        
        var queryParams = "";
        this.loading = true
        if (this.serverSidePagination) {
            var queryParamObject = {
                perPage: this.pagination.rowsPerPage,
                page: this.pagination.page,
                sortOrder: (this.pagination.sortBy ? this.pagination.sortBy : "") + (this.pagination.descending ? " desc" : ""),
                filter: (this.filter ? this.filter : "")
            }
            queryParams = "?" + new URLSearchParams(queryParamObject)
        }
        viperFetch(vueApp, this.urlBase + queryParams, {}, [])
            .then(r => {
                if (r) {
                    if (this.serverSidePagination) {
                        //record data and pagination
                        this.rows = r.result
                        this.pagination.rowsNumber = r.pagination.totalRecords
                        this.pagination.rowsPerPage = r.pagination.perPage
                        this.pagination.page = r.pagination.page
                    }
                    else {
                        this.rows = r
                    }
                }
                
                if (this.onLoad) {
                    this.onLoad.call(this, this.rows, vueApp)
                }
            })
            .then(r => {
                this.loading = false
                this.clear()
            })
    }

    savePagination(v) {
        this.pagination = v
        if (this.sessionKey != null && this.sessionKey.length) {
            putItemInStorage(this.sessionKey + "_pagination", this.pagination)
        }        
    }

    //Select an item for editing
    selectRow(item) {
        this.editing = true
        this.object = item
        if (this.selectObject) {
            this.selectObject.call(this, item)
        }
        this.showForm = true
    }

    //Clear the selected item
    clear() {
        this.editing = false
        this.object = {}
        this.showForm = false
        this.errors = {}
    }

    //Submit (create or update) the selected item
    submit(vueApp) {
        var bodyObject = this.createBody
            ? this.createBody(vueApp, this.object)
            : this.object
        if (!this.editing)
            this.create(vueApp, bodyObject)
        else
            this.update(vueApp, bodyObject)
    }

    create(vueApp, bodyObject) {
        viperFetch(vueApp,
            this.urlBase,
            {
                method: "POST",
                body: JSON.stringify(bodyObject),
                headers: { "Content-Type": "application/json" }
            },
            [
                (r => this.load(this))
            ],
            this.errors
        )
    }

    update(vueApp, bodyObject) {
        viperFetch(vueApp,
            this.getUpdateURL(),
            {
                method: "PUT",
                body: JSON.stringify(bodyObject),
                headers: { "Content-Type": "application/json" }
            },
            [
                (r => this.load(this))
            ],
            this.errors
        )
    }

    //Delete the selected item
    delete(vueApp) {
        viperFetch(vueApp,
            this.getUpdateURL(),
            { method: "DELETE" },
            [
                (r => this.load(this))
            ],
            this.errors)
    }

    //Get the URL to create or update the item
    //See comment under quasarTableDefaultConfig.urlBase
    getUpdateURL() {
        var url = this.urlBase
        if (Array.isArray(this.keys)) {
            for (var k of this.keys) {
                if (typeof k === 'object' && k !== null && !Array.isArray(k)) {
                    if (k?.urlPrefix) {
                        url += "/" + k.urlPrefix
                    }
                    url += "/" + this.object[k.column]
                }
                else {
                    url += "/" + this.object[k]
                }
            }
        }
        else {
            url += "/" + this.object[this.keys]
        }
        return url
    }

    //Function called for server side paging when pagination options or filter options are changed
    request(props, vueApp) {
        this.pagination.page = props.pagination.page
        this.pagination.rowsPerPage = props.pagination.rowsPerPage 
        if (props.pagination.sortBy) {
            this.pagination.sortOrder = props.pagination.sortBy
            this.pagination.descending = props.pagination.descending
        }
        else {
            this.pagination.sortOrder = ""
        }
        
        this.load(vueApp)
    }

    //Export function
    exportTable() {
        // naive encoding to csv format
        const columnsMinusExcludes = this.columns
            .filter(c => this.excludeFromExport.findIndex(e => e == c.name) == -1)
        const content = [columnsMinusExcludes.map(col => this.wrapCsvValue(col.label))]
                .concat(
                    this.rows.map(row => columnsMinusExcludes
                        .map(col => this.wrapCsvValue(
                            typeof col.field === 'function'
                                ? col.field(row)
                                : row[col.field === void 0 ? col.name : col.field],
                            col.format,
                            row))
                        .join(',')
                    ))
                .join('\r\n')

        const { useQuasar, exportFile } = Quasar;
        const $q = useQuasar();

        const status = exportFile(
            'table-export.csv',
            content,
            'text/csv'
        )

        if (status !== true) {
            $q.notify({
                message: 'Browser denied file download...',
                color: 'negative',
                icon: 'warning'
            })
        }
    }

    //Escape double quotes (" -> "") and new lines in the content
    wrapCsvValue(val, formatFn, row) {
        let formatted = formatFn !== void 0
            ? formatFn(val, row)
            : val

        formatted = formatted === void 0 || formatted === null
            ? ''
            : String(formatted)

        formatted = formatted.split('"').join('""')
            /**
            * Excel accepts \n and \r in strings, but some other CSV parsers do not
            * Uncomment the next two lines to escape new lines
            */
            .split('\n').join('\\n')
            .split('\r').join('\\r')

        return `"${formatted}"`
    }
}