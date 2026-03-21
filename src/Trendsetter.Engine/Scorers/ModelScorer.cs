namespace Trendsetter.Engine.Scorers;

using System.Reflection;
using System.Text.Json;
using Trendsetter.Engine.Configuration;
using Trendsetter.Engine.Models;

public sealed class ModelScorer
{
    private readonly ScorerFactory _factory;

    public ModelScorer(ScorerFactory factory)
        => _factory = factory;

    public ItemResult Score<TModel>(TModel expected, TModel actual, ScoringConfiguration config)
    {
        return Score(expected, actual, config, typeof(TModel));
    }

    private ItemResult Score(object? expected, object? actual, ScoringConfiguration config, Type type)
    {
        var fieldScores = new List<FieldScore>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var name = prop.Name;

            // --- Owned single object ---
            if (config.OwnedOne.TryGetValue(name, out var ownedConfig))
            {
                var expVal = expected is null ? null : prop.GetValue(expected);
                var actVal = actual is null ? null : prop.GetValue(actual);
                var nested = Score(expVal, actVal, ownedConfig, prop.PropertyType);
                // Flatten nested field scores prefixed with the nav name
                fieldScores.AddRange(nested.FieldScores.Select(fs =>
                {
                    return fs with
                    {
                        FieldName = $"{name}.{fs.FieldName}"
                    };
                }));
                continue;
            }

            // --- Owned collection ---
            if (config.OwnedMany.TryGetValue(name, out var manyConfig))
            {
                var expList = ToObjectList(expected is null ? null : prop.GetValue(expected));
                var actList = ToObjectList(actual is null ? null : prop.GetValue(actual));
                var elementType = GetElementType(prop.PropertyType)!;
                var nestedScores = ScoreList(expList, actList, manyConfig, elementType);
                fieldScores.AddRange(nestedScores.Select(fs => fs with
                {
                    FieldName = $"{name}[].{fs.FieldName}"
                }));
                continue;
            }

            // --- Skip explicitly marked fields ---
            if (config.FieldModes.TryGetValue(name, out var explicitMode) && explicitMode == ScoringMode.Skip)
            {
                continue;
            }

            // --- Scalar field ---
            var mode = config.FieldModes.TryGetValue(name, out var m)
                ? m
                : ScoringModeResolver.Resolve(prop.PropertyType, config.DefaultMode);

            if (mode == ScoringMode.Skip)
            {
                continue;
            }

            var expStr = Serialize(expected is null ? null : prop.GetValue(expected));
            var actStr = Serialize(actual is null ? null : prop.GetValue(actual));
            var scorer = _factory.Get(mode);

            fieldScores.Add(new FieldScore
            {
                FieldName = name,
                Mode = mode,
                Score = scorer.Score(expStr, actStr),
                Expected = expStr,
                Actual = actStr
            });
        }

        return new ItemResult { FieldScores = fieldScores };
    }

    private List<FieldScore> ScoreList(
        List<object?> expected,
        List<object?> actual,
        ScoringConfiguration config,
        Type elementType)
    {
        if (expected.Count == 0 && actual.Count == 0)
        {
            return [];
        }

        // Build NxM score matrix, greedy best-match assignment
        var matrix = new double[expected.Count, actual.Count];
        var itemResults = new ItemResult[expected.Count, actual.Count];

        for (int i = 0; i < expected.Count; i++)
        {
            for (int j = 0; j < actual.Count; j++)
            {
                var result = Score(expected[i], actual[j], config, elementType);
                itemResults[i, j] = result;
                matrix[i, j] = result.Score;
            }
        }

        var paired = GreedyAssign(matrix);
        var fieldScores = new List<FieldScore>();

        for (int i = 0; i < expected.Count; i++)
        {
            if (paired.TryGetValue(i, out var j))
            {
                fieldScores.AddRange(itemResults[i, j].FieldScores);
            }
            else
            {
                // Unmatched expected item — score 0 for all its fields
                fieldScores.AddRange(Score(expected[i], null, config, elementType).FieldScores
                    .Select(fs => fs with { Score = 0.0 }));
            }
        }

        return fieldScores;
    }

    private static Dictionary<int, int> GreedyAssign(double[,] matrix)
    {
        var assigned = new Dictionary<int, int>();
        var usedJ = new HashSet<int>();
        int rows = matrix.GetLength(0), cols = matrix.GetLength(1);

        for (int i = 0; i < rows; i++)
        {
            int bestJ = -1;
            double bestScore = -1;
            for (int j = 0; j < cols; j++)
            {
                if (!usedJ.Contains(j) && matrix[i, j] > bestScore)
                {
                    bestScore = matrix[i, j];
                    bestJ = j;
                }
            }

            if (bestJ >= 0)
            {
                assigned[i] = bestJ;
                usedJ.Add(bestJ);
            }
        }

        return assigned;
    }

    private static string? Serialize(object? value)
    {
        return value is null ? null : JsonSerializer.Serialize(value);
    }

    private static List<object?> ToObjectList(object? value)
    {
        if (value is null)
        {
            return [];
        }

        if (value is System.Collections.IEnumerable enumerable)
        {
            return [.. enumerable.Cast<object?>()];
        }

        return [value];
    }

    private static Type? GetElementType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        var iEnum = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType &&
                                 i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return iEnum?.GetGenericArguments()[0];
    }
}