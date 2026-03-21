namespace Trendsetter.Engine.Reports;

using System.Text;

/// <summary>
/// Generates a single aggregated dashboard.html in the reports base directory.
/// Summarises all trends with scores, run counts, stability indicators, and a tree view.
/// </summary>
public sealed class HtmlDashboardWriter
{
    private readonly string _baseDirectory;

    public HtmlDashboardWriter(string baseDirectory = "reports")
        => _baseDirectory = baseDirectory;

    public async Task<string> WriteAsync(IReadOnlyList<TrendReport> reports)
    {
        var html = Render(reports);
        var path = Path.Combine(_baseDirectory, "dashboard.html");
        Directory.CreateDirectory(_baseDirectory);
        await File.WriteAllTextAsync(path, html, Encoding.UTF8);
        return path;
    }

    private string Render(IReadOnlyList<TrendReport> reports)
    {
        var totalRuns = reports.Sum(r => r.RunCount);
        var avgScore = reports.Count == 0 ? 0 : reports.Average(r => r.LatestScore);
        var unstableCount = reports.Sum(r => r.UnstableFields.Count);
        var regressionCount = reports.Count(r => r.HasRegression);
        var overallScoreJson = ToJson(reports.Select(r => r.LatestScore));
        var overallLabels = ToJson(reports.Select(r => ShortenId(r.TestId)));
        var treeHtml = BuildTree(reports);
        var cardRows = RenderCards(reports);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'");

        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8"/>
<meta name="viewport" content="width=device-width, initial-scale=1.0"/>
<title>Trendsetter — Dashboard</title>
<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js"></script>
<style>
  @import url('https://fonts.googleapis.com/css2?family=IBM+Plex+Mono:wght@400;600&family=IBM+Plex+Sans:wght@300;400;600&display=swap');

  :root {
    --bg:         #0d0f14;
    --surface:    #161a23;
    --surface2:   #1e2330;
    --border:     #2a3040;
    --text:       #c9d1e0;
    --text-dim:   #5a6480;
    --accent:     #4f9cf9;
    --accent-dim: #1a3a6e;
    --green:      #3dd68c;
    --green-dim:  #0f3d25;
    --red:        #f95f5f;
    --red-dim:    #3d1010;
    --yellow:     #f9c34f;
    --yellow-dim: #3d2e0f;
    --mono:       'IBM Plex Mono', monospace;
    --sans:       'IBM Plex Sans', sans-serif;
    --radius:     6px;
  }

  *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

  body {
    background: var(--bg);
    color: var(--text);
    font-family: var(--sans);
    font-size: 14px;
    line-height: 1.6;
    min-height: 100vh;
    display: flex;
    flex-direction: column;
  }

  /* ---------- Top bar ---------- */
  .topbar {
    background: var(--surface);
    border-bottom: 1px solid var(--border);
    padding: 0 32px;
    height: 52px;
    display: flex;
    align-items: center;
    justify-content: space-between;
    position: sticky;
    top: 0;
    z-index: 100;
  }
  .topbar-left { display: flex; align-items: center; gap: 12px; }
  .topbar-logo {
    font-family: var(--mono);
    font-size: 13px;
    font-weight: 600;
    color: var(--accent);
    letter-spacing: 0.08em;
    text-transform: uppercase;
  }
  .topbar-badge {
    font-family: var(--mono);
    font-size: 10px;
    background: var(--accent-dim);
    color: var(--accent);
    padding: 2px 8px;
    border-radius: 3px;
  }
  .topbar-time {
    font-family: var(--mono);
    font-size: 11px;
    color: var(--text-dim);
  }

  /* ---------- Layout ---------- */
  .body-wrap {
    display: flex;
    flex: 1;
  }

  /* ---------- Sidebar ---------- */
  .sidebar {
    width: 240px;
    flex-shrink: 0;
    background: var(--surface);
    border-right: 1px solid var(--border);
    padding: 24px 16px;
    overflow-y: auto;
    position: sticky;
    top: 52px;
    height: calc(100vh - 52px);
  }
  .sidebar-title {
    font-family: var(--mono);
    font-size: 10px;
    text-transform: uppercase;
    letter-spacing: 0.1em;
    color: var(--text-dim);
    margin-bottom: 12px;
  }
  .tree { font-size: 12px; }
  .tree-node { margin: 2px 0; }
  .tree-folder {
    display: flex;
    align-items: center;
    gap: 6px;
    color: var(--text-dim);
    font-family: var(--mono);
    padding: 3px 4px;
    cursor: pointer;
    border-radius: 3px;
    user-select: none;
  }
  .tree-folder:hover { background: var(--surface2); color: var(--text); }
  .tree-folder-icon { font-size: 10px; transition: transform 0.15s; }
  .tree-folder.collapsed .tree-folder-icon { transform: rotate(-90deg); }
  .tree-children { padding-left: 16px; }
  .tree-children.hidden { display: none; }
  .tree-leaf {
    display: flex;
    align-items: center;
    gap: 6px;
    font-family: var(--mono);
    font-size: 11px;
    padding: 3px 4px;
    border-radius: 3px;
    cursor: pointer;
    color: var(--text);
    text-decoration: none;
  }
  .tree-leaf:hover { background: var(--accent-dim); color: var(--accent); }
  .tree-dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    flex-shrink: 0;
  }

