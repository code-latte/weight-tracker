import { describe, expect, it } from "vitest";
import type {
  ExerciseHistorySession,
  ExerciseSummary,
  SetRecord,
  WorkoutById,
  WorkoutByIdExercise,
  WorkoutSummary,
} from "@weight-tracker/core/domain";
import {
  countedRecordsOf,
  elapsedMsOf,
  elapsedPartsOf,
  exerciseStateOf,
  historyPointsOf,
  initiallyExpandedOf,
  isReviewStatus,
  isWeighted,
  muscleDistributionOf,
  percentOf,
  plannedRowsOf,
  plannedSetsOf,
  restOf,
  reviewSummaryOf,
  weightChangeOf,
  weightedExercisesOf,
} from "@/presentation/workouts/workout-review";

function record(
  id: string,
  values: Partial<Pick<SetRecord, "reps" | "weight" | "durationSeconds">> = {},
): SetRecord {
  return {
    id,
    type: "Reps",
    reps: null,
    durationSeconds: null,
    weight: null,
    registeredAtUtc: "2026-11-10T10:00:00Z",
    ...values,
  };
}

function exercise(
  id: string,
  planned: number[],
  records: SetRecord[] = [],
  overrides: Partial<WorkoutByIdExercise> = {},
): WorkoutByIdExercise {
  return {
    id,
    exerciseId: `catalog-${id}`,
    translations: [{ languageCode: "es", name: id }],
    videoUrl: null,
    muscle: "Quadriceps",
    bodyPart: "Legs",
    type: "STRENGTH",
    effort: "",
    perSide: false,
    restMinutes: null,
    sets: planned.map((count) => ({ count, target: { type: "Reps", value: 8 } })),
    superSeriesId: null,
    setRecords: records,
    ...overrides,
  } as WorkoutByIdExercise;
}

function workout(exercises: WorkoutByIdExercise[]): WorkoutById {
  return { exercises } as WorkoutById;
}

describe("isReviewStatus", () => {
  it.each(["STARTED", "COMPLETED", "REVIEWED"])("answers true for %s", (status) => {
    // Arrange / Act
    const result = isReviewStatus(status);

    // Assert
    expect(result).toBe(true);
  });

  it.each(["DRAFT", "PUBLISHED", "ARCHIVED"])("answers false for %s", (status) => {
    // Arrange / Act
    const result = isReviewStatus(status);

    // Assert
    expect(result).toBe(false);
  });
});

describe("plannedSetsOf", () => {
  it("sums the counts of every set group", () => {
    // Arrange
    const target = exercise("a", [2, 3]);

    // Act
    const result = plannedSetsOf(target);

    // Assert
    expect(result).toBe(5);
  });

  it("reads counts the contract types as strings", () => {
    // Arrange
    const target = exercise("a", []);
    target.sets = [{ count: "4", target: { type: "Reps", value: 8 } }] as never;

    // Act
    const result = plannedSetsOf(target);

    // Assert
    expect(result).toBe(4);
  });
});

describe("countedRecordsOf", () => {
  it("discards the records beyond the planned sets", () => {
    // Arrange
    const target = exercise("a", [3], [record("1"), record("2"), record("3"), record("4")]);

    // Act
    const result = countedRecordsOf(target);

    // Assert
    expect(result.map(({ id }) => id)).toEqual(["1", "2", "3"]);
  });

  it("keeps every record while fewer than planned", () => {
    // Arrange
    const target = exercise("a", [4], [record("1")]);

    // Act
    const result = countedRecordsOf(target);

    // Assert
    expect(result).toHaveLength(1);
  });
});

