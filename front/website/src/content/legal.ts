/** The legal pages. A `body` of `null` is a text the product owner has not supplied yet: the page says so. */
export interface LegalPage {
  path: "/aviso-legal" | "/privacidad" | "/cookies" | "/condiciones";
  /** The link text, in the footer and on the form. */
  label: string;
  title: string;
  body: string[] | null;
}

export const legalPages: LegalPage[] = [
  { path: "/aviso-legal", label: "Aviso legal", title: "Aviso legal", body: null },
  {
    path: "/privacidad",
    label: "Política de privacidad",
    title: "Política de privacidad",
    body: null,
  },
  { path: "/cookies", label: "Política de cookies", title: "Política de cookies", body: null },
  { path: "/condiciones", label: "Condiciones de uso", title: "Condiciones de uso", body: null },
];

export const pendingNotice = {
  title: "Contenido pendiente",
  text: "Estamos preparando el texto de esta página. Estará disponible antes de que Trainera abra al público.",
  back: "Volver al inicio",
};

export function legalPageOf(path: LegalPage["path"]): LegalPage {
  return legalPages.find((page) => page.path === path)!;
}