  /* ---------- Main content ---------- */
  .main { flex: 1; padding: 32px 36px 80px; min-width: 0; }

  .page-header { margin-bottom: 32px; }
  .page-title {
    font-family: var(--mono);
    font-size: 20px;
    font-weight: 600;
    color: #fff;
    margin-bottom: 4px;
  }
  .page-sub { font-size: 13px; color: var(--text-dim); }

  /* ---------- Summary stats ---------- */
  .stats-row {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 12px;
    margin-bottom: 32px;
  }
  .stat-card {
    background: var(--surface);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 18px 20px;
  }
  .stat-label {
    font-size: 11px;
    font-family: var(--mono);
    text-transform: uppercase;
    letter-spacing: 0.08em;
    color: var(--text-dim);
    margin-bottom: 6px;
  }
  .stat-value {
    font-size: 28px;
    font-family: var(--mono);
    font-weight: 600;
    color: #fff;
  }
  .stat-value.good  { color: var(--green); }
  .stat-value.warn  { color: var(--yellow); }
  .stat-value.bad   { color: var(--red); }

  /* ---------- Overview chart ---------- */
  .section { margin-bottom: 40px; }
  .section-title {
    font-family: var(--mono);
    font-size: 11px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.1em;
    color: var(--text-dim);
    margin-bottom: 14px;
    padding-bottom: 10px;
    border-bottom: 1px solid var(--border);
  }
  .chart-card {
    background: var(--surface);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 20px 24px;
  }
  .chart-wrap { position: relative; height: 200px; }

  /* ---------- Trend cards grid ---------- */
  .trend-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
    gap: 14px;
  }
  .trend-card {
    background: var(--surface);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 18px 20px;
    text-decoration: none;
    color: inherit;
    display: block;
    transition: border-color 0.15s, background 0.15s;
    position: relative;
    overflow: hidden;
  }
  .trend-card:hover {
    border-color: var(--accent);
    background: var(--surface2);
  }
  .trend-card.regression { border-left: 3px solid var(--red); }
  .trend-card.improvement { border-left: 3px solid var(--green); }

  .trend-card-header {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    margin-bottom: 12px;
  }
  .trend-card-id {
    font-family: var(--mono);
    font-size: 12px;
    font-weight: 600;
    color: #fff;
    word-break: break-all;
  }
  .trend-card-score {
    font-family: var(--mono);
    font-size: 22px;
    font-weight: 600;
    flex-shrink: 0;
    margin-left: 12px;
  }
  .trend-card-meta {
    display: flex;
    align-items: center;
    gap: 10px;
    font-size: 11px;
    color: var(--text-dim);
    font-family: var(--mono);
    margin-bottom: 12px;
  }
  .sparkline-mini { position: relative; height: 36px; }

  .delta-badge {
    font-family: var(--mono);
    font-size: 10px;
    padding: 2px 6px;
    border-radius: 3px;
  }
  .delta-up   { background: var(--green-dim); color: var(--green); }
  .delta-down { background: var(--red-dim);   color: var(--red); }
  .delta-flat { background: var(--surface2);  color: var(--text-dim); }

  .unstable-count {
    font-family: var(--mono);
    font-size: 10px;
    background: var(--yellow-dim);
    color: var(--yellow);
    padding: 2px 6px;
    border-radius: 3px;
  }

  /* ---------- Empty state ---------- */
  .empty {
    text-align: center;
    padding: 80px 20px;
    color: var(--text-dim);
    font-family: var(--mono);
    font-size: 13px;
  }
  .empty-icon { font-size: 40px; margin-bottom: 16px; }
</style>
</head>
<body>

<div class="topbar">
  <div class="topbar-left">
    <span class="topbar-logo">Trendsetter</span>
    <span class="topbar-badge">dashboard</span>
  </div>
  <span class="topbar-time">Generated {{timestamp}}</span>
</div>

