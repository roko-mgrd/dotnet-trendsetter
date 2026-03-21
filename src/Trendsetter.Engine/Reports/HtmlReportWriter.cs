namespace Trendsetter.Engine.Reports;

using System.Text;
using Trendsetter.Engine.Models;

/// <summary>
/// Generates a self-contained HTML report for a single trend.
/// Written to report.html in the trend's directory.
/// </summary>
public sealed class HtmlReportWriter
{
    /// <summary>
    /// Writes report.html into the trend's own directory.
    /// Returns the path of the written file.
    /// </summary>
    public async Task<string> WriteAsync(TrendReport report)
    {
        var html = Render(report);
        var path = Path.Combine(report.DirectoryPath, "report.html");
        await File.WriteAllTextAsync(path, html, Encoding.UTF8);
        return path;
    }

    private string Render(TrendReport report)
    {
        var breadcrumb = BuildBreadcrumb(report.RelativePath);
        var scoreHistoryJson = ToJson(report.ScoreHistory);
        var runLabelsJson = ToJson(report.Runs.Select((_, i) => $"Run {i}"));
        var unstableWarning = report.UnstableFields.Count > 0
            ? $"""<div class="alert alert-warn">⚠ {report.UnstableFields.Count} unstable field(s): {string.Join(", ", report.UnstableFields.Select(f => $"<code>{f.FieldName}</code>"))}</div>"""
            : string.Empty;
        var deltaHtml = report.ScoreDelta.HasValue
            ? $"""<span class="delta {(report.ScoreDelta > 0 ? "up" : report.ScoreDelta < 0 ? "down" : "flat")}">{(report.ScoreDelta > 0 ? "▲" : "▼")} {Math.Abs(report.ScoreDelta.Value):P1} vs prev run</span>"""
            : string.Empty;

        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8"/>
<meta name="viewport" content="width=device-width, initial-scale=1.0"/>
<title>{{report.TestId}} — Trendsetter</title>
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
  }

  /* ---------- Top bar ---------- */
  .topbar {
    background: var(--surface);
    border-bottom: 1px solid var(--border);
    padding: 0 32px;
    height: 52px;
    display: flex;
    align-items: center;
    gap: 12px;
    position: sticky;
    top: 0;
    z-index: 100;
  }
  .topbar-logo {
    font-family: var(--mono);
    font-size: 13px;
    font-weight: 600;
    color: var(--accent);
    letter-spacing: 0.08em;
    text-transform: uppercase;
  }
  .topbar-sep { color: var(--border); }
  .breadcrumb {
    display: flex;
    align-items: center;
    gap: 6px;
    font-family: var(--mono);
    font-size: 12px;
    color: var(--text-dim);
  }
  .breadcrumb a { color: var(--text-dim); text-decoration: none; }
  .breadcrumb a:hover { color: var(--accent); }
  .breadcrumb .current { color: var(--text); }

  /* ---------- Layout ---------- */
  .page { max-width: 1100px; margin: 0 auto; padding: 40px 32px 80px; }

  .page-header { margin-bottom: 36px; }
  .page-title {
    font-family: var(--mono);
    font-size: 22px;
    font-weight: 600;
    color: #fff;
    letter-spacing: -0.01em;
    margin-bottom: 6px;
  }
  .page-meta {
    font-size: 13px;
    color: var(--text-dim);
    display: flex;
    align-items: center;
    gap: 16px;
  }
  .delta {
    font-family: var(--mono);
    font-size: 12px;
    padding: 2px 8px;
    border-radius: 4px;
  }
  .delta.up   { background: var(--green-dim); color: var(--green); }
  .delta.down { background: var(--red-dim);   color: var(--red); }
  .delta.flat { background: var(--surface2);  color: var(--text-dim); }

  /* ---------- Stat cards ---------- */
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

  /* ---------- Alert ---------- */
  .alert {
    padding: 12px 16px;
    border-radius: var(--radius);
    font-size: 13px;
    margin-bottom: 28px;
    border-left: 3px solid;
  }
  .alert-warn {
    background: var(--yellow-dim);
    border-color: var(--yellow);
    color: var(--yellow);
  }
  .alert-warn code {
    font-family: var(--mono);
    background: rgba(249,195,79,0.15);
    padding: 1px 5px;
    border-radius: 3px;
  }

