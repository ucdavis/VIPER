/*
 * Shared code for quasar tables
 */


/*
 * quasarTable - a quasar table with an edit dialog and add/update/delete functions, with optional server side paging/filtering and export to csv
 */
quasarTableDefaultConfig = {
    //base of the url and keys of the objects, e.g. a urlBase of "Permissions" and a key of "id"" means the following ajax calls will be made
    //GET Permissions - load objects
    //POST Permissions - create a new permission
    //PUT Permissions/5 - update permission with ID 5
    //DELETE Permissions/5 - delete permission with ID 5
    //multiple keys can be specified in an array of objects. For example, keys=["id", {column: "memberId", urlPrefix="member"}]
    //would create the PUT and DELETE url Permissions/5/member/12345678
    urlBase: "",
    keys: "id",
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
    excludeFromExport: []
}
class quasarTable {
    constructor(config) {
        //override default config properties with input
        this.config = { ...quasarTableDefaultConfig, ...config }
        //the data being shown
        this.data = []
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
    }

    //Load data. For server side pagination, send the pagination options as query params.
    load(vueApp) {
        var queryParams = "";
        if (this.config.serverSidePagination) {
            this.loading = true
            var queryParamObject = {
                perPage: this.config.pagination.rowsPerPage,
                page: this.config.pagination.page,
                sortOrder: (this.config.pagination.sortBy ? this.config.pagination.sortBy : "") + (this.config.pagination.descending ? " desc" : ""),
                filter: (this.filter ? this.filter : "")
            }
            queryParams = "?" + new URLSearchParams(queryParamObject)
        }
        viperFetch(vueApp, this.config.urlBase + queryParams, {}, [])
            .then(r => {
                if (r) {
                    if (this.config.serverSidePagination) {
                        //record data and pagination
                        this.data = r.result
                        this.config.pagination.rowsNumber = r.pagination.totalRecords
                        this.config.pagination.rowsPerPage = r.pagination.perPage
                        this.config.pagination.page = r.pagination.page
                    }
                    else {
                        this.data = r
                    }
                }
                
                if (this.config.onLoad) {
                    this.config.onLoad.call(this, this.data, vueApp)
                }
            })
            .then(r => {
                this.loading = false
                this.clear()
            })
    }

    //Select an item for editing
    selectRow(item) {
        this.editing = true
        this.object = item
        if (this.config.selectObject) {
            this.config.selectObject.call(this, item)
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
        var bodyObject = this.config.createBody
            ? this.config.createBody(vueApp, this.object)
            : this.object
        if (!this.editing)
            this.create(vueApp, bodyObject)
        else
            this.update(vueApp, bodyObject)
    }

    create(vueApp, bodyObject) {
        viperFetch(vueApp,
            this.config.urlBase,
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
        var url = this.config.urlBase
        if (Array.isArray(this.config.keys)) {
            for (var k of this.config.keys) {
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
            url += "/" + this.object[this.config.keys]
        }
        return url
    }

    //Function called for server side paging when pagination options or filter options are changed
    request(props, vueApp) {
        this.config.pagination.page = props.pagination.page
        this.config.pagination.rowsPerPage = props.pagination.rowsPerPage 
        if (props.pagination.sortBy) {
            this.config.pagination.sortOrder = props.pagination.sortBy
            this.config.pagination.descending = props.pagination.descending
        }
        else {
            this.config.pagination.sortOrder = ""
        }
        
        this.load(vueApp)
    }

    //Export function
    exportTable() {
        // naive encoding to csv format
        const columnsMinusExcludes = this.config.columns
            .filter(c => this.config.excludeFromExport.findIndex(e => e == c.name) == -1)
        const content = [columnsMinusExcludes.map(col => this.wrapCsvValue(col.label))]
                .concat(
                    this.data.map(row => columnsMinusExcludes
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