import { Text } from "react-native";
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
import useSession from "@/presentation/auth/hooks/use-session";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";
import renderWithProviders from "../../../support/render-with-providers";

const session: Session = { accessToken: "access", refreshToken: "refresh" };

const complete: UserProfile = {
  id: "2c3d4e5f-6a7b-4c8d-9e0f-1a2b3c4d5e6f",
  email: "ana@example.com",
  name: "Ana",
  surname: "López",
  role: "USER",
};

function failure(code: DomainErrorCode) {
  return fail(new DomainError(code, "Something failed"));
}

function signedIn(...profiles: Result<UserProfile, DomainError>[]) {
  const execute = jest.fn(async () => profiles.shift() ?? ok(complete));
  return {
    execute,
    dependencies: {
      getSessionUseCase: () => ({ execute: async () => ok(session) }),
      getMyProfileUseCase: () => ({ execute }),
    },
  };
}

function Probe() {
  const { status, errorCode, settled, onboardingPending, reload, setProfile } = useCurrentUser();
  const { reload: reloadSession } = useSession();

  return (
    <>
      <Text>{`status:${status}`}</Text>
      <Text>{`error:${errorCode ?? "none"}`}</Text>
      <Text>{`settled:${settled}`}</Text>
      <Text>{`pending:${onboardingPending}`}</Text>
      <Text onPress={() => void reload()}>reload</Text>
      <Text onPress={() => setProfile(complete)}>complete</Text>
      <Text onPress={() => void reloadSession()}>reloadSession</Text>
    </>
  );
}

describe("CurrentUserProvider", () => {
  it("is idle and reads nothing while signed out", async () => {
    // Arrange
    const execute = jest.fn(async () => ok(complete));

    // Act
    await renderWithProviders(<Probe />, { getMyProfileUseCase: () => ({ execute }) });

    // Assert
    expect(await screen.findByText("status:idle")).toBeOnTheScreen();
    expect(screen.getByText("pending:false")).toBeOnTheScreen();
    expect(execute).not.toHaveBeenCalled();
  });

  it("is loading and not settled while the profile is read", async () => {
    // Arrange
    const dependencies = {
      getSessionUseCase: () => ({ execute: async () => ok(session) }),
      getMyProfileUseCase: () => ({ execute: () => new Promise<never>(() => {}) }),
    };

    // Act
    await renderWithProviders(<Probe />, dependencies);

    // Assert
    expect(await screen.findByText("status:loading")).toBeOnTheScreen();
    expect(screen.getByText("settled:false")).toBeOnTheScreen();
  });

  it("is not pending when the profile is complete", async () => {
    // Arrange
    const { dependencies } = signedIn(ok(complete));

    // Act
    await renderWithProviders(<Probe />, dependencies);

    // Assert
    expect(await screen.findByText("status:loaded")).toBeOnTheScreen();
    expect(screen.getByText("settled:true")).toBeOnTheScreen();
    expect(screen.getByText("pending:false")).toBeOnTheScreen();
  });

  it.each([
    ["an empty name", { name: "" }],
    ["an empty surname", { surname: "" }],
    ["a whitespace-only name", { name: "   " }],
  ])("is pending when the profile has %s", async (_case, change) => {
    // Arrange
    const { dependencies } = signedIn(ok({ ...complete, ...change }));

    // Act
    await renderWithProviders(<Probe />, dependencies);

    // Assert
    expect(await screen.findByText("status:loaded")).toBeOnTheScreen();
    expect(screen.getByText("pending:true")).toBeOnTheScreen();
  });

  it("is pending and holds the code when the profile cannot be read", async () => {
    // Arrange
    const { dependencies } = signedIn(failure("NETWORK"));

    // Act
    await renderWithProviders(<Probe />, dependencies);

    // Assert
    expect(await screen.findByText("status:error")).toBeOnTheScreen();
    expect(screen.getByText("error:NETWORK")).toBeOnTheScreen();
    expect(screen.getByText("settled:true")).toBeOnTheScreen();
    expect(screen.getByText("pending:true")).toBeOnTheScreen();
  });

  it("keeps the error and stays pending while reloading", async () => {
    // Arrange
    const results: Promise<Result<UserProfile, DomainError>>[] = [
      Promise.resolve(failure("NETWORK")),
      new Promise<never>(() => {}),
    ];
    const dependencies = {
      getSessionUseCase: () => ({ execute: async () => ok(session) }),
      getMyProfileUseCase: () => ({
        execute: () => results.shift() ?? Promise.resolve(ok(complete)),
      }),
    };
    await renderWithProviders(<Probe />, dependencies);
    await screen.findByText("status:error");

    // Act
    await fireEvent.press(screen.getByText("reload"));

    // Assert
    expect(screen.getByText("status:error")).toBeOnTheScreen();
    expect(screen.getByText("pending:true")).toBeOnTheScreen();
  });

  it("ends pending when a reload reads a complete profile", async () => {
    // Arrange
    const { dependencies } = signedIn(failure("NETWORK"), ok(complete));
    await renderWithProviders(<Probe />, dependencies);
    await screen.findByText("status:error");

    // Act
    await fireEvent.press(screen.getByText("reload"));

    // Assert
    expect(await screen.findByText("status:loaded")).toBeOnTheScreen();
    expect(screen.getByText("pending:false")).toBeOnTheScreen();
  });

  it("ends pending when a profile is stored", async () => {
    // Arrange
    const { dependencies } = signedIn(ok({ ...complete, name: "" }));
    await renderWithProviders(<Probe />, dependencies);
    await screen.findByText("pending:true");

    // Act
    await fireEvent.press(screen.getByText("complete"));

    // Assert
    expect(await screen.findByText("pending:false")).toBeOnTheScreen();
  });

  it("goes back to idle when the session ends", async () => {
    // Arrange
    const sessions = [ok(session), ok(null)];
    const dependencies = {
      getSessionUseCase: () => ({ execute: async () => sessions.shift() ?? ok(null) }),
      getMyProfileUseCase: () => ({ execute: async () => ok(complete) }),
    };
    await renderWithProviders(<Probe />, dependencies);
    await screen.findByText("status:loaded");

    // Act
    await fireEvent.press(screen.getByText("reloadSession"));

    // Assert
    expect(await screen.findByText("status:idle")).toBeOnTheScreen();
  });

  it("reads through the session, so an unauthorized read is refreshed and retried", async () => {
    // Arrange
    const { execute, dependencies } = signedIn(failure("UNAUTHORIZED"), ok(complete));
    const refresh = jest.fn(async () => ok(session));

    // Act
    await renderWithProviders(<Probe />, {
      ...dependencies,
      refreshSessionUseCase: () => ({ execute: refresh }),
    });

    // Assert
    expect(await screen.findByText("status:loaded")).toBeOnTheScreen();
    await waitFor(() => expect(execute).toHaveBeenCalledTimes(2));
    expect(refresh).toHaveBeenCalledTimes(1);
  });
});
