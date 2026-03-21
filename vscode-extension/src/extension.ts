import * as vscode from "vscode";
import { TrendTreeProvider } from "./trendTreeProvider";
import { registerCommands } from "./commands";

export function activate(context: vscode.ExtensionContext): void {
    const folders = vscode.workspace.workspaceFolders ?? [];

    const treeProvider = new TrendTreeProvider(folders);

    const treeView = vscode.window.createTreeView("trendsetterTests", {
        treeDataProvider: treeProvider,
        showCollapseAll: true,
    });
    context.subscriptions.push(treeView);

    registerCommands(context, treeProvider);

    // Watch for new run results anywhere in the workspace and auto-refresh
    for (const folder of folders) {
        const watcher = vscode.workspace.createFileSystemWatcher(new vscode.RelativePattern(folder, "**/run_*.json"));
        watcher.onDidCreate(() => treeProvider.refresh());
        watcher.onDidChange(() => treeProvider.refresh());
        watcher.onDidDelete(() => treeProvider.refresh());
        context.subscriptions.push(watcher);
    }
}

export function deactivate(): void {
    // nothing to clean up
}
