import { ref, type Ref, computed } from 'vue'
import { useGenericErrorHandler } from './ErrorHandler'
const errorHandler = useGenericErrorHandler();

export function useFetch() {
    const errors : Ref<string[]> = ref([])
    const result: any = ref(null)
    const success = computed(() => errors.value.length == 0)
    
    class ValidationError extends Error {
        constructor(message: string, errors: []) {
            super(message)
            this.errors = errors
        }

        errors = []
    }

    function addHeader(options: any, headerName: string, headerValue: string) {
        if (options.headers === undefined) {
            options.headers = {}
        }
        if (options.headers[headerName] === undefined) {
            options.headers[headerName] = headerValue
        }
    }

    async function get(url: string = "", options: any = {}) {
        options.method = "GET"
        addHeader(options, "Content-Type", "application / json")
        await vfetch(url, options)
    }

    async function post(url: string = "", body: any = {}, options: any = {}) {
        options.method = "POST"
        options.body = JSON.stringify(body)
        addHeader(options, "Content-Type", "application / json")
        await vfetch(url, options)
    }

    async function put(url: string = "", body: any = {}, options: any = {}) {
        options.method = "PUT"
        options.body = JSON.stringify(body)
        addHeader(options, "Content-Type", "application / json")
        await vfetch(url, options)
    }

    async function remove(url: string = "", options: any = {}) {
        options.method = "DELETE"
        addHeader(options, "Content-Type", "application / json")
        await vfetch(url, options)
    }

    async function vfetch(url: string, options: any = {}) {
        if (options.headers === undefined) {
            options.headers = {}
        }
        addHeader(options, "X-CSRF-TOKEN", "")

        result.value = await fetch(url, options)
            //handle 4xx and 5xx status codes
            .then(r => handleViperFetchError(r))
            //return json (unless we got 204 No Content or 202 Accepted)
            .then(r => (r.status == 204 || r.status == 202) ? r : r.json())
            //check for success flag and result being defined. call additional functions
            .then(r => {
                let intialResult = r
                if (r.success !== undefined) {
                    if (!r.success || typeof (r.result) == "undefined") {
                        errorHandler.handleError(r)
                    }
                    intialResult = r.result
                }
                return r.pagination ? { result: intialResult, pagination: r.pagination } : intialResult
            })
            //catch errors, including those thrown by handleViperFetchError
            .catch(e => {
                errorHandler.handleError(e)
                errors.value = errorHandler.errors.value
            })
    }

    async function handleViperFetchError(response: any) {
        if (!response.ok) {
            let result = null
            let message = ""
            try {
                result = await response.json()
                message = result.errorMessage != null
                    ? result.errorMessage
                    : result.detail != null
                        ? result.detail
                        : result.statusText
            }
            catch (e) {
                throw Error("An error occurred")
            }         
            throw new ValidationError(message, result?.errors)
        }
        return response
    }

    return { result, errors, success, vfetch, get, post, put, remove }
}
