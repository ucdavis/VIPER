import { useGenericErrorHandler } from './ErrorHandler'
const errorHandler = useGenericErrorHandler();

export function useFetch() {
    type Pagination = {
        page: number,
        perPage: number,
        pages: number,
        totalRecords: number,
        next: string | null,
        previous: string | null
    }

    type Result = {
        result: any,
        errors: any,
        success: boolean,
        pagination: Pagination | null
    }

    type UrlParams = {
        [key: string]: string | number | null | undefined
    }

    class ValidationError extends Error {
        constructor(message: string, errors: []) {
            super(message)
            this.errors = errors
        }

        errors = []
    }

    class AuthError extends Error {
        constructor(message: string, status: number) {
            super(message)
            this.status = status
        }
        status = 0
    }

    async function get(url: string = "", options: any = {}): Promise<Result> {
        options.method = "GET"
        addHeader(options, "Content-Type", "application / json")
        return await fetchWrapper(url, options)
    }

    async function post(url: string = "", body: any = {}, options: any = {}): Promise<Result> {
        options.method = "POST"
        options.body = JSON.stringify(body)
        addHeader(options, "Content-Type", "application / json")
        return await fetchWrapper(url, options)
    }

    async function put(url: string = "", body: any = {}, options: any = {}): Promise<Result> {
        options.method = "PUT"
        options.body = JSON.stringify(body)
        addHeader(options, "Content-Type", "application / json")
        return await fetchWrapper(url, options)
    }

    async function del(url: string = "", options: any = {}): Promise<Result> {
        options.method = "DELETE"
        addHeader(options, "Content-Type", "application / json")
        return await fetchWrapper(url, options)
    }

    function createUrlSearchParams(obj: UrlParams): URLSearchParams {
        const params = new URLSearchParams
        for (const key in obj) {
            const value = obj[key]
            if (value != null) {
                params.append(key, value.toString())
            }
        }
        return params
    }

    function addHeader(options: any, headerName: string, headerValue: string) {
        if (options.headers === undefined) {
            options.headers = {}
        }
        if (options.headers[headerName] === undefined) {
            options.headers[headerName] = headerValue
        }
    }

    async function fetchWrapper(url: string, options: any = {}) {
        if (options.headers === undefined) {
            options.headers = {}
        }
        addHeader(options, "X-CSRF-TOKEN", "")

        let errors: string[] = []
        const result = await fetch(url, options)
            //handle 4xx and 5xx status codes
            .then(r => handleViperFetchError(r))
            //return json (unless we got 204 No Content or 202 Accepted)
            .then(r => (r.status == 204 || r.status == 202) ? r : r.json())
            //check for success flag and result being defined.
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
                if (e?.status !== undefined) {
                    errorHandler.handleAuthError(e.status)
                }
                else {
                    errorHandler.handleError(e)
                    errors = errorHandler.errors.value
                }
            })
        const resultObj: Result = {
            result: result,
            errors: errors,
            success: errors.length == 0,
            pagination: null
        }
        if (result && result.pagination) {
            resultObj.pagination = result.pagination
            resultObj.result = result.result
        }
        return resultObj
    }

    async function handleViperFetchError(response: any) {
        //handle 4XX and 5XX errors
        if (!response.ok) {
            let result = null
            let message = ""
            let isAuthError = false
            try {
                if (response.status == 401 || response.status == 403) {
                    isAuthError = true
                }
                else {
                    result = await response.json()
                    message = result.errorMessage != null
                        ? result.errorMessage
                        : result.detail != null
                            ? result.detail
                            : result.statusText
                }
            }
            catch (e) {
                throw Error("An error occurred")
            }
            if (!isAuthError) {
                throw new ValidationError(message, result?.errors)
            }
            else {
                throw new AuthError("Auth Error", response.status)
            }
        }

        return response
    }

    return { get, post, put, del, createUrlSearchParams }
}