describe("exerciseStateOf", () => {
  it("is completed when the counted records reach the planned sets", () => {
    // Arrange
    const target = exercise("a", [2], [record("1"), record("2"), record("3")]);

    // Act
    const result = exerciseStateOf(target);

    // Assert
    expect(result).toBe("completed");
  });

  it("is in progress with some records", () => {
    // Arrange
    const target = exercise("a", [4], [record("1")]);

    // Act
    const result = exerciseStateOf(target);

    // Assert
    expect(result).toBe("inProgress");
  });

  it("is pending without records", () => {
    // Arrange
    const target = exercise("a", [3]);

    // Act
    const result = exerciseStateOf(target);

    // Assert
    expect(result).toBe("pending");
  });

  it("is pending with no planned sets, even with records", () => {
    // Arrange
    const target = exercise("a", [], [record("1")]);

    // Act
    const result = exerciseStateOf(target);

    // Assert
    expect(result).toBe("pending");
  });
});

describe("reviewSummaryOf", () => {
  it("counts sets and exercises across the workout", () => {
    // Arrange
    const target = workout([
      exercise("a", [3], [record("1"), record("2"), record("3")]),
      exercise("b", [4], [record("4")]),
      exercise("c", [3]),
    ]);

    // Act
    const result = reviewSummaryOf(target);

    // Assert
    expect(result).toMatchObject({
      planned: 10,
      counted: 4,
      pending: 6,
      percent: 40,
      completedExercises: 1,
      totalExercises: 3,
    });
  });

  it("rounds the percentage", () => {
    // Arrange
    const records = Array.from({ length: 8 }, (_, index) => record(String(index)));
    const target = workout([exercise("a", [29], records)]);

    // Act
    const result = reviewSummaryOf(target);

    // Assert
    expect(result).toMatchObject({ percent: 28, pending: 21 });
  });

  it("answers 0% when nothing is planned", () => {
    // Arrange
    const target = workout([exercise("a", [])]);

    // Act
    const result = reviewSummaryOf(target);

    // Assert
    expect(result).toMatchObject({ planned: 0, percent: 0, pending: 0 });
  });

  it("sums reps by weight and ignores timed and weightless records", () => {
    // Arrange
    const target = workout([
      exercise(
        "a",
        [4],
        [
          record("1", { reps: 8, weight: 80 }),
          record("2", { reps: 8, weight: 85 }),
          record("3", { durationSeconds: 60 }),
          record("4", { reps: 10 }),
        ],
      ),
    ]);

    // Act
    const result = reviewSummaryOf(target);

    // Assert
    expect(result.volumeKg).toBe(1320);
  });

  it("leaves a discarded record out of the volume", () => {
    // Arrange
    const target = workout([
      exercise(
        "a",
        [1],
        [record("1", { reps: 5, weight: 100 }), record("2", { reps: 5, weight: 100 })],
      ),
    ]);

    // Act
    const result = reviewSummaryOf(target);

    // Assert
    expect(result.volumeKg).toBe(500);
  });

  it("does not double a per-side exercise", () => {
    // Arrange
    const target = workout([
      exercise("a", [1], [record("1", { reps: 10, weight: 20 })], { perSide: true }),
    ]);

    // Act
    const result = reviewSummaryOf(target);

    // Assert
    expect(result.volumeKg).toBe(200);
  });
});

describe("plannedRowsOf", () => {
  it("expands the set groups and pairs the counted records in order", () => {
    // Arrange
    const target = exercise("a", [2, 2], [record("1"), record("2"), record("3")]);

    // Act
    const result = plannedRowsOf(target);

    // Assert
    expect(result.map(({ number }) => number)).toEqual([1, 2, 3, 4]);
    expect(result.map(({ record: paired }) => paired?.id ?? null)).toEqual(["1", "2", "3", null]);
  });

  it("adds no row for a discarded record", () => {
    // Arrange
    const target = exercise("a", [1], [record("1"), record("2")]);

    // Act
    const result = plannedRowsOf(target);

    // Assert
    expect(result).toHaveLength(1);
  });

  it("keeps each group's target on its rows", () => {
    // Arrange
    const target = exercise("a", [1, 1]);
    target.sets = [
      { count: 1, target: { type: "Reps", value: 8 } },
      { count: 1, target: { type: "MaxReps" } },
    ] as never;

    // Act
    const result = plannedRowsOf(target);

    // Assert
    expect(result.map(({ target: rowTarget }) => rowTarget.type)).toEqual(["Reps", "MaxReps"]);
  });
});

