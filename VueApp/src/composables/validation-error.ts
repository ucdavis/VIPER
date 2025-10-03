export class ValidationError extends Error {
    constructor(message: string, errors: []) {
        super(message)
        this.errors = errors
    }

    errors = []
}
