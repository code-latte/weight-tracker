import Link from "next/link";
import { type LegalPage as LegalPageContent, pendingNotice } from "@/content/legal";
import { nav } from "@/content/landing";
import styles from "./legal-page.module.css";

/** A legal page: its title and text, or a clear notice while its text has not been supplied. */
export default function LegalPage({ page }: { page: LegalPageContent }) {
  return (
    <div className={styles.page}>
      <header className={styles.bar}>
        <div className={styles.container}>
          <Link href="/" className={styles.logo} aria-label={nav.homeLabel}>
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img src="/brand/logo-light.svg" alt="" width={145} height={32} />
          </Link>
        </div>
      </header>
      <main className={`${styles.container} ${styles.content}`}>
        <h1 className={styles.title}>{page.title}</h1>
        {page.body ? (
          <div className={styles.body}>
            {page.body.map((paragraph) => (
              <p key={paragraph}>{paragraph}</p>
            ))}
          </div>
        ) : (
          <section className={styles.notice} aria-labelledby="contenido-pendiente">
            <h2 id="contenido-pendiente">{pendingNotice.title}</h2>
            <p>{pendingNotice.text}</p>
          </section>
        )}
        <Link href="/" className={styles.back}>
          {pendingNotice.back}
        </Link>
      </main>
    </div>
  );
}
