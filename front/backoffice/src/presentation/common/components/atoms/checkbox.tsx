"use client";

import { type ComponentProps } from "react";
import MuiCheckbox from "@mui/material/Checkbox";

/** The project's Checkbox: the MUI widget, themed by the MUI theme. */
export default function Checkbox(props: ComponentProps<typeof MuiCheckbox>) {
  return <MuiCheckbox {...props} />;
}
