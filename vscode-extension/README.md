# Trendsetter VS Code Extension

View, run, and analyze Trendsetter AI trend tests directly from VS Code.

## Installation

### From the repo (recommended)

```bash
cd vscode-extension
npm install
npm run install-ext
```

This compiles the TypeScript, packages a `.vsix` file, and installs it into your VS Code. Restart VS Code and the Trendsetter panel appears in the Activity Bar.

### Manual `.vsix` install

```bash
cd vscode-extension
npm install
npm run package
```

This creates `trendsetter-0.1.0.vsix`. Install it with:

```bash
code --install-extension trendsetter-0.1.0.vsix
```

Or in VS Code: `Ctrl+Shift+P` → **Extensions: Install from VSIX...** → select the file.

### Development mode

```bash
cd vscode-extension
npm install
npm run compile
code --extensionDevelopmentPath="$(pwd)" "/path/to/your/trendsetter/project"
```

Or open the `vscode-extension` folder in VS Code and press **F5** to launch the Extension Development Host.

## Features

### Sidebar Tree View

A **Trendsetter** panel appears in the Activity Bar (beaker icon). It automatically scans your workspace for `run_*.json` files and displays:

- **Trend nodes** — test ID, latest score %, number of runs
- **Run nodes** — run number, score, timestamp
- **Item nodes** — per-item score
- **Field nodes** — field name, score, scoring mode, expected vs actual (in tooltip)

Scores are color-coded: green (≥95%), yellow (≥70%), red (<70%).

### Click Actions

| Click target  | Action                                          |
| ------------- | ----------------------------------------------- |
| **Test name** | Opens the latest run details in a webview panel |
| **Run node**  | Opens that run's details in a webview panel     |

### Inline Buttons (per test)

| Button | Action                                     |
| ------ | ------------------------------------------ |
| ▶      | Run that single test via `--test <testId>` |
| 👁     | Open `report.html` in the browser          |

Right-click a test for additional options like **Generate Report**.

### Toolbar Buttons (panel header)

| Button    | Action                                   |
| --------- | ---------------------------------------- |
| Run All   | Execute all trend tests via `dotnet run` |
| Dashboard | Open `dashboard.html` in the browser     |
| Refresh   | Reload test data from disk               |

### Commands (Command Palette)

All commands are available via `Ctrl+Shift+P`:

| Command                             | Description                                                  |
| ----------------------------------- | ------------------------------------------------------------ |
| **Trendsetter: Run All Tests**      | Run all trend tests (`dotnet run --project <auto-detected>`) |
| **Trendsetter: Run Test**           | Run a single test (`dotnet run -- --test <testId>`)          |
| **Trendsetter: Open Dashboard**     | Open `dashboard.html` in the browser                         |
| **Trendsetter: Generate Dashboard** | Generate the aggregated HTML dashboard via CLI               |
| **Trendsetter: Open Report**        | Open a trend's `report.html` in the browser                  |
| **Trendsetter: Generate Report**    | Generate HTML report for a specific trend via CLI            |
| **Trendsetter: Show Run Details**   | Show a run's field scores in a webview panel                 |
| **Trendsetter: Refresh**            | Reload all test data from disk                               |

### Auto-Discovery

The extension requires **no configuration**. It automatically:

- Scans all workspace folders recursively for `run_*.json` files
- Reads `test_id` from the JSON to identify trends
- Detects `report.html` files next to run data
- Finds the `.csproj` that references `Trendsetter.Engine` for running tests
- Skips `node_modules`, `.git`, `bin`, `obj`, `.vs`, `out`, `dist` directories

### Auto-Refresh

A file watcher monitors the workspace for changes to `run_*.json` files. The tree view refreshes automatically when tests finish running.

### Run Details Webview

Clicking a test name or run node opens a dark-themed webview panel showing:

- Overall score with color coding
- Item count and timestamp
- Per-item table with field name, score, scoring mode, expected value, and actual value
- Mismatched fields highlighted for quick identification

## How It Works

```
Workspace
  └── **/run_*.json          ← extension discovers these automatically
       ├── test_id            → groups into trend nodes
       ├── run_number         → ordered run history
       └── items/field_scores → drill-down details

  └── **/*.csproj            ← auto-detected for "Run" commands
       └── references Trendsetter.Engine → used as dotnet run target
```

### CLI Integration

The extension calls `dotnet run` on the auto-detected project:

- **Run All**: `dotnet run --project <path>`
- **Run Single**: `dotnet run --project <path> -- --test <testId>`
- **Dashboard**: `dotnet run --project <path> -- dashboard`
- **Report**: `dotnet run --project <path> -- <testId> --html`

The `--test` flag is handled by `RunCommand` in `Trendsetter.Engine`.

## Requirements

- VS Code 1.85+
- .NET 9.0+ SDK (for running tests)
- A project using `Trendsetter.Engine` with `run_*.json` output files
