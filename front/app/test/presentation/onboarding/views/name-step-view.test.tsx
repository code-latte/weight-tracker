import { fireEvent, screen, waitFor } from "@testing-library/react-native";
import {
  DomainError,
  type DomainErrorCode,
  fail,
  ok,
  type Result,
  type Session,
  type UserProfile,
} from "@weight-tracker/core/domain";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";
import OnboardingProvider from "@/presentation/onboarding/context/onboarding-provider";
import NameStepView from "@/presentation/onboarding/views/name-step-view";
import { Text } from "react-native";
import renderWithProviders from "../../../support/render-with-providers";

jest.mock("expo-router", () => ({
  router: { push: jest.fn(), back: jest.fn() },
}));

// Paper's icons load their font through expo-font, which has no assets under Jest.
jest.mock("expo-font", () => ({
  ...jest.requireActual("expo-font"),
  isLoaded: () => true,
  loadAsync: async () => {},
}));

const session: Session = { accessToken: "access", refreshToken: "refresh" };

const incomplete: UserProfile = {
  id: "2c3d4e5f-6a7b-4c8d-9e0f-1a2b3c4d5e6f",
  email: "ana@example.com",
  name: "Ana",
  surname: "",
  role: "USER",
};

function failure(code: DomainErrorCode) {
  return fail(new DomainError(code, "Something failed"));
}

interface SetupOptions {
  profiles?: Result<UserProfile, DomainError>[];
  updateResult?: () => Promise<Result<UserProfile, DomainError>>;
}

function setup({
  profiles = [ok(incomplete)],
  updateResult = async () => ok({ ...incomplete, surname: "López" }),
}: SetupOptions = {}) {
  const getProfile = jest.fn(async () => profiles.shift() ?? ok(incomplete));
  const update = jest.fn(updateResult);
  const logout = jest.fn(async () => ok(undefined));
  const dependencies = {
    getSessionUseCase: () => ({ execute: async () => ok(session) }),
    getMyProfileUseCase: () => ({ execute: getProfile }),
    updateMyProfileUseCase: () => ({ execute: update }),
    logoutUseCase: () => ({ execute: logout }),
  };

  return { getProfile, update, logout, dependencies };
}

/** Shows whether the gate would still keep the athlete in onboarding. */
function PendingProbe() {
  const { onboardingPending } = useCurrentUser();
  return <Text>{`pending:${onboardingPending}`}</Text>;
}

async function renderStep(dependencies: ReturnType<typeof setup>["dependencies"]) {
  await renderWithProviders(
    <OnboardingProvider>
      <NameStepView />
      <PendingProbe />
    </OnboardingProvider>,
    dependencies,
  );
}

describe("NameStepView", () => {
  it("shows the progress, the title and only Finalizar", async () => {
    // Arrange & Act
    const { dependencies } = setup();
    await renderStep(dependencies);

    // Assert
    expect(await screen.findByText("Paso 1 de 1")).toBeOnTheScreen();
    expect(screen.getByRole("heading", { name: "Dinos cómo te llamas" })).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Finalizar" })).toBeOnTheScreen();
    expect(screen.queryByRole("button", { name: "Siguiente" })).not.toBeOnTheScreen();
    expect(screen.queryByLabelText("Volver")).not.toBeOnTheScreen();
  });

  it("pre-fills the fields with the current profile", async () => {
    // Arrange & Act
    const { dependencies } = setup();
    await renderStep(dependencies);

    // Assert
    expect(await screen.findByDisplayValue("Ana")).toBeOnTheScreen();
    expect(screen.getByLabelText("Apellidos").props.value).toBe("");
  });

  it.each([
    ["Nombre", "Al", "Debe tener al menos 3 caracteres."],
    ["Apellidos", "", "Este campo es obligatorio."],
    ["Apellidos", "x".repeat(101), "No puede superar los 100 caracteres."],
  ])("shows the error of %s for %j and sends no update", async (field, value, message) => {
    // Arrange
    const { update, dependencies } = setup();
    await renderStep(dependencies);
    await screen.findByDisplayValue("Ana");
    await fireEvent.changeText(screen.getByLabelText(field), value);

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Finalizar" }));

    // Assert
    expect(screen.getByText(message)).toBeOnTheScreen();
    expect(update).not.toHaveBeenCalled();
  });

  it("sends the trimmed values and ends onboarding", async () => {
    // Arrange
    const { update, dependencies } = setup();
    await renderStep(dependencies);
    await screen.findByDisplayValue("Ana");
    await fireEvent.changeText(screen.getByLabelText("Nombre"), " Ana ");
    await fireEvent.changeText(screen.getByLabelText("Apellidos"), "López ");

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Finalizar" }));

    // Assert
    await waitFor(() => expect(update).toHaveBeenCalledWith({ name: "Ana", surname: "López" }));
    expect(await screen.findByText("pending:false")).toBeOnTheScreen();
  });

  it("disables Finalizar while the update is pending", async () => {
    // Arrange
    const { dependencies } = setup({ updateResult: () => new Promise<never>(() => {}) });
    await renderStep(dependencies);
    await screen.findByDisplayValue("Ana");
    await fireEvent.changeText(screen.getByLabelText("Apellidos"), "López");

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Finalizar" }));

    // Assert
    expect(screen.getByRole("button", { name: "Finalizar" })).toBeDisabled();
  });

  it("keeps the values and shows the message when the update fails", async () => {
    // Arrange
    const { dependencies } = setup({ updateResult: async () => failure("NETWORK") });
    await renderStep(dependencies);
    await screen.findByDisplayValue("Ana");
    await fireEvent.changeText(screen.getByLabelText("Apellidos"), "López");

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Finalizar" }));

    // Assert
    expect(await screen.findByText("No hay conexión con el servidor.")).toBeOnTheScreen();
    expect(screen.getByDisplayValue("López")).toBeOnTheScreen();
    expect(screen.getByText("pending:true")).toBeOnTheScreen();
  });

  it("shows the error and Reintentar, and no fields, when the profile cannot be read", async () => {
    // Arrange & Act
    const { dependencies } = setup({ profiles: [failure("NETWORK")] });
    await renderStep(dependencies);

    // Assert
    expect(await screen.findByText("No hay conexión con el servidor.")).toBeOnTheScreen();
    expect(screen.getByRole("button", { name: "Reintentar" })).toBeOnTheScreen();
    expect(screen.queryByLabelText("Nombre")).not.toBeOnTheScreen();
    expect(screen.queryByRole("button", { name: "Finalizar" })).not.toBeOnTheScreen();
  });

  it("reads the profile again on Reintentar and shows the pre-filled step", async () => {
    // Arrange
    const { getProfile, dependencies } = setup({ profiles: [failure("NETWORK"), ok(incomplete)] });
    await renderStep(dependencies);
    await screen.findByRole("button", { name: "Reintentar" });

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Reintentar" }));

    // Assert
    expect(await screen.findByDisplayValue("Ana")).toBeOnTheScreen();
    expect(getProfile).toHaveBeenCalledTimes(2);
  });

  it("logs out from the step", async () => {
    // Arrange
    const { logout, dependencies } = setup();
    await renderStep(dependencies);
    await screen.findByDisplayValue("Ana");

    // Act
    await fireEvent.press(screen.getByRole("button", { name: "Cerrar sesión" }));

    // Assert
    await waitFor(() => expect(logout).toHaveBeenCalledTimes(1));
  });
});
