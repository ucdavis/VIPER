export class AuthError extends Error {
    constructor(message: string, status: number) {
        super(message)
        this.status = status
    }
    status = 0
}