describe("initiallyExpandedOf", () => {
  it("picks the first exercise in progress", () => {
    // Arrange
    const exercises = [
      exercise("a", [1], [record("1")]),
      exercise("b", [2], [record("2")]),
      exercise("c", [2]),
    ];

    // Act
    const result = initiallyExpandedOf(exercises);

    // Assert
    expect(result).toBe("b");
  });

  it("picks the first not completed one when none is in progress", () => {
    // Arrange
    const exercises = [exercise("a", [1], [record("1")]), exercise("b", [2]), exercise("c", [2])];

    // Act
    const result = initiallyExpandedOf(exercises);

    // Assert
    expect(result).toBe("b");
  });

  it("picks none when every exercise is completed", () => {
    // Arrange
    const exercises = [exercise("a", [1], [record("1")])];

    // Act
    const result = initiallyExpandedOf(exercises);

    // Assert
    expect(result).toBeNull();
  });
});

describe("restOf", () => {
  it.each([
    [2, "2:00"],
    [1.5, "1:30"],
    [0.75, "0:45"],
    ["3", "3:00"],
  ])("answers %s minutes as %s", (minutes, expected) => {
    // Arrange / Act
    const result = restOf(minutes);

    // Assert
    expect(result).toBe(expected);
  });

  it("answers null without a rest", () => {
    // Arrange / Act
    const result = restOf(null);

    // Assert
    expect(result).toBeNull();
  });
});

describe("elapsedMsOf", () => {
  const minute = 60_000;
  const startedAt = "2026-11-13T10:00:00Z";
  const now = Date.parse(startedAt) + 25 * minute;

  it("counts from the start to now while the workout is started", () => {
    // Arrange
    const workout = { status: "STARTED", startedAt, completedAt: null };

    // Act
    const result = elapsedMsOf(workout, now);

    // Assert
    expect(result).toBe(25 * minute);
  });

  it.each(["COMPLETED", "REVIEWED"])(
    "counts from the start to the completion when %s",
    (status) => {
      // Arrange
      const workout = { status, startedAt, completedAt: "2026-11-13T11:05:00Z" };

      // Act
      const result = elapsedMsOf(workout, now);

      // Assert
      expect(result).toBe(65 * minute);
    },
  );

  it.each(["STARTED", "COMPLETED", "REVIEWED"])(
    "answers null without a start when %s",
    (status) => {
      // Arrange
      const workout = { status, startedAt: null, completedAt: "2026-11-13T11:05:00Z" };

      // Act
      const result = elapsedMsOf(workout, now);

      // Assert
      expect(result).toBeNull();
    },
  );

  it.each(["COMPLETED", "REVIEWED"])("answers null without a completion when %s", (status) => {
    // Arrange
    const workout = { status, startedAt, completedAt: null };

    // Act
    const result = elapsedMsOf(workout, now);

    // Assert
    expect(result).toBeNull();
  });

  it("answers null when the completion is before the start", () => {
    // Arrange
    const workout = { status: "COMPLETED", startedAt, completedAt: "2026-11-13T09:00:00Z" };

    // Act
    const result = elapsedMsOf(workout, now);

    // Assert
    expect(result).toBeNull();
  });

  it("answers null when the start is in the future while the workout is started", () => {
    // Arrange
    const workout = { status: "STARTED", startedAt, completedAt: null };

    // Act
    const result = elapsedMsOf(workout, Date.parse(startedAt) - minute);

    // Assert
    expect(result).toBeNull();
  });

  it.each(["DRAFT", "PUBLISHED"])("answers null when the status is %s", (status) => {
    // Arrange
    const workout = { status, startedAt, completedAt: "2026-11-13T11:05:00Z" };

    // Act
    const result = elapsedMsOf(workout, now);

    // Assert
    expect(result).toBeNull();
  });
});

