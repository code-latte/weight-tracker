"use client";

import { type ElementType } from "react";
import MuiButton, { type ButtonProps } from "@mui/material/Button";

/**
 * The project's Button: the MUI widget, themed by the MUI theme. Accepts `component` to render a router link; the
 * props follow that component, a `button` by default.
 */
export default function Button<C extends ElementType = "button">(
  props: ButtonProps<C, { component?: C }>,
) {
  return <MuiButton {...props} />;
}