<div class="body-wrap">

  <aside class="sidebar">
    <div class="sidebar-title">Trends</div>
    <div class="tree" id="tree">
      {{treeHtml}}
    </div>
  </aside>

  <main class="main">

    <div class="page-header">
      <div class="page-title">Test Dashboard</div>
      <div class="page-sub">{{reports.Count}} trend{{(reports.Count != 1 ? "s" : "")}} tracked across {{totalRuns}} total runs</div>
    </div>

    <div class="stats-row">
      <div class="stat-card">
        <div class="stat-label">Avg Score</div>
        <div class="stat-value {{ScoreClass(avgScore)}}">{{avgScore:P1}}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">Total Runs</div>
        <div class="stat-value">{{totalRuns}}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">Regressions</div>
        <div class="stat-value {{(regressionCount > 0 ? "bad" : "good")}}">{{regressionCount}}</div>
      </div>
      <div class="stat-card">
        <div class="stat-label">Unstable Fields</div>
        <div class="stat-value {{(unstableCount > 0 ? "warn" : "good")}}">{{unstableCount}}</div>
      </div>
    </div>

    {{(reports.Count > 0 ? $$"""
    <div class="section">
      <div class="section-title">Latest Score per Trend</div>
      <div class="chart-card">
        <div class="chart-wrap">
          <canvas id="overviewChart"></canvas>
        </div>
      </div>
    </div>
    """ : "")}}

    <div class="section">
      <div class="section-title">All Trends</div>
      {{(reports.Count > 0 ? $"""<div class="trend-grid">{cardRows}</div>""" : """<div class="empty"><div class="empty-icon">📭</div>No trend results found yet. Run your trend tests to see results here.</div>""")}}
    </div>

  </main>
</div>

