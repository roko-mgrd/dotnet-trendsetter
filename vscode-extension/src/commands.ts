import * as vscode from "vscode";
import * as path from "path";
import * as fs from "fs";
import { TrendTreeProvider, TrendNode, RunNode } from "./trendTreeProvider";

export function registerCommands(context: vscode.ExtensionContext, treeProvider: TrendTreeProvider): void {
    /** Map from testId (or "__all__") to the running task execution. */
    const runningTasks = new Map<string, vscode.TaskExecution>();
    const ALL_KEY = "__all__";

    /** Find the first .csproj that references Trendsetter.Engine in the workspace. */
    async function findProjectPath(): Promise<string | undefined> {
        const files = await vscode.workspace.findFiles("**/*.csproj", "**/node_modules/**", 20);
        for (const file of files) {
            const content = (await vscode.workspace.fs.readFile(file)).toString();
            if (content.includes("Trendsetter.Engine")) {
                return path.dirname(file.fsPath);
            }
        }
        return undefined;
    }

    /** Run a dotnet command as a VS Code task and return the execution handle. */
    async function runDotnet(label: string, projectPath: string, args: string): Promise<vscode.TaskExecution> {
        const cmd = `dotnet run --project "${projectPath}"${args ? ` ${args}` : ""}`;
        const task = new vscode.Task(
            { type: "trendsetter" },
            vscode.TaskScope.Workspace,
            label,
            "Trendsetter",
            new vscode.ShellExecution(cmd),
        );
        task.presentationOptions = {
            reveal: vscode.TaskRevealKind.Always,
            panel: vscode.TaskPanelKind.Dedicated,
        };
        return vscode.tasks.executeTask(task);
    }

    /** Clean up running state when a tracked task process ends. */
    context.subscriptions.push(
        vscode.tasks.onDidEndTaskProcess((e) => {
            for (const [key, execution] of runningTasks) {
                if (execution === e.execution) {
                    runningTasks.delete(key);
                    if (key === ALL_KEY) {
                        treeProvider.clearAllRunning();
                    } else {
                        treeProvider.setRunning(key, false);
                    }
                    break;
                }
            }
            vscode.commands.executeCommand("setContext", "trendsetter.anyRunning", treeProvider.isAnyRunning());
        }),
    );

    // ── Refresh ────────────────────────────────────────────────────
    context.subscriptions.push(
        vscode.commands.registerCommand("trendsetter.refresh", () => {
            treeProvider.refresh();
        }),
    );

    // ── Run All Tests ──────────────────────────────────────────────
    context.subscriptions.push(
        vscode.commands.registerCommand("trendsetter.runAllTests", async () => {
            const projectPath = await findProjectPath();
            if (!projectPath) {
                vscode.window.showWarningMessage("No project referencing Trendsetter.Engine found in the workspace.");
                return;
            }
            const execution = await runDotnet("Run All Tests", projectPath, "");

            runningTasks.set(ALL_KEY, execution);
            for (const trend of treeProvider.getTrends()) {
                treeProvider.setRunning(trend.testId, true);
            }
            vscode.commands.executeCommand("setContext", "trendsetter.anyRunning", true);
        }),
    );

    // ── Run Single Test ───────────────────────────────────────────
    context.subscriptions.push(
        vscode.commands.registerCommand("trendsetter.runTest", async (node?: TrendNode) => {
            const testId = node?.trend.testId ?? (await pickTestId(treeProvider));
            if (!testId) {
                return;
            }
            const projectPath = await findProjectPath();
            if (!projectPath) {
                vscode.window.showWarningMessage("No project referencing Trendsetter.Engine found in the workspace.");
                return;
            }
            const execution = await runDotnet(`Run ${testId}`, projectPath, `-- --test ${testId}`);

            runningTasks.set(testId, execution);
            treeProvider.setRunning(testId, true);
            vscode.commands.executeCommand("setContext", "trendsetter.anyRunning", true);
        }),
    );

    // ── Stop Single Test ──────────────────────────────────────────
    context.subscriptions.push(
        vscode.commands.registerCommand("trendsetter.stopTest", (node?: TrendNode) => {
            const testId = node?.trend.testId;
            if (!testId) {
                return;
            }
            const execution = runningTasks.get(testId);
            if (execution) {
                execution.terminate();
            }
        }),
    );

    // ── Stop All Tests ────────────────────────────────────────────
    context.subscriptions.push(
        vscode.commands.registerCommand("trendsetter.stopAllTests", () => {
            for (const [, execution] of runningTasks) {
                execution.terminate();
            }
        }),
    );

    // ── Generate Dashboard ─────────────────────────────────────────
    context.subscriptions.push(
        vscode.commands.registerCommand("trendsetter.generateDashboard", async () => {
            const projectPath = await findProjectPath();
            if (!projectPath) {
                vscode.window.showWarningMessage("No project referencing Trendsetter.Engine found in the workspace.");
                return;
            }
            const terminal = vscode.window.createTerminal("Trendsetter Dashboard");
            terminal.show();
            terminal.sendText(`dotnet run --project "${projectPath}" -- dashboard`);
        }),
    );

    // ── Open Dashboard ─────────────────────────────────────────────
    context.subscriptions.push(
        vscode.commands.registerCommand("trendsetter.openDashboard", async () => {
            const dashboards = await vscode.workspace.findFiles("**/dashboard.html", "**/node_modules/**", 5);
            if (dashboards.length === 0) {
                const choice = await vscode.window.showWarningMessage(
                    "Dashboard not found. Generate it first?",
                    "Generate",
                    "Cancel",
                );
                if (choice === "Generate") {
                    await vscode.commands.executeCommand("trendsetter.generateDashboard");
                }
                return;
            }

            vscode.env.openExternal(dashboards[0]);
        }),
    );

    // ── Generate Report (for a specific trend) ────────────────────
    context.subscriptions.push(
        vscode.commands.registerCommand("trendsetter.generateReport", async (node?: TrendNode) => {
            const testId = node?.trend.testId ?? (await pickTestId(treeProvider));
            if (!testId) {
                return;
            }

            const projectPath = await findProjectPath();
            if (!projectPath) {
                vscode.window.showWarningMessage("No project referencing Trendsetter.Engine found in the workspace.");
                return;
            }
            const terminal = vscode.window.createTerminal("Trendsetter Report");
            terminal.show();
            terminal.sendText(`dotnet run --project "${projectPath}" -- ${testId} --html`);
        }),
    );

    // ── Open Report in browser (inline button) ─────────────────────
    context.subscriptions.push(
        vscode.commands.registerCommand("trendsetter.openReport", async (node?: TrendNode) => {
            const trend = node?.trend;
            if (!trend) {
                const testId = await pickTestId(treeProvider);
                if (!testId) {
                    return;
                }
                const found = treeProvider.getTrends().find((t) => t.testId === testId);
                if (!found) {
                    return;
                }
                return openReportInBrowser(found);
            }
            return openReportInBrowser(trend);
        }),
    );

    // ── Show Run Details (webview panel) ───────────────────────────
    context.subscriptions.push(
        vscode.commands.registerCommand("trendsetter.showRunDetails", (node: RunNode) => {
            showRunWebview(context, node);
        }),
    );
}

