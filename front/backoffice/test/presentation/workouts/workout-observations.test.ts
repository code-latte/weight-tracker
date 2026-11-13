import { describe, expect, it } from "vitest";
import type {
  WorkoutByIdExercise,
  WorkoutObservation,
  WorkoutObservations,
} from "@weight-tracker/core/domain";
import {
  exerciseOf,
  highlightedIdsOf,
  highlightedObservationsOf,
  observationCopyOf,
  observationGroupsOf,
  type ObservationFormat,
} from "@/presentation/workouts/workout-observations";

const format: ObservationFormat = {
  kg: (value) => `${value} kg`,
  signedPercent: (value) => `${value >= 0 ? "+" : "−"}${Math.abs(value)} %`,
  percent: (value) => `${value} %`,
  integer: (value) => String(value),
};

/** The contract types `data` as `Record<string, never>`, so the numbers a fixture holds need this cast. */
const dataOf = (values: Record<string, number | number[]>) => values as WorkoutObservation["data"];

function observation(
  id: string,
  code: string,
  exerciseId: string | null = null,
  data: Record<string, number | number[]> = {},
): WorkoutObservation {
  return { id, code, severity: "INFO", exerciseId, priority: null, data: dataOf(data) };
}

function response(
  observations: WorkoutObservation[],
  highlights: [string, number][] = [],
): WorkoutObservations {
  return {
    observations,
    highlights: highlights.map(([observationId, rank]) => ({
      observationId,
      rank,
      priority: "MEDIUM",
    })),
  };
}

describe("highlightedObservationsOf", () => {
  it("returns the highlighted observations in rank order", () => {
    // Arrange
    const answer = response(
      [
        observation("a", "ALL_TARGETS_MET"),
        observation("b", "WEIGHT_INCREASE"),
        observation("c", "REPS_INCREASE"),
      ],
      [
        ["c", 3],
        ["a", 1],
        ["b", 2],
      ],
    );

    // Act
    const highlighted = highlightedObservationsOf(answer);

    // Assert
    expect(highlighted.map(({ id }) => id)).toEqual(["a", "b", "c"]);
  });

  it("skips a highlight whose observation is missing", () => {
    // Arrange
    const answer = response(
      [observation("a", "ALL_TARGETS_MET")],
      [
        ["ghost", 1],
        ["a", 2],
      ],
    );

    // Act
    const highlighted = highlightedObservationsOf(answer);

    // Assert
    expect(highlighted.map(({ id }) => id)).toEqual(["a"]);
  });

  it("returns nothing when there are no highlights", () => {
    // Arrange
    const answer = response([observation("a", "ALL_TARGETS_MET")]);

    // Act
    const highlighted = highlightedObservationsOf(answer);

    // Assert
    expect(highlighted).toEqual([]);
  });
});

describe("highlightedIdsOf", () => {
  it("collects the ids of the highlighted observations", () => {
    // Arrange
    const answer = response(
      [observation("a", "ALL_TARGETS_MET"), observation("b", "REPS_INCREASE")],
      [["b", 1]],
    );

    // Act
    const ids = highlightedIdsOf(answer);

    // Assert
    expect([...ids]).toEqual(["b"]);
  });
});

describe("observationGroupsOf", () => {
  it("puts the workout group first and the exercises in order of first appearance", () => {
    // Arrange
    const observations = [
      observation("1", "WEIGHT_INCREASE", "ex-b"),
      observation("2", "REPS_INCREASE", "ex-a"),
      observation("3", "WORKOUT_INCOMPLETE"),
      observation("4", "VOLUME_INCREASE", "ex-b"),
    ];

    // Act
    const groups = observationGroupsOf(observations);

    // Assert
    expect(groups.map(({ exerciseId }) => exerciseId)).toEqual([null, "ex-b", "ex-a"]);
    expect(groups[1].observations.map(({ id }) => id)).toEqual(["1", "4"]);
  });

  it("has no workout group when no observation is about the whole workout", () => {
    // Arrange
    const observations = [observation("1", "WEIGHT_INCREASE", "ex-a")];

    // Act
    const groups = observationGroupsOf(observations);

    // Assert
    expect(groups.map(({ exerciseId }) => exerciseId)).toEqual(["ex-a"]);
  });

  it("returns no groups for no observations", () => {
    // Arrange
    const observations: WorkoutObservation[] = [];

    // Act
    const groups = observationGroupsOf(observations);

    // Assert
    expect(groups).toEqual([]);
  });
});

describe("exerciseOf", () => {
  const first = { id: "w-1", exerciseId: "ex-a" } as WorkoutByIdExercise;
  const second = { id: "w-2", exerciseId: "ex-a" } as WorkoutByIdExercise;
  const other = { id: "w-3", exerciseId: "ex-b" } as WorkoutByIdExercise;
  const workout = { exercises: [first, second, other] };

  it("returns the first exercise with that catalog id", () => {
    // Arrange
    const exerciseId = "ex-a";

    // Act
    const found = exerciseOf(workout, exerciseId);

    // Assert
    expect(found).toBe(first);
  });

  it("returns null when no exercise matches", () => {
    // Arrange
    const exerciseId = "ex-z";

    // Act
    const found = exerciseOf(workout, exerciseId);

    // Assert
    expect(found).toBeNull();
  });

  it("returns null for a workout-level observation", () => {
    // Arrange
    const exerciseId = null;

    // Act
    const found = exerciseOf(workout, exerciseId);

    // Assert
    expect(found).toBeNull();
  });
});

