# Tasks

`core` is unchanged: no domain alias, port, repository, use case or `container.ts` registration. All work is in
`backoffice/`.

## 1. Review helpers

- [x] 1.1 Create `src/presentation/workouts/workout-review.ts` with `isReviewStatus`, `plannedSetsOf`,
      `countedRecordsOf`, `exerciseStateOf`, `reviewSummaryOf`, `plannedRowsOf`, `initiallyExpandedOf` and `restOf`,
      as in design.md. Verify by typecheck.
- [x] 1.2 Add `test/presentation/workouts/workout-review.test.ts`, mirroring `week.test.ts` and using Arrange / Act /
      Assert. - It covers string and number counts, a discarded extra record, the three states (including an exercise with
      no planned sets), the summary of the spec's example (4 / 10, 1 / 3), the percentage with 0 planned, volume
      exclusions (timed, weightless, discarded, no per-side doubling), row pairing across set groups, all three
      initial-expansion cases, and `restOf`.

      Verify with `pnpm --filter backoffice test workout-review`.

## 2. Atoms

- [x] 2.1 Add `tabs.tsx`, `tab.tsx` and `linear-progress.tsx` to `src/presentation/common/components/atoms`, as thin
      MUI wrappers modelled on `collapse.tsx`. Verify by typecheck and lint.

## 3. Translations

- [x] 3.1 Add the `review.*` keys to `src/i18n/es/workouts.json` and `src/i18n/en/workouts.json`: - title, live, startedAt, notAvailable, lastUpdate; - stats._, progress._, tabs._; - exercise.completed, exercise.inProgress, exercise.pending; - table._, target._, done._; - expand, collapse.

      Reuse `workout.back`, `person.role.athlete`, `exercise.effortLine` and the status copy. Verify the
      translation parity test passes.

## 4. Molecules

- [x] 4.1 Add `components/molecules/review-stat-card.tsx` (icon, label, value, outlined card). Verify by typecheck.
- [x] 4.2 Add `components/molecules/session-progress-card.tsx`, which shows the title, the percentage, a
      `LinearProgress`, the sets done and the sets pending. Verify by typecheck.
- [x] 4.3 Add `components/molecules/review-sets-table.tsx` from `plannedRowsOf`, following the table usage in
      `workouts-table.tsx`. - It has the effort line (hidden when the effort is empty), the four columns, the target wording, and a
      "Realizado" cell with a done mark and a success tint or "Pendiente". - "Descanso" shows `m:ss` or "—".

      Verify by typecheck.

## 5. Organisms

- [x] 5.1 Add `components/organisms/workout-review-header.tsx`, with the back link, title, subtitle, a status chip
      through `workoutStatusColorOf`, the outlined red live chip with a slow fade for `STARTED`, and a "Guardar como plantilla"
      button (disabled while saving). Verify by typecheck.
- [x] 5.2 Add `components/organisms/workout-review-person-strip.tsx`, modelled on `workout-context-strip.tsx`. - It shows the avatar, the name, "Atleta", the trainer when given, "Inicio: —", and "Última actualización"
      via `Intl.RelativeTimeFormat`, hidden when `resultsUpdatedAt` is null.

      Verify by typecheck.

- [x] 5.3 Add `components/organisms/review-exercise-card.tsx`. - It shows the state icon, the position, the name, the chips from `exerciseLabelsOf` and the state chip. - It has an expand/collapse `IconButton` labelled "Expandir" or "Contraer", and a `Collapse` with
      `ReviewSetsTable`.

      Verify by typecheck.

- [x] 5.4 Add `components/organisms/workout-review-tabs.tsx`, with three tabs; "Ejercicios" renders its children and
      the other two render empty panels. It has no "Expandir todo" and no menu. Verify by typecheck.

## 6. View and branch

- [x] 6.1 Add `views/workout-review-view.tsx` (`WorkoutReview`). - It composes the header, the template outcome alerts (via `useSaveWorkoutAsTemplate`), the strip (trainer for `ADMIN` only via `useCurrentUser`), the four stat cards
      ("Tiempo transcurrido" shows "—"), the progress card, the tabs and the exercise cards. - It holds the selected tab and the expanded ids, initialised from `initiallyExpandedOf`. - Volume uses `Intl.NumberFormat` with `useGrouping: "always"`.

      Verify by typecheck.

- [x] 6.2 In `views/workout-view.tsx`, return `<WorkoutReview>` from `WorkoutEditor` once the workout is loaded and
      `isReviewStatus` holds, before the catalog guards. Leave the editor and its locked mode untouched. Verify by
      typecheck.
- [x] 6.3 In `test/presentation/workouts/views/workout-view.test.tsx`, update the editor tests that open `STARTED`,
      `COMPLETED` or `REVIEWED` workouts (the locked-editor block around the `it.each` on those statuses). - Assertions that belonged to the removed requirement are removed. - Editor coverage stays on `DRAFT` and `PUBLISHED`.

      Verify that the file passes.

- [x] 6.4 Add a `describe("review")` block to `workout-view.test.tsx` on its existing `setup` and fakes, using
      Arrange / Act / Assert. It covers every scenario in `specs/backoffice-workout-review/spec.md` and the branch
      scenarios in `specs/backoffice-workout-editor/spec.md`: - the branch per status, including an unknown status; - a catalog failure that does not block the review; - the header and subtitle, placed and standalone; - the live chip present and absent; - the strip with and without `resultsUpdatedAt`; - the trainer shown to `ADMIN` and hidden from `TRAINER`; - the summary figures and volume; - the progress, including 0 planned; - the tabs and their empty panels; - the card content and toggling, and the initial expansion; - the effort line shown and hidden; - the rows, targets, "Realizado" variants, a discarded record and "Descanso"; - no edit actions, but "Guardar como plantilla" shown, creating one template, and its refusal; - English.

      Verify with `pnpm --filter backoffice test workout-view`.

## 7. Review and verification

- [x] 7.1 Review the backoffice result against its mapped references. - the view against `workout-view.tsx`; - the strip against `workout-context-strip.tsx`; - the card chips against `exercise-list-item.tsx`; - the table against `workouts-table.tsx`; - the atoms against existing atoms.

      Also confirm that only atoms import MUI widgets, that there are no imports from `app`, and that
      `.stpr/template/` has no drift (the `cmp` loop in `AGENTS.md`). Verify that the review notes nothing
      outstanding.

- [x] 7.2 Run `pnpm typecheck && pnpm lint && pnpm test` from `front/` and verify that it passes.
- [x] 7.3 Manually open a `STARTED` and a `COMPLETED` workout with `pnpm dev:backoffice` in light and dark mode.
      Verify: - the layout matches `brand/workout_review.png`, without the excluded pieces; - the cards wrap on a narrow window.

## Workflow follow-up

- Archive the change with `/opsx:archive` after review.
