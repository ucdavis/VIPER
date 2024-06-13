import { ref, type Ref } from 'vue'
import { useGenericErrorHandler } from './ErrorHandler'
const errorHandler = useGenericErrorHandler();

export function useFetch(url: string, options: any = {}) {
    const errors : Ref<string[]> = ref([])
    const result : any = ref(null)

    class ValidationError extends Error {
        constructor(message: string, errors: []) {
            super(message)
            this.errors = errors
        }

        errors = []
    }

    async function vfetch() {
        if (options.headers === undefined) {
            options.headers = {}
        }
        if (options.headers["X-CSRF-TOKEN"] === undefined) {
            options.headers["X-CSRF-TOKEN"] = ''//csrfToken
        }
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
            try {
                const result = await response.json()
                const message = result.errorMessage != null ? result.errorMessage
                    : result.detail != null ? result.detail
                        : result.statusText
                throw new ValidationError(message, result?.errors)
            }
            catch (e) {
                throw Error("An error occurred")
            }            
        }
        return response
    }

    return { result, errors, vfetch }
}
