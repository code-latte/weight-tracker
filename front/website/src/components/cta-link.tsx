"use client";

import type { ReactNode } from "react";
import { track } from "@/analytics/track";
import type { CtaLocation } from "@/content/landing";
import { type ButtonVariant, buttonClass } from "./button";

interface CtaLinkProps {
  href: string;
  location: CtaLocation;
  variant?: ButtonVariant;
  size?: "medium" | "large";
  block?: boolean;
  /** Rendered as a plain link with this class instead of a button, as in the footer. */
  linkClassName?: string;
  onClick?: () => void;
  children: ReactNode;
}

/** A call to action: an anchor that looks like a button and reports its click with where it sits. */
export default function CtaLink({
  href,
  location,
  variant,
  size,
  block,
  linkClassName,
  onClick,
  children,
}: CtaLinkProps) {
  return (
    <a
      href={href}
      className={linkClassName ?? buttonClass({ variant, size, block })}
      onClick={() => {
        track({ name: "cta_click", location });
        onClick?.();
      }}
    >
      {children}
    </a>
  );
}
