"use client";

import Box from "@mui/material/Box";
import Avatar from "@/presentation/common/components/atoms/avatar";
import Card from "@/presentation/common/components/atoms/card";
import Typography from "@/presentation/common/components/atoms/typography";

export interface PersonCardProps {
  /** The full name, or the email when the person has no name. */
  name: string;
  /** "AG". */
  initials: string;
  /** What the person is here, such as "Atleta". */
  role: string;
}

/** A person as a small card sized to its content: their avatar, their name and their role. */
export default function PersonCard({ name, initials, role }: PersonCardProps) {
  return (
    <Card
      variant="outlined"
      sx={{ display: "inline-flex", alignItems: "center", gap: 1.5, py: 1, px: 1.5, minWidth: 0 }}
    >
      <Avatar sx={{ width: 32, height: 32, fontSize: "0.875rem" }}>{initials}</Avatar>
      <Box sx={{ minWidth: 0 }}>
        <Typography variant="body2" sx={{ fontWeight: 600, overflowWrap: "anywhere" }}>
          {name}
        </Typography>
        <Typography variant="caption" color="text.secondary" component="p">
          {role}
        </Typography>
      </Box>
    </Card>
  );
}
