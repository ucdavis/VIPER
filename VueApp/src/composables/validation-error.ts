export class ValidationError extends Error {
    constructor(message: string, errors: Record<string, string[]>) {
        super(message)
        this.errors = errors
    }

    errors: Record<string, string[]> = {}
}
