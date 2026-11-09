import { useState } from "react";
import { Text } from "react-native";
import { fireEvent, screen, waitFor } from "@testing-library/react-native";
import { DomainError, fail, ok, type UserProfile } from "@weight-tracker/core/domain";
import OnboardingProvider from "@/presentation/onboarding/context/onboarding-provider";
import useOnboarding from "@/presentation/onboarding/hooks/use-onboarding";
import type { OnboardingStep } from "@/presentation/onboarding/steps";
import renderWithProviders from "../../../support/render-with-providers";

const updated: UserProfile = {
  id: "2c3d4e5f-6a7b-4c8d-9e0f-1a2b3c4d5e6f",
  email: "ana@example.com",
  name: "Ana",
  surname: "López",
  role: "USER",
};

/** Two fake steps: the first owns the name, the second the surname. */
const twoSteps: OnboardingStep[] = [
  {
    id: "first",
    href: "/",
    titleKey: "first",
    validate: (values) => (values.name.trim() ? {} : { name: "required" }),
  },
  {
    id: "second",
    href: "/",
    titleKey: "second",
    validate: (values) => (values.surname.trim() ? {} : { surname: "required" }),
  },
];

function Probe() {
  const { values, errors, errorCode, change, validateStep, finish } = useOnboarding();
  const [valid, setValid] = useState("none");

  return (
    <>
      <Text>{`values:${values.name}|${values.surname}`}</Text>
      <Text>{`errors:${errors.name ?? "-"}|${errors.surname ?? "-"}`}</Text>
      <Text>{`saveError:${errorCode ?? "none"}`}</Text>
      <Text>{`valid:${valid}`}</Text>
      <Text onPress={() => change("name", " Ana ")}>typeName</Text>
      <Text onPress={() => change("surname", "López ")}>typeSurname</Text>
      <Text onPress={() => setValid(String(validateStep("first")))}>validateFirst</Text>
      <Text onPress={() => void finish("second")}>finish</Text>
    </>
  );
}

function renderFlow(dependencies: Parameters<typeof renderWithProviders>[1] = {}) {
  return renderWithProviders(
    <OnboardingProvider steps={twoSteps}>
      <Probe />
    </OnboardingProvider>,
    dependencies,
  );
}

describe("OnboardingProvider", () => {
  it("validates only the fields of the given step", async () => {
    // Arrange
    await renderFlow();
    await fireEvent.press(screen.getByText("typeName"));

    // Act
    await fireEvent.press(screen.getByText("validateFirst"));

    // Assert
    expect(screen.getByText("valid:true")).toBeOnTheScreen();
    expect(screen.getByText("errors:-|-")).toBeOnTheScreen();
  });

  it("keeps the error of an invalid step", async () => {
    // Arrange
    await renderFlow();

    // Act
    await fireEvent.press(screen.getByText("validateFirst"));

    // Assert
    expect(screen.getByText("valid:false")).toBeOnTheScreen();
    expect(screen.getByText("errors:required|-")).toBeOnTheScreen();
  });

  it("keeps the values of every step", async () => {
    // Arrange
    await renderFlow();
    await fireEvent.press(screen.getByText("typeName"));

    // Act
    await fireEvent.press(screen.getByText("typeSurname"));

    // Assert
    expect(screen.getByText("values: Ana |López ")).toBeOnTheScreen();
  });

  it("saves what every step collected, trimmed", async () => {
    // Arrange
    const execute = jest.fn(async () => ok(updated));
    await renderFlow({ updateMyProfileUseCase: () => ({ execute }) });
    await fireEvent.press(screen.getByText("typeName"));
    await fireEvent.press(screen.getByText("typeSurname"));

    // Act
    await fireEvent.press(screen.getByText("finish"));

    // Assert
    await waitFor(() => expect(execute).toHaveBeenCalledWith({ name: "Ana", surname: "López" }));
  });

  it("does not save when the last step is invalid", async () => {
    // Arrange
    const execute = jest.fn(async () => ok(updated));
    await renderFlow({ updateMyProfileUseCase: () => ({ execute }) });
    await fireEvent.press(screen.getByText("typeName"));

    // Act
    await fireEvent.press(screen.getByText("finish"));

    // Assert
    expect(screen.getByText("errors:-|required")).toBeOnTheScreen();
    expect(execute).not.toHaveBeenCalled();
  });

  it("keeps the values and the code when the save fails", async () => {
    // Arrange
    const execute = jest.fn(async () => fail(new DomainError("NETWORK", "No connection")));
    await renderFlow({ updateMyProfileUseCase: () => ({ execute }) });
    await fireEvent.press(screen.getByText("typeName"));
    await fireEvent.press(screen.getByText("typeSurname"));

    // Act
    await fireEvent.press(screen.getByText("finish"));

    // Assert
    expect(await screen.findByText("saveError:NETWORK")).toBeOnTheScreen();
    expect(screen.getByText("values: Ana |López ")).toBeOnTheScreen();
  });
});
