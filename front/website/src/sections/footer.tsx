import Link from "next/link";
import CtaLink from "@/components/cta-link";
import { footer, formAnchor } from "@/content/landing";
import { legalPages } from "@/content/legal";
import { site } from "@/content/site";
import styles from "./footer.module.css";

/** The footer: brand, navigation and legal pages. No social profile or contact address until they exist. */
export default function Footer() {
  return (
    <footer className={styles.footer}>
      <div className={styles.grid}>
        <div className={styles.brand}>
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src="/brand/logo-mono-white.svg" alt={site.name} width={140} height={31} />
          <p>{footer.description}</p>
        </div>
        <nav aria-labelledby="pie-navegacion">
          <h2 id="pie-navegacion" className={styles.title}>
            {footer.navTitle}
          </h2>
          <ul className={styles.links}>
            {footer.nav.map((link) =>
              link.href === formAnchor ? (
                <li key={link.label}>
                  <CtaLink href={link.href} location="footer" linkClassName={styles.link}>
                    {link.label}
                  </CtaLink>
                </li>
              ) : (
                <li key={link.label}>
                  <a href={link.href} className={styles.link}>
                    {link.label}
                  </a>
                </li>
              ),
            )}
          </ul>
        </nav>
        <nav aria-labelledby="pie-legal">
          <h2 id="pie-legal" className={styles.title}>
            {footer.legalTitle}
          </h2>
          <ul className={styles.links}>
            {legalPages.map((page) => (
              <li key={page.path}>
                <Link href={page.path} className={styles.link}>
                  {page.label}
                </Link>
              </li>
            ))}
          </ul>
        </nav>
        <p className={styles.copyright}>{footer.copyright}</p>
      </div>
    </footer>
  );
}
