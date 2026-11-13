import type { SetRecord, WorkoutByIdExercise } from "@weight-tracker/core/domain";
import {
  countdownTextOf,
  englishSubtitleOf,
  expandSets,
  isRecordable,
  nameOf,
  restSecondsOf,
  restTimeOf,
  setKindOf,
  setRecordBodyOf,
  setTargetLabel,
} from "@/presentation/workouts/set-rows";

const reps = (value: number) => ({ type: "Reps", value, maxValue: null, minutes: null });

function record(id: string): SetRecord {
  return {
    id,
    type: "Reps",
    reps: 10,
    durationSeconds: null,
    weight: 20,
    registeredAtUtc: "2026-11-10T10:00:00Z",
  };
}

function exercise(overrides: Partial<WorkoutByIdExercise> = {}): WorkoutByIdExercise {
  return {
    id: "we1",
    exerciseId: "ex1",
    translations: [
      { languageCode: "es", name: "Sentadilla" },
      { languageCode: "en", name: "Squat" },
    ],
    videoUrl: null,
    muscle: "QUADS",
    bodyPart: "LEGS",
    type: "STRENGTH",
    effort: "MID",
    perSide: false,
    restMinutes: null,
    sets: [],
    superSeriesId: null,
    setRecords: [],
    ...overrides,
  } as WorkoutByIdExercise;
}

describe("expandSets", () => {
  it("expands every prescription into numbered rows", () => {
    // Arrange
    const sets = [
      { count: 3, target: reps(10) },
      { count: 1, target: reps(8) },
    ];

    // Act
    const rows = expandSets(exercise({ sets }));

    // Assert
    expect(rows.map((row) => row.number)).toEqual([1, 2, 3, 4]);
    expect(rows.map((row) => Number(row.target.value))).toEqual([10, 10, 10, 8]);
  });

  it("gives an exercise without prescriptions no rows", () => {
    // Arrange
    const sets: WorkoutByIdExercise["sets"] = [];

    // Act
    const rows = expandSets(exercise({ sets }));

    // Assert
    expect(rows).toEqual([]);
  });

  it("pairs each row with the record at its position", () => {
    // Arrange
    const sets = [{ count: 3, target: reps(10) }];
    const setRecords = [record("r0"), record("r1")];

    // Act
    const rows = expandSets(exercise({ sets, setRecords }));

    // Assert
    expect(rows.map((row) => row.record?.id ?? null)).toEqual(["r0", "r1", null]);
  });

  it("does not show records beyond the prescribed sets", () => {
    // Arrange
    const sets = [{ count: 1, target: reps(10) }];
    const setRecords = [record("r0"), record("r1")];

    // Act
    const rows = expandSets(exercise({ sets, setRecords }));

    // Assert
    expect(rows).toHaveLength(1);
    expect(rows[0]?.record?.id).toBe("r0");
  });
});

describe("setKindOf", () => {
  it.each([
    ["Reps", "reps"],
    ["RepsRange", "range"],
    ["MaxReps", "range"],
    ["Duration", "timed"],
    ["Minutes", "timed"],
  ])("reads a %s target as %s", (type, kind) => {
    // Arrange
    const target = { type, value: 1, maxValue: 2, minutes: 1 };

    // Act
    const result = setKindOf(target);

    // Assert
    expect(result).toBe(kind);
  });
});

describe("setTargetLabel", () => {
  it.each([
    [
      { type: "Reps", value: 10, maxValue: null, minutes: null },
      "exercise.target.reps",
      { count: 10 },
    ],
    [
      { type: "RepsRange", value: 8, maxValue: 12, minutes: null },
      "exercise.target.range",
      { min: 8, max: 12 },
    ],
    [{ type: "MaxReps", value: null, maxValue: null, minutes: null }, "exercise.target.max", {}],
    [
      { type: "Duration", value: 45, maxValue: null, minutes: null },
      "exercise.target.seconds",
      { count: 45 },
    ],
    [
      { type: "Minutes", value: null, maxValue: null, minutes: 1.5 },
      "exercise.target.minutes",
      { count: 1.5 },
    ],
  ])("reads %j", (target, key, values) => {
    // Arrange & Act
    const label = setTargetLabel(target);

    // Assert
    expect(label).toEqual({ key, values });
  });
});

describe("nameOf", () => {
  it("is the translation of the language", () => {
    // Arrange
    const subject = exercise();

    // Act
    const name = nameOf(subject, "es");

    // Assert
    expect(name).toBe("Sentadilla");
  });

  it("falls back to the first translation", () => {
    // Arrange
    const subject = exercise({ translations: [{ languageCode: "en", name: "Squat" }] });

    // Act
    const name = nameOf(subject, "es");

    // Assert
    expect(name).toBe("Squat");
  });

  it("is null without translations", () => {
    // Arrange
    const subject = exercise({ translations: [] });

    // Act
    const name = nameOf(subject, "es");

    // Assert
    expect(name).toBeNull();
  });
});

