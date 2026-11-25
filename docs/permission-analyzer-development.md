# Permission Analyzer development

This guide is for contributors building the tool from source. End users should follow the
[Permission Analyzer README](../src/PermissionAnalyzer/PermissionAnalyzer.Tool/README.md);
installed tools do not require Node.js.

## Build and test

Run the [repository validation commands](../AGENTS.md#build-and-validation). For focused analyzer tests:

```bash
dotnet test src/PermissionAnalyzer/PermissionAnalyzer.Tests/PermissionAnalyzer.Tests.csproj
```

## Frontend development

Building the permission tool from source requires **Node.js 24 LTS and npm**, in addition to the .NET SDK.
The tool's MSBuild targets restore the lockfile-pinned frontend dependencies, run TypeScript checking
and the Vite production build, and embed the resulting assets. Generated `dist/` and `node_modules/`
are ignored; neither needs to be committed or distributed separately. Normal incremental builds reuse
unchanged frontend output. CI also runs the React component/API tests.

```bash
cd src/PermissionAnalyzer/PermissionAnalyzer.Tool/Web/Client
npm ci --include=dev
npm test
npm run build
```

For hot-reload development, first start the CLI with `--explore` on its default port **8765**, then run
`npm run dev` from the frontend directory and open `http://127.0.0.1:5173/`. Vite's loopback-only dev
server proxies `/api` to the CLI, retaining the session token and rewriting Host/Origin for the local
backend. Production `--explore` serves only the embedded Vite build; it never starts Vite or an npm process.

## Web client organization

The React client lives in `src/PermissionAnalyzer/PermissionAnalyzer.Tool/Web/Client/src/`:

```text
src/
  app/                       Application shell and integration tests
  features/
    query-builder/           Query form, column/role pickers, and query summaries
    results/                 Result tables, finding filters, and sorting
    role-catalog/            Role grant browser
  shared/
    api/                     Wire types and the session-aware API client
    components/SuggestionPicker/  Reusable editable combobox
    ui/                      Shared CSS-module primitives and class-name helper
  styles/global.css          Brand tokens, resets, and element defaults only
  testing/                   Test setup
  main.tsx                   Client entry point
```

Colocate component styles and tests with their component. Use `*.module.css` for local styles;
shared surfaces, buttons, hints, accessibility utilities, and table defaults belong in
`shared/ui/ui.module.css`. Keep domain-specific states in the owning feature. Use native CSS nesting
for descendants, hover/selection states, and responsive overrides where it makes ownership clearer.
Avoid selectors that reach into another component's module: pass a class or use an owned structural
selector instead. Shared table defaults use `:where()` so local table states can override them.

Vite still emits one `styles.css` resource for the packaged CLI; CSS modules do not add extra served
assets. Tests should prefer roles and accessible names over generated CSS-module class names.

## Architecture and contracts

- [Analyzer design](design/permission-analyzer.md)
- [SDK integration and boundaries](permission-analysis-sdk.md)
- [Command and output reference](permission-analyzer-reference.md)

The browser UI is a local client of the CLI, not a separate deployed service. Preserve the
loopback-only binding, same-origin checks, session token, read-only model, and frozen analysis
inputs when changing it. Snapshots and reports are separate contracts; do not use a report as
a replayable graph.