<script>
(function() {
  // Overview bar chart
  const overviewCanvas = document.getElementById('overviewChart');
  if (overviewCanvas) {
    const labels = {{overallLabels}};
    const data   = {{overallScoreJson}};
    new Chart(overviewCanvas.getContext('2d'), {
      type: 'bar',
      data: {
        labels,
        datasets: [{
          data: data.map(v => +(v * 100).toFixed(1)),
          backgroundColor: data.map(v =>
            v >= 0.8 ? 'rgba(61,214,140,0.7)' :
            v >= 0.5 ? 'rgba(249,195,79,0.7)' :
                       'rgba(249,95,95,0.7)'
          ),
          borderColor: data.map(v =>
            v >= 0.8 ? '#3dd68c' : v >= 0.5 ? '#f9c34f' : '#f95f5f'
          ),
          borderWidth: 1,
          borderRadius: 3,
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: '#1e2330',
            borderColor: '#2a3040',
            borderWidth: 1,
            titleColor: '#c9d1e0',
            bodyColor: '#c9d1e0',
            callbacks: { label: ctx => ` ${ctx.parsed.y.toFixed(1)}%` }
          }
        },
        scales: {
          x: {
            grid: { display: false },
            ticks: { color: '#5a6480', font: { family: "'IBM Plex Mono'", size: 10 } }
          },
          y: {
            min: 0, max: 100,
            grid: { color: '#1e2330' },
            ticks: {
              color: '#5a6480',
              font: { family: "'IBM Plex Mono'", size: 11 },
              callback: v => v + '%'
            }
          }
        }
      }
    });
  }

  // Sparkline mini charts per card
  document.querySelectorAll('[data-sparkline]').forEach(canvas => {
    const values = JSON.parse(canvas.dataset.sparkline);
    if (!values.length) return;
    new Chart(canvas.getContext('2d'), {
      type: 'line',
      data: {
        labels: values.map((_, i) => i),
        datasets: [{
          data: values.map(v => +(v * 100).toFixed(1)),
          borderColor: '#4f9cf9',
          borderWidth: 1.5,
          pointRadius: 0,
          tension: 0.35,
          fill: false
        }]
      },
      options: {
        responsive: true, maintainAspectRatio: false,
        plugins: { legend: { display: false }, tooltip: { enabled: false } },
        scales: { x: { display: false }, y: { display: false, min: 0, max: 100 } },
        animation: false
      }
    });
  });

  // Tree collapse/expand
  document.querySelectorAll('.tree-folder').forEach(el => {
    el.addEventListener('click', () => {
      const children = el.nextElementSibling;
      if (!children) return;
      el.classList.toggle('collapsed');
      children.classList.toggle('hidden');
    });
  });
})();
</script>
</body>
</html>
""";
    }

    // -------------------------------------------------------------------------

    private string RenderCards(IReadOnlyList<TrendReport> reports)
    {
        var sb = new StringBuilder();

        foreach (var r in reports)
        {
            var scoreColor = ScoreHexColor(r.LatestScore);
            var cardClass = r.HasRegression ? "trend-card regression" : r.HasImprovement ? "trend-card improvement" : "trend-card";
            var sparklineJson = ToJson(r.ScoreHistory);
            var deltaHtml = r.ScoreDelta.HasValue
                ? $"""<span class="delta-badge {(r.ScoreDelta > 0 ? "delta-up" : r.ScoreDelta < 0 ? "delta-down" : "delta-flat")}">{(r.ScoreDelta > 0 ? "▲" : "▼")}{Math.Abs(r.ScoreDelta.Value):P1}</span>"""
                : string.Empty;
            var unstableHtml = r.UnstableFields.Count > 0
                ? $"""<span class="unstable-count">⚡ {r.UnstableFields.Count} unstable</span>"""
                : string.Empty;

            // Relative path from dashboard to report.html
            var reportLink = r.RelativePath.Replace(Path.DirectorySeparatorChar, '/') + "/report.html";

            sb.AppendLine($$"""
              <a href="{{reportLink}}" class="{{cardClass}}">
                <div class="trend-card-header">
                  <div class="trend-card-id">{{r.TestId}}</div>
                  <div class="trend-card-score" style="color:{{scoreColor}}">{{r.LatestScore:P1}}</div>
                </div>
                <div class="trend-card-meta">
                  <span>{{r.RunCount}} run{{(r.RunCount != 1 ? "s" : "")}}</span>
                  {{deltaHtml}}
                  {{unstableHtml}}
                </div>
                <div class="sparkline-mini">
                  <canvas data-sparkline='{{sparklineJson}}'></canvas>
                </div>
              </a>
            """);
        }

        return sb.ToString();
    }

    private string BuildTree(IReadOnlyList<TrendReport> reports)
    {
        // Build a nested dictionary tree from dot-separated test IDs
        var root = new TreeNode("root");

        foreach (var report in reports)
        {
            var parts = report.RelativePath
                .Split([Path.DirectorySeparatorChar, '/'], StringSplitOptions.RemoveEmptyEntries);

            var node = root;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                node = node.GetOrAddChild(parts[i]);
            }

            var leafName = parts.Length > 0 ? parts[^1] : report.TestId;
            node.AddLeaf(leafName, report);
        }

        return RenderTreeNode(root, isRoot: true);
    }

    private string RenderTreeNode(TreeNode node, bool isRoot)
    {
        var sb = new StringBuilder();

        foreach (var folder in node.Children.Values.OrderBy(c => c.Name))
        {
            sb.AppendLine($$"""
              <div class="tree-node">
                <div class="tree-folder">
                  <span class="tree-folder-icon">▾</span>
                  <span>{{folder.Name}}</span>
                </div>
                <div class="tree-children">
                  {{RenderTreeNode(folder, isRoot: false)}}
                </div>
              </div>
            """);
        }

        foreach (var (name, report) in node.Leaves.OrderBy(l => l.name))
        {
            var dotColor = report.HasRegression ? "var(--red)"
                : report.HasImprovement ? "var(--green)"
                : ScoreHexColor(report.LatestScore);

            var link = report.RelativePath.Replace(Path.DirectorySeparatorChar, '/') + "/report.html";

            sb.AppendLine($$"""
              <a href="{{link}}" class="tree-leaf">
                <span class="tree-dot" style="background:{{dotColor}}"></span>
                <span>{{name}}</span>
              </a>
            """);
        }

        return sb.ToString();
    }

    private static string ScoreClass(double score)
    {
        return score >= 0.8 ? "good" : score >= 0.5 ? "warn" : "bad";
    }

    private static string ScoreHexColor(double score)
    {
        return score >= 0.8 ? "var(--green)" : score >= 0.5 ? "var(--yellow)" : "var(--red)";
    }

    private static string ToJson(IEnumerable<double> values)
    {
        return "[" + string.Join(",", values.Select(v => v.ToString("F4", System.Globalization.CultureInfo.InvariantCulture))) + "]";
    }

    private static string ToJson(IEnumerable<string> values)
    {
        return "[" + string.Join(",", values.Select(v => $"\"{v.Replace("\"", "\\\"")}\"")) + "]";
    }

    private static string ShortenId(string id)
    {
        var parts = id.Split('.');
        return parts.Length > 2 ? "…" + string.Join(".", parts[^2..]) : id;
    }

    // Small helper tree for sidebar rendering
    private sealed class TreeNode(string name)
    {
        public string Name { get; } = name;
        public Dictionary<string, TreeNode> Children { get; } = new();
        public List<(string name, TrendReport report)> Leaves { get; } = new();

        public TreeNode GetOrAddChild(string name)
        {
            if (!Children.TryGetValue(name, out var child))
            {
                child = new TreeNode(name);
                Children[name] = child;
            }

            return child;
        }

        public void AddLeaf(string name, TrendReport report)
        {
            Leaves.Add((name, report));
        }
    }
}
