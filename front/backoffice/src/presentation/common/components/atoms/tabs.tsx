"use client";

import { type ComponentProps } from "react";
import MuiTabs from "@mui/material/Tabs";

/** The project's Tabs: the MUI widget, themed by the MUI theme. */
export default function Tabs(props: ComponentProps<typeof MuiTabs>) {
  return <MuiTabs {...props} />;
}
