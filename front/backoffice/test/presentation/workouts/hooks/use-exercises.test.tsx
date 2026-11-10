import type { ReactNode } from "react";
import { act, renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { Dependencies } from "@weight-tracker/core/application";
import { type Exercise, ok } from "@weight-tracker/core/domain";
import { Container } from "@weight-tracker/core/infrastructure";
import SessionProvider from "@/presentation/auth/components/session-provider";
import ExerciseCatalogProvider from "@/presentation/common/components/exercise-catalog-provider";
import ContainerProvider from "@/presentation/common/context/container-provider";
import useExercises from "@/presentation/workouts/hooks/use-exercises";

function exercise(id: string): Exercise {
  return {
    id,
    freeExerciseDbId: null,
    videoUrl: null,
    muscle: "CHEST",
    bodyPart: "CHEST",
    type: "STRENGTH",
    translations: [{ languageCode: "es", name: id }],
    ownerId: null,
    archivedAtUtc: null,
  };
}

/** The hook under the session's catalog provider, with only the registrations it reads: the session and the catalog. */
function renderExercises(read: () => Promise<ReturnType<typeof ok<Exercise[]>>>) {
  const container = new Container({
    getSessionUseCase: () => ({
      execute: async () => ok({ accessToken: "a", refreshToken: "r" }),
    }),
    getExercisesUseCase: () => ({ execute: read }),
  } as unknown as ConstructorParameters<typeof Container<Dependencies>>[0]);

  return renderHook(() => useExercises(), {
    wrapper: ({ children }: { children: ReactNode }) => (
      <ContainerProvider container={container}>
        <SessionProvider>
          <ExerciseCatalogProvider>{children}</ExerciseCatalogProvider>
        </SessionProvider>
      </ContainerProvider>
    ),
  });
}

describe("useExercises", () => {
  it("lists an added exercise after the ones it read", async () => {
    // Arrange
    const { result } = renderExercises(async () => ok([exercise("a")]));
    await waitFor(() => expect(result.current.status).toBe("loaded"));

    // Act
    act(() => result.current.add(exercise("new")));

    // Assert
    expect(result.current.exercises.map(({ id }) => id)).toEqual(["a", "new"]);
  });

  it("replaces the list, added exercise included, with what a reload reads", async () => {
    // Arrange
    const read = vi
      .fn()
      .mockResolvedValueOnce(ok([exercise("a")]))
      .mockResolvedValue(ok([exercise("a"), exercise("new")]));
    const { result } = renderExercises(read);
    await waitFor(() => expect(result.current.status).toBe("loaded"));
    act(() => result.current.add(exercise("new")));

    // Act
    act(() => result.current.reload());

    // Assert
    await waitFor(() => expect(result.current.status).toBe("loaded"));
    expect(result.current.exercises.map(({ id }) => id)).toEqual(["a", "new"]);
    expect(read).toHaveBeenCalledTimes(2);
  });
});
