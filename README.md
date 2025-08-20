# VIPER

Clinical, curriculum, and student management application for UC Davis School of Veterinary Medicine.

## Prerequisites

- [.NET 7 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)
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
Start both frontend and backend with hot reload enabled:

```sh
npm run dev
```

- **Frontend**: Vue.js with Vite dev server (https://localhost:5173) + Hot Module Replacement (HMR)
- **Backend**: ASP.NET Core with dotnet watch (https://localhost:7157) + hot reload
- **Proxy**: Backend automatically proxies Vue asset requests to Vite for seamless development
- **Access**: https://localhost:7157 (all apps work through the backend)
- **Best for**: Active development with instant updates

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

### Development Modes Explained

**Option 1: Full Development (`npm run dev`)**
- Vite dev server handles all Vue asset requests with HMR
- TypeScript files compiled on-demand by Vite
- Instant updates for Vue components, CSS, and TypeScript
- Backend proxies asset requests to Vite automatically
- Best development experience with hot module replacement

**Option 2: Static File Development**
- Vue files pre-built into static assets in `wwwroot/vue/`
- Backend serves files like production (no proxy to Vite)
- Useful for testing production builds or CI/CD validation
- Requires manual rebuild (`npm run dev:frontend-build`) when Vue files change
- More similar to production environment

**Path Configuration**
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
