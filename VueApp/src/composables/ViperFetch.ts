import { useGenericErrorHandler } from "./ErrorHandler"
import { useUserStore } from "@/store/UserStore"
import { ValidationError } from "./validation-error"
import { AuthError } from "./auth-error"

const errorHandler = useGenericErrorHandler()

const HTTP_STATUS = {
    UNAUTHORIZED: 401,
    FORBIDDEN: 403,
    NO_CONTENT: 204,
    ACCEPTED: 202,
} as const

type Pagination = {
    page: number
    perPage: number
    pages: number
    totalRecords: number
    next: string | null
    previous: string | null
}

type Result = {
    result: any
    errors: any
    success: boolean
    pagination: Pagination | null
}

type UrlParams = {
    [key: string]: string | number | null | undefined
}

function createUrlSearchParams(obj: UrlParams): URLSearchParams {
    const params = new URLSearchParams()
    for (const [key, value] of Object.entries(obj)) {
        if (value != null && typeof key === "string") {
            params.append(key, value.toString())
        }
    }
    return params
}

function addHeader(options: any, headerName: string, headerValue: string) {
    if (options.headers === undefined) {
        options.headers = {}
    }
    if (typeof headerName === "string" && !Object.hasOwn(options.headers, headerName)) {
        Object.defineProperty(options.headers, headerName, {
            value: headerValue,
            enumerable: true,
            writable: true,
            configurable: true,
        })
    }
}

async function handleViperFetchError(response: any) {
    if (!response.ok) {
        let result = null
        let message = ""
        let isAuthError = false
        try {
            try {
                result = await response.json()
            } catch {
                result = response
            }
            if (response.status === HTTP_STATUS.UNAUTHORIZED || response.status === HTTP_STATUS.FORBIDDEN) {
                isAuthError = true
            } else if (result.errorMessage !== null && result.errorMessage !== undefined) {
                message = result.errorMessage
            } else if (result.detail !== null && result.detail !== undefined) {
                message = result.detail
            } else if (result.statusText !== null && result.statusText !== undefined) {
                message = result.statusText
            }
        } catch {
            throw new Error("An error occurred")
        }
        if (isAuthError) {
            throw new AuthError(result.errorMessage ?? "Auth Error", response.status)
        } else {
            throw new ValidationError(message, result?.errors)
        }
    }
    return response
}

async function fetchWrapper(url: string, options: any = {}) {
    if (options.headers === undefined) {
        options.headers = {}
    }

    const userStore = useUserStore()
    const token = userStore.userInfo.token || ""
    addHeader(options, "X-CSRF-TOKEN", token)

    let errors: string[] = []
    const result = await fetch(url, options)
        .then((r) => handleViperFetchError(r))
        .then((r) => (r.status === HTTP_STATUS.NO_CONTENT || r.status === HTTP_STATUS.ACCEPTED ? r : r.json()))
        .then((r) => {
            let intialResult = r
            if (r.success !== undefined) {
                if (!r.success || r.result === undefined) {
                    errorHandler.handleError(r)
                }
                intialResult = r.result
            }
            return r.pagination ? { result: intialResult, pagination: r.pagination } : intialResult
        })
        .catch((error) => {
            if (error?.status === undefined) {
                errorHandler.handleError(error)
                errors = errorHandler.errors.value
            } else {
                errorHandler.handleAuthError(error?.message, error.status)
                errors = [error?.message || "Authentication error"]
            }
        })
    const resultObj: Result = {
        result: result,
        errors: errors,
        success: errors.length === 0,
        pagination: null,
    }
    if (result && result.pagination) {
        resultObj.pagination = result.pagination
        resultObj.result = result.result
    }
    return resultObj
}

async function postForBlob(url: string = "", body: any = {}, options: any = {}): Promise<Blob> {
    options.method = "POST"
    options.body = JSON.stringify(body)
    addHeader(options, "Content-Type", "application/json")

    if (options.headers === undefined) {
        options.headers = {}
    }

    const userStore = useUserStore()
    const token = userStore.userInfo.token || ""
    addHeader(options, "X-CSRF-TOKEN", token)

    const response = await fetch(url, options)

    if (!response.ok) {
        let message = ""
        let isAuthError = false

        if (response.status === HTTP_STATUS.UNAUTHORIZED || response.status === HTTP_STATUS.FORBIDDEN) {
            isAuthError = true
            message = "Authentication required"
        } else {
            try {
                const result = await response.json()
                message = result.errorMessage || result.detail || response.statusText
            } catch {
                message = response.statusText || "An error occurred"
            }
        }

        if (isAuthError) {
            errorHandler.handleAuthError(message, response.status)
        } else {
            errorHandler.handleError(message)
        }

        throw new Error(message)
    }

    return await response.blob()
}

function useFetch() {
    async function get(url: string = "", options: any = {}): Promise<Result> {
        options.method = "GET"
        addHeader(options, "Content-Type", "application/json")
        return await fetchWrapper(url, options)
    }

    async function post(url: string = "", body: any = {}, options: any = {}): Promise<Result> {
        options.method = "POST"
        options.body = JSON.stringify(body)
        addHeader(options, "Content-Type", "application/json")
        return await fetchWrapper(url, options)
    }

    async function put(url: string = "", body: any = {}, options: any = {}): Promise<Result> {
        options.method = "PUT"
        options.body = JSON.stringify(body)
        addHeader(options, "Content-Type", "application/json")
        return await fetchWrapper(url, options)
    }

    async function del(url: string = "", options: any = {}): Promise<Result> {
        options.method = "DELETE"
        addHeader(options, "Content-Type", "application/json")
        return await fetchWrapper(url, options)
    }

    return { get, post, put, del, createUrlSearchParams }
}

export { useFetch, postForBlob }
