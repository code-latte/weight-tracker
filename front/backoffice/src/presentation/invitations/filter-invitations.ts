import type { AthleteInvitation } from "@weight-tracker/core/domain";
import { fullNameOf } from "@/presentation/common/full-name";
import { normalized } from "@/presentation/common/search-text";

/** The trainer's full name of an invitation, or `null` when the trainer has neither name nor surname. */
export function trainerNameOf(invitation: AthleteInvitation): string | null {
  return fullNameOf({ name: invitation.trainerName, surname: invitation.trainerSurname });
}

/**
 * The invitations the table shows: those of the selected status, if any, whose invited email contains the search
 * text, or also the trainer's full name when `byTrainer` is set. A view filter over the loaded list; it sends nothing
 * and keeps the API's order.
 */
export function filterInvitations(
  invitations: AthleteInvitation[],
  search: string,
  status: string | null,
  { byTrainer }: { byTrainer: boolean },
): AthleteInvitation[] {
  const text = normalized(search.trim());

  return invitations.filter((invitation) => {
    const fields = byTrainer
      ? [invitation.inviteEmail, trainerNameOf(invitation) ?? ""]
      : [invitation.inviteEmail];

    return (
      (status === null || invitation.status === status) &&
      (text === "" || fields.some((field) => normalized(field).includes(text)))
    );
  });
}
