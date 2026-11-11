"use client";

import { useEffect, useRef, useState } from "react";
import CtaLink from "@/components/cta-link";
import MobileMenu from "@/components/mobile-menu";
import { config } from "@/config";
import { ctas, formAnchor, nav } from "@/content/landing";
import styles from "./header.module.css";

/** The sticky header: logo, section links, the early-access button, and a menu on small screens. */
export default function Header() {
  const sentinel = useRef<HTMLDivElement>(null);
  const [compact, setCompact] = useState(false);

  // Compact once the top of the page has scrolled away; no scroll listener.
  useEffect(() => {
    const target = sentinel.current;
    if (!target || typeof IntersectionObserver === "undefined") {
      return;
    }
    const observer = new IntersectionObserver(([entry]) => setCompact(!entry.isIntersecting));
    observer.observe(target);
    return () => observer.disconnect();
  }, []);

  return (
    <>
      <div ref={sentinel} className={styles.sentinel} aria-hidden="true" />
      <header
        className={`${styles.header} ${compact ? styles.compact : ""}`}
        data-compact={compact}
      >
        <div className={styles.bar}>
          <a href="#inicio" className={styles.logo} aria-label={nav.homeLabel}>
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img src="/brand/logo-light.svg" alt="" width={132} height={29} />
          </a>
          <nav aria-label={nav.menuLabel}>
            <ul className={styles.links}>
              {nav.links.map((link) => (
                <li key={link.href}>
                  <a href={link.href} className={styles.link}>
                    {link.label}
                  </a>
                </li>
              ))}
            </ul>
          </nav>
          <div className={styles.cta}>
            <CtaLink href={formAnchor} location="header">
              {ctas[config.launchState].header}
            </CtaLink>
          </div>
          <div className={styles.menu}>
            <MobileMenu
              links={[...nav.links, { label: ctas[config.launchState].header, href: formAnchor }]}
              openLabel={nav.menuOpenLabel}
              closeLabel={nav.menuCloseLabel}
              menuLabel={nav.menuLabel}
            />
          </div>
        </div>
      </header>
    </>
  );
}
