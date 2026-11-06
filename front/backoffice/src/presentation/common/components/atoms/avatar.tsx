"use client";

import { type ComponentProps } from "react";
import MuiAvatar from "@mui/material/Avatar";

/** The project's Avatar: the MUI widget, themed by the MUI theme. */
export default function Avatar(props: ComponentProps<typeof MuiAvatar>) {
  return <MuiAvatar {...props} />;
}
