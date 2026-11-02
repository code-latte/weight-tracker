/** Name and surname in one line, or `null` when the person has neither. */
export function fullNameOf(person: { name: string; surname: string }): string | null {
  const fullName = `${person.name} ${person.surname}`.trim();

  return fullName === "" ? null : fullName;
}