describe("elapsedPartsOf", () => {
  it.each([
    [30_000, 0, 0],
    [12 * 60_000 + 59_000, 0, 12],
    [60 * 60_000, 1, 0],
    [2 * 3_600_000 + 3 * 60_000 + 59_000, 2, 3],
  ])("splits %i ms into %i h and %i min, truncating the seconds", (ms, hours, minutes) => {
    // Arrange / Act
    const result = elapsedPartsOf(ms);

    // Assert
    expect(result).toEqual({ hours, minutes });
  });
});

function summaryEntry(workoutExerciseId: string, volume: number | null): ExerciseSummary {
  return {
    workoutExerciseId,
    exerciseId: `catalog-${workoutExerciseId}`,
    setsInTarget: { inTarget: 0, evaluated: 0 },
    volume,
    topSet: null,
    previous: null,
  };
}

function session(
  workoutDate: string,
  values: Partial<Pick<ExerciseHistorySession, "maxWeight" | "volume">> = {},
): ExerciseHistorySession {
  return {
    workoutId: `workout-${workoutDate}`,
    workoutDate,
    maxWeight: null,
    volume: null,
    estimatedOneRepMax: null,
    ...values,
  };
}

describe("isWeighted", () => {
  it.each([
    [3190, true],
    [0, true],
    [null, false],
  ])("answers for a volume of %s", (volume, expected) => {
    // Arrange
    const entry = summaryEntry("a", volume);

    // Act
    const weighted = isWeighted(entry);

    // Assert
    expect(weighted).toBe(expected);
  });
});

describe("weightedExercisesOf", () => {
  it("keeps the weighted exercises in workout order with their position, whatever the summary order", () => {
    // Arrange
    const stretch = exercise("stretch", [1]);
    const squat = exercise("squat", [3]);
    const press = exercise("press", [3]);
    const summary = {
      exercises: [
        summaryEntry("press", 1200),
        summaryEntry("stretch", null),
        summaryEntry("squat", 3190),
      ],
    } as WorkoutSummary;

    // Act
    const weighted = weightedExercisesOf(summary, workout([stretch, squat, press]));

    // Assert
    expect(weighted.map(({ exercise: { id }, position }) => [id, position])).toEqual([
      ["squat", 2],
      ["press", 3],
    ]);
    expect(weighted[0].summary.volume).toBe(3190);
  });

  it("leaves out a workout exercise that the summary does not list", () => {
    // Arrange
    const summary = { exercises: [summaryEntry("squat", 3190)] } as WorkoutSummary;

    // Act
    const weighted = weightedExercisesOf(
      summary,
      workout([exercise("squat", [3]), exercise("press", [3])]),
    );

    // Assert
    expect(weighted.map(({ exercise: { id } }) => id)).toEqual(["squat"]);
  });
});

describe("weightChangeOf", () => {
  it("reports a rise with its percentage", () => {
    // Arrange
    const previous = 120;
    const current = 130;

    // Act
    const change = weightChangeOf(previous, current);

    // Assert
    expect(change?.direction).toBe("up");
    expect(change?.percent).toBeCloseTo(8.33, 2);
  });

  it("reports a fall with a negative percentage", () => {
    // Arrange
    const previous = 100;
    const current = 95;

    // Act
    const change = weightChangeOf(previous, current);

    // Assert
    expect(change).toEqual({ direction: "down", percent: -5 });
  });

  it("reports equal weights as equal", () => {
    // Arrange
    const previous = "100";
    const current = 100;

    // Act
    const change = weightChangeOf(previous, current);

    // Assert
    expect(change).toEqual({ direction: "equal", percent: 0 });
  });

  it.each([
    [null, 100],
    [100, null],
    [undefined, 100],
    [0, 100],
  ])("has no change from %s to %s", (previous, current) => {
    // Arrange
    // (the inputs are the case)

    // Act
    const change = weightChangeOf(previous, current);

    // Assert
    expect(change).toBeNull();
  });
});

