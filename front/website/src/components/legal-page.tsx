import type { ReactNode } from "react";
import Link from "next/link";
import {
  type LegalBlock,
  type LegalPage as LegalPageContent,
  legalLabels,
  pendingNotice,
} from "@/content/legal";
import { nav } from "@/content/landing";
import styles from "./legal-page.module.css";

const boldPattern = /(\*\*.+?\*\*)/;
const addressPattern = /([\w.+-]+@[\w-]+(?:\.[\w-]+)+|https:\/\/[^\s,;)]+)/;

/** An e-mail address becomes a mail link; a web address opens in a new tab, so the reader keeps the policy open. */
function addressLink(address: string, key: number) {
  if (!address.startsWith("https://")) {
    return (
      <a key={key} href={`mailto:${address}`}>
        {address}
      </a>
    );
  }
  return (
    <a key={key} href={address} target="_blank" rel="noopener noreferrer">
      {address.replace(/^https:\/\//, "")}
    </a>
  );
}

function withLinks(text: string): ReactNode[] {
  return text
    .split(addressPattern)
    .map((part, index) => (index % 2 === 1 ? addressLink(part, index) : part))
    .filter((part) => part !== "");
}

/** Renders `**bold**` and addresses inside a content string. */
function inline(text: string): ReactNode[] {
  return text
    .split(boldPattern)
    .flatMap<ReactNode>((part, index) =>
      index % 2 === 1
        ? [<strong key={index}>{withLinks(part.slice(2, -2))}</strong>]
        : withLinks(part),
    );
}

function Block({ block }: { block: LegalBlock }) {
  if ("paragraph" in block) {
    return <p>{inline(block.paragraph)}</p>;
  }
  if ("list" in block) {
    return (
      <ul className={styles.list}>
        {block.list.map((item) => (
          <li key={item.term ?? item.text}>
            {item.term && <strong>{item.term}: </strong>}
            {inline(item.text)}
          </li>
        ))}
      </ul>
    );
  }
  if ("details" in block) {
    return (
      <dl className={styles.details}>
        {block.details.map((item) => (
          <div key={item.term} className={styles.detail}>
            <dt>{item.term}</dt>
            <dd>{inline(item.text)}</dd>
          </div>
        ))}
      </dl>
    );
  }
  return <p className={styles.highlight}>{inline(block.highlight)}</p>;
}

/** A legal page: its title, index and sections, or a clear notice while its text has not been supplied. */
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
        {page.updated && (
          <p className={styles.updated}>
            {legalLabels.updated}: <time dateTime={page.updated.date}>{page.updated.text}</time>
          </p>
        )}
        {page.body ? (
          <>
            <nav className={styles.index} aria-labelledby="indice">
              <h2 id="indice" className={styles.indexTitle}>
                {legalLabels.index}
              </h2>
              <ol>
                {page.body.map((section) => (
                  <li key={section.id}>
                    <a href={`#${section.id}`}>{section.heading}</a>
                  </li>
                ))}
              </ol>
            </nav>
            <div className={styles.body}>
              {page.body.map((section) => (
                <section key={section.id} id={section.id} aria-labelledby={`${section.id}-titulo`}>
                  <h2 id={`${section.id}-titulo`} className={styles.heading}>
                    {section.heading}
                  </h2>
                  <div className={styles.blocks}>
                    {section.blocks.map((block, index) => (
                      <Block key={index} block={block} />
                    ))}
                  </div>
                </section>
              ))}
            </div>
          </>
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
