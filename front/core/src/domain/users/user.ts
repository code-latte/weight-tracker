import type { components } from "../../contract/generated/api";

/** The sets a user recorded the last time they finished a workout with an exercise, with that workout's date and effort. */
export type ExerciseLastPerformance = components["schemas"]["ExerciseLastPerformanceResponse"];
/** One set of an {@link ExerciseLastPerformance}: reps or seconds, and the weight in kg, `null` when not tracked. */
export type LastPerformanceSet = components["schemas"]["LastPerformanceSetResponse"];
/** One session in which a user did an exercise: the workout's date and the session's max weight, volume and estimated one-rep max. */
export type ExerciseHistorySession = components["schemas"]["ExerciseHistorySessionResponse"];
/** The user and the catalog exercise travel in the path; `limit` (1 to 50) is the number of latest sessions to read. */
export type GetUserExerciseHistoryInput = { userId: string; exerciseId: string; limit: number };
export type UserProfile = components["schemas"]["UserProfileResponse"];
/**
 * One user in the list: the profile, whether the account is active and since when it is not, and their current
 * trainer (`null` when they have none).
 */
export type UserSummary = components["schemas"]["UserListItemResponse"];
/** A user as an activation change answers it: the list fields without the trainer. */
export type UserActivationSummary = components["schemas"]["UserSummaryResponse"];
export type UpdateProfileRequest = components["schemas"]["UpdateProfileRequest"];
export type CreateUserRequest = components["schemas"]["CreateUserRequest"];
export type CreatedUserResponse = components["schemas"]["CreatedUserResponse"];
