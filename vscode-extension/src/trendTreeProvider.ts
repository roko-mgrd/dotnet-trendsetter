import * as vscode from "vscode";
import * as fs from "fs";
import * as path from "path";
import { RunResultJson, TrendInfo, ScoringModeLabel } from "./types";

type TreeNode = TrendNode | RunNode | ItemNode | FieldNode;

export class TrendTreeProvider implements vscode.TreeDataProvider<TreeNode> {
    private _onDidChangeTreeData = new vscode.EventEmitter<TreeNode | undefined>();
    readonly onDidChangeTreeData = this._onDidChangeTreeData.event;

    private trends: TrendInfo[] = [];
    private readonly runningTests = new Set<string>();

    constructor(private readonly workspaceFolders: readonly vscode.WorkspaceFolder[]) {
        this.loadTrends();
    }

    refresh(): void {
        this.loadTrends();
        this._onDidChangeTreeData.fire(undefined);
    }

    /** Mark a test as running and refresh the tree. */
    setRunning(testId: string, running: boolean): void {
        if (running) {
            this.runningTests.add(testId);
        } else {
            this.runningTests.delete(testId);
        }
        this._onDidChangeTreeData.fire(undefined);
    }

    /** Mark all tests as stopped and refresh. */
    clearAllRunning(): void {
        this.runningTests.clear();
        this._onDidChangeTreeData.fire(undefined);
    }

    isRunning(testId: string): boolean {
        return this.runningTests.has(testId);
    }

    isAnyRunning(): boolean {
        return this.runningTests.size > 0;
    }

    getTrends(): TrendInfo[] {
        return this.trends;
    }

    getTreeItem(element: TreeNode): vscode.TreeItem {
        return element;
    }

    getChildren(element?: TreeNode): TreeNode[] {
        if (!element) {
            return this.getRootNodes();
        }
        if (element instanceof TrendNode) {
            return this.getRunNodes(element.trend);
        }
        if (element instanceof RunNode) {
            return this.getItemNodes(element.run);
        }
        if (element instanceof ItemNode) {
            return this.getFieldNodes(element.item);
        }
        return [];
    }

    /**
     * Scan all workspace folders for directories containing run_*.json files.
     * Discovers reports automatically regardless of folder structure.
     */
    private loadTrends(): void {
        this.trends = [];

        for (const folder of this.workspaceFolders) {
            const root = folder.uri.fsPath;
            this.scanDirectory(root, root);
        }

        this.trends.sort((a, b) => a.testId.localeCompare(b.testId));
    }

    private static readonly SKIP_DIRS = new Set([
        "node_modules",
        ".git",
        "bin",
        "obj",
        ".vs",
        ".vscode",
        "out",
        "dist",
    ]);

    private scanDirectory(dir: string, baseDir: string): void {
        let entries;
        try {
            entries = fs.readdirSync(dir, { withFileTypes: true });
        } catch {
            return;
        }

        const subdirs = entries.filter((e) => e.isDirectory() && !TrendTreeProvider.SKIP_DIRS.has(e.name));
        const jsonFiles = entries.filter((e) => e.isFile() && e.name.startsWith("run_") && e.name.endsWith(".json"));

        if (jsonFiles.length > 0) {
            const runs: RunResultJson[] = [];

            for (const file of jsonFiles) {
                try {
                    const content = fs.readFileSync(path.join(dir, file.name), "utf-8");
                    const run: RunResultJson = JSON.parse(content);
                    if (run.test_id && typeof run.run_number === "number") {
                        runs.push(run);
                    }
                } catch {
                    // skip malformed files
                }
            }

            if (runs.length > 0) {
                runs.sort((a, b) => b.run_number - a.run_number);
                const testId = runs[0].test_id;
                const hasReport = fs.existsSync(path.join(dir, "report.html"));
                this.trends.push({ testId, directory: dir, runs, hasReport });
            }
        }

        for (const entry of subdirs) {
            this.scanDirectory(path.join(dir, entry.name), baseDir);
        }
    }

    private getRootNodes(): TreeNode[] {
        if (this.trends.length === 0) {
            return [new InfoNode("No trend tests found. Run tests first.")];
        }

        return this.trends.map((t) => new TrendNode(t, this.runningTests.has(t.testId)));
    }

    private getRunNodes(trend: TrendInfo): RunNode[] {
        return trend.runs.map((r) => new RunNode(r, trend.directory));
    }

    private getItemNodes(run: RunResultJson): ItemNode[] {
        return run.items.map((item, index) => new ItemNode(item, index));
    }