async function pickTestId(provider: TrendTreeProvider): Promise<string | undefined> {
    const trends = provider.getTrends();
    if (trends.length === 0) {
        vscode.window.showInformationMessage("No trend tests found.");
        return undefined;
    }
    return vscode.window.showQuickPick(
        trends.map((t) => t.testId),
        { placeHolder: "Select a trend test" },
    );
}

function openReportInBrowser(trend: { directory: string; testId: string; hasReport: boolean }): void {
    const reportPath = path.join(trend.directory, "report.html");
    if (!fs.existsSync(reportPath)) {
        vscode.window.showWarningMessage(
            `No report found for ${trend.testId}. Generate it with "Trendsetter: Generate Report".`,
        );
        return;
    }
    vscode.env.openExternal(vscode.Uri.file(reportPath));
}

function showRunWebview(context: vscode.ExtensionContext, node: RunNode): void {
    const run = node.run;
    const panel = vscode.window.createWebviewPanel(
        "trendsetterRun",
        `Run #${run.run_number} — ${run.test_id}`,
        vscode.ViewColumn.One,
        { enableScripts: false },
    );

    const scoreModes = ["Exact", "Partial", "Semantic", "Structural", "Skip"];

    const itemsHtml = run.items
        .map((item, i) => {
            const fieldsHtml = item.field_scores
                .map((f) => {
                    const scoreColor = f.score >= 0.95 ? "#4ec9b0" : f.score >= 0.7 ? "#dcdcaa" : "#f44747";
                    const mode = scoreModes[f.mode] ?? `Mode(${f.mode})`;
                    return `
                        <tr>
                            <td>${escapeHtml(f.field_name)}</td>
                            <td style="color:${scoreColor};font-weight:bold">${(f.score * 100).toFixed(1)}%</td>
                            <td>${mode}</td>
                            <td><code>${escapeHtml(f.expected ?? "—")}</code></td>
                            <td><code>${escapeHtml(f.actual ?? "—")}</code></td>
                        </tr>`;
                })
                .join("");

            const itemColor = item.score >= 0.95 ? "#4ec9b0" : item.score >= 0.7 ? "#dcdcaa" : "#f44747";
            return `
                <h3>Item [${i}] — <span style="color:${itemColor}">${(item.score * 100).toFixed(1)}%</span></h3>
                <table>
                    <thead><tr><th>Field</th><th>Score</th><th>Mode</th><th>Expected</th><th>Actual</th></tr></thead>
                    <tbody>${fieldsHtml}</tbody>
                </table>`;
        })
        .join("<hr/>");

    const ts = new Date(run.timestamp);
    const overallColor = run.score >= 0.95 ? "#4ec9b0" : run.score >= 0.7 ? "#dcdcaa" : "#f44747";

    panel.webview.html = `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <style>
        body { font-family: var(--vscode-font-family, 'Segoe UI', sans-serif); color: #ccc; background: #1e1e1e; padding: 16px; }
        h2 { color: #569cd6; }
        h3 { margin-top: 24px; }
        table { border-collapse: collapse; width: 100%; margin-top: 8px; }
        th, td { padding: 6px 12px; text-align: left; border-bottom: 1px solid #333; }
        th { color: #9cdcfe; border-bottom: 2px solid #444; }
        code { background: #2d2d2d; padding: 2px 6px; border-radius: 3px; font-size: 0.9em; }
        hr { border: none; border-top: 1px solid #333; margin: 24px 0; }
        .meta { color: #888; margin-bottom: 16px; }
    </style>
</head>
<body>
    <h2>${escapeHtml(run.test_id)} — Run #${run.run_number}</h2>
    <div class="meta">
        <strong>Overall:</strong> <span style="color:${overallColor};font-weight:bold">${(run.score * 100).toFixed(1)}%</span>
        &nbsp;·&nbsp; <strong>Items:</strong> ${run.items.length}
        &nbsp;·&nbsp; <strong>Time:</strong> ${ts.toLocaleDateString()} ${ts.toLocaleTimeString()}
    </div>
    ${itemsHtml}
</body>
</html>`;
}

function escapeHtml(text: string): string {
    return text.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
}
