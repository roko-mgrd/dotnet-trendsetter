/** Mirrors Trendsetter.Engine.Models.ScoringMode */
export enum ScoringMode {
    Exact = 0,
    Partial = 1,
    Semantic = 2,
    Structural = 3,
    Skip = 4,
}

export const ScoringModeLabel: Record<number, string> = {
    [ScoringMode.Exact]: "Exact",
    [ScoringMode.Partial]: "Partial",
    [ScoringMode.Semantic]: "Semantic",
    [ScoringMode.Structural]: "Structural",
    [ScoringMode.Skip]: "Skip",
};

/** Mirrors run_*.json schema (snake_case) */
export interface RunResultJson {
    test_id: string;
    run_number: number;
    timestamp: string;
    score: number;
    items: ItemResultJson[];
}

export interface ItemResultJson {
    score: number;
    field_scores: FieldScoreJson[];
}

export interface FieldScoreJson {
    field_name: string;
    score: number;
    mode: number;
    expected: string | null;
    actual: string | null;
}

/** Discovered trend (grouped test runs) */
export interface TrendInfo {
    testId: string;
    directory: string;
    runs: RunResultJson[];
    hasReport: boolean;
}
