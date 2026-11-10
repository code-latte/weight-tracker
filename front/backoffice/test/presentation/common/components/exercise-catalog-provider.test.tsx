import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { DomainError, fail, ok } from "@weight-tracker/core/domain";
import type { Exercise, Result } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useExerciseCatalog from "@/presentation/common/hooks/use-exercise-catalog";
import renderWithProviders from "../../../support/render-with-providers";

function exercise(id: string, name: string): Exercise {
  return {
    id,
    freeExerciseDbId: null,
    videoUrl: null,
    muscle: "CHEST",
    bodyPart: "CHEST",
    type: "STRENGTH",
    translations: [{ languageCode: "es", name }],
    ownerId: null,
    archivedAtUtc: null,
  };
}

const bench = exercise("ex-bench", "Press de banca");
const squat = exercise("ex-squat", "Sentadilla");
const row = exercise("ex-row", "Remo con banda");

/** Shows the catalog it reads and offers each of its actions, plus ending the session. */
function CatalogProbe({ name }: { name: string }) {
  const catalog = useExerciseCatalog();
  const { end } = useSession();

  return (
    <section aria-label={name}>
      <span data-testid={`${name}-status`}>{catalog.status}</span>
      <span data-testid={`${name}-names`}>
        {catalog.exercises.map((item) => item.translations[0]?.name).join(", ")}
      </span>
      <span data-testid={`${name}-error`}>{catalog.errorCode ?? ""}</span>
      <button onClick={catalog.load}>{`${name} load`}</button>
      <button onClick={catalog.reload}>{`${name} reload`}</button>
      <button onClick={catalog.refresh}>{`${name} refresh`}</button>
      <button onClick={() => catalog.add(row)}>{`${name} add`}</button>
      <button onClick={end}>{`${name} end`}</button>
    </section>
  );
}

/** A read the test answers when it wants to, to see what happens while it is in flight. */
function deferred() {
  let answer: (result: Result<Exercise[], DomainError>) => void = () => {};
  const promise = new Promise<Result<Exercise[], DomainError>>((resolve) => {
    answer = resolve;
  });

  return { promise, answer };
}

async function renderCatalog(
  reads: (() => Promise<Result<Exercise[], DomainError>>)[],
  probes: string[] = ["probe"],
) {
  // Each read answers with the next result given; any further read repeats the last one.
  const execute = vi.fn(() => (reads.length > 1 ? reads.shift()! : reads[0])());
  await renderWithProviders(
    <>
      {probes.map((name) => (
        <CatalogProbe key={name} name={name} />
      ))}
    </>,
    { getExercisesUseCase: () => ({ execute }) },
  );

  return { execute, user: userEvent.setup() };
}

const statusOf = (name = "probe") => screen.getByTestId(`${name}-status`);
const namesOf = (name = "probe") => screen.getByTestId(`${name}-names`);

