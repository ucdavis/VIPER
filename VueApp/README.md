# vueapp

This template should help get you started developing with Vue 3 in Vite.

## Recommended IDE Setup

[VSCode](https://code.visualstudio.com/) + [Volar](https://marketplace.visualstudio.com/items?itemName=Vue.volar) (and disable Vetur).

## Type Support for `.vue` Imports in TS

TypeScript cannot handle type information for `.vue` imports by default, so we replace the `tsc` CLI with `vue-tsc` for type checking. In editors, we need [Volar](https://marketplace.visualstudio.com/items?itemName=Vue.volar) to make the TypeScript language service aware of `.vue` types.

## Customize configuration

See [Vite Configuration Reference](https://vitejs.dev/config/).

## Project Setup

```sh
npm install
```

### Compile and Hot-Reload for Development

```sh
npm run dev
```

### Type-Check, Compile and Minify for Production

```sh
npm run build
```

### Lint with [ESLint](https://eslint.org/)

```sh
npm run lint
```

### Unit Tests with [Vitest](https://vitest.dev/)

```sh
npm run test:run    # single run
npm run test        # watch mode
```

From the repo root, `npm run test:frontend` runs the same suite and
`npm run test:frontend -- <file-pattern>` narrows it to matching files.

Mock call history is cleared before every test, because Vitest enables
`clearMocks` by default. A `beforeEach(() => vi.clearAllMocks())` is therefore
redundant: clear a mock explicitly only mid-test, when an assertion needs to
ignore calls made earlier in the same test.
