"use client";

import { type ComponentProps } from "react";
import MuiListItemButton from "@mui/material/ListItemButton";

/** The project's ListItemButton: the MUI widget, themed by the MUI theme. */
export default function ListItemButton(props: ComponentProps<typeof MuiListItemButton>) {
  return <MuiListItemButton {...props} />;
}
