import type { components } from "../../contract/generated/api";

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
