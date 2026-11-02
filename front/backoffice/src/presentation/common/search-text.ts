/** Lower case and without accents, so "garcia" finds "García". Used by the screens' client-side searches. */
export function normalized(value: string): string {
  return value
    .normalize("NFD")
    .replace(/\p{Diacritic}/gu, "")
    .toLocaleLowerCase();
}