describe("observationCopyOf", () => {
  it("formats a count and a percentage for an incomplete workout", () => {
    // Arrange
    const incomplete = observation("1", "WORKOUT_INCOMPLETE", null, {
      recordedSets: 9,
      prescribedSets: 15,
      completionPercent: 60,
    });

    // Act
    const copy = observationCopyOf(incomplete, format);

    // Assert
    expect(copy.title).toBe("codes.WORKOUT_INCOMPLETE.title");
    expect(copy.text).toBe("codes.WORKOUT_INCOMPLETE.text");
    expect(copy.values).toEqual({
      recordedSets: "9",
      prescribedSets: "15",
      completionPercent: "60 %",
    });
  });

  it("formats weights and a signed change for a weight record", () => {
    // Arrange
    const record = observation("1", "WEIGHT_PERSONAL_RECORD", "ex-a", {
      previousMaxKg: 120,
      currentMaxKg: 130,
      changePercent: 8.33,
    });

    // Act
    const copy = observationCopyOf(record, format);

    // Assert
    expect(copy.values).toEqual({
      previousMaxKg: "120 kg",
      currentMaxKg: "130 kg",
      changePercent: "+8.33 %",
    });
  });

  it("formats a volume change", () => {
    // Arrange
    const volume = observation("1", "VOLUME_DECREASE", "ex-a", {
      previousVolumeKg: 2640,
      currentVolumeKg: 2400,
      changePercent: -9.09,
    });

    // Act
    const copy = observationCopyOf(volume, format);

    // Assert
    expect(copy.values).toEqual({
      previousVolumeKg: "2640 kg",
      currentVolumeKg: "2400 kg",
      changePercent: "−9.09 %",
    });
  });

  it("formats a repetition record with the weight it was done at", () => {
    // Arrange
    const record = observation("1", "REPS_PERSONAL_RECORD", "ex-a", {
      weightKg: 80,
      previousBestReps: 8,
      currentReps: 10,
    });

    // Act
    const copy = observationCopyOf(record, format);

    // Assert
    expect(copy.values).toEqual({ weightKg: "80 kg", previousBestReps: "8", currentReps: "10" });
  });

  it("formats the length of a streak", () => {
    // Arrange
    const streak = observation("1", "REPEATED_TARGET_MISS", "ex-a", { consecutiveMisses: 3 });

    // Act
    const copy = observationCopyOf(streak, format);

    // Assert
    expect(copy.values).toEqual({ consecutiveMisses: "3" });
  });

  it("zips the affected sets of a below-target observation", () => {
    // Arrange
    const below = observation("1", "REPS_BELOW_TARGET", "ex-a", {
      setPositions: [3, 4],
      reps: [6, 5],
      targetMins: [8, 8],
    });

    // Act
    const copy = observationCopyOf(below, format);

    // Assert
    expect(copy.text).toBe("codes.REPS_BELOW_TARGET.text");
    expect(copy.details).toEqual([
      { position: 3, reps: 6, target: 8 },
      { position: 4, reps: 5, target: 8 },
    ]);
  });

  it("reads the maximums of an above-range observation", () => {
    // Arrange
    const above = observation("1", "REPS_ABOVE_TARGET", "ex-a", {
      setPositions: [1],
      reps: [14],
      targetMaxes: [12],
    });

    // Act
    const copy = observationCopyOf(above, format);

    // Assert
    expect(copy.details).toEqual([{ position: 1, reps: 14, target: 12 }]);
  });

  it("has no text when the lists of a rep observation do not line up", () => {
    // Arrange
    const broken = observation("1", "REPS_BELOW_TARGET", "ex-a", {
      setPositions: [3, 4],
      reps: [6],
      targetMins: [8, 8],
    });

    // Act
    const copy = observationCopyOf(broken, format);

    // Assert
    expect(copy.title).toBe("codes.REPS_BELOW_TARGET.title");
    expect(copy.text).toBeNull();
  });

  it("adds the earlier reference when the baseline is present", () => {
    // Arrange
    const decrease = observation("1", "WEIGHT_DECREASE", "ex-a", {
      previousMaxKg: 84,
      currentMaxKg: 82,
      changePercent: -2.38,
      baselineMaxKg: 100,
    });

    // Act
    const copy = observationCopyOf(decrease, format);

    // Assert
    expect(copy.baseline).toEqual({ key: "baseline.weight", value: "100 kg" });
  });

  it("has no earlier reference when the baseline is absent", () => {
    // Arrange
    const decrease = observation("1", "WEIGHT_DECREASE", "ex-a", {
      previousMaxKg: 84,
      currentMaxKg: 82,
      changePercent: -2.38,
    });

    // Act
    const copy = observationCopyOf(decrease, format);

    // Assert
    expect(copy.baseline).toBeNull();
    expect(copy.text).toBe("codes.WEIGHT_DECREASE.text");
  });

  it("has no text when a required value is missing, but keeps the title", () => {
    // Arrange
    const missing = observation("1", "WEIGHT_INCREASE", "ex-a", { previousMaxKg: 100 });

    // Act
    const copy = observationCopyOf(missing, format);

    // Assert
    expect(copy.title).toBe("codes.WEIGHT_INCREASE.title");
    expect(copy.text).toBeNull();
    expect(copy.values).toEqual({});
  });

  it("shows the generic title for a code it does not know", () => {
    // Arrange
    const unknown = observation("1", "SOMETHING_NEW", "ex-a", { value: 1 });

    // Act
    const copy = observationCopyOf(unknown, format);

    // Assert
    expect(copy).toEqual({
      title: "unknown.title",
      text: null,
      values: {},
      details: [],
      baseline: null,
    });
  });
});
