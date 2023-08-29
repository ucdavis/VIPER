/*
 * Javascript to use the Vue variables defined in _VIPERLayout.html, but in a way that "looks" more like
 * typical Vue documentation and examples.
 * 
 * To use, call the createVueApp function in the "Scripts" section, for example:
 * <script asp-add-nonce="true">
 *      createVueApp({
 *          data() {
 *              return {
 *                  myvar1: myvalue1,
 *                  myvar2: myvalue2
 *              }
 *          },
 *          methods: {
 *              myAction: function() {
 *              }
 *          }
 *          mounted() {
 *              myAction()
 *          }
 *      })
 *  </script>
 */
let vueApps = []
function createVueApp(vueAppConfig) {
    vueApps.push(new vueApp(vueAppConfig))
}

class vueApp {
    constructor(config) {
        Object.assign(this, config)
    }

    //add each of the options to the Viper Vue variables
    create() {
        //options that are objects, or return objects
        if (this.data !== undefined) {
            Object.assign(vueObjects, this.data())
        }
        if (this.methods !== undefined) {
            Object.assign(vueMethods, this.methods)
        }
        if (this.watch !== undefined) {
            Object.assign(vueWatchers, this.watch)
        }
        if (this.emits !== undefined) {
            Object.assign(vueEmits, this.emits)
        }
        if (this.props !== undefined) {
            Object.assign(vueProps, this.props)
        }
        if (this.computed !== undefined) {
            Object.assign(computed, this.computed)
        }

        //vue lifecycle methods
        if (this.setup !== undefined) {
            vueSetupActions.push(this.setup)
        }
        if (this.beforeCreate !== undefined) {
            vueBeforeCreateActions.push(this.beforeCreate)
        }
        if (this.created !== undefined) {
            vueCreatedActions.push(this.created)
        }
        if (this.beforeMount !== undefined) {
            vueMountedActions.push(this.beforeMount)
        }
        if (this.mounted !== undefined) {
            vueBeforeMountActions.push(this.mounted)
        }
        if (this.beforeUpdate !== undefined) {
            vueBeforeUpdateActions.push(this.beforeUpdate)
        }
        if (this.updated !== undefined) {
            vueUpdatedActions.push(this.updated)
        }
        if (this.beforeUnmount !== undefined) {
            vueBeforeUnmountActions.push(this.beforeUnmount)
        }
        if (this.unmounted !== undefined) {
            vueUnmountedActions.push(this.unmounted)
        }
    }
}