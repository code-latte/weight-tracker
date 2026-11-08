import { useCallback, useEffect, useState } from "react";
import type { ContainerInterface, Dependencies } from "@weight-tracker/core/application";
import type { DomainErrorCode, UserProfile } from "@weight-tracker/core/domain";
import useContainer from "@/presentation/common/hooks/use-container";

type ProfileState =
  | { status: "loading" }
  | { status: "ready"; profile: UserProfile; trainer: UserProfile | null }
  | { status: "error"; errorCode: DomainErrorCode };

/** Reads the profile, then the trainer. Either failure leaves no partial data. */
async function readProfile(container: ContainerInterface<Dependencies>): Promise<ProfileState> {
  const profileResult = await container.resolve("getMyProfileUseCase").execute();
  if (!profileResult.ok) {
    return { status: "error", errorCode: profileResult.error.code };
  }

  const trainerResult = await container
    .resolve("getUserTrainerUseCase")
    .execute(profileResult.value.id);
  if (!trainerResult.ok) {
    return { status: "error", errorCode: trainerResult.error.code };
  }

  return { status: "ready", profile: profileResult.value, trainer: trainerResult.value };
}

/** The signed-in athlete's profile and trainer, read when the screen mounts and again on retry. */
export default function useProfile() {
  const container = useContainer();
  const [state, setState] = useState<ProfileState>({ status: "loading" });

  useEffect(() => {
    let active = true;

    void readProfile(container).then((next) => {
      if (active) {
        setState(next);
      }
    });

    return () => {
      active = false;
    };
  }, [container]);

  const retry = useCallback(() => {
    setState({ status: "loading" });
    void readProfile(container).then(setState);
  }, [container]);

  return { state, retry };
}
