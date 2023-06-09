/*
 * Shared code for quasar tables
 */


/*
 * quasarTableEditable - a quasar table with an edit dialog and add/update/delete functions
 */
quasarTableEditableRowsDefault = {
    //base of the url and keys of the objects, e.g. a urlBase of "Permissions" and a key of "id"" means the following ajax calls will be made
    //GET Permissions - load objects
    //POST Permissions - create a new permission
    //PUT Permissions/5 - update permission with ID 5
    //DELETE Permissions/5 - delete permission with ID 5
    //multiple keys can be specified in an array of objects. For example, keys=["id", {column: "memberId", urlPrefix="member"}]
    //would create the PUT and DELETE url Permissions/5/member/12345678
    urlBase: "",
    keys: "id",
    //function to execute after data has been loaded, for example to add columns
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
    
}
class quasarTableEditable {
    constructor(config) {
        this.config = { ...quasarTableEditableRowsDefault, ...config }
        this.data = []
        this.loading = true
        this.editing = false
        this.showForm = false
        this.editing = false
        this.object = {}
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

    selectRow(item) {
        this.editing = true
        this.object = item
        if (this.config.selectObject) {
            this.config.selectObject.call(this, item)
        }
        this.showForm = true
    }

    clear() {
        this.editing = false
        this.object = {}
        this.showForm = false
        this.errors = {}
    }

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

    delete(vueApp) {
        viperFetch(vueApp,
            this.getUpdateURL(),
            { method: "DELETE" },
            [
                (r => this.load(this))
            ],
            this.errors)
    }

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
}