import { privacyPolicy } from "./privacy-policy";

/**
 * A block of a legal section. Its texts render `**bold**`, e-mail addresses and `https://` addresses as such.
 * - `paragraph`: running text;
 * - `list`: bullets, each optionally led by a bold term;
 * - `details`: label and value pairs, shown together as a card;
 * - `highlight`: one short line that stands out, such as the address to write to.
 */
export type LegalBlock =
  | { paragraph: string }
  | { list: { term?: string; text: string }[] }
  | { details: { term: string; text: string }[] }
  | { highlight: string };

export interface LegalSection {
  /** The anchor the index links to. */
  id: string;
  heading: string;
  blocks: LegalBlock[];
}

/** The legal pages. A `body` of `null` is a text the product owner has not supplied yet: the page says so. */
export interface LegalPage {
  path: "/aviso-legal" | "/privacidad" | "/cookies" | "/condiciones";
  /** The link text, in the footer and on the form. */
  label: string;
  title: string;
  /** When the text was last updated: an ISO date and how it reads. */
  updated?: { date: string; text: string };
  body: LegalSection[] | null;
}

export const legalPages: LegalPage[] = [
  { path: "/aviso-legal", label: "Aviso legal", title: "Aviso legal", body: null },
  {
    path: "/privacidad",
    label: "Política de privacidad",
    title: "Política de privacidad",
    updated: { date: "2026-11-11", text: "11 de noviembre de 2026" },
    body: privacyPolicy,
  },
  { path: "/cookies", label: "Política de cookies", title: "Política de cookies", body: null },
  { path: "/condiciones", label: "Condiciones de uso", title: "Condiciones de uso", body: null },
];

export const pendingNotice = {
  title: "Contenido pendiente",
  text: "Estamos preparando el texto de esta página. Estará disponible antes de que Trainera abra al público.",
  back: "Volver al inicio",
};

export const legalLabels = {
  updated: "Última actualización",
  index: "Índice",
};

export function legalPageOf(path: LegalPage["path"]): LegalPage {
  return legalPages.find((page) => page.path === path)!;
}
