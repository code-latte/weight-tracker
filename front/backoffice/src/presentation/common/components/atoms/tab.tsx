"use client";

import { type ComponentProps } from "react";
import MuiTab from "@mui/material/Tab";

/** The project's Tab: the MUI widget, themed by the MUI theme. */
export default function Tab(props: ComponentProps<typeof MuiTab>) {
  return <MuiTab {...props} />;
}
