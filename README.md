# VIPER

Clinical, curriculum, and student management application for UC Davis School of Veterinary Medicine.

## Prerequisites

- [.NET 7 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)
- [Node.js v18+](https://nodejs.org/) (v20 recommended)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

## Quick Start

### 1. Setup
```sh
npm install  # Installs Husky hooks and frontend dependencies
```

### 2. Database Setup
Place `awscredentials.xml` in the `web` folder. The app will configure AWS credentials automatically on first run and delete the file.

### 3. Start Application

This command will install the VueJS packages and start frontend and .NET backend services.

```sh
npm run dev
```

Access at https://localhost:7157

## Project Structure

- `web/` - ASP.NET Core backend (C#)
- `VueApp/` - Vue 3 + TypeScript frontend
- `test/` - Unit tests

## Common Commands

### Code Quality

- `npm run lint` - Check code style for both frontend and backend
- `npm run lint:frontend` - Check all frontend code style and types
- `npm run lint:backend` - Check all backend code style
- `npm run lint:staged` - Run pre-commit checks manually
- `npm run fix:frontend` - Auto-fix all frontend issues
- `npm run fix:backend` - Auto-fix all backend issues
- `npm run fix:staged` - Auto-fix only staged files

## Troubleshooting

**Certificate errors**: 
- Vue dev server: Delete certs in `%APPDATA%/ASP.NET/https`, run `npm run dev` again
- .NET HTTPS: Run `dotnet dev-certs https --clean` then `dotnet dev-certs https --trust`

**Database errors**: Ensure `awscredentials.xml` is in `web` folder when doing initial application startup.

### Pre-commit Hook Setup

The project uses [Husky](https://typicode.github.io/husky/) for Git hooks that automatically check code style before commits.

The pre-commit hook is automatically installed when you run:
```sh
npm install
```

The hook will:
- Check C# code style with `dotnet format` on staged .cs files
- Run ESLint and TypeScript checks on staged Vue/TypeScript files (uses Node.js script for Windows compatibility)
- Only run on files you've actually changed
- Block commits if issues are found (bypass with `git commit --no-verify` if needed)

## License

See [LICENSE](LICENSE) for details.
