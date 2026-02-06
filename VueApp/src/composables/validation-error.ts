export type ValidationErrors = string[] | Record<string, string[]>

export class ValidationError extends Error {
    errors: ValidationErrors = []
    status?: number

    constructor(message: string, errors?: ValidationErrors, status?: number) {
        super(message)
        this.errors = errors ?? []
        this.status = status
    }
}
