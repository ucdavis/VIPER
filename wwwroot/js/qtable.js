/*
 * Shared code for quasar tables
 */


quasarTableEditableRowsDefault = {
    urlBase: "",
    key: "id",
    onLoadFunctions: [],
    pagination: { rowsPerPage: 15 }

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
    }

    load() {
        viperFetch(this, "Permissions", {})
            .then(r => {
                this.data = r
                for (var i = 0; i < this.config.onLoadFunctions.length; i++) {
                    this.config.onLoadFunctions[i].call(this, this.config.vueApp, this.data)
                }
            })
        this.loading = false
        this.clear()
    }

    selectRow(item) {
        this.editing = true
        this.object = item
        this.showForm = true
    }

    clear() {
        this.editing = false
        this.object = {}
        this.showForm = false
        this.errors = {}
    }

    submit(vueApp) {
        if (!this.editing)
            this.create(vueApp)
        else
            this.update(vueApp)
    }

    create(vueApp) {
        viperFetch(vueApp,
            this.config.urlBase,
            {
                method: "POST",
                body: JSON.stringify(this.object),
                headers: { "Content-Type": "application/json" }
            },
            [
                (r => this.load(this))
            ],
            this.errors
        )
    }

    update(vueApp) {
        viperFetch(vueApp,
            this.config.urlBase + "/" + this.object[this.config.key],
            {
                method: "PUT",
                body: JSON.stringify(this.object),
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
            "Permissions/" + this.object[this.config.key],
            { method: "DELETE" },
            [
                (r => this.load(this))
            ],
            this.errors)
    }
}