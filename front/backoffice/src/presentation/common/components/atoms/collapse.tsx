"use client";

import { type ComponentProps } from "react";
import MuiCollapse from "@mui/material/Collapse";

/** The project's Collapse: the MUI widget, themed by the MUI theme. */
export default function Collapse(props: ComponentProps<typeof MuiCollapse>) {
  return <MuiCollapse {...props} />;
}
