import { useEffect, useMemo, useRef, useState, type RefObject } from "react";
import { arrayMove } from "@dnd-kit/sortable";
import type { Exercise, SuperSeriesId, WorkoutExerciseDetail } from "@weight-tracker/core/domain";
import {
  unitsOf,
  type WorkoutExerciseItem,
} from "@/presentation/workouts/components/organisms/workout-exercises-list";
import type { ExercisesOwner } from "@/presentation/workouts/exercises-owner";
import { singleMuscleBodyPartsOf } from "@/presentation/workouts/catalog";
import useExerciseCatalogFilters from "@/presentation/workouts/hooks/use-exercise-catalog-filters";
import useRemoveWorkoutExercise from "@/presentation/workouts/hooks/use-remove-workout-exercise";
import useReorderWorkoutExercises from "@/presentation/workouts/hooks/use-reorder-workout-exercises";
import useSuperSeries from "@/presentation/workouts/hooks/use-super-series";

/** The dialog open over the screen. Only one at a time: choosing from the catalog hands over to the configuration. */
export type OpenDialog =
  | { kind: "none" }
  | { kind: "catalog" }
  | { kind: "configure"; item: WorkoutExerciseItem; adding: boolean };

/**
 * A catalog exercise chosen to be added: no effort, no rest, not per side and no sets yet; they are defined in the
 * configuration dialog.
 */
function newItemOf(exercise: Exercise): WorkoutExerciseItem {
  return {
    workoutExercise: {
      id: `new-${exercise.id}`,
      exerciseId: exercise.id,
      effort: "",
      perSide: false,
      restMinutes: null,
      sets: [],
      superSeriesId: null,
      setRecords: [],
    },
    exercise,
  };
}

/** The entries sorted by their place in `ids`; any entry not in `ids` goes last, in its current order. */
function inOrderOf(entries: WorkoutExerciseDetail[], ids: string[]) {
  const placeOf = (entry: WorkoutExerciseDetail) => {
    const index = ids.indexOf(entry.id);
    return index === -1 ? ids.length : index;
  };
  return [...entries].sort((a, b) => placeOf(a) - placeOf(b));
}

function withoutId(ids: ReadonlySet<string>, id: string): ReadonlySet<string> {
  const next = new Set(ids);
  next.delete(id);
  return next;
}

export interface ExercisesEditorState {
  owner: ExercisesOwner;
  ownerId: string;
  exercises: Exercise[];
  items: WorkoutExerciseItem[];
  /** The catalog without the exercises already saved: one cannot be added twice. */
  available: Exercise[];
  singleMuscleBodyParts: ReturnType<typeof singleMuscleBodyPartsOf>;
  filters: ReturnType<typeof useExerciseCatalogFilters>;
  dialog: OpenDialog;
  selectedIds: ReadonlySet<string>;
  breakTarget: SuperSeriesId | null;
  addButtonRef: RefObject<HTMLButtonElement | null>;
  removal: ReturnType<typeof useRemoveWorkoutExercise>;
  reordering: ReturnType<typeof useReorderWorkoutExercises>;
  superSeries: ReturnType<typeof useSuperSeries>;
  openCatalog: () => void;
  openEdit: (item: WorkoutExerciseItem) => void;
  choose: (exercise: Exercise) => void;
  closeDialog: () => void;
  handleExerciseSaved: (entry: WorkoutExerciseDetail, adding: boolean) => void;
  handleRemove: (item: WorkoutExerciseItem) => Promise<void>;
  handleToggleSelected: (id: string) => void;
  handleGroup: () => Promise<void>;
  setBreakTarget: (superSeriesId: SuperSeriesId | null) => void;
  handleBreak: (superSeriesId: SuperSeriesId) => Promise<void>;
  handleReorder: (activeId: string, overId: string) => Promise<void>;
}

/**
 * The exercise list of a workout or a template as it is edited: the saved entries, kept up to date with what each
 * request answers, the open dialog, the superseries selection and every exercise action. The requests go to the
 * owner's use cases; the rules stay the API's.
 */
