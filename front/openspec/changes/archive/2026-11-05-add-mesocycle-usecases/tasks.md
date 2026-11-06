# Tasks

## 1. Contract snapshot

- [x] 1.1 Start the API (Debug API, port 5199) from a branch containing `add-mesocycles-usecases-endpoints`, run
      `pnpm --filter @weight-tracker/core openapi:refresh` and `openapi:generate`; verify `core/openapi/v1.json` lists
      the seven mesocycle operations and the placement pair on `CreateWorkoutRequest`, `UpdateWorkoutRequest` and
      `WorkoutResponse`, with no other unexpected diff (stop the change if not)
- [x] 1.2 Run `pnpm typecheck` from `front/` and verify it passes on the regenerated types alone, then commit the
      snapshot and generated types as their own step

## 2. Core domain and port

- [x] 2.1 Add `core/src/domain/mesocycles/mesocycle.ts` with the aliases and input types of design decision 2, all over
      `components["schemas"][...]`, and re-export it from `core/src/domain/index.ts`; verify with `pnpm typecheck`
- [x] 2.2 Add `core/src/application/ports/mesocycle-repository-interface.ts` with the seven methods of design decision 3
      and export it from the ports barrel; verify with `pnpm typecheck`

## 3. Core repository

- [x] 3.1 Add `core/src/infrastructure/repositories/mesocycle-repository.ts` following `workout-repository.ts`, and
      export it from the repositories barrel; verify with `pnpm typecheck`
- [x] 3.2 Add `core/test/infrastructure/repositories/mesocycle-repository.test.ts` following `auth-repository.test.ts`
      and `workout-repository.test.ts`: method, path and body per operation (path keys not in the body), success
      returns the response, every status the contract lists per operation mapped to its code, `500` -> `UNEXPECTED`,
      network failure -> `NETWORK`, with labelled Arrange / Act / Assert; verify with
      `pnpm --filter @weight-tracker/core test`
- [x] 3.3 Extend `core/test/infrastructure/repositories/workout-repository.test.ts`: `create` and `update` send the
      placement pair unchanged, and an omitted pair is absent from the body; verify with
      `pnpm --filter @weight-tracker/core test`

## 4. Core use cases

- [x] 4.1 Add the seven use cases of design decision 4 under `core/src/application/use-cases/mesocycles/`, with their
      `index.ts`, re-exported from `use-cases/index.ts`; verify with `pnpm typecheck`
- [x] 4.2 Add one test per use case under `core/test/application/use-cases/mesocycles/`, following
      `signup-use-case.test.ts` and the workout use case tests: input forwarded unchanged (including an omitted
      `trainerId` on create), path keys split from the body, success returned, repository failure returned unchanged;
      verify with `pnpm --filter @weight-tracker/core test`
- [x] 4.3 Extend `create-workout-use-case.test.ts` and `update-workout-use-case.test.ts`: the placement pair is
      forwarded unchanged, an omitted pair is not added, the response pair is returned; verify with
      `pnpm --filter @weight-tracker/core test`
- [x] 4.4 Add the eight keys of design decision 6 to `core/src/application/dependencies.ts`; verify with
      `pnpm typecheck` (fails in both fronts until group 5)
- [x] 4.5 Review core against its references (`auth.ts`, `auth-repository*.ts`, `workout-repository.ts`, the workout
      use cases and their tests) and the drift check in `AGENTS.md`; verify no deviation and no drift

## 5. Backoffice wiring

- [x] 5.1 Register the eight keys in `backoffice/src/infrastructure/container.ts` after the workout entries; verify
      with `pnpm --filter backoffice typecheck`
- [x] 5.2 Add the placeholders in `backoffice/test/support/render-with-providers.tsx` and the new keys in
      `backoffice/test/infrastructure/container.test.ts`; verify with `pnpm --filter backoffice test`
- [x] 5.3 Review the backoffice diff against the existing container and helper structure; verify only registrations
      and placeholders changed

## 6. App wiring

- [x] 6.1 Register the eight keys in `app/src/infrastructure/container.ts` after the workout entries; verify with
      `pnpm --filter app typecheck`
- [x] 6.2 Add the placeholders in `app/test/support/render-with-providers.tsx` and the new keys in
      `app/test/infrastructure/container.test.ts`; verify with `pnpm --filter app test`
- [x] 6.3 Review the app diff against the existing container and helper structure; verify only registrations and
      placeholders changed

## 7. Integration check

- [x] 7.1 Run `pnpm typecheck && pnpm lint && pnpm test` from `front/` and verify it passes (no E2E flow is planned)