    private getFieldNodes(item: {
        field_scores: {
            field_name: string;
            score: number;
            mode: number;
            expected: string | null;
            actual: string | null;
        }[];
    }): FieldNode[] {
        return item.field_scores.map((f) => new FieldNode(f));
    }
}

function scoreIcon(score: number): vscode.ThemeIcon {
    if (score >= 0.95) {
        return new vscode.ThemeIcon("pass", new vscode.ThemeColor("testing.iconPassed"));
    }
    if (score >= 0.7) {
        return new vscode.ThemeIcon("warning", new vscode.ThemeColor("testing.iconQueued"));
    }
    return new vscode.ThemeIcon("error", new vscode.ThemeColor("testing.iconFailed"));
}

function pct(score: number): string {
    return `${(score * 100).toFixed(1)}%`;
}

export class TrendNode extends vscode.TreeItem {
    constructor(
        public readonly trend: TrendInfo,
        public readonly running: boolean = false,
    ) {
        super(trend.testId, vscode.TreeItemCollapsibleState.Collapsed);

        if (running) {
            this.description = "running…";
            this.iconPath = new vscode.ThemeIcon("loading~spin");
            this.contextValue = "trendRunning";
            this.tooltip = new vscode.MarkdownString(`**${trend.testId}** — _running…_`);
        } else {
            const latestScore = trend.runs.length > 0 ? trend.runs[0].score : 0;
            this.description = `${pct(latestScore)} · ${trend.runs.length} run(s)`;
            this.iconPath = scoreIcon(latestScore);
            this.contextValue = "trend";
            this.tooltip = new vscode.MarkdownString(
                `**${trend.testId}**\n\n` +
                    `Latest score: ${pct(latestScore)}\n\n` +
                    `Runs: ${trend.runs.length}\n\n` +
                    (trend.hasReport ? "📄 Report available" : "_No report generated_"),
            );
        }

        if (trend.runs.length > 0) {
            const latestRunNode = new RunNode(trend.runs[0], trend.directory);
            this.command = {
                command: "trendsetter.showRunDetails",
                title: "Show Latest Run",
                arguments: [latestRunNode],
            };
        }
    }
}

export class RunNode extends vscode.TreeItem {
    constructor(
        public readonly run: RunResultJson,
        public readonly directory: string,
    ) {
        super(`Run #${run.run_number}`, vscode.TreeItemCollapsibleState.Collapsed);

        const ts = new Date(run.timestamp);
        this.description = `${pct(run.score)} · ${ts.toLocaleDateString()} ${ts.toLocaleTimeString()}`;
        this.iconPath = scoreIcon(run.score);
        this.contextValue = "run";
        this.command = {
            command: "trendsetter.showRunDetails",
            title: "Show Run Details",
            arguments: [this],
        };
        this.tooltip = new vscode.MarkdownString(
            `**Run #${run.run_number}**\n\n` +
                `Score: ${pct(run.score)}\n\n` +
                `Items: ${run.items.length}\n\n` +
                `Time: ${ts.toISOString()}`,
        );
    }
}

class ItemNode extends vscode.TreeItem {
    constructor(
        public readonly item: RunResultJson["items"][0],
        index: number,
    ) {
        super(`Item [${index}]`, vscode.TreeItemCollapsibleState.Collapsed);
        this.description = pct(item.score);
        this.iconPath = scoreIcon(item.score);
        this.contextValue = "item";
    }
}

class FieldNode extends vscode.TreeItem {
    constructor(field: RunResultJson["items"][0]["field_scores"][0]) {
        super(field.field_name, vscode.TreeItemCollapsibleState.None);

        const modeLabel = ScoringModeLabel[field.mode] ?? `Mode(${field.mode})`;
        this.description = `${pct(field.score)} (${modeLabel})`;
        this.iconPath = scoreIcon(field.score);
        this.contextValue = "field";

        const lines = [`**${field.field_name}** — ${pct(field.score)} (${modeLabel})`];
        if (field.expected !== null) {
            lines.push(`\n\n**Expected:** \`${field.expected}\``);
        }
        if (field.actual !== null) {
            lines.push(`\n\n**Actual:** \`${field.actual}\``);
        }
        this.tooltip = new vscode.MarkdownString(lines.join(""));
    }
}

class InfoNode extends vscode.TreeItem {
    constructor(message: string) {
        super(message, vscode.TreeItemCollapsibleState.None);
        this.iconPath = new vscode.ThemeIcon("info");
    }
}