export default function useExercisesEditor(
  owner: ExercisesOwner,
  ownerId: string,
  initialEntries: WorkoutExerciseDetail[],
  exercises: Exercise[],
): ExercisesEditorState {
  const filters = useExerciseCatalogFilters();
  const [dialog, setDialog] = useState<OpenDialog>({ kind: "none" });
  // The saved exercises, kept up to date with what each exercise save answers.
  const [savedExercises, setSavedExercises] = useState<WorkoutExerciseDetail[]>(initialEntries);
  const removal = useRemoveWorkoutExercise(owner);
  const reordering = useReorderWorkoutExercises(owner);
  const superSeries = useSuperSeries(owner);
  // The entries checked to be grouped in a superseries.
  const [selectedIds, setSelectedIds] = useState<ReadonlySet<string>>(new Set());
  // The superseries whose break is being confirmed.
  const [breakTarget, setBreakTarget] = useState<SuperSeriesId | null>(null);
  const addButtonRef = useRef<HTMLButtonElement>(null);
  // The button that started an add flow is gone once it ends, so the focus goes back to Añadir ejercicio.
  const addFlowEnded = useRef(false);

  useEffect(() => {
    if (dialog.kind === "none" && addFlowEnded.current) {
      addFlowEnded.current = false;
      addButtonRef.current?.focus();
    }
  }, [dialog]);

  const catalogById = useMemo(
    () => new Map<string, Exercise>(exercises.map((exercise) => [exercise.id, exercise])),
    [exercises],
  );
  const singleMuscleBodyParts = useMemo(() => singleMuscleBodyPartsOf(exercises), [exercises]);
  const items = useMemo<WorkoutExerciseItem[]>(
    () =>
      savedExercises.map((workoutExercise) => ({
        workoutExercise,
        exercise: catalogById.get(workoutExercise.exerciseId),
      })),
    [savedExercises, catalogById],
  );
  const available = useMemo(() => {
    const saved = new Set(savedExercises.map((entry) => entry.exerciseId));
    return exercises.filter((exercise) => !saved.has(exercise.id));
  }, [exercises, savedExercises]);

  function closeDialog() {
    if (dialog.kind === "configure" && dialog.adding) {
      addFlowEnded.current = true;
    }
    setDialog({ kind: "none" });
  }

  /** An added exercise joins the end of the list; an edited one replaces its entry. Either way the dialog closes. */
  function handleExerciseSaved(entry: WorkoutExerciseDetail, adding: boolean) {
    if (adding) {
      setSavedExercises((current) => [...current, entry]);
    } else {
      setSavedExercises((current) =>
        current.map((other) => (other.exerciseId === entry.exerciseId ? entry : other)),
      );
    }
    closeDialog();
  }

  /** The exercise is removed through the API; on success it returns to the catalog. */
  async function handleRemove(item: WorkoutExerciseItem) {
    const { id, exerciseId } = item.workoutExercise;

    if (await removal.remove(ownerId, id, exerciseId)) {
      setSavedExercises((current) => current.filter((other) => other.id !== id));
      setSelectedIds((current) => withoutId(current, id));
    }
  }

  function handleToggleSelected(id: string) {
    setSelectedIds((current) =>
      current.has(id) ? withoutId(current, id) : new Set(current).add(id),
    );
  }

  /**
   * The checked exercises are grouped through the API. On success they are brought together: the first keeps its place
   * and the others follow it, in their order, and that order is sent. A refused order is put back by entry id; the
   * superseries stays.
   */
  async function handleGroup() {
    const members = savedExercises.filter((entry) => selectedIds.has(entry.id));
    const superSeriesId = await superSeries.create(
      ownerId,
      members.map((entry) => entry.id),
    );
    if (superSeriesId === null) {
      return;
    }

    const memberIds = new Set(members.map((entry) => entry.id));
    const first = savedExercises.findIndex((entry) => memberIds.has(entry.id));
    const previousIds = savedExercises.map((entry) => entry.id);
    const next = [
      ...savedExercises.slice(0, first),
      ...members,
      ...savedExercises.slice(first).filter((entry) => !memberIds.has(entry.id)),
    ].map((entry) => (memberIds.has(entry.id) ? { ...entry, superSeriesId } : entry));
    setSavedExercises(next);
    setSelectedIds(new Set());

    if (next.every((entry, index) => entry.id === previousIds[index])) {
      return;
    }
    if (
      !(await reordering.reorder(
        ownerId,
        next.map((entry) => entry.exerciseId),
      ))
    ) {
      setSavedExercises((current) => inOrderOf(current, previousIds));
    }
  }

  /** The confirmed superseries is broken through the API; on success its members stay where they are, ungrouped. */
  async function handleBreak(superSeriesId: SuperSeriesId) {
    const broken = await superSeries.breakUp(ownerId, superSeriesId);
    setBreakTarget(null);

    if (broken) {
      setSavedExercises((current) =>
        current.map((entry) =>
          String(entry.superSeriesId) === String(superSeriesId)
            ? { ...entry, superSeriesId: null }
            : entry,
        ),
      );
    }
  }

  /**
   * The dropped unit, an exercise or a whole superseries, moves at once and the full saved order is sent. A refused
   * order is put back by entry id, so an exercise saved or removed while the request was in flight is kept as it is.
   */
  async function handleReorder(activeId: string, overId: string) {
    const units = unitsOf(savedExercises);
    const from = units.findIndex((unit) => unit.id === activeId);
    const to = units.findIndex((unit) => unit.id === overId);
    if (from === -1 || to === -1 || from === to) {
      return;
    }

    const previousIds = savedExercises.map((entry) => entry.id);
    const next = arrayMove(units, from, to).flatMap(({ start, length }) =>
      savedExercises.slice(start, start + length),
    );
    setSavedExercises(next);

    if (
      !(await reordering.reorder(
        ownerId,
        next.map((entry) => entry.exerciseId),
      ))
    ) {
      setSavedExercises((current) => inOrderOf(current, previousIds));
    }
  }

  return {
    owner,
    ownerId,
    exercises,
    items,
    available,
    singleMuscleBodyParts,
    filters,
    dialog,
    selectedIds,
    breakTarget,
    addButtonRef,
    removal,
    reordering,
    superSeries,
    openCatalog: () => setDialog({ kind: "catalog" }),
    openEdit: (item) => setDialog({ kind: "configure", item, adding: false }),
    choose: (exercise) => setDialog({ kind: "configure", item: newItemOf(exercise), adding: true }),
    closeDialog,
    handleExerciseSaved,
    handleRemove,
    handleToggleSelected,
    handleGroup,
    setBreakTarget,
    handleBreak,
    handleReorder,
  };
}
