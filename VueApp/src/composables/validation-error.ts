export type ValidationErrors = string[] | Record<string, string[]>

export class ValidationError extends Error {
    constructor(message: string, errors?: ValidationErrors) {
        super(message)
        this.errors = errors ?? []
    }

    errors: ValidationErrors = []
}
