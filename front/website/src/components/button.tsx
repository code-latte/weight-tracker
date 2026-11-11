import type { ButtonHTMLAttributes, ReactNode } from "react";
import styles from "./button.module.css";

export type ButtonVariant = "cta" | "primary" | "outline";

interface ButtonStyle {
  variant?: ButtonVariant;
  size?: "medium" | "large";
  block?: boolean;
}

/** The class names of a button style, for the elements that look like a button. */
export function buttonClass({ variant = "cta", size = "medium", block = false }: ButtonStyle = {}) {
  return [styles.button, styles[variant], size === "large" && styles.large, block && styles.block]
    .filter(Boolean)
    .join(" ");
}

type ButtonProps = ButtonStyle &
  Omit<ButtonHTMLAttributes<HTMLButtonElement>, "className"> & { children: ReactNode };

export default function Button({ variant, size, block, children, ...props }: ButtonProps) {
  return (
    <button className={buttonClass({ variant, size, block })} {...props}>
      {children}
    </button>
  );
}
