"use client";

import { type ComponentProps } from "react";
import MuiLinearProgress from "@mui/material/LinearProgress";

/** The project's LinearProgress: the MUI widget, themed by the MUI theme. */
export default function LinearProgress(props: ComponentProps<typeof MuiLinearProgress>) {
  return <MuiLinearProgress {...props} />;
}