describe("englishSubtitleOf", () => {
  it("is the English name when another language is active", () => {
    // Arrange
    const subject = exercise();

    // Act
    const subtitle = englishSubtitleOf(subject, "es", "Sentadilla");

    // Assert
    expect(subtitle).toBe("Squat");
  });

  it("is null when English is active", () => {
    // Arrange
    const subject = exercise();

    // Act
    const subtitle = englishSubtitleOf(subject, "en", "Squat");

    // Assert
    expect(subtitle).toBeNull();
  });

  it("is null without an English name", () => {
    // Arrange
    const subject = exercise({ translations: [{ languageCode: "es", name: "Sentadilla" }] });

    // Act
    const subtitle = englishSubtitleOf(subject, "es", "Sentadilla");

    // Assert
    expect(subtitle).toBeNull();
  });

  it("is null when the English name is the one already shown", () => {
    // Arrange
    const subject = exercise({ translations: [{ languageCode: "en", name: "Squat" }] });

    // Act
    const subtitle = englishSubtitleOf(subject, "es", "Squat");

    // Assert
    expect(subtitle).toBeNull();
  });
});

describe("isRecordable", () => {
  it.each([
    ["STARTED", true],
    ["COMPLETED", true],
    ["DRAFT", false],
    ["PUBLISHED", false],
    ["REVIEWED", false],
    [null, false],
  ])("is %s -> %s", (status, expected) => {
    // Arrange & Act
    const result = isRecordable(status);

    // Assert
    expect(result).toBe(expected);
  });
});

describe("setRecordBodyOf", () => {
  const rowOf = (target: Record<string, unknown>) =>
    expandSets(exercise({ sets: [{ count: 1, target: target as never }] }))[0]!;

  it("sends the prescribed reps for a reps target, with the typed weight", () => {
    // Arrange
    const row = rowOf(reps(10));

    // Act
    const body = setRecordBodyOf(row, { reps: null, weight: 60 });

    // Assert
    expect(body).toEqual({ reps: 10, weight: 60 });
  });

  it("leaves the weight out when it was not typed", () => {
    // Arrange
    const row = rowOf(reps(10));

    // Act
    const body = setRecordBodyOf(row, { reps: null, weight: null });

    // Assert
    expect(body).toEqual({ reps: 10 });
  });

  it("sends the typed reps for a range target", () => {
    // Arrange
    const row = rowOf({ type: "RepsRange", value: 8, maxValue: 12, minutes: null });

    // Act
    const body = setRecordBodyOf(row, { reps: 11, weight: null });

    // Assert
    expect(body).toEqual({ reps: 11 });
  });

  it("sends the prescribed seconds for a duration target", () => {
    // Arrange
    const row = rowOf({ type: "Duration", value: 45, maxValue: null, minutes: null });

    // Act
    const body = setRecordBodyOf(row, { reps: null, weight: null });

    // Assert
    expect(body).toEqual({ durationSeconds: 45 });
  });

  it("sends minutes as rounded seconds", () => {
    // Arrange
    const row = rowOf({ type: "Minutes", value: null, maxValue: null, minutes: 1.55 });

    // Act
    const body = setRecordBodyOf(row, { reps: null, weight: null });

    // Assert
    expect(body).toEqual({ durationSeconds: 93 });
  });
});

describe("restTimeOf", () => {
  it.each([
    [1.5, "1:30"],
    [2, "2:00"],
    [0.75, "0:45"],
    ["1.33", "1:20"],
  ])("reads %s minutes as %s", (minutes, expected) => {
    // Arrange & Act
    const text = restTimeOf(minutes);

    // Assert
    expect(text).toBe(expected);
  });
});

describe("restSecondsOf", () => {
  it.each([
    [2, 120],
    [1.5, 90],
    ["0.75", 45],
    ["1.33", 80],
  ])("reads %s minutes as %s seconds", (minutes, expected) => {
    // Arrange & Act
    const seconds = restSecondsOf(minutes);

    // Assert
    expect(seconds).toBe(expected);
  });
});

describe("countdownTextOf", () => {
  it.each([
    [0, "0:00"],
    [9, "0:09"],
    [60, "1:00"],
    [90, "1:30"],
    [600, "10:00"],
  ])("reads %s seconds as %s", (seconds, expected) => {
    // Arrange & Act
    const text = countdownTextOf(seconds);

    // Assert
    expect(text).toBe(expected);
  });
});
