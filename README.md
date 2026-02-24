# VIPER

[![codecov](https://codecov.io/github/ucdavis/VIPER/graph/badge.svg?token=4Q8KQ3HHUF)](https://codecov.io/github/ucdavis/VIPER)

Clinical, curriculum, and student management application for UC Davis School of Veterinary Medicine.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Volta](https://volta.sh/) - Node.js version manager
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

### Node.js Setup with Volta (Recommended)

This project uses Node.js v20.6.1. We recommend using [Volta](https://volta.sh/) to manage Node.js versions:

**Install Volta on Windows:**
```sh
winget install Volta.Volta

# After installation, restart your terminal and install Node.js 20.6.1
volta install node@20.6.1
```

## Quick Start

### 1. Setup
```sh
npm install  # Installs Husky hooks and frontend dependencies
```

### 2. Database Setup
Place `awscredentials.xml` in the `web` folder. The app will configure AWS credentials automatically on first run and delete the file.

### 3. Start Application

#### Option 1: Full Development (Recommended)
Start both frontend and backend with hot reload enabled, plus Chrome in debug mode:

```sh
npm run dev
```

- **Frontend**: Vue.js with Vite dev server (https://localhost:5173) + Hot Module Replacement (HMR)
- **Backend**: ASP.NET Core with dotnet watch (https://localhost:7157) + hot reload
- **Browser**: Chrome launches automatically in debug mode on port 9222
- **Proxy**: Backend automatically proxies Vue asset requests to Vite for seamless development
- **Access**: https://localhost:7157 (all apps work through the backend)
- **Debugging**: Chrome can be attached to from VS Code for frontend debugging
- **Best for**: Active development with instant updates and debugging capabilities

#### Option 2: Static File Development
For testing production-like behavior or when Vite dev server isn't needed:

```sh
# Start backend only
npm run dev:backend

# In separate terminal, build and watch frontend files
npm run dev:frontend-build
```

- **Frontend**: Pre-built Vue.js files served as static assets
- **Backend**: ASP.NET Core with dotnet watch + hot reload
- **Files**: Serves built static files from `wwwroot/vue/` (like production)
- **Best for**: Testing production builds, debugging static file serving, or CI/CD validation

#### Individual Services
- `npm run dev:frontend` - Start only Vite dev server
- `npm run dev:frontend-build` - Build and watch Vue files (outputs to wwwroot)
- `npm run dev:backend` - Start only .NET backend (https://localhost:7157)

### Running Multiple Instances

To run multiple instances of the application simultaneously on the same computer:

1. **Copy the environment template:**
   ```bash
   # Unix/Linux/macOS or Git Bash on Windows
   cp .env.local.example .env.local

   # Windows PowerShell/Command Prompt
   Copy-Item .env.local.example .env.local
   ```

2. **Configure ports for the second instance:**
   Edit `.env.local` and uncomment one of the instance blocks:
   ```env
   # Instance 2
   VITE_PORT=5174
   VITE_HMR_PORT=24679
   ASPNETCORE_HTTPS_PORT=7158
   ASPNETCORE_URLS=https://localhost:7158;http://localhost:5001
   VITE_SERVER_URL=https://localhost:5174
   ```

3. **Start the second instance:**
   Use the commands you would normally use to start the frontend and backend and use one of the urls defined in ASPNETCORE_URLS to access the new instance.

## Development Architecture

### Hot Reload Setup
VIPER2 features a sophisticated development setup with hot reload for both frontend and backend:

**Frontend Hot Reload (Vite HMR)**:
- Changes to `.vue`, `.ts`, `.js` files trigger instant updates without page refresh
- Component state is preserved during updates when possible
- CSS changes are applied instantly

**Backend Hot Reload (.NET Watch)**:
- C# code changes trigger automatic recompilation and server restart
- Most changes apply instantly without restart (hot reload)
- "Rude edits" (method signature changes, new classes) trigger automatic restart
- Configuration optimized to ignore build artifacts and frontend files

**Intelligent Proxying**:
- Backend automatically detects Vue asset requests (`.js`, `.ts`, `.css`, etc.) and proxies them to Vite dev server
- Fallback to static files when Vite server is unavailable
- All requests use `/2/vue/` base path for consistency between development and production
- Vite server URL configurable via `VITE_SERVER_URL` environment variable (default: https://localhost:5173)

### File Watching & Building
- **Frontend**: `vite-watch.js` debounces build operations to prevent excessive rebuilds
- **Backend**: `dotnet-watch.json` configures optimal file watching patterns
- **Concurrent**: Both systems can run simultaneously without conflicts

### Path Configuration
- All Vue applications use `/2/vue/` as the base path
- Development and production use identical URL structure
- Vite configuration: `base: '/2/vue/'`
- Backend configuration: Routes rewrite to `/2/vue/src/{app}/index.html`

## Project Structure

- `web/` - ASP.NET Core backend (C#)
  - `Areas/` - Feature-based organization (CTS, Students, etc.)
  - `Program.cs` - Application configuration with Vue.js integration
  - `dotnet-watch.json` - Hot reload configuration
- `VueApp/` - Vue 3 + TypeScript frontend
  - `src/` - Multi-SPA architecture (one SPA per functional area)
  - `vite.config.ts` - Vite configuration with backend proxy
  - `vite-watch.js` - Debounced build script for development
- `test/` - Unit tests

## IDE Setup

### Prettier Formatting (Automatic code formatting)

**VS Code:**
1. Install "Prettier - Code formatter" extension
2. Configuration is automatic (uses `.prettierrc.json` and `.editorconfig`)

**Visual Studio:**
1. Install "Prettier - Code formatter" extension
2. Configuration is automatic (uses `.prettierrc.json` and `.editorconfig`)

Files auto-format on save with consistent 4-space indentation and project style.

### Vue Inspector (Click-to-Open Components)

Vue Inspector is enabled in development mode and allows you to click Vue components in the browser to open them directly in your IDE.

**How to Use:**
1. Start development mode: `npm run dev`
2. Press `Ctrl+Shift` to toggle the inspector overlay
3. Click any Vue component in the browser to open it in your editor

**Editor Configuration:**
- **VS Code (Default)**: Works automatically
- **Visual Studio**: Set `VITE_EDITOR=visual-studio` in your `.env.local` file
- **Other Editors**: Set `VITE_EDITOR` to your editor command (e.g., `webstorm`, `sublime`)

The inspector only works in development mode and is automatically disabled in production builds.

## Debugging

### VS Code Debugging Support

The project includes VS Code launch configurations for debugging both frontend and backend code:

**Frontend Debugging (Chrome):**
1. Start the application with Chrome debugging: `npm run dev`
2. In VS Code, go to Run and Debug (Ctrl+Shift+D)
3. Select "Attach to Chrome" and press F5
4. Chrome will launch automatically in debug mode, and VS Code will attach to it
5. Set breakpoints in your TypeScript/Vue files and debug in VS Code

**Backend Debugging (.NET):**
1. Use "`Attach to .NET Core`" configuration to attach to a running VIPER process
2. Use "`.NET Tests (Debug)`" to debug unit tests
3. The debugger will automatically find and attach to the running VIPER backend
4. **Note**: "Attach to .NET Core" may fail on the first attempt due to timing - simply try again and it will work

## Common Commands

### Code Quality

- `npm run lint` - Check code style for specified files or directories
- `npm run lint:staged` - Lint only git-staged files
- `npm run precommit` - Run full pre-commit checks manually (lint, test, build verify)

### Build Cache

The linter and build verification scripts use caching to avoid redundant rebuilds. If you encounter stale cache issues (e.g., linter showing warnings for already-fixed code), clear the cache:

- `npm run lint -- --clear-cache <path>` - Clear cache and lint specific files
- `npm run lint:staged -- --clear-cache` - Clear cache and lint staged files
- `npm run verify:build -- --clear-cache` - Clear cache and verify build

## Troubleshooting

### Hot Reload Issues

**Frontend not updating**:
- Ensure Vite dev server is running (you should see "vite connected" in browser console)
- Check that you're accessing https://localhost:7157 (not the Vite server directly)
- Try refreshing the browser or restarting `npm run dev`
- Verify Vite server URL matches configuration (default: https://localhost:5173)

**Backend not restarting**:
- Check that `dotnet watch` is running (console shows "dotnet watch" messages)
- Verify no compilation errors in the backend
- Try stopping and restarting `npm run dev:backend`

**Assets not loading (404 errors)**:
- **Option 1 (Vite dev server)**: Check that Vite dev server is running on https://localhost:5173
- **Option 2 (Static files)**: Ensure `npm run dev:frontend-build` has been run to build files
- **Path issues**: All Vue apps use `/2/vue/` base path - verify this matches your configuration
- **Mixed mode issues**: Stop all services and restart with your chosen development mode

### Certificate Issues

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

## Email Testing with Mailpit

Mailpit captures emails sent by the application during development without sending real emails.

### How to Run

Mailpit starts automatically:
```bash
npm run dev:backend        # Starts backend + Mailpit
npm run dev      # Starts full stack + Mailpit
```

Manual control:
```bash
npm run mailpit:start      # Start Mailpit
npm run mailpit:stop       # Stop Mailpit  
npm run mailpit:status     # Check status and send test email
```

### How to View Emails

- **Web Interface**: http://localhost:8025 (primary method)
- **If Mailpit is unavailable**: Email sending will be skipped in Development (check console logs for warnings). Use `npm run mailpit:status` to verify Mailpit is running and send a test email.
