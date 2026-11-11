"use client";

import { useEffect, useRef, useState } from "react";
import { Menu, X } from "lucide-react";
import styles from "./mobile-menu.module.css";

interface MobileMenuProps {
  links: { label: string; href: string }[];
  openLabel: string;
  closeLabel: string;
  menuLabel: string;
}

/** The section links behind a button, for small screens. Following a link or pressing Escape closes it. */
export default function MobileMenu({ links, openLabel, closeLabel, menuLabel }: MobileMenuProps) {
  const [open, setOpen] = useState(false);
  const toggle = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (!open) {
      return;
    }
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setOpen(false);
        toggle.current?.focus();
      }
    };
    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [open]);

  return (
    <>
      <button
        ref={toggle}
        type="button"
        className={styles.toggle}
        aria-expanded={open}
        aria-controls="menu-movil"
        aria-label={open ? closeLabel : openLabel}
        onClick={() => setOpen((current) => !current)}
      >
        {open ? <X size={22} aria-hidden="true" /> : <Menu size={22} aria-hidden="true" />}
      </button>
      <nav id="menu-movil" aria-label={menuLabel} className={styles.panel} hidden={!open}>
        <ul className={styles.links}>
          {links.map((link) => (
            <li key={link.href}>
              <a href={link.href} className={styles.link} onClick={() => setOpen(false)}>
                {link.label}
              </a>
            </li>
          ))}
        </ul>
      </nav>
    </>
  );
}
