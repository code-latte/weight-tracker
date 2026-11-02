"use client";

import Chip from "@/presentation/common/components/atoms/chip";

interface InvitationStatusChipProps {
  /** The translated status, such as "Pendiente". */
  label: string;
  /** Pending invitations stand out; the others are settled. */
  pending: boolean;
}

/** A small outlined chip with an invitation's status. */
export default function InvitationStatusChip({ label, pending }: InvitationStatusChipProps) {
  return (
    <Chip label={label} size="small" variant="outlined" color={pending ? "primary" : "default"} />
  );
}
