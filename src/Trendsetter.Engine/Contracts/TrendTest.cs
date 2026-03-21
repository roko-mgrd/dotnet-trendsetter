namespace Trendsetter.Engine.Contracts;

using Trendsetter.Engine.Builders;
using Trendsetter.Engine.Models;
using Trendsetter.Engine.Scorers;

/// <summary>
/// Base class for trend tests.
/// <typeparamref name="TResponse"/> is the AI return type — either <c>TModel</c> (single object)
/// or <c>IReadOnlyList&lt;TModel&gt;</c> (collection).
/// </summary>
public abstract class TrendTest<TModel, TResponse>
{
    public abstract string TestId { get; }

    /// <summary>
    /// Called once to configure field-level scoring for TModel.
    /// </summary>
    public abstract void Configure(TrendModelBuilder<TModel> builder);

    /// <summary>
    /// Return the expected (ground truth) data.
    /// </summary>
    public abstract Task<TResponse> GetExpectedAsync();

    /// <summary>
    /// Call the AI service and return actual extracted data.
    /// </summary>
    public abstract Task<TResponse> GetActualAsync();

    /// <summary>
    /// Run the trend, score results, and return a RunResult.
    /// </summary>
    public async Task<RunResult> RunAsync(RunResult[]? history = null)
    {
        var builder = new TrendModelBuilder<TModel>();
        Configure(builder);

        var scorerFactory = new ScorerFactory();
        var engine = new ModelScorer(scorerFactory);

        var expected = Normalize(await GetExpectedAsync());
        var actual = Normalize(await GetActualAsync());

        var config = builder.Configuration;
        var itemResults = new List<ItemResult>();

        // Align expected vs actual with greedy matching
        var expList = expected.Cast<object?>().ToList();
        var actList = actual.Cast<object?>().ToList();

        // Use a temporary ModelScorer path for top-level list scoring
        var matrix = new double[expList.Count, actList.Count];
        var resultMatrix = new ItemResult[expList.Count, actList.Count];

        for (int i = 0; i < expList.Count; i++)
            for (int j = 0; j < actList.Count; j++)
            {
                var r = engine.Score((TModel)expList[i]!, (TModel)actList[j]!, config);
                resultMatrix[i, j] = r;
                matrix[i, j] = r.Score;
            }

        var paired = GreedyAssign(matrix, expList.Count, actList.Count);

        for (int i = 0; i < expList.Count; i++)
        {
            itemResults.Add(paired.TryGetValue(i, out var j)
                ? resultMatrix[i, j]
                : new ItemResult { FieldScores = [] }); // unmatched = 0
        }

        var runNumber = history?.Length ?? 0;

        return new RunResult
        {
            TestId = TestId,
            RunNumber = runNumber,
            Timestamp = DateTimeOffset.UtcNow,
            Items = itemResults
        };
    }

    private static Dictionary<int, int> GreedyAssign(double[,] matrix, int rows, int cols)
    {
        var assigned = new Dictionary<int, int>();
        var usedJ = new HashSet<int>();

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

    private static IReadOnlyList<TModel> Normalize(TResponse response)
    {
        return response switch
        {
            IReadOnlyList<TModel> list => list,
            TModel item => [item],
            _ => throw new InvalidOperationException(
                $"TResponse must be either {typeof(TModel).Name} or IReadOnlyList<{typeof(TModel).Name}>.")
        };
    }
}