  /* ---------- Section ---------- */
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

  /* ---------- Chart container ---------- */
  .chart-card {
    background: var(--surface);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 20px 24px;
  }
  .chart-wrap { position: relative; height: 180px; }

  /* ---------- Field table ---------- */
  .field-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 13px;
  }
  .field-table th {
    font-family: var(--mono);
    font-size: 10px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.1em;
    color: var(--text-dim);
    text-align: left;
    padding: 8px 12px;
    border-bottom: 1px solid var(--border);
    white-space: nowrap;
  }
  .field-table td {
    padding: 9px 12px;
    border-bottom: 1px solid var(--border);
    vertical-align: middle;
  }
  .field-table tr:last-child td { border-bottom: none; }
  .field-table tr:hover td { background: var(--surface2); }

  .field-name {
    font-family: var(--mono);
    font-size: 12px;
    color: #fff;
  }
  .field-name.nested { color: var(--text-dim); padding-left: 16px; }
  .field-name.nested::before { content: '└ '; color: var(--border); }

  .mode-badge {
    font-family: var(--mono);
    font-size: 10px;
    padding: 2px 7px;
    border-radius: 3px;
    text-transform: lowercase;
    white-space: nowrap;
  }
  .mode-exact     { background: #1a3a6e; color: var(--accent); }
  .mode-partial   { background: #1a3d25; color: var(--green); }
  .mode-semantic  { background: #2a1f4a; color: #a78bfa; }
  .mode-structural{ background: #2a2a0f; color: var(--yellow); }
  .mode-skip      { background: var(--surface2); color: var(--text-dim); }

  .score-bar-wrap {
    display: flex;
    align-items: center;
    gap: 10px;
    min-width: 160px;
  }
  .score-bar-bg {
    flex: 1;
    height: 6px;
    background: var(--surface2);
    border-radius: 3px;
    overflow: hidden;
  }
  .score-bar-fill {
    height: 100%;
    border-radius: 3px;
    transition: width 0.3s ease;
  }
  .score-num {
    font-family: var(--mono);
    font-size: 12px;
    width: 42px;
    text-align: right;
    flex-shrink: 0;
  }

  .sparkline-cell { width: 100px; }
  .sparkline-wrap { position: relative; height: 28px; }

  .unstable-tag {
    font-family: var(--mono);
    font-size: 10px;
    color: var(--yellow);
    background: var(--yellow-dim);
    padding: 1px 6px;
    border-radius: 3px;
    white-space: nowrap;
  }

  /* ---------- Run history table ---------- */
  .run-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 13px;
  }
  .run-table th {
    font-family: var(--mono);
    font-size: 10px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.1em;
    color: var(--text-dim);
    text-align: left;
    padding: 8px 12px;
    border-bottom: 1px solid var(--border);
  }
  .run-table td {
    padding: 9px 12px;
    border-bottom: 1px solid var(--border);
    font-family: var(--mono);
    font-size: 12px;
  }
  .run-table tr:last-child td { border-bottom: none; }
  .run-table tr:hover td { background: var(--surface2); }
  .run-table td.run-score { font-weight: 600; }

  .pill {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    font-size: 10px;
    font-family: var(--mono);
    padding: 2px 8px;
    border-radius: 20px;
  }
  .pill-latest { background: var(--accent-dim); color: var(--accent); }

  /* ---------- Run selector ---------- */
  .run-selector {
    display: flex;
    align-items: center;
    gap: 12px;
  }
  .run-selector select {
    background: var(--surface2);
    color: var(--text);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 4px 10px;
    font-family: var(--mono);
    font-size: 12px;
    cursor: pointer;
  }
  .run-selector select:focus { outline: 1px solid var(--accent); }
</style>
</head>
<body>

<div class="topbar">
  <span class="topbar-logo">Trendsetter</span>
  <span class="topbar-sep">/</span>
  <nav class="breadcrumb">{{breadcrumb}}</nav>
</div>

<div class="page">

  <div class="page-header">
    <div class="page-title">{{report.TestId}}</div>
    <div class="page-meta">
      <span>{{report.RunCount}} run{{(report.RunCount != 1 ? "s" : "")}}</span>
      {{deltaHtml}}
    </div>
  </div>

  {{unstableWarning}}

  <div class="stats-row">
    <div class="stat-card">
      <div class="stat-label">Latest Score</div>
      <div class="stat-value {{ScoreClass(report.LatestScore)}}">{{report.LatestScore:P1}}</div>
    </div>
    <div class="stat-card">
      <div class="stat-label">Best Score</div>
      <div class="stat-value good">{{report.BestScore:P1}}</div>
    </div>
    <div class="stat-card">
      <div class="stat-label">Worst Score</div>
      <div class="stat-value {{ScoreClass(report.WorstScore)}}">{{report.WorstScore:P1}}</div>
    </div>
    <div class="stat-card">
      <div class="stat-label">Unstable Fields</div>
      <div class="stat-value {{(report.UnstableFields.Count > 0 ? "warn" : "good")}}">{{report.UnstableFields.Count}}</div>
    </div>
  </div>

  <div class="section">
    <div class="section-title">Score History</div>
    <div class="chart-card">
      <div class="chart-wrap">
        <canvas id="scoreChart"></canvas>
      </div>
    </div>
  </div>

  <div class="section">
    <div class="section-title">Field Stability</div>
    <div class="chart-card" style="padding: 0; overflow: hidden;">
      <table class="field-table">
        <thead>
          <tr>
            <th>Field</th>
            <th>Mode</th>
            <th>Mean Score</th>
            <th>Std Dev</th>
            <th>History</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          {{RenderFieldRows(report.FieldStats)}}
        </tbody>
      </table>
    </div>
  </div>

  <div class="section">
    <div class="section-title">Run History</div>
    <div class="chart-card" style="padding: 0; overflow: hidden;">
      <table class="run-table">
        <thead>
          <tr>
            <th>#</th>
            <th>Timestamp</th>
            <th>Score</th>
            <th>Items</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {{RenderRunRows(report.Runs)}}
        </tbody>
      </table>
    </div>
  </div>

  {{RenderRunDetailSections(report.Runs)}}

</div>

<script>
(function() {
  const scoreData = {{scoreHistoryJson}};
  const runLabels = {{runLabelsJson}};

  const ctx = document.getElementById('scoreChart').getContext('2d');
  new Chart(ctx, {
    type: 'line',
    data: {
      labels: runLabels,
      datasets: [{
        data: scoreData.map(v => +(v * 100).toFixed(1)),
        borderColor: '#4f9cf9',
        backgroundColor: 'rgba(79,156,249,0.08)',
        borderWidth: 2,
        pointBackgroundColor: scoreData.map(v =>
          v >= 0.8 ? '#3dd68c' : v >= 0.5 ? '#f9c34f' : '#f95f5f'
        ),
        pointRadius: 5,
        pointHoverRadius: 7,
        tension: 0.3,
        fill: true
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
          callbacks: {
            label: ctx => ` Score: ${ctx.parsed.y.toFixed(1)}%`
          }
        }
      },
      scales: {
        x: {
          grid: { color: '#1e2330' },
          ticks: { color: '#5a6480', font: { family: "'IBM Plex Mono'", size: 11 } }
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

  // Run detail selector
  document.getElementById('runSelector')?.addEventListener('change', function() {
    document.querySelectorAll('.run-detail-panel').forEach(p => p.style.display = 'none');
    const panel = document.getElementById('run-detail-' + this.value);
    if (panel) panel.style.display = '';
  });

  // Render sparkline mini charts
  document.querySelectorAll('[data-sparkline]').forEach(canvas => {
    const values = JSON.parse(canvas.dataset.sparkline);
    new Chart(canvas.getContext('2d'), {
      type: 'line',
      data: {
        labels: values.map((_, i) => i),
        datasets: [{
          data: values.map(v => +(v * 100).toFixed(1)),
          borderColor: '#4f9cf9',
          borderWidth: 1.5,
          pointRadius: 0,
          tension: 0.3,
          fill: false
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false }, tooltip: { enabled: false } },
        scales: {
          x: { display: false },
          y: { display: false, min: 0, max: 100 }
        },
        animation: false
      }
    });
  });
})();
</script>
</body>
</html>
""";
    }

    // -------------------------------------------------------------------------
    //  Helpers called from the template
    // -------------------------------------------------------------------------

    private string BuildBreadcrumb(string relativePath)
    {
        var parts = relativePath.Split([Path.DirectorySeparatorChar, '/'], StringSplitOptions.RemoveEmptyEntries);
        var backPath = string.Concat(Enumerable.Repeat("../", parts.Length)) + "dashboard.html";
        var sb = new StringBuilder();
        sb.Append($"""<a href="{backPath}">dashboard</a>""");

        for (int i = 0; i < parts.Length; i++)
        {
            sb.Append(""" <span class="topbar-sep">/</span> """);
            if (i == parts.Length - 1)
                sb.Append($"""<span class="current">{parts[i]}</span>""");
            else
                sb.Append($"""<span>{parts[i]}</span>""");
        }

        return sb.ToString();
    }

    private string RenderFieldRows(IReadOnlyList<FieldTrendStats> stats)
    {
        if (stats.Count == 0)
            return """<tr><td colspan="6" style="color:var(--text-dim);padding:20px 12px;">No field data available.</td></tr>""";

        var sb = new StringBuilder();

        foreach (var field in stats.OrderBy(f => f.FieldName))
        {
            var isNested = field.FieldName.Contains('.');
            var nameClass = isNested ? "field-name nested" : "field-name";
            var displayName = isNested ? field.FieldName.Split('.').Last() : field.FieldName;
            var fullName = isNested ? field.FieldName : null;
            var modeStr = "partial"; // will be set from actual data when available
            var scoreColor = ScoreBarColor(field.Mean);
            var sparklineJson = ToJson(field.History);
            var statusHtml = field.IsUnstable
                ? """<span class="unstable-tag">⚡ unstable</span>"""
                : """<span style="color:var(--green);font-size:11px;font-family:var(--mono);">✓ stable</span>""";

            sb.AppendLine($$"""
              <tr>
                <td>
                  <span class="{{nameClass}}"{{(fullName is not null ? $""" title="{fullName}" """ : "")}}>
                    {{displayName}}
                  </span>
                </td>
                <td><span class="mode-badge mode-{{modeStr}}">{{modeStr}}</span></td>
                <td>
                  <div class="score-bar-wrap">
                    <div class="score-bar-bg">
                      <div class="score-bar-fill" style="width:{{field.Mean * 100:F1}}%;background:{{scoreColor}};"></div>
                    </div>
                    <span class="score-num" style="color:{{scoreColor}}">{{field.Mean:P1}}</span>
                  </div>
                </td>
                <td style="font-family:var(--mono);font-size:12px;color:{{(field.IsUnstable ? "var(--yellow)" : "var(--text-dim)")}}">
                  {{field.StdDev:F3}}
                </td>
                <td class="sparkline-cell">
                  <div class="sparkline-wrap">
                    <canvas data-sparkline='{{sparklineJson}}'></canvas>
                  </div>
                </td>
                <td>{{statusHtml}}</td>
              </tr>
            """);
        }

        return sb.ToString();
    }

    private string RenderRunRows(IReadOnlyList<RunResult> runs)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < runs.Count; i++)
        {
            var run = runs[i];
            var isLatest = i == runs.Count - 1;
            var scoreColor = ScoreBarColor(run.Score);
            var latestPill = isLatest ? """<span class="pill pill-latest">latest</span>""" : string.Empty;
            var timestamp = run.Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC");

            sb.AppendLine($$"""
              <tr>
                <td>{{i}}</td>
                <td style="color:var(--text-dim)">{{timestamp}}</td>
                <td class="run-score" style="color:{{scoreColor}}">{{run.Score:P1}}</td>
                <td style="color:var(--text-dim)">{{run.Items.Count}}</td>
                <td>{{latestPill}}</td>
              </tr>
            """);
        }

        return sb.ToString();
    }

    private string RenderRunDetailSections(IReadOnlyList<RunResult> runs)
    {
        if (runs.Count == 0)
            return string.Empty;

        var options = new StringBuilder();
        var panels = new StringBuilder();

        for (int i = runs.Count - 1; i >= 0; i--)
        {
            var run = runs[i];
            var isLatest = i == runs.Count - 1;
            var label = $"Run {i}";
            if (isLatest)
                label += " (latest)";
            options.AppendLine($"""<option value="{i}"{(isLatest ? " selected" : "")}>{label} — {run.Score:P1}</option>""");
            panels.Append(RenderSingleRunDetail(run, i, isLatest));
        }

        return $$"""
          <div class="section">
            <div class="section-title">
              <div class="run-selector">
                <span>Run — Field Detail</span>
                <select id="runSelector">{{options}}</select>
              </div>
            </div>
            {{panels}}
          </div>
        """;
    }

    private string RenderSingleRunDetail(RunResult run, int index, bool visible)
    {
        var allFields = run.Items
            .SelectMany(item => item.FieldScores)
            .GroupBy(f => f.FieldName)
            .Select(g => g.First())
            .OrderBy(f => f.FieldName)
            .ToList();

        if (allFields.Count == 0)
            return string.Empty;

        var rows = new StringBuilder();
        foreach (var field in allFields)
        {
            if (field.Mode == ScoringMode.Skip)
                continue;

            var scoreColor = ScoreBarColor(field.Score);
            var modeStr = field.Mode.ToString().ToLowerInvariant();
            var expected = Truncate(field.Expected ?? "—", 80);
            var actual = Truncate(field.Actual ?? "—", 80);

            rows.AppendLine($$"""
              <tr>
                <td><span class="field-name">{{field.FieldName}}</span></td>
                <td><span class="mode-badge mode-{{modeStr}}">{{modeStr}}</span></td>
                <td>
                  <div class="score-bar-wrap">
                    <div class="score-bar-bg">
                      <div class="score-bar-fill" style="width:{{field.Score * 100:F1}}%;background:{{scoreColor}};"></div>
                    </div>
                    <span class="score-num" style="color:{{scoreColor}}">{{field.Score:P1}}</span>
                  </div>
                </td>
                <td style="font-family:var(--mono);font-size:11px;color:var(--text-dim);max-width:200px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;" title="{{HtmlEncode(field.Expected)}}">{{HtmlEncode(expected)}}</td>
                <td style="font-family:var(--mono);font-size:11px;color:var(--text-dim);max-width:200px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;" title="{{HtmlEncode(field.Actual)}}">{{HtmlEncode(actual)}}</td>
              </tr>
            """);
        }

        return $$"""
            <div id="run-detail-{{index}}" class="run-detail-panel" style="{{(visible ? "" : "display:none;")}}">
              <div class="chart-card" style="padding:0;overflow:hidden;">
                <table class="field-table">
                  <thead>
                    <tr>
                      <th>Field</th>
                      <th>Mode</th>
                      <th>Score</th>
                      <th>Expected</th>
                      <th>Actual</th>
                    </tr>
                  </thead>
                  <tbody>
                    {{rows}}
                  </tbody>
                </table>
              </div>
            </div>
        """;
    }

    private static string ScoreClass(double score)
    {
        return score >= 0.8 ? "good" : score >= 0.5 ? "warn" : "bad";
    }

    private static string ScoreBarColor(double score)
    {
        return score >= 0.8 ? "var(--green)" : score >= 0.5 ? "var(--yellow)" : "var(--red)";
    }

    private static string ToJson(IEnumerable<double> values)
    {
        return "[" + string.Join(",", values.Select(v => v.ToString("F4", System.Globalization.CultureInfo.InvariantCulture))) + "]";
    }

    private static string ToJson(IEnumerable<string> values)
    {
        return "[" + string.Join(",", values.Select(v => $"\"{v}\"")) + "]";
    }

    private static string Truncate(string s, int max)
    {
        return s.Length <= max ? s : s[..max] + "…";
    }

    private static string HtmlEncode(string? s)
    {
        return System.Net.WebUtility.HtmlEncode(s ?? string.Empty);
    }
}