describe("percentOf", () => {
  it.each([
    [27, 29, 93],
    [29, 29, 100],
    [0, 29, 0],
    ["2", "3", 67],
  ])("answers %s of %s as %s", (part, total, expected) => {
    // Arrange
    // (the inputs are the case)

    // Act
    const percent = percentOf(part, total);

    // Assert
    expect(percent).toBe(expected);
  });

  it("has no percentage when there is no total", () => {
    // Arrange
    // (the inputs are the case)

    // Act
    const percent = percentOf(0, 0);

    // Assert
    expect(percent).toBeNull();
  });
});

describe("muscleDistributionOf", () => {
  it("sums the counted records per muscle, most sets first", () => {
    // Arrange
    const squat = exercise("squat", [3], [record("1"), record("2"), record("3")], {
      muscle: "QUADRICEPS",
    });
    const extension = exercise("extension", [2], [record("4"), record("5")], {
      muscle: "QUADRICEPS",
    });
    const thrust = exercise("thrust", [2], [record("6"), record("7")], { muscle: "GLUTES" });

    // Act
    const distribution = muscleDistributionOf(workout([thrust, squat, extension]));

    // Assert
    expect(distribution).toEqual([
      { muscle: "QUADRICEPS", sets: 5 },
      { muscle: "GLUTES", sets: 2 },
    ]);
  });

  it("leaves out a muscle whose exercises have no counted record", () => {
    // Arrange
    const squat = exercise("squat", [3], [record("1")], { muscle: "QUADRICEPS" });
    const calves = exercise("calves", [3], [], { muscle: "CALVES" });

    // Act
    const distribution = muscleDistributionOf(workout([squat, calves]));

    // Assert
    expect(distribution).toEqual([{ muscle: "QUADRICEPS", sets: 1 }]);
  });

  it("discards the records beyond the plan", () => {
    // Arrange
    const squat = exercise("squat", [2], [record("1"), record("2"), record("3")], {
      muscle: "QUADRICEPS",
    });

    // Act
    const distribution = muscleDistributionOf(workout([squat]));

    // Assert
    expect(distribution).toEqual([{ muscle: "QUADRICEPS", sets: 2 }]);
  });

  it("orders muscles with the same sets by their code", () => {
    // Arrange
    const glutes = exercise("thrust", [1], [record("1")], { muscle: "GLUTES" });
    const biceps = exercise("curl", [1], [record("2")], { muscle: "BICEPS" });

    // Act
    const distribution = muscleDistributionOf(workout([glutes, biceps]));

    // Assert
    expect(distribution.map(({ muscle }) => muscle)).toEqual(["BICEPS", "GLUTES"]);
  });
});

describe("historyPointsOf", () => {
  it("puts the oldest session first", () => {
    // Arrange
    const sessions = [
      session("2026-11-12", { maxWeight: 130 }),
      session("2026-10-27", { maxWeight: 120 }),
    ];

    // Act
    const points = historyPointsOf(sessions, "maxWeight");

    // Assert
    expect(points).toEqual([
      { workoutDate: "2026-10-27", value: 120 },
      { workoutDate: "2026-11-12", value: 130 },
    ]);
  });

  it("drops the sessions that have no value for the field and reads strings as numbers", () => {
    // Arrange
    const sessions = [
      session("2026-11-12", { maxWeight: null, volume: "3190" as unknown as number }),
      session("2026-10-27", { maxWeight: 120, volume: null }),
    ];

    // Act
    const points = historyPointsOf(sessions, "volume");

    // Assert
    expect(points).toEqual([{ workoutDate: "2026-11-12", value: 3190 }]);
  });

  it("keeps each field's own sessions", () => {
    // Arrange
    const sessions = [
      session("2026-11-12", { maxWeight: null, volume: 3190 }),
      session("2026-10-27", { maxWeight: 120, volume: null }),
    ];

    // Act
    const points = historyPointsOf(sessions, "maxWeight");

    // Assert
    expect(points).toEqual([{ workoutDate: "2026-10-27", value: 120 }]);
  });

  it("answers an empty list for no sessions", () => {
    // Arrange
    const sessions: ExerciseHistorySession[] = [];

    // Act
    const points = historyPointsOf(sessions, "volume");

    // Assert
    expect(points).toEqual([]);
  });
});
