namespace Trendsetter.Engine.Reports;

using System;
using System.Collections.Generic;
using System.Linq;
using Trendsetter.Engine.Models;

public static class TrendAnalyzer
{
    public static IReadOnlyList<FieldTrendStats> Analyze(IReadOnlyList<RunResult> runs)
    {
        if (runs.Count == 0)
        {
            return [];
        }

        // Group all field scores across all runs by field name
        var byField = runs
            .SelectMany(r => r.Items)
            .SelectMany(i => i.FieldScores)
            .GroupBy(f => f.FieldName);

        return byField.Select(g =>
        {
            var scores = g.Select(f => f.Score).ToList();
            var mean = scores.Average();
            var variance = scores.Average(s => Math.Pow(s - mean, 2));

            return new FieldTrendStats
            {
                FieldName = g.Key,
                Mean = mean,
                StdDev = Math.Sqrt(variance),
                Min = scores.Min(),
                Max = scores.Max(),
                History = scores
            };
        }).ToList();
    }
}