describe("ExerciseCatalogProvider", () => {
  it("reads nothing until the catalog is loaded", async () => {
    // Arrange & Act
    const { execute } = await renderCatalog([async () => ok([bench])]);

    // Assert
    expect(statusOf()).toHaveTextContent("idle");
    expect(execute).not.toHaveBeenCalled();
  });

  it("reads the catalog once when it is loaded", async () => {
    // Arrange
    const { execute, user } = await renderCatalog([async () => ok([bench, squat])]);

    // Act
    await user.click(screen.getByRole("button", { name: "probe load" }));

    // Assert
    await waitFor(() => expect(statusOf()).toHaveTextContent("loaded"));
    expect(namesOf()).toHaveTextContent("Press de banca, Sentadilla");
    expect(execute).toHaveBeenCalledTimes(1);
  });

  it("shares one read between consumers that both load it", async () => {
    // Arrange
    const pending = deferred();
    const { execute, user } = await renderCatalog([() => pending.promise], ["first", "second"]);
    await user.click(screen.getByRole("button", { name: "first load" }));
    await user.click(screen.getByRole("button", { name: "second load" }));
    expect(statusOf("second")).toHaveTextContent("loading");

    // Act
    pending.answer(ok([bench]));

    // Assert
    await waitFor(() => expect(namesOf("second")).toHaveTextContent("Press de banca"));
    expect(namesOf("first")).toHaveTextContent("Press de banca");
    expect(execute).toHaveBeenCalledTimes(1);
  });

  it("does not read again when it is loaded a second time", async () => {
    // Arrange
    const { execute, user } = await renderCatalog([async () => ok([bench])]);
    await user.click(screen.getByRole("button", { name: "probe load" }));
    await waitFor(() => expect(statusOf()).toHaveTextContent("loaded"));

    // Act
    await user.click(screen.getByRole("button", { name: "probe load" }));

    // Assert
    expect(execute).toHaveBeenCalledTimes(1);
  });

  it("reads again on reload after a failure", async () => {
    // Arrange
    const { execute, user } = await renderCatalog([
      async () => fail(new DomainError("NETWORK", "")),
      async () => ok([bench]),
    ]);
    await user.click(screen.getByRole("button", { name: "probe load" }));
    await waitFor(() => expect(statusOf()).toHaveTextContent("failed"));
    expect(screen.getByTestId("probe-error")).toHaveTextContent("NETWORK");

    // Act
    await user.click(screen.getByRole("button", { name: "probe reload" }));

    // Assert
    await waitFor(() => expect(statusOf()).toHaveTextContent("loaded"));
    expect(namesOf()).toHaveTextContent("Press de banca");
    expect(screen.getByTestId("probe-error")).toHaveTextContent("");
    expect(execute).toHaveBeenCalledTimes(2);
  });

  it("keeps the catalog shown while a refresh reads it again", async () => {
    // Arrange
    const pending = deferred();
    const { execute, user } = await renderCatalog([async () => ok([bench]), () => pending.promise]);
    await user.click(screen.getByRole("button", { name: "probe load" }));
    await waitFor(() => expect(statusOf()).toHaveTextContent("loaded"));

    // Act
    await user.click(screen.getByRole("button", { name: "probe refresh" }));

    // Assert
    expect(execute).toHaveBeenCalledTimes(2);
    expect(statusOf()).toHaveTextContent("loaded");
    expect(namesOf()).toHaveTextContent("Press de banca");
  });

  it("replaces the catalog when a refresh answers", async () => {
    // Arrange
    const pending = deferred();
    const { user } = await renderCatalog([async () => ok([bench]), () => pending.promise]);
    await user.click(screen.getByRole("button", { name: "probe load" }));
    await waitFor(() => expect(statusOf()).toHaveTextContent("loaded"));
    await user.click(screen.getByRole("button", { name: "probe refresh" }));

    // Act
    pending.answer(ok([bench, squat]));

    // Assert
    await waitFor(() => expect(namesOf()).toHaveTextContent("Press de banca, Sentadilla"));
    expect(statusOf()).toHaveTextContent("loaded");
  });

  it("keeps the catalog and shows no error when a refresh fails", async () => {
    // Arrange
    const { execute, user } = await renderCatalog([
      async () => ok([bench]),
      async () => fail(new DomainError("NETWORK", "")),
    ]);
    await user.click(screen.getByRole("button", { name: "probe load" }));
    await waitFor(() => expect(statusOf()).toHaveTextContent("loaded"));

    // Act
    await user.click(screen.getByRole("button", { name: "probe refresh" }));

    // Assert
    await waitFor(() => expect(execute).toHaveBeenCalledTimes(2));
    expect(statusOf()).toHaveTextContent("loaded");
    expect(namesOf()).toHaveTextContent("Press de banca");
    expect(screen.getByTestId("probe-error")).toHaveTextContent("");
  });

  it("lists an added exercise at once", async () => {
    // Arrange
    const { user } = await renderCatalog([async () => ok([bench])]);
    await user.click(screen.getByRole("button", { name: "probe load" }));
    await waitFor(() => expect(statusOf()).toHaveTextContent("loaded"));

    // Act
    await user.click(screen.getByRole("button", { name: "probe add" }));

    // Assert
    expect(namesOf()).toHaveTextContent("Press de banca, Remo con banda");
  });

  it("reads once more after a read in flight when a refresh is asked for meanwhile", async () => {
    // Arrange
    const first = deferred();
    const { execute, user } = await renderCatalog([
      () => first.promise,
      async () => ok([bench, row]),
    ]);
    await user.click(screen.getByRole("button", { name: "probe load" }));
    await user.click(screen.getByRole("button", { name: "probe refresh" }));
    expect(execute).toHaveBeenCalledTimes(1);

    // Act
    first.answer(ok([bench]));

    // Assert
    await waitFor(() => expect(namesOf()).toHaveTextContent("Press de banca, Remo con banda"));
    expect(execute).toHaveBeenCalledTimes(2);
  });

  it("drops the catalog when the session ends", async () => {
    // Arrange
    const { user } = await renderCatalog([async () => ok([bench])]);
    await user.click(screen.getByRole("button", { name: "probe load" }));
    await waitFor(() => expect(statusOf()).toHaveTextContent("loaded"));

    // Act
    await user.click(screen.getByRole("button", { name: "probe end" }));

    // Assert
    expect(statusOf()).toHaveTextContent("idle");
    expect(namesOf()).toHaveTextContent("");
  });

  it("ignores an answer that arrives after the session ended", async () => {
    // Arrange
    const pending = deferred();
    const { user } = await renderCatalog([async () => ok([bench]), () => pending.promise]);
    await user.click(screen.getByRole("button", { name: "probe load" }));
    await waitFor(() => expect(statusOf()).toHaveTextContent("loaded"));
    await user.click(screen.getByRole("button", { name: "probe refresh" }));
    await user.click(screen.getByRole("button", { name: "probe end" }));

    // Act
    pending.answer(ok([bench, squat]));
    await new Promise((resolve) => setTimeout(resolve, 0));

    // Assert
    expect(statusOf()).toHaveTextContent("idle");
    expect(namesOf()).toHaveTextContent("");
  });
});
