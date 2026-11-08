/** A person's initials for an avatar: the first letter of the name and of the surname, upper-cased. */
export default function initials(name: string, surname: string): string {
  return [name, surname]
    .map((part) => part.trim().charAt(0))
    .join("")
    .toUpperCase();
}
