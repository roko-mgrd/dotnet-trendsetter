namespace Trendsetter.Engine.Scorers;

using Trendsetter.Engine.Models;

public static class ScoringModeResolver
{
    public static ScoringMode Resolve(Type type, ScoringMode @default)
    {
        // Unwrap nullable
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(string))
        {
            return @default; // strings use whatever the caller's default is (Partial by convention)
        }

        if (underlying == typeof(int) ||
            underlying == typeof(long) ||
            underlying == typeof(double) ||
            underlying == typeof(float) ||
            underlying == typeof(decimal) ||
            underlying == typeof(bool) ||
            underlying == typeof(DateOnly) ||
            underlying == typeof(DateTime) ||
            underlying == typeof(DateTimeOffset) ||
            underlying.IsEnum)
        {
            return ScoringMode.Exact;
        }

        if (underlying.IsAssignableTo(typeof(System.Collections.IEnumerable)))
        {
            return ScoringMode.Structural;
        }

        // Complex objects: recurse (Structural as a safe default)
        return ScoringMode.Structural;
    }
}
