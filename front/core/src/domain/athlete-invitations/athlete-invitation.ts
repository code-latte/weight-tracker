import type { components } from "../../contract/generated/api";

/** An invitation without its secret. The invited user's id and names are null for an unregistered email. */
export type AthleteInvitation = components["schemas"]["InvitationResponse"];
/** A new invitation with its raw token: the only response that ever carries it. */
export type CreatedAthleteInvitation = components["schemas"]["CreatedInvitationResponse"];
export type InviteAthleteRequest = components["schemas"]["InviteAthleteRequest"];
export type InvitationTokenRequest = components["schemas"]["InvitationTokenRequest"];

/** The invitation id travels in the path; the raw token in the body. */
export type InvitationResponseInput = InvitationTokenRequest & { invitationId: string };
