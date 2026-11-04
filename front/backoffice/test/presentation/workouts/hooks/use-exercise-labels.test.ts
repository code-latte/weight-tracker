import { act, renderHook } from "@testing-library/react";
import Favorite from "@mui/icons-material/Favorite";
import { afterEach, describe, expect, it } from "vitest";
import i18n from "@/infrastructure/i18n";
import type { Exercise } from "@weight-tracker/core/domain";
import useExerciseLabels, {
  EXERCISE_MUSCLES,
} from "@/presentation/workouts/hooks/use-exercise-labels";

const levels = ["LOW", "MIDLOW", "MID", "MIDHIGH", "HIGH"];

describe("useExerciseLabels", () => {
  afterEach(async () => {
    await act(() => i18n.changeLanguage("es"));
  });

  it.each(["es", "en"])(
    "names every muscle an exercise can be created with in %s",
    async (language) => {
      // Arrange
      await act(() => i18n.changeLanguage(language));
      const { result } = renderHook(() => useExerciseLabels());

      // Act
      const labels = EXERCISE_MUSCLES.map(result.current.muscleLabel);

      // Assert
      expect(labels).toHaveLength(17);
      expect(labels.filter((label, index) => label === EXERCISE_MUSCLES[index])).toEqual([]);
    },
  );

  describe("englishNameOf", () => {
    const exercise = (translations: Exercise["translations"]): Exercise => ({
      id: "ex",
      freeExerciseDbId: null,
      videoUrl: null,
      muscle: "QUADRICEPS",
      bodyPart: "LEGS",
      type: "STRENGTH",
      translations,
      ownerId: null,
      archivedAtUtc: null,
    });

    it("gives the English name when another language is selected", () => {
      // Arrange
      const { result } = renderHook(() => useExerciseLabels());

      // Act
      const name = result.current.englishNameOf(
        exercise([
          { languageCode: "es", name: "Sentadilla" },
          { languageCode: "en", name: "Squat" },
        ]),
      );

      // Assert
      expect(name).toBe("Squat");
    });

    it("gives nothing when English is selected", async () => {
      // Arrange
      await act(() => i18n.changeLanguage("en"));
      const { result } = renderHook(() => useExerciseLabels());

      // Act
      const name = result.current.englishNameOf(
        exercise([
          { languageCode: "es", name: "Sentadilla" },
          { languageCode: "en", name: "Squat" },
        ]),
      );

      // Assert
      expect(name).toBeNull();
    });

    it("gives nothing without an English name or when it equals the name shown", () => {
      // Arrange
      const { result } = renderHook(() => useExerciseLabels());

      // Act
      const names = [
        result.current.englishNameOf(exercise([{ languageCode: "es", name: "Remo" }])),
        result.current.englishNameOf(
          exercise([
            { languageCode: "es", name: "Plank" },
            { languageCode: "en", name: "Plank" },
          ]),
        ),
      ];

      // Assert
      expect(names).toEqual([null, null]);
    });
  });

  describe("searchTextOf", () => {
    const squat: Exercise = {
      id: "ex-squat",
      freeExerciseDbId: null,
      videoUrl: null,
      muscle: "QUADRICEPS",
      bodyPart: "LEGS",
      type: "STRENGTH",
      translations: [
        { languageCode: "es", name: "Sentadilla" },
        { languageCode: "en", name: "Squat" },
      ],
      ownerId: null,
      archivedAtUtc: null,
    };
    const onlySpanish: Exercise = {
      ...squat,
      translations: [{ languageCode: "es", name: "Remo con banda" }],
    };

    it("looks in the Spanish and the English names when Spanish is selected", () => {
      // Arrange
      const { result } = renderHook(() => useExerciseLabels());

      // Act
      const text = result.current.searchTextOf(squat);

      // Assert
      expect(text).toBe("Sentadilla Squat");
    });

    it("looks only in the English name when English is selected", async () => {
      // Arrange
      await act(() => i18n.changeLanguage("en"));
      const { result } = renderHook(() => useExerciseLabels());

      // Act
      const text = result.current.searchTextOf(squat);

      // Assert
      expect(text).toBe("Squat");
    });

    it("looks only in the name shown when the exercise has no English name", () => {
      // Arrange
      const { result } = renderHook(() => useExerciseLabels());

      // Act
      const text = result.current.searchTextOf(onlySpanish);

      // Assert
      expect(text).toBe("Remo con banda");
    });

    it("looks in the name shown, even if it is not English, when English is selected and there is no English name", async () => {
      // Arrange
      await act(() => i18n.changeLanguage("en"));
      const { result } = renderHook(() => useExerciseLabels());

      // Act
      const text = result.current.searchTextOf(onlySpanish);

      // Assert
      expect(text).toBe("Remo con banda");
    });
  });

  it("names the known types and gives each its colour, icon and pastel tone", () => {
    // Arrange & Act
    const { result } = renderHook(() => useExerciseLabels());

    // Assert
    const { typeLabel, typeColor, typeIcon, typeIconColor } = result.current;
    expect(["STRENGTH", "CARDIO", "STRETCHING"].map(typeLabel)).toEqual([
      "Fuerza",
      "Cardio",
      "Estiramiento",
    ]);
    expect(["STRENGTH", "CARDIO", "STRETCHING"].map(typeColor)).toEqual([
      "strength",
      "cardio",
      "stretching",
    ]);
    expect(typeIcon("CARDIO")).toBe(Favorite);
    expect(typeIconColor("CARDIO")).toBe("cardio.pastel");
  });

  it("names an unknown type as it comes, neutral and with no icon", () => {
    // Arrange & Act
    const { result } = renderHook(() => useExerciseLabels());

    // Assert
    expect(result.current.typeLabel("YOGA")).toBe("YOGA");
    expect(result.current.typeColor("YOGA")).toBe("default");
    expect(result.current.typeIcon("YOGA")).toBeUndefined();
    expect(result.current.typeWatermark("YOGA")).toBeUndefined();
    expect(result.current.typeColor("toString")).toBe("default");
  });

  it("reads RIR and RPE the same way in English", async () => {
    // Arrange
    const { result } = renderHook(() => useExerciseLabels());

    // Act
    await act(() => i18n.changeLanguage("en"));

    // Assert
    expect(levels.map((level) => result.current.effortLabel(level, "STRENGTH"))).toEqual([
      "RIR 5+",
      "RIR 4",
      "RIR 3",
      "RIR 1-2",
      "RIR 0",
    ]);
    expect(levels.map((level) => result.current.effortLabel(level, "CARDIO"))).toEqual([
      "RPE 1-2",
      "RPE 3-4",
      "RPE 5-6",
      "RPE 7-8",
      "RPE 9-10",
    ]);
    expect(result.current.typeLabel("STRENGTH")).toBe("Strength");
  });
